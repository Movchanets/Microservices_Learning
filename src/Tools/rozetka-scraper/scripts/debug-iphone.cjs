const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  // Go to iPhone category
  await page.goto('https://rozetka.com.ua/ua/mobile-phones/c80259/producer=apple/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main article', { timeout: 15000 });
  await page.waitForTimeout(3000);
  
  // Get first iPhone URL
  const productUrl = await page.evaluate(() => {
    const link = document.querySelector('main article a[href*="/p"]');
    return link?.getAttribute('href') || '';
  });
  console.log('Product URL:', productUrl);
  await page.close();
  
  // Fresh context for product
  const ctx2 = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx2.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page2 = await ctx2.newPage();
  
  await page2.goto(productUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page2.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page2.waitForTimeout(5000);
  await page2.evaluate(async () => {
    for (let i = 0; i < 8; i++) { window.scrollBy(0, 400); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result = await page2.evaluate(() => {
    // Find all links that look like variant options
    const allLinks = Array.from(document.querySelectorAll('a[href*="/p"]'));
    const seenHrefs = new Set();
    const variantGroups = {};
    
    for (const a of allLinks) {
      const href = a.getAttribute('href') || '';
      const text = a.textContent?.trim() || '';
      if (!text || text.length > 80 || seenHrefs.has(href)) continue;
      
      // Walk up to find the group container
      let groupEl = a.parentElement;
      let groupName = '';
      for (let i = 0; i < 5 && groupEl; i++) {
        const cls = groupEl.className || '';
        if (cls.includes('product') || cls.includes('varian') || cls.includes('option') || cls.includes('group') || cls.includes('card') || cls.includes('offer')) {
          groupName = cls.substring(0, 60);
          break;
        }
        groupEl = groupEl.parentElement;
      }
      
      // Also check siblings for context (e.g., label "Колір:" or "Пам'ять:")
      const parent = a.parentElement;
      const grandparent = parent?.parentElement;
      const label = grandparent?.querySelector('span, strong, b')?.textContent?.trim() || '';
      
      seenHrefs.add(href);
      
      const key = groupName || parent?.className?.substring(0, 60) || 'ungrouped';
      if (!variantGroups[key]) variantGroups[key] = { label: label.substring(0, 30), items: [] };
      variantGroups[key].items.push({ text: text.substring(0, 50), href: href.substring(0, 100) });
    }
    
    // Also look for buttons that change variants (not links)
    const variantButtons = [];
    document.querySelectorAll('button, [role="button"], [role="option"]').forEach(el => {
      const text = el.textContent?.trim();
      if (text && text.length > 1 && text.length < 50) {
        const parent = el.closest('[class*="varian"], [class*="option"], [class*="group"], [class*="config"]');
        if (parent) {
          variantButtons.push({ text, parentClass: parent.className?.substring(0, 60) });
        }
      }
    });
    
    // Look for "Код:" (SKU)
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    
    return {
      variantGroups: Object.entries(variantGroups).slice(0, 10).map(([k, v]) => ({ containerClass: k, ...v })),
      variantButtons: variantButtons.slice(0, 10),
      sku,
      title: document.title.substring(0, 100),
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
