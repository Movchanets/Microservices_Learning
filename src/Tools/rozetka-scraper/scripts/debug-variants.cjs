const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  // iPhone page with variants
  await page.goto('https://rozetka.com.ua/ua/apple-iphone-15-pro-256gb-natural-titanium/p373959663/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  
  const result = await page.evaluate(() => {
    // Look for variant selectors (color, memory, etc.)
    const variants = [];
    
    // Method 1: Look for "select" or "option" patterns
    document.querySelectorAll('[class*="varian"], [class*="option"], [class*="select"], [class*="config"]').forEach(el => {
      const text = el.textContent?.trim()?.substring(0, 200);
      if (text && text.length > 2) {
        variants.push({ class: el.className?.substring(0, 80), text, tag: el.tagName });
      }
    });
    
    // Method 2: Look for product groups/options with links
    const optionLinks = [];
    document.querySelectorAll('a[href*="/p"]').forEach(a => {
      const text = a.textContent?.trim();
      const href = a.getAttribute('href');
      if (text && text.length < 50 && href) {
        const parent = a.closest('[class*="varian"], [class*="option"], [class*="select"], [class*="config"], [class*="group"]');
        if (parent) {
          optionLinks.push({ text, href: href.substring(0, 100), parentClass: parent.className?.substring(0, 60) });
        }
      }
    });
    
    // Method 3: Look for "Колір" (Color) or "Пам'ять" (Memory) labels
    const labels = [];
    document.querySelectorAll('span, div, label').forEach(el => {
      const t = el.textContent?.trim() || '';
      if (t.match(/^(Колір|Пам'ять|Color|Memory|Storage|Розмір|Size):?$/i)) {
        const parent = el.parentElement;
        const siblings = parent ? Array.from(parent.querySelectorAll('a, button, [role="option"]')).map(s => s.textContent?.trim()).filter(Boolean) : [];
        labels.push({ label: t, options: siblings.slice(0, 10) });
      }
    });
    
    // Method 4: Look for JSON-LD product data with offers
    const jsonLd = [];
    document.querySelectorAll('script[type="application/ld+json"]').forEach(s => {
      try {
        const d = JSON.parse(s.textContent || '{}');
        if (d['@type'] === 'Product' || d.offers) {
          jsonLd.push({ type: d['@type'], name: d.name?.substring(0, 80), offers: d.offers ? 'yes' : 'no', offerCount: d.offers?.length });
        }
      } catch {}
    });
    
    // Method 5: Check for "sku" in structured data
    let skuFromMeta = '';
    document.querySelectorAll('meta[property*="sku"], meta[property*="retailer_item_id"]').forEach(m => {
      skuFromMeta = m.getAttribute('content') || '';
    });
    
    return { 
      variantElements: variants.slice(0, 10), 
      optionLinks: optionLinks.slice(0, 15),
      labels: labels.slice(0, 5),
      jsonLd,
      skuFromMeta,
      title: document.title.substring(0, 80),
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
