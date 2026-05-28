const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA',
  });
  const page = await ctx.newPage();
  
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);
  
  const result = await page.evaluate(() => {
    // === BREADCRUMBS ===
    const breadcrumbs = [];
    document.querySelectorAll('a').forEach(a => {
      const href = a.getAttribute('href') || '';
      const text = a.textContent?.trim() || '';
      // Rozetka breadcrumbs: links to category hierarchy
      if (href.includes('rozetka.com.ua/ua/') && href.includes('/c') && text.length > 1 && text.length < 60) {
        const parent = a.closest('nav, [class*="bread"], [class*="path"], ol, ul');
        if (parent || breadcrumbs.length < 8) {
          breadcrumbs.push({ text, href: href.substring(0, 120) });
        }
      }
    });
    
    // === SKU/ARTICLE ===
    const skuResults = [];
    // Look for text containing "Код" or article number patterns
    const allSpans = document.querySelectorAll('span, div, p, li, dt, dd');
    allSpans.forEach(el => {
      const t = el.textContent?.trim() || '';
      if (t.match(/^(Код|Артикул|SKU|Код товару|Номер)\s*[:#]?\s*\d+/i)) {
        skuResults.push({ text: t.substring(0, 80), tag: el.tagName, class: el.className?.substring(0, 60) });
      }
    });
    
    // Also check meta tags
    const metaSku = document.querySelector('meta[property="product:retailer_item_id"], meta[property="og:sku"]');
    if (metaSku) {
      skuResults.push({ meta: metaSku.getAttribute('content') });
    }
    
    // === IMAGE GALLERY ===
    const allImages = [];
    const seenSrcs = new Set();
    
    // All images on page
    document.querySelectorAll('img').forEach(img => {
      const src = img.getAttribute('src') || img.getAttribute('data-src') || '';
      if (src.includes('rozetka.com.ua/goods/images') && !src.includes('tag') && !seenSrcs.has(src)) {
        seenSrcs.add(src);
        allImages.push({
          src: src.substring(0, 150),
          alt: img.getAttribute('alt')?.substring(0, 60),
          width: img.naturalWidth || img.width,
          parentClass: img.parentElement?.className?.substring(0, 60),
        });
      }
    });
    
    // Check for gallery-specific containers
    const galleryContainers = [];
    document.querySelectorAll('[class*="gallery"], [class*="slider"], [class*="carousel"], [class*="thumb"], [class*="preview"]').forEach(el => {
      const imgs = el.querySelectorAll('img');
      if (imgs.length > 0) {
        const imgSrcs = Array.from(imgs).map(i => (i.getAttribute('src') || i.getAttribute('data-src') || '').substring(0, 100)).filter(Boolean);
        galleryContainers.push({ class: el.className?.substring(0, 80), imgCount: imgs.length, srcs: imgSrcs.slice(0, 5) });
      }
    });
    
    // === CATEGORIES FROM SIDEBAR/MENU ===
    const categoryTree = [];
    document.querySelectorAll('[class*="sidebar"] a, [class*="menu"] a, [class*="catalog-nav"] a, [class*="category"] a').forEach(a => {
      const href = a.getAttribute('href') || '';
      const text = a.textContent?.trim() || '';
      if (href.includes('rozetka.com.ua') && text.length > 1 && text.length < 60) {
        categoryTree.push({ text, href: href.substring(0, 120) });
      }
    });
    
    return { breadcrumbs: breadcrumbs.slice(0, 10), skuResults, allImages: allImages.slice(0, 15), galleryContainers: galleryContainers.slice(0, 10), categoryTree: categoryTree.slice(0, 20) };
  });
  
  console.log(JSON.stringify(result, null, 2));
  
  await browser.close();
})();
