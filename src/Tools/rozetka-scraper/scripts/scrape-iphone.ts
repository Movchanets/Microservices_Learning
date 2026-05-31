import { chromium } from 'playwright';
import * as fs from 'fs/promises';
import * as path from 'path';

const UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36';
const DATA_DIR = 'D:/code/Microservices/src/Tools/Seeder.App/Data';
const IMAGES_DIR = path.join(DATA_DIR, 'Images');
const PRODUCTS_JSON = path.join(DATA_DIR, 'products.json');

function slugify(t) { return t.toLowerCase().replace(/[^\w\s-]/g,'').replace(/[\s_]+/g,'-').replace(/^-+|-+$/g,'').substring(0,80); }
function delay(min=2000,max=4000) { return new Promise(r=>setTimeout(r,Math.floor(Math.random()*(max-min+1))+min)); }

async function main() {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({ userAgent: UA, viewport: {width:1920,height:1080}, locale:'uk-UA', timezoneId:'Europe/Kiev', extraHTTPHeaders:{'Accept-Language':'uk-UA,uk;q=0.9,en;q=0.7'} });
  await ctx.addInitScript(() => { Object.defineProperty(navigator,'webdriver',{get:()=>false}); });
  const page = await ctx.newPage();

  const url = 'https://rozetka.com.ua/ua/apple-iphone-17-pro-max-512gb-cosmic-orange-mfyt4af-a/p543553245/';
  console.log('Navigating to iPhone...');
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(()=>{});
  await page.waitForTimeout(5000);
  await page.evaluate(async () => {
    for (let i=0;i<10;i++) { window.scrollBy(0,400); await new Promise(r=>setTimeout(r,500)); }
    window.scrollTo(0,0); await new Promise(r=>setTimeout(r,1000));
  });

  const details = await page.evaluate(() => {
    // SKU
    let sku = '';
    for (const el of document.querySelectorAll('span,div')) {
      const t = el.textContent?.trim()||'';
      const m = t.match(/^Код:\s*(\d+)$/);
      if (m) { sku = m[1]; break; }
    }

    // Gallery
    const images = [];
    const seen = new Set();
    document.querySelectorAll('img').forEach(img => {
      const src = img.getAttribute('src')||'';
      if (src.includes('/goods/images/big/') && !src.includes('goods_tags') && !seen.has(src)) { seen.add(src); images.push(src); }
    });
    const uniqueImages = [...new Set(images.map(u=>u.replace('/big/','/original/')))];

    // Breadcrumbs from JSON-LD
    const breadcrumbs = [];
    document.querySelectorAll('script[type="application/ld+json"]').forEach(s => {
      try {
        const d = JSON.parse(s.textContent||'{}');
        if (d.itemListElement) {
          d.itemListElement.forEach(item => {
            breadcrumbs.push({ name: item.item?.name||item.name||'', url: item.item?.['@id'], position: item.position });
          });
        }
      } catch {}
    });

    // Variants
    const currentPid = window.location.href.match(/\/p(\d+)\//)?.[1]||'';
    const variants = [];
    const seenPids = new Set();
    document.querySelectorAll('a[href*="/p"]').forEach(a => {
      const href = a.getAttribute('href')||'';
      const pid = href.match(/\/p(\d+)\//)?.[1];
      if (!pid || pid === currentPid || seenPids.has(pid)) return;
      const cls = a.className||'';
      if (cls.includes('service-product')||cls.includes('footer')||cls.includes('tile-image')) return;
      const text = a.textContent?.trim()||'';
      const title = a.getAttribute('title')||'';
      const img = a.querySelector('img');
      const fullText = (text+' '+title).toLowerCase();
      if (fullText.includes('чохол')||fullText.includes('скло')||fullText.includes('кабель')||fullText.includes('заряд')) return;
      seenPids.add(pid);
      const fullUrl = href.startsWith('http') ? href : `https://rozetka.com.ua${href}`;
      let type = 'other';
      if (text.match(/^\d+\s*(ГБ|GB|ТБ|TB)$/i)) type = 'storage';
      else if (text.match(/^(iPhone|Galaxy|MacBook|iPad|Pixel)/i)) type = 'model';
      else if (!text && img) type = 'color';
      else if (text.match(/^(Black|White|Blue|Red|Green|Gold|Silver|Purple|Pink|Orange|Titanium|Midnight|Starlight|Cosmic|Deep|Natural|Slate|Space|Graphite|Rose)/i)) type = 'color';
      else {
        const slug = href.toLowerCase();
        if (slug.match(/(black|white|blue|red|green|gold|silver|purple|pink|orange|titanium|midnight|starlight|cosmic|deep|natural|slate|space|graphite|rose)/)) type = 'color';
      }
      variants.push({ pid, url: fullUrl, name: (text||title||pid).substring(0,80), type });
    });

    // Description
    let desc = '';
    for (const sel of ['[class*="product-about"] p','[class*="about__brief"]','article p']) {
      const el = document.querySelector(sel);
      const t = el?.textContent?.trim();
      if (t && t.length > 20) { desc = t.substring(0,500); break; }
    }

    const title = document.querySelector('h1')?.textContent?.trim() || document.title;
    return { sku, images: uniqueImages, breadcrumbs, variants, desc, title: title.substring(0,200) };
  });

  // Download images
  await fs.mkdir(IMAGES_DIR, { recursive: true });
  const slug = slugify(details.title).substring(0,60);
  const imgDir = path.join(IMAGES_DIR, slug);
  await fs.mkdir(imgDir, { recursive: true });

  const localImages = [];
  for (let i = 0; i < Math.min(details.images.length, 10); i++) {
    try {
      const resp = await fetch(details.images[i], { headers: { 'User-Agent': UA, 'Referer': 'https://rozetka.com.ua/' } });
      if (resp.ok) {
        const buf = Buffer.from(await resp.arrayBuffer());
        const file = `Images/${slug}/image${i}.jpg`;
        await fs.writeFile(path.join(DATA_DIR, file), buf);
        localImages.push(file);
        console.log(`  img ${i}: ${file}`);
      }
    } catch(e) { console.log(`  img ${i} fail: ${e.message}`); }
    await delay(200,500);
  }

  // Build product
  const breadcrumbNames = details.breadcrumbs.filter(b=>b.name && b.name !== 'Інтернет-магазин Rozetka').map(b=>b.name);
  const catPath = breadcrumbNames.length > 2 ? breadcrumbNames.slice(1,-1).join(' > ') : 'Electronics';

  const product = {
    StoreName: 'Tech Store',
    CategoryName: catPath,
    Name: details.title,
    Description: details.desc || details.title,
    Price: 62999,
    Currency: 'UAH',
    Sku: `ROZ-${details.sku}`,
    RozetkaCode: details.sku,
    Tags: ['smartphone','phone','mobile','apple','iphone',...breadcrumbNames.filter(b=>b.length<30&&b.length>2).map(b=>b.toLowerCase())],
    ImageUrl: localImages[0]||'',
    Gallery: localImages,
    Breadcrumbs: details.breadcrumbs,
    CategoryPath: breadcrumbNames.join(' > '),
    InitialStock: 25,
    Variants: details.variants.map(v => ({ RozetkaCode: v.pid, Name: v.name, Type: v.type, Price: 62999 })),
  };

  // Load existing, add, save
  const existing = JSON.parse(await fs.readFile(PRODUCTS_JSON, 'utf-8'));
  const existingSkus = new Set(existing.map(p=>p.Sku));
  if (!existingSkus.has(product.Sku)) {
    existing.push(product);
    await fs.writeFile(PRODUCTS_JSON, JSON.stringify(existing, null, 2));
    console.log(`\nAdded: ${product.Sku} - ${product.Name.substring(0,60)}`);
    console.log(`  Gallery: ${product.Gallery.length} images`);
    console.log(`  Breadcrumbs: ${product.Breadcrumbs.length}`);
    console.log(`  Variants: ${product.Variants.length}`);
    for (const v of product.Variants) {
      console.log(`    ${v.Type}: ${v.Name} (ROZ-${v.RozetkaCode})`);
    }
  } else {
    console.log('Already exists');
  }

  await browser.close();
  console.log('Done');
}

main().catch(e => { console.error(e); process.exit(1); });
