const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  await page.goto('https://rozetka.com.ua/ua/apple-iphone-17-pro-max-512gb-cosmic-orange-mfyt4af-a/p543553245/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  
  // Scroll to find all variant sections
  await page.evaluate(async () => {
    for (let i = 0; i < 15; i++) { window.scrollBy(0, 300); await new Promise(r => setTimeout(r, 300)); }
    window.scrollTo(0, 0); await new Promise(r => setTimeout(r, 500));
  });
  
  const result = await page.evaluate(() => {
    // Get ALL product links on the page (variant links point to /p{id}/)
    const allProductLinks = [];
    document.querySelectorAll('a[href*="/p"]').forEach(a => {
      const href = a.getAttribute('href') || '';
      const text = a.textContent?.trim() || '';
      const pid = href.match(/\/p(\d+)\//)?.[1];
      if (pid && text.length < 100) {
        const img = a.querySelector('img');
        const imgSrc = img?.getAttribute('src') || '';
        const title = a.getAttribute('title') || '';
        const cls = a.className || '';
        allProductLinks.push({
          pid,
          text: text.substring(0, 80),
          href: href.substring(0, 150),
          title: title.substring(0, 80),
          hasImg: !!img,
          imgSrc: imgSrc.substring(0, 80),
          cls: cls.substring(0, 80),
        });
      }
    });
    
    // Deduplicate by pid
    const seen = new Set();
    const unique = allProductLinks.filter(l => {
      if (seen.has(l.pid)) return false;
      seen.add(l.pid);
      return true;
    });
    
    // Group by container class to find variant groups
    const containers = [];
    document.querySelectorAll('[class*="product-config"], [class*="varian"], [class*="option-group"], [class*="sku-select"]').forEach(el => {
      const links = el.querySelectorAll('a[href*="/p"]');
      if (links.length > 1) {
        const items = [];
        links.forEach(a => {
          const pid = (a.getAttribute('href') || '').match(/\/p(\d+)\//)?.[1] || '';
          items.push({ pid, text: a.textContent?.trim()?.substring(0, 50), href: (a.getAttribute('href') || '').substring(0, 120) });
        });
        containers.push({ class: el.className?.substring(0, 80), items });
      }
    });
    
    // Look for color circles/swatches (images with product links)
    const colorSwatches = [];
    document.querySelectorAll('a[href*="/p"] img').forEach(img => {
      const a = img.closest('a');
      const href = a?.getAttribute('href') || '';
      const pid = href.match(/\/p(\d+)\//)?.[1];
      if (pid) {
        colorSwatches.push({
          pid,
          href: href.substring(0, 120),
          alt: img.getAttribute('alt')?.substring(0, 50) || '',
          src: img.getAttribute('src')?.substring(0, 80) || '',
          title: a?.getAttribute('title')?.substring(0, 50) || '',
        });
      }
    });
    
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    
    return {
      uniqueProductLinks: unique.slice(0, 20),
      variantContainers: containers,
      colorSwatches: colorSwatches.slice(0, 20),
      sku,
      title: document.title.substring(0, 100),
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
