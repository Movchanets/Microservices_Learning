const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);
  
  // Scroll to trigger lazy loading
  await page.evaluate(() => window.scrollBy(0, 300));
  await page.waitForTimeout(1000);
  
  const result = await page.evaluate(() => {
    // Count all images
    const allImgs = document.querySelectorAll('img');
    const imgData = [];
    
    for (const img of allImgs) {
      const src = img.getAttribute('src') || '';
      const dataSrc = img.getAttribute('data-src') || '';
      
      if (src.includes('goods/images') || dataSrc.includes('goods/images')) {
        imgData.push({
          src: src.substring(0, 100),
          dataSrc: dataSrc.substring(0, 100),
          parentClass: img.parentElement?.className?.substring(0, 60),
          hasBig: src.includes('/big/') || dataSrc.includes('/big/'),
          hasMedium: src.includes('/medium/') || dataSrc.includes('/medium/'),
          hasOriginal: src.includes('/original/') || dataSrc.includes('/original/'),
        });
      }
    }
    
    // Check JSON-LD
    const jsonLd = [];
    document.querySelectorAll('script[type="application/ld+json"]').forEach(s => {
      try {
        const d = JSON.parse(s.textContent || '{}');
        if (d.itemListElement) jsonLd.push(d);
      } catch {}
    });
    
    return { 
      totalImgs: allImgs.length, 
      goodsImages: imgData.slice(0, 20),
      jsonLdCount: jsonLd.length,
      mainSlider: document.querySelectorAll('.main-slider__item').length,
    };
  });
  
  console.log(JSON.stringify(result, null, 2));
  
  await browser.close();
})();
