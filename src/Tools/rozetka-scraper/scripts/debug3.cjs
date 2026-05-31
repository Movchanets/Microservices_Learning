const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  // Wait for the gallery container to load (JS rendering)
  try { await page.waitForSelector('.main-slider__item, [class*="product-photo"], [class*="gallery"]', { timeout: 15000 }); } catch {}
  // Extra wait for all images
  await page.waitForTimeout(3000);
  const result = await page.evaluate(() => {
    const allImgs = document.querySelectorAll('img');
    const goodsImgs = [];
    for (const img of allImgs) {
      const src = img.getAttribute('src') || '';
      const dataSrc = img.getAttribute('data-src') || '';
      if (src.includes('goods/images') || dataSrc.includes('goods/images')) {
        goodsImgs.push({ src: (src || dataSrc).substring(0, 120) });
      }
    }
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    const jsonLd = [];
    document.querySelectorAll('script[type="application/ld+json"]').forEach(s => {
      try { jsonLd.push(JSON.parse(s.textContent || '{}')); } catch {}
    });
    return {
      goodsImgs: goodsImgs.slice(0, 15),
      mainSliderItems: document.querySelectorAll('.main-slider__item').length,
      thumbnailButtons: document.querySelectorAll('.thumbnail-button').length,
      sku,
      jsonLdTypes: jsonLd.map(d => d['@type']),
      hasRzBreadcrumbs: !!document.querySelector('rz-breadcrumbs'),
    };
  });
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
})();
