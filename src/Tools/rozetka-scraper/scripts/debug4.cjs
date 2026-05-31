const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA',
    viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  try { await page.waitForSelector('main', { timeout: 10000 }); } catch {}
  await page.waitForTimeout(5000);
  // Scroll to trigger images
  await page.evaluate(async () => {
    for (let i = 0; i < 5; i++) { window.scrollBy(0, 300); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0);
    await new Promise(r => setTimeout(r, 1000));
  });
  const result = await page.evaluate(() => {
    const title = document.title;
    const h1 = document.querySelector('h1')?.textContent?.trim()?.substring(0, 100);
    const imgCount = document.querySelectorAll('img').length;
    const goodsImgs = Array.from(document.querySelectorAll('img')).filter(i => (i.src || '').includes('goods')).map(i => i.src.substring(0, 120));
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    return { title, h1, imgCount, goodsImgs: goodsImgs.slice(0, 10), sku, url: window.location.href };
  });
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
