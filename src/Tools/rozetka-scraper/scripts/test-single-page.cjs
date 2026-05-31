const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  
  const page = await ctx.newPage();
  
  // Category page
  await page.goto('https://rozetka.com.ua/ua/notebooks/c80004/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main article', { timeout: 15000 });
  await page.waitForTimeout(2000);
  
  const productUrl = await page.evaluate(() => {
    const link = document.querySelector('main article a[href*="/p"]');
    return link?.getAttribute('href') || '';
  });
  console.log('Product URL:', productUrl);
  
  // Navigate to product (SAME page)
  await page.goto(productUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  await page.evaluate(async () => {
    for (let i = 0; i < 5; i++) { window.scrollBy(0, 300); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result = await page.evaluate(() => {
    const images = [];
    const seenBig = new Set();
    document.querySelectorAll('img').forEach(img => {
      const src = img.getAttribute('src') || img.getAttribute('data-src') || '';
      if (src.includes('/goods/images/big/') && !src.includes('goods_tags') && !seenBig.has(src)) {
        seenBig.add(src);
        images.push(src);
      }
    });
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    return { images, sku, totalImgs: document.querySelectorAll('img').length };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
