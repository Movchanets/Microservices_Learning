const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA',
  });
  const page = await ctx.newPage();
  
  // Check product page breadcrumbs
  await page.goto('https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);
  
  const result = await page.evaluate(() => {
    // === BREADCRUMBS - look for rz-breadcrumbs or nav with breadcrumb role ===
    const breadcrumbs = [];
    
    // Method 1: Look for nav with aria-label containing "bread" or "навігаці"
    document.querySelectorAll('nav, [role="navigation"]').forEach(nav => {
      const label = nav.getAttribute('aria-label') || '';
      if (label.toLowerCase().includes('bread') || label.toLowerCase().includes('навігац') || label.toLowerCase().includes('path')) {
        nav.querySelectorAll('a').forEach(a => {
          breadcrumbs.push({ text: a.textContent?.trim(), href: a.getAttribute('href'), source: 'nav-aria' });
        });
      }
    });
    
    // Method 2: Look for rz-breadcrumbs custom element
    const rzBread = document.querySelector('rz-breadcrumbs, app-breadcrumbs');
    if (rzBread) {
      rzBread.querySelectorAll('a').forEach(a => {
        breadcrumbs.push({ text: a.textContent?.trim(), href: a.getAttribute('href'), source: 'rz-breadcrumbs' });
      });
    }
    
    // Method 3: Look for ol/ul with breadcrumb class
    document.querySelectorAll('ol[class*="bread"], ul[class*="bread"], [class*="breadcrumb"]').forEach(el => {
      el.querySelectorAll('a').forEach(a => {
        breadcrumbs.push({ text: a.textContent?.trim(), href: a.getAttribute('href'), source: 'class-breadcrumb' });
      });
    });
    
    // Method 4: Look for structured data (JSON-LD)
    const jsonLd = document.querySelector('script[type="application/ld+json"]');
    let structuredData = null;
    if (jsonLd) {
      try {
        const data = JSON.parse(jsonLd.textContent || '{}');
        if (data.breadcrumb || data['@type'] === 'BreadcrumbList') {
          structuredData = data;
        }
        // Also check for itemListElement which is common in Rozetka
        if (data.itemListElement) {
          structuredData = data;
        }
      } catch {}
    }
    
    // === CATEGORY TREE from main page ===
    // Will be done on the category page
    
    return { breadcrumbs, structuredData, bodyClasses: document.body.className?.substring(0, 100) };
  });
  
  console.log('=== PRODUCT PAGE BREADCRUMBS ===');
  console.log(JSON.stringify(result, null, 2));
  
  // Now check category page for category tree
  await page.goto('https://rozetka.com.ua/ua/notebooks/c80004/', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);
  
  const catResult = await page.evaluate(() => {
    // Look for sidebar category tree
    const categories = [];
    
    // Method 1: Look for category links in sidebar
    document.querySelectorAll('[class*="sidebar"] a, [class*="filter"] a, [class*="category"] a').forEach(a => {
      const href = a.getAttribute('href') || '';
      const text = a.textContent?.trim() || '';
      if (href.includes('rozetka.com.ua') && text.length > 1 && text.length < 60) {
        categories.push({ text, href: href.substring(0, 120), source: 'sidebar' });
      }
    });
    
    // Method 2: Look for the category tree/list
    const catTree = [];
    document.querySelectorAll('[class*="tree"] a, [class*="catalog-nav"] a, [class*="menu"] a').forEach(a => {
      const href = a.getAttribute('href') || '';
      const text = a.textContent?.trim() || '';
      if (href.includes('rozetka.com.ua') && text.length > 1 && text.length < 60) {
        catTree.push({ text, href: href.substring(0, 120), source: 'tree' });
      }
    });
    
    // Method 3: Look for the main category heading and subcategories
    const mainHeading = document.querySelector('h1');
    const headingText = mainHeading?.textContent?.trim();
    
    // Find all links that point to subcategories
    const subcats = [];
    document.querySelectorAll('a[href*="/c80004/"]').forEach(a => {
      const text = a.textContent?.trim() || '';
      const href = a.getAttribute('href') || '';
      if (text.length > 1 && text.length < 50 && !href.includes('producer=')) {
        subcats.push({ text, href: href.substring(0, 120) });
      }
    });
    
    return { 
      sidebarCategories: categories.slice(0, 15),
      treeCategories: catTree.slice(0, 15),
      mainHeading: headingText,
      subcategories: subcats.slice(0, 20)
    };
  });
  
  console.log('\n=== CATEGORY PAGE ===');
  console.log(JSON.stringify(catResult, null, 2));
  
  await browser.close();
})();
