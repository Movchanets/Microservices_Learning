const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  const url = 'https://rozetka.com.ua/ua/apple-iphone-17-pro-max-512gb-cosmic-orange-mfyt4af-a/p543553245/';
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  await page.evaluate(async () => {
    for (let i = 0; i < 10; i++) { window.scrollBy(0, 400); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result = await page.evaluate(() => {
    const variantGroups = [];
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
    const seenLabels = new Set();
    while (walker.nextNode()) {
      const text = walker.currentNode.textContent?.trim() || '';
      if (text.match(/^(Пам|Колір|Обсяг|Color|Memory|Storage|Розмір|Модель|Model)/i) && text.length < 30 && !seenLabels.has(text)) {
        seenLabels.add(text);
        const labelEl = walker.currentNode.parentElement;
        let container = labelEl;
        for (let i = 0; i < 6; i++) {
          container = container?.parentElement;
          if (!container) break;
          const links = container.querySelectorAll('a[href*="/p"]');
          if (links.length > 1) {
            const options = [];
            links.forEach(a => {
              const t = a.textContent?.trim();
              const href = a.getAttribute('href') || '';
              const cls = a.className || '';
              const isActive = cls.includes('active') || cls.includes('checked') || cls.includes('selected');
              if (t && t.length < 50) options.push({ text: t, href: href.substring(0, 120), active: isActive, cls: cls.substring(0, 60) });
            });
            if (options.length > 1) { variantGroups.push({ label: text, options }); break; }
          }
        }
      }
    }
    
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    
    const title = document.querySelector('h1')?.textContent?.trim()?.substring(0, 150) || '';
    
    return { variantGroups, sku, title, url: window.location.href };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
