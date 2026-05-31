const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true, args: ['--disable-blink-features=AutomationControlled'] });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA', viewport: { width: 1920, height: 1080 },
  });
  await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });
  const page = await ctx.newPage();
  
  // AirPods page
  await page.goto('https://rozetka.com.ua/ua/448744721/p448744721/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForSelector('main', { timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(5000);
  
  const result = await page.evaluate(() => {
    const currentPid = window.location.href.match(/\/p(\d+)\//)?.[1] || '';
    const links = [];
    document.querySelectorAll('a[href*="/p"]').forEach(a => {
      const href = a.getAttribute('href') || '';
      const pid = href.match(/\/p(\d+)\//)?.[1];
      if (pid && pid !== currentPid) {
        const text = a.textContent?.trim() || '';
        const cls = a.className || '';
        if (!cls.includes('service-product') && !cls.includes('footer') && !cls.includes('tile-image')) {
          links.push({ pid, text: text.substring(0, 60) || '(no text)', href: href.substring(0, 120) });
        }
      }
    });
    return { 
      title: document.title.substring(0, 80),
      pid: currentPid,
      variantLinks: links.slice(0, 10),
      totalLinks: links.length,
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
