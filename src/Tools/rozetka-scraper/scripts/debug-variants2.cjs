const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  // Search for iPhone on Rozetka
  await page.goto('https://rozetka.com.ua/ua/mobile-phones/c80259/producer=apple/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main article', { timeout: 15000 });
  await page.waitForTimeout(3000);
  
  // Get first product URL
  const productUrl = await page.evaluate(() => {
    const link = document.querySelector('main article a[href*="/p"]');
    return link?.getAttribute('href') || '';
  });
  console.log('Product URL:', productUrl);
  
  // Navigate to product
  await page.goto(productUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  await page.evaluate(async () => {
    for (let i = 0; i < 5; i++) { window.scrollBy(0, 300); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result = await page.evaluate(() => {
    // Look for variant/configuration options
    const variantData = [];
    
    // Check all links that point to other products (variants)
    const allLinks = Array.from(document.querySelectorAll('a[href*="/p"]'));
    const variantLinks = [];
    const seenHrefs = new Set();
    
    for (const a of allLinks) {
      const href = a.getAttribute('href') || '';
      const text = a.textContent?.trim() || '';
      const parentClass = a.parentElement?.className || '';
      const grandparentClass = a.parentElement?.parentElement?.className || '';
      
      // Look for links in variant containers
      if (href.includes('/p') && text.length > 0 && text.length < 80 && !seenHrefs.has(href)) {
        // Check if parent has variant-related class
        const container = a.closest('[class*="varian"], [class*="option"], [class*="select"], [class*="config"], [class*="group"], [class*="product-card"], [class*="offer"]');
        if (container) {
          seenHrefs.add(href);
          variantLinks.push({ text, href: href.substring(0, 100), containerClass: container.className?.substring(0, 80) });
        }
      }
    }
    
    // Look for specific text patterns indicating variants
    const bodyText = document.body.innerText;
    const colorMatch = bodyText.match(/Колір[:\s]+([^\n]+)/);
    const memoryMatch = bodyText.match(/(?:Пам'ять|Обсяг)[:\s]+([^\n]+)/);
    
    // Check for "також доступний" (also available) sections
    const alsoAvailable = [];
    document.querySelectorAll('[class*="also"], [class*="similar"], [class*="available"]').forEach(el => {
      const text = el.textContent?.trim()?.substring(0, 200);
      if (text) alsoAvailable.push(text);
    });
    
    // Check for SKU/variant code patterns in URL
    const currentUrl = window.location.href;
    const urlSku = currentUrl.match(/\/p(\d+)\//)?.[1] || '';
    
    return {
      variantLinks: variantLinks.slice(0, 15),
      colorMatch: colorMatch?.[1]?.substring(0, 50),
      memoryMatch: memoryMatch?.[1]?.substring(0, 50),
      alsoAvailable: alsoAvailable.slice(0, 3),
      currentUrl,
      urlSku,
      title: document.title.substring(0, 80),
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
