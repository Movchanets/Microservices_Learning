
const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  // Go to a product page
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);
  
  // Extract breadcrumbs
  const breadcrumbs = await page.evaluate(() => {
    const items = [];
    // Try various breadcrumb selectors
    const selectors = [
      'nav[class*="breadcrumb"] a',
      '[class*="breadcrumb"] a', 
      'rz-breadcrumbs a',
      '.breadcrumb a',
      'a[href*="c80004"]',  // category links
    ];
    
    for (const sel of selectors) {
      const els = document.querySelectorAll(sel);
      if (els.length > 0) {
        els.forEach(el => {
          items.push({ selector: sel, text: el.textContent?.trim(), href: el.getAttribute('href') });
        });
        break;
      }
    }
    return items;
  });
  
  // Extract SKU/Article
  const skuInfo = await page.evaluate(() => {
    const selectors = [
      '[class*="article"]',
      '[class*="sku"]', 
      '[data-testid*="article"]',
      '[data-testid*="sku"]',
      'span[class*="code"]',
    ];
    
    const results = [];
    for (const sel of selectors) {
      document.querySelectorAll(sel).forEach(el => {
        results.push({ selector, text: el.textContent?.trim()?.substring(0, 100), className: el.className?.substring(0, 80) });
      });
    }
    
    // Also look for text patterns like "Код: 123456789"
    const allText = document.body.innerText;
    const codeMatch = allText.match(/(?:Код|Артикул|SKU|Code):?\s*(\S+)/i);
    
    return { elements: results, codeMatch: codeMatch?.[0] };
  });
  
  // Extract image gallery
  const gallery = await page.evaluate(() => {
    const images = [];
    
    // All product images (not tags/promos)
    document.querySelectorAll('img').forEach(img => {
      const src = img.getAttribute('src') || img.getAttribute('data-src') || '';
      if (src.includes('rozetka.com.ua/goods') && !src.includes('tag')) {
        images.push({ src: src.substring(0, 120), alt: img.getAttribute('alt')?.substring(0, 60) });
      }
    });
    
    // Check for gallery-specific elements
    const galleryEls = document.querySelectorAll('[class*="gallery"] img, [class*="slider"] img, [class*="carousel"] img, [class*="thumb"] img');
    const galleryImages = [];
    galleryEls.forEach(img => {
      const src = img.getAttribute('src') || img.getAttribute('data-src') || '';
      if (src && src.includes('rozetka')) {
        galleryImages.push(src.substring(0, 120));
      }
    });
    
    return { allImages: images.slice(0, 10), gallerySpecific: galleryImages.slice(0, 10) };
  });
  
  // Extract category tree from page
  const categories = await page.evaluate(() => {
    // Look for sidebar/menu category tree
    const catLinks = [];
    document.querySelectorAll('[class*="category"] a, [class*="catalog"] a, [class*="menu"] a').forEach(a => {
      const href = a.getAttribute('href') || '';
      if (href.includes('rozetka.com.ua') && href.includes('/c')) {
        catLinks.push({ text: a.textContent?.trim()?.substring(0, 50), href: href.substring(0, 100) });
      }
    });
    return catLinks.slice(0, 20);
  });
  
  console.log(JSON.stringify({ breadcrumbs, skuInfo, gallery, categories }, null, 2));
  
  await browser.close();
})();
