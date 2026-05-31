import { chromium, type Browser } from 'playwright';
import * as fs from 'fs/promises';
import * as path from 'path';
import { program } from 'commander';
import { RozetkaCategoryPage, type ProductTile } from '../pages/rozetka-category.page';
import { RozetkaProductPage } from '../pages/rozetka-product.page';
import { ImageDownloader } from '../utils/image-downloader';
import { toSeederProduct, generateSku, slugify, parsePrice, type SeederProduct, type CategoryConfig } from '../utils/rozetka-transformer';

const CATEGORIES: Record<string, { name: string; url: string } & CategoryConfig> = {
  laptops: { name: 'Laptops', url: 'https://rozetka.com.ua/ua/notebooks/c80004/', storeName: 'Tech Store', categoryName: 'Electronics', tags: ['laptop', 'notebook', 'computer'] },
  phones: { name: 'Smartphones', url: 'https://rozetka.com.ua/ua/mobile-phones/c80259/', storeName: 'Tech Store', categoryName: 'Electronics', tags: ['smartphone', 'phone', 'mobile'] },
  tablets: { name: 'Tablets', url: 'https://rozetka.com.ua/ua/tablets/c130309/', storeName: 'Tech Store', categoryName: 'Electronics', tags: ['tablet', 'ipad'] },
  headphones: { name: 'Headphones', url: 'https://rozetka.com.ua/ua/headphones/c80027/', storeName: 'Tech Store', categoryName: 'Electronics', tags: ['headphones', 'audio', 'wireless'] },
};

const PROJECT_ROOT = path.resolve(import.meta.dirname, '../../../..');
const DATA_DIR = path.join(PROJECT_ROOT, 'src/Tools/Seeder.App/Data');
const IMAGES_DIR = path.join(DATA_DIR, 'Images');
const PRODUCTS_JSON = path.join(DATA_DIR, 'products.json');
const UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36';

function log(msg: string, lvl: 'info'|'warn'|'error' = 'info') {
  const icon = lvl === 'error' ? '❌' : lvl === 'warn' ? '⚠️' : 'ℹ️';
  console.log(`${icon} [${new Date().toISOString()}] ${msg}`);
}
function delay(min = 2000, max = 4000) { return new Promise(r => setTimeout(r, Math.floor(Math.random()*(max-min+1))+min)); }

class RozetkaScraper {
  private browser: Browser | null = null;
  private imgDownloader: ImageDownloader;
  private existing: SeederProduct[] = [];
  private existingSkus = new Set<string>();

  constructor() { this.imgDownloader = new ImageDownloader(IMAGES_DIR); }

  async init() {
    log('Initializing...');
    await fs.mkdir(DATA_DIR, { recursive: true });
    await fs.mkdir(IMAGES_DIR, { recursive: true });
    try { this.existing = JSON.parse(await fs.readFile(PRODUCTS_JSON, 'utf-8')); this.existingSkus = new Set(this.existing.map(p => p.Sku)); log(`Loaded ${this.existing.length} existing`); } catch { this.existing = []; this.existingSkus = new Set(); }
    this.browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
    log('Browser ready');
  }

  private async newCtx() {
    const ctx = await this.browser!.newContext({ userAgent: UA, viewport: { width: 1920, height: 1080 }, locale: 'uk-UA', timezoneId: 'Europe/Kiev', extraHTTPHeaders: { 'Accept-Language': 'uk-UA,uk;q=0.9,en;q=0.7' } });
    await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
    return ctx;
  }

  /** PHASE 1: Collect product URLs from category listing */
  private async collectUrls(limit: number, catUrl: string): Promise<ProductTile[]> {
    log('Phase 1: Collecting URLs...');
    const ctx = await this.newCtx();
    const page = await ctx.newPage();
    const catPage = new RozetkaCategoryPage(page);
    try {
      await catPage.goto(catUrl);
      await delay(1500, 2500);
      const tiles: ProductTile[] = [];
      let pg = 1;
      while (tiles.length < limit && pg <= 5) {
        log(`  Page ${pg}...`);
        const all = await catPage.extractProductTiles();
        const fresh = all.filter(t => !this.existingSkus.has(generateSku(t.articleId.replace('p', ''))));
        tiles.push(...fresh.slice(0, limit - tiles.length));
        log(`  ${all.length} tiles, ${tiles.length} new`);
        if (tiles.length >= limit || !(await catPage.nextPage())) break;
        pg++;
      }
      return tiles.slice(0, limit);
    } finally { await page.close(); await ctx.close(); }
  }

