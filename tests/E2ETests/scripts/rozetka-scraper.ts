/**
 * Rozetka Product Scraper for Marketplace Seeder.App
 * 
 * Scrapes real products from Rozetka.com.ua, downloads images locally,
 * and generates a products.json file compatible with the .NET Seeder.App.
 * 
 * Architecture:
 * - Page Objects: RozetkaCategoryPage, RozetkaProductPage
 * - Utilities: ImageDownloader, RozetkaTransformer
 * - Fixtures: RozetkaScraperFixture (browser context with anti-bot)
 * 
 * Usage:
 *   npx tsx rozetka-scraper.ts --category laptops --limit 10
 */

import { chromium, type Browser } from 'playwright';
import * as fs from 'fs/promises';
import * as path from 'path';
import { program } from 'commander';

// Import POMs and utilities
import { RozetkaCategoryPage, type ProductTile } from '../pages/rozetka-category.page';
import { RozetkaProductPage } from '../pages/rozetka-product.page';
import { ImageDownloader } from '../utils/image-downloader';
import { 
  toSeederProduct, 
  generateSku, 
  type SeederProduct, 
  type CategoryConfig 
} from '../utils/rozetka-transformer';

// ============================================================================
// Configuration
// ============================================================================

const CATEGORIES: Record<string, { name: string; url: string } & CategoryConfig> = {
  laptops: {
    name: 'Laptops',
    url: 'https://rozetka.com.ua/ua/notebooks/c80004/',
    storeName: 'Tech Store',
    categoryName: 'Electronics',
    tags: ['laptop', 'notebook', 'computer'],
  },
  phones: {
    name: 'Smartphones',
    url: 'https://rozetka.com.ua/ua/mobile-phones/c80259/',
    storeName: 'Tech Store',
    categoryName: 'Electronics',
    tags: ['smartphone', 'phone', 'mobile'],
  },
  tablets: {
    name: 'Tablets',
    url: 'https://rozetka.com.ua/ua/tablets/c130309/',
    storeName: 'Tech Store',
    categoryName: 'Electronics',
    tags: ['tablet', 'ipad'],
  },
  headphones: {
    name: 'Headphones',
    url: 'https://rozetka.com.ua/ua/headphones/c80027/',
    storeName: 'Tech Store',
    categoryName: 'Electronics',
    tags: ['headphones', 'audio', 'wireless'],
  },
};

// Paths
const PROJECT_ROOT = path.resolve(import.meta.dirname, '../../..');
const DATA_DIR = path.join(PROJECT_ROOT, 'src/Tools/Seeder.App/Data');
const IMAGES_DIR = path.join(DATA_DIR, 'Images');
const PRODUCTS_JSON = path.join(DATA_DIR, 'products.json');

// Anti-bot config
const USER_AGENT = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36';
const VIEWPORT = { width: 1920, height: 1080 };

// ============================================================================
// Logging
// ============================================================================

function log(message: string, level: 'info' | 'warn' | 'error' = 'info'): void {
  const ts = new Date().toISOString();
  const icon = level === 'error' ? '❌' : level === 'warn' ? '⚠️' : 'ℹ️';
  console.log(`${icon} [${ts}] ${message}`);
}

function randomDelay(min = 1000, max = 2500): Promise<void> {
  const delay = Math.floor(Math.random() * (max - min + 1)) + min;
  return new Promise(resolve => setTimeout(resolve, delay));
}

// ============================================================================
// Scraper Class
// ============================================================================

class RozetkaScraper {
  private browser: Browser | null = null;
  private categoryPage: RozetkaCategoryPage | null = null;
  private productPage: RozetkaProductPage | null = null;
  private imageDownloader: ImageDownloader;
  private existingProducts: SeederProduct[] = [];
  private existingSkus: Set<string> = new Set();

  constructor() {
    this.imageDownloader = new ImageDownloader(IMAGES_DIR);
  }

  /**
   * Initialize browser and load existing products
   */
  async initialize(): Promise<void> {
    log('Initializing...');
    await fs.mkdir(DATA_DIR, { recursive: true });
    await fs.mkdir(IMAGES_DIR, { recursive: true });
    await this.loadExisting();

    this.browser = await chromium.launch({
      headless: true,
      args: ['--disable-blink-features=AutomationControlled'],
    });

    const context = await this.browser.newContext({
      userAgent: USER_AGENT,
      viewport: VIEWPORT,
      locale: 'uk-UA',
      timezoneId: 'Europe/Kiev',
      extraHTTPHeaders: { 'Accept-Language': 'uk-UA,uk;q=0.9,en;q=0.7' },
    });

    // Stealth
    await context.addInitScript(() => {
      Object.defineProperty(navigator, 'webdriver', { get: () => false });
    });

    // Create page objects
    const page = await context.newPage();
    this.categoryPage = new RozetkaCategoryPage(page);
    this.productPage = new RozetkaProductPage(page);

    log('Browser ready');
  }

