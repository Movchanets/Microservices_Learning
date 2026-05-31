const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  // Use networkidle to wait for full page load
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'networkidle', timeout: 60000 });
  await page.waitForTimeout(3000);
  
  const result = await page.evaluate(() => {
    const allImgs = document.querySelectorAll('img');
    const imgData = [];
    
    for (const img of allImgs) {
      const src = img.getAttribute('src') || '';
      const dataSrc = img.getAttribute('data-src') || '';
      
      if (src.includes('goods/images') || dataSrc.includes('goods/images')) {
        imgData.push({
          src: src.substring(0, 120),
          parentClass: img.parentElement?.className?.substring(0, 60),
        });
      }
    }
    
    // JSON-LD
    const jsonLd = [];
    document.querySelectorAll('script[type="application/ld+json"]').forEach(s => {
      try { jsonLd.push(JSON.parse(s.textContent || '{}')); } catch {}
    });
    
    // SKU
    let sku = '';
    for (const el of document.querySelectorAll('span, div')) {
      const t = el.textContent?.trim() || '';
      const match = t.match(/^Код:\s*(\d+)$/);
      if (match) { sku = match[1]; break; }
    }
    
    return { 
      totalImgs: allImgs.length, 
      goodsImages: imgData.slice(0, 15),
      mainSlider: document.querySelectorAll('.main-slider__item').length,
      thumbnails: document.querySelectorAll('.thumbnail-button').length,
      jsonLdTypes: jsonLd.map(d => d['@type']),
      sku,
      hasBreadcrumbs: !!document.querySelector('rz-breadcrumbs'),
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  
  await browser.close();
})();