  /** Scrape a single variant page and download its images */
  private async scrapeVariantImages(variantUrl: string, variantSlug: string): Promise<string[]> {
    const ctx = await this.newCtx();
    const page = await ctx.newPage();
    const pom = new RozetkaProductPage(page);
    try {
      await pom.goto(variantUrl);
      const gallery = await pom.extractGallery();
      const imgs = gallery.images.length > 0 ? gallery.images : [];
      if (imgs.length === 0) return [];
      const local = await this.imgDownloader.downloadMultiple(imgs, variantSlug, 10);
      return local;
    } catch (e) {
      log(`    Variant scrape fail: ${e}`, 'warn');
      return [];
    } finally { await page.close(); await ctx.close(); }
  }

  /** PHASE 2: Visit product URL directly with fresh context */
  private async scrapeProduct(tile: ProductTile, cat: CategoryConfig): Promise<SeederProduct | null> {
    const code = tile.articleId.replace('p', '');
    if (this.existingSkus.has(generateSku(code))) return null;
    log(`  ${tile.title.substring(0, 50)}...`);

    const ctx = await this.newCtx();
    const page = await ctx.newPage();
    const pom = new RozetkaProductPage(page);
    try {
      await pom.goto(tile.url);
      const details = await pom.extractDetails();
      const finalCode = details.sku || code;
      if (this.existingSkus.has(generateSku(finalCode))) return null;

      // Download main product images
      const imgs = details.images.length > 0 ? details.images : (tile.imgSrc ? [tile.imgSrc] : []);
      const slug = slugify(tile.title).substring(0, 60);
      const local = await this.imgDownloader.downloadMultiple(imgs, slug, 10);
      if (local.length) log(`  📸 ${local.length} imgs: ${local[0]}`);

      // Build product
      const product = toSeederProduct(tile, details, local, cat);

      // Scrape variant images
      if (details.variants.length > 0) {
        log(`  🔀 ${details.variants.length} variants found, scraping images...`);
        for (const variant of details.variants) {
          const variantSlug = slugify(variant.name || variant.pid).substring(0, 60);
          const variantImgs = await this.scrapeVariantImages(variant.url, variantSlug);
          
          // Update variant with image paths
          const existingVariant = product.Variants.find(v => v.RozetkaCode === variant.pid);
          if (existingVariant) {
            (existingVariant as any).Gallery = variantImgs;
            (existingVariant as any).ImageUrl = variantImgs[0] || '';
          }
          
          if (variantImgs.length) log(`    📸 ${variant.name}: ${variantImgs.length} imgs`);
          else log(`    ⚠️ ${variant.name}: no images`);
          
          await delay(2000, 4000);
        }
      }

      return product;
    } finally { await page.close(); await ctx.close(); }
  }

  async scrape(category: string, limit: number) {
    const cat = CATEGORIES[category];
    if (!cat) throw new Error(`Unknown: ${category}`);
    log(`${cat.name}, limit ${limit}`);

    const urls = await this.collectUrls(limit, cat.url);
    log(`Phase 2: ${urls.length} products`);

    const added: SeederProduct[] = [];
    for (let i = 0; i < urls.length; i++) {
      log(`[${i+1}/${urls.length}] ${urls[i].title.substring(0, 50)}`);
      const p = await this.scrapeProduct(urls[i], cat);
      if (p) { added.push(p); this.existing.push(p); this.existingSkus.add(p.Sku); }
      await delay(3000, 6000);
    }

    if (added.length) { await fs.writeFile(PRODUCTS_JSON, JSON.stringify(this.existing, null, 2)); log(`✅ ${added.length} new (total ${this.existing.length})`); }
    else log('No new');
    log('Done!');
  }

  async cleanup() { await this.browser?.close(); log('Closed'); }
}

program.name('rozetka-scraper').version('2.0').option('-c, --category <c>', 'cat', 'laptops').option('-l, --limit <n>', 'max', '10').action(async o => {
  const s = new RozetkaScraper();
  try { await s.init(); await s.scrape(o.category, parseInt(o.limit)); } catch(e) { log(`Fatal: ${e}`, 'error'); process.exit(1); } finally { await s.cleanup(); }
}).parse();