  /**
   * Load existing products from JSON
   */
  private async loadExisting(): Promise<void> {
    try {
      const data = await fs.readFile(PRODUCTS_JSON, 'utf-8');
      this.existingProducts = JSON.parse(data);
      this.existingSkus = new Set(this.existingProducts.map(p => p.Sku));
      log(`Loaded ${this.existingProducts.length} existing products`);
    } catch {
      this.existingProducts = [];
      this.existingSkus = new Set();
    }
  }

  /**
   * Extract product listings using CategoryPage POM
   */
  private async scrapeListings(limit: number, categoryUrl: string): Promise<ProductTile[]> {
    if (!this.categoryPage) throw new Error('Not initialized');

    await this.categoryPage.goto(categoryUrl);
    await randomDelay(1500, 2500);

    const products: ProductTile[] = [];
    let pageNum = 1;

    while (products.length < limit && pageNum <= 5) {
      log(`Scanning page ${pageNum}...`);

      const tiles = await this.categoryPage.extractProductTiles();
      
      // Filter existing
      const newTiles = tiles.filter(t => {
        const sku = generateSku(t.articleId);
        return !this.existingSkus.has(sku);
      });

      products.push(...newTiles.slice(0, limit - products.length));
      log(`Page ${pageNum}: ${tiles.length} tiles, ${products.length} new so far`);

      if (products.length >= limit) break;

      // Try next page
      const hasNext = await this.categoryPage.nextPage();
      if (!hasNext) break;
      
      pageNum++;
    }

    return products.slice(0, limit);
  }

  /**
   * Process a single product: get details + download images
   */
  private async processProduct(
    tile: ProductTile,
    config: CategoryConfig
  ): Promise<SeederProduct | null> {
    const sku = generateSku(tile.articleId);
    if (this.existingSkus.has(sku)) return null;

    if (!this.productPage) throw new Error('Not initialized');

    log(`  Details: ${tile.title.substring(0, 50)}...`);

    // Get product details using POM
    await this.productPage.goto(tile.url);
    const details = await this.productPage.extractDetails();

    // Download images using utility
    const allImages = details.images.length > 0 ? details.images : (tile.imgSrc ? [tile.imgSrc] : []);
    const slug = tile.title
      .toLowerCase()
      .replace(/[^\w\s-]/g, '')
      .replace(/[\s_]+/g, '-')
      .substring(0, 60);

    const localImages = await this.imageDownloader.downloadMultiple(allImages, slug);

    if (localImages.length > 0) {
      log(`  📸 ${localImages[0]}`);
    }

    // Transform to seeder format using utility
    return toSeederProduct(tile, details, localImages, config);
  }

  /**
   * Main scrape workflow
   */
  async scrape(category: string, limit: number): Promise<void> {
    const cat = CATEGORIES[category];
    if (!cat) throw new Error(`Unknown: ${category}. Use: ${Object.keys(CATEGORIES).join(', ')}`);

    log(`Category: ${cat.name}, limit: ${limit}`);

    // Get listings
    const listings = await this.scrapeListings(limit, cat.url);
    log(`Processing ${listings.length} products...`);

    // Process each product
    const added: SeederProduct[] = [];
    for (let i = 0; i < listings.length; i++) {
      log(`[${i + 1}/${listings.length}] ${listings[i].title.substring(0, 50)}`);

      const product = await this.processProduct(listings[i], cat);
      if (product) {
        added.push(product);
        this.existingProducts.push(product);
        this.existingSkus.add(product.Sku);
      }

      await randomDelay();
    }

    // Save results
    if (added.length > 0) {
      await fs.writeFile(PRODUCTS_JSON, JSON.stringify(this.existingProducts, null, 2));
      log(`✅ Saved ${added.length} new products (total: ${this.existingProducts.length})`);
    } else {
      log('No new products');
    }

    log('Done!');
  }

  /**
   * Cleanup browser
   */
  async cleanup(): Promise<void> {
    await this.browser?.close();
    log('Browser closed');
  }
}

// ============================================================================
// CLI
// ============================================================================

program
  .name('rozetka-scraper')
  .description('Scrape Rozetka products for Seeder.App')
  .version('1.0.0')
  .option('-c, --category <cat>', 'laptops|phones|tablets|headphones', 'laptops')
  .option('-l, --limit <n>', 'Max products', '10')
  .action(async (opts) => {
    const scraper = new RozetkaScraper();
    try {
      await scraper.initialize();
      await scraper.scrape(opts.category, parseInt(opts.limit, 10));
    } catch (err) {
      log(`Fatal: ${err}`, 'error');
      process.exit(1);
    } finally {
      await scraper.cleanup();
    }
  });

program.parse();
