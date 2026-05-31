const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  // Direct to a known Samsung phone with variants
  await page.goto('https://rozetka.com.ua/ua/samsung-sm-s936bzbgekc/p424156498/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  await page.evaluate(async () => {
    for (let i = 0; i < 8; i++) { window.scrollBy(0, 400); await new Promise(r => setTimeout(r, 500)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 1000));
  });
  
  const result = await page.evaluate(() => {
    // Look for variant groups - memory/color selectors
    const variantInfo = [];
    
    // Method: Find all elements with "Пам'ять" or "Колір" text
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
    while (walker.nextNode()) {
      const text = walker.currentNode.textContent?.trim() || '';
      if (text.match(/^(Пам[''']ять|Колір|Обсяг памяті|Color|Memory|Storage|Розмір)$/i)) {
        const labelEl = walker.currentNode.parentElement;
        const container = labelEl?.closest('div, section, fieldset');
        if (container) {
          const options = [];
          container.querySelectorAll('a[href], button, [role="option"], label').forEach(opt => {
            const t = opt.textContent?.trim();
            const href = opt.getAttribute('href') || '';
            if (t && t.length < 50 && t !== text) {
              options.push({ text: t, href: href.substring(0, 100), tag: opt.tagName });
            }
          });
          if (options.length > 0) {
            variantInfo.push({ label: text, options: options.slice(0, 10) });
          }
        }
      }
    }
    
    // Also find all links that share a common parent pattern (variant links)
    const productLinks = Array.from(document.querySelectorAll('a[href*="/p"]'));
    const groupedByParent = {};
    for (const a of productLinks) {
      const text = a.textContent?.trim() || '';
      if (!text || text.length > 80) continue;
      const parentClass = a.parentElement?.className || 'unknown';
      if (!groupedByParent[parentClass]) groupedByParent[parentClass] = [];
      groupedByParent[parentClass].push({ text: text.substring(0, 50), href: (a.getAttribute('href') || '').substring(0, 100) });
    }
    // Only keep groups with multiple items (likely variants)
    const variantLinkGroups = Object.entries(groupedByParent)
      .filter(([_, items]) => items.length > 1 && items.length < 20)
      .slice(0, 5)
      .map(([cls, items]) => ({ parentClass: cls.substring(0, 60), items }));
    
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    
    return {
      variantInfo,
      variantLinkGroups,
      sku,
      title: document.title.substring(0, 100),
      url: window.location.href,
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
