const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  // Go directly to product
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  await page.evaluate(async () => {
    for (let i = 0; i < 5; i++) { window.scrollBy(0, 300); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result = await page.evaluate(() => {
    const images = [];
    document.querySelectorAll('img').forEach(img => {
      const src = img.getAttribute('src') || '';
      if (src.includes('/goods/images/big/') && !src.includes('goods_tags')) images.push(src.substring(0, 80));
    });
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    return { title: document.title.substring(0, 60), images: images.slice(0, 5), sku, totalImgs: document.querySelectorAll('img').length };
  });
  
  console.log('Direct navigation:', JSON.stringify(result, null, 2));
  
  // Now navigate away and back
  await page.goto('https://rozetka.com.ua/ua/notebooks/c80004/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(2000);
  
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  await page.evaluate(async () => {
    for (let i = 0; i < 5; i++) { window.scrollBy(0, 300); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result2 = await page.evaluate(() => {
    const images = [];
    document.querySelectorAll('img').forEach(img => {
      const src = img.getAttribute('src') || '';
      if (src.includes('/goods/images/big/') && !src.includes('goods_tags')) images.push(src.substring(0, 80));
    });
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    return { title: document.title.substring(0, 60), images: images.slice(0, 5), sku, totalImgs: document.querySelectorAll('img').length };
  });
  
  console.log('After navigation back:', JSON.stringify(result2, null, 2));
  
  await browser.close();
})();
