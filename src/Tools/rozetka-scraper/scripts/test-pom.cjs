const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  
  // Simulate the scraper: category page then fresh product page
  const catPage = await ctx.newPage();
  await catPage.goto('https://rozetka.com.ua/ua/notebooks/c80004/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await catPage.waitForSelector('main article', { timeout: 15000 });
  await catPage.waitForTimeout(2000);
  
  const productUrl = await catPage.evaluate(() => {
    const link = document.querySelector('main article a[href*="/p"]');
    return link?.getAttribute('href') || '';
  });
  console.log('Product URL:', productUrl);
  
  // Close category page
  await catPage.close();
  
  // Create fresh page for product
  const prodPage = await ctx.newPage();
  await prodPage.goto(productUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await prodPage.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await prodPage.waitForTimeout(5000);
  await prodPage.evaluate(async () => {
    for (let i = 0; i < 5; i++) { window.scrollBy(0, 300); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result = await prodPage.evaluate(() => {
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
    return { images: images.slice(0, 10), sku, totalImgs: document.querySelectorAll('img').length, title: document.title.substring(0, 60) };
  });
  
  console.log(JSON.stringify(result, null, 2));
  
  await browser.close();
})();
