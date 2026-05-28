import { chromium } from 'playwright';
import * as fs from 'fs/promises';
import * as path from 'path';
import { RozetkaProductPage } from '../pages/rozetka-product.page';
import { ImageDownloader } from '../utils/image-downloader';
import { slugify, generateSku, parsePrice } from '../utils/rozetka-transformer';

const UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36';
const PROJECT_ROOT = path.resolve(import.meta.dirname, '../../../..');
const DATA_DIR = path.join(PROJECT_ROOT, 'src/Tools/Seeder.App/Data');
const IMAGES_DIR = path.join(DATA_DIR, 'Images');
const PRODUCTS_JSON = path.join(DATA_DIR, 'products.json');

function log(msg: string) { console.log(`[${new Date().toISOString()}] ${msg}`); }
function delay(min=2000,max=4000) { return new Promise(r=>setTimeout(r,Math.floor(Math.random()*(max-min+1))+min)); }

async function main() {
  await fs.mkdir(IMAGES_DIR, { recursive: true });
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const downloader = new ImageDownloader(IMAGES_DIR);

  async function newCtx() {
    const ctx = await browser.newContext({ userAgent: UA, viewport:{width:1920,height:1080}, locale:'uk-UA', timezoneId:'Europe/Kiev', extraHTTPHeaders:{'Accept-Language':'uk-UA,uk;q=0.9,en;q=0.7'} });
    await ctx.addInitScript(() => { Object.defineProperty(navigator,'webdriver',{get:()=>false}); });
    return ctx;
  }

  const url = 'https://rozetka.com.ua/ua/apple-iphone-17-pro-max-512gb-cosmic-orange-mfyt4af-a/p543553245/';
  log('Scraping iPhone...');

  const ctx = await newCtx();
  const page = await ctx.newPage();
  const pom = new RozetkaProductPage(page);
  await pom.goto(url);
  const details = await pom.extractDetails();
  await page.close();
  await ctx.close();

  log(`Gallery: ${details.images.length}, Variants: ${details.variants.length}`);

  // Download main images
  const mainSlug = slugify(details.description).substring(0, 60);
  const mainImgs = await downloader.downloadMultiple(details.images, mainSlug, 10);
  log(`Main: ${mainImgs.length} imgs`);

  // Scrape each variant
  const variants = [];
  for (const v of details.variants) {
    log(`  Variant: ${v.name} (${v.type})`);
    const vCtx = await newCtx();
    const vPage = await vCtx.newPage();
    const vPom = new RozetkaProductPage(vPage);
    try {
      await vPom.goto(v.url);
      const vGallery = await vPom.extractGallery();
      const vSlug = slugify(v.name || v.pid).substring(0, 60);
      const vImgs = await downloader.downloadMultiple(vGallery.images, vSlug, 10);
      
      const priceText = await vPage.evaluate(() => {
        const el = document.querySelector('[class*="price"], [class*="cost"]');
        return el?.textContent?.trim() || '';
      });
      const price = parsePrice(priceText) || 62999;
      
      variants.push({ RozetkaCode: v.pid, Name: v.name, Type: v.type, Price: price, ImageUrl: vImgs[0] || '', Gallery: vImgs });
      log(`    ${vImgs.length} imgs, price: ${price}`);
    } catch(e) {
      log(`    FAIL: ${e}`);
      variants.push({ RozetkaCode: v.pid, Name: v.name, Type: v.type, Price: 62999, ImageUrl: '', Gallery: [] });
    } finally { await vPage.close(); await vCtx.close(); }
    await delay(2000, 3000);
  }

  const bcNames = details.breadcrumbs.filter(b=>b.name && b.name !== '\u0406\u043d\u0442\u0435\u0440\u043d\u0435\u0442-\u043c\u0430\u0433\u0430\u0437\u0438\u043d Rozetka').map(b=>b.name);
  const catPath = bcNames.length > 2 ? bcNames.slice(1,-1).join(' > ') : 'Electronics';

  const product = {
    StoreName: 'Tech Store', CategoryName: catPath,
    Name: details.description, Description: details.description,
    Price: 62999, Currency: 'UAH', Sku: generateSku(details.sku), RozetkaCode: details.sku,
    Tags: ['smartphone','phone','mobile','apple','iphone'],
    ImageUrl: mainImgs[0] || '', Gallery: mainImgs,
    Breadcrumbs: details.breadcrumbs, CategoryPath: bcNames.join(' > '),
    InitialStock: 25, Variants: variants,
  };

  const existing = JSON.parse(await fs.readFile(PRODUCTS_JSON, 'utf-8'));
  existing.push(product);
  await fs.writeFile(PRODUCTS_JSON, JSON.stringify(existing, null, 2));

  log(`Added: ${product.Sku}`);
  log(`  Gallery: ${product.Gallery.length}`);
  log(`  Variants: ${product.Variants.length}`);
  for (const v of product.Variants) log(`    ${v.Type}: ${v.Name} -> ${v.Gallery.length} imgs`);
  await browser.close();
}
main().catch(e => { console.error(e); process.exit(1); });
