/**
 * Rozetka Product Detail Page Object
 *
 * Extracts full product details from Rozetka product pages:
 * - SKU/Article number (Код товару)
 * - Full image gallery (big/ URLs, not medium/)
 * - Breadcrumbs (from rz-breadcrumbs or JSON-LD)
 * - Description (from "Опис" tab or meta tag)
 * - Brand (from product attributes or meta)
 * - Subtitle / brief summary
 * - Price (from the current page, not parent tile)
 * - Product specifications (Характеристики section)
 * - Availability status
 * - Variant links with type classification
 */

import { Page } from 'playwright';

// ── Interfaces ──────────────────────────────────────────────────

export interface Breadcrumb {
  name: string;
  url?: string;
  position: number;
}

export interface ProductVariant {
  pid: string;
  url: string;
  name: string;
  type: 'color' | 'storage' | 'model' | 'ram' | 'other';
}

export interface ProductSpecification {
  key: string;
  value: string;
  group?: string;  // e.g. "Екран", "Процесор", "Пам'ять"
}

export interface ProductDetails {
  sku: string;                // Rozetka SKU code (e.g., "528975609")
  name: string;               // Full product name from page
  subtitle: string;            // Brief summary below title
  brand: string;               // Brand name
  description: string;         // Full description text
  price: number;               // Current page price (UAH)
  priceText: string;           // Raw price text
  images: string[];            // Full-size image URLs (/goods/images/big/)
  thumbnails: string[];        // Thumbnail URLs (/goods/images/medium/)
  breadcrumbs: Breadcrumb[];   // Category hierarchy
  categoryPath: string;        // "Комп'ютери > Ноутбуки" (without product name)
  variants: ProductVariant[];
  specifications: ProductSpecification[];  // Structured key-value specs
  availability: string;        // "available" | "out_of_stock" | "preorder" | ""
  warranty: string;            // e.g. "12 місяців"
  seller: string;              // Seller name if available
}

// ── Page Object ─────────────────────────────────────────────────

export class RozetkaProductPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(url: string): Promise<void> {
    await this.page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await this.randomDelay(2000, 3000);
  }

  /**
   * Extract all product details from the page.
   * Runs independent extractions in parallel for speed.
   */
  async extractDetails(): Promise<ProductDetails> {
    const [sku, gallery, breadcrumbs, variants, inlineData] = await Promise.all([
      this.extractSku(),
      this.extractGallery(),
      this.extractBreadcrumbs(),
      this.extractVariants(),
      this.extractInlineJsonLd(),
    ]);

    // These depend on page state and may need scrolling
    const [description, specs, availability, warranty, seller] = await Promise.all([
      this.extractDescription(),
      this.extractSpecifications(),
      this.extractAvailability(),
      this.extractWarranty(),
      this.extractSeller(),
    ]);

    const brand = await this.extractBrand();
    const subtitle = await this.extractSubtitle();
    const price = await this.extractPrice();
    const categoryPath = this.buildCategoryPath(breadcrumbs);

    // Use JSON-LD data as fallback for missing fields
    const name = inlineData?.name || await this.extractName();
    const finalDescription = description || inlineData?.description || '';
    const finalBrand = brand || inlineData?.brand || '';
    const finalPrice = price.value || (inlineData?.price ?? 0);

    return {
      sku,
      name,
      subtitle,
      brand: finalBrand,
      description: finalDescription,
      price: finalPrice,
      priceText: price.text,
      images: gallery.images,
      thumbnails: gallery.thumbnails,
      breadcrumbs,
      categoryPath,
      variants,
      specifications: specs,
      availability,
      warranty,
      seller,
    };
  }

  // ── SKU ─────────────────────────────────────────────────────

  /**
   * Extract SKU/article number.
   * Pattern: <span class="ms-auto color-black-60">Код:  528975609</span>
   */
  async extractSku(): Promise<string> {
    const fromDom = await this.page.evaluate(() => {
      // Method 1: Look for "Код:" text
      const spans = document.querySelectorAll('span, div, p');
      for (const el of spans) {
        const t = el.textContent?.trim() || '';
        const match = t.match(/^Код:\s*(\d+)$/);
        if (match) return match[1];
      }
      // Method 2: Look in product code section
      const codeEl = document.querySelector('[class*="product-code"], [class*="code"]');
      if (codeEl) {
        const match = codeEl.textContent?.match(/(\d{6,})/);
        if (match) return match[1];
      }
      return '';
    });
    if (fromDom) return fromDom;

    // Method 3: Extract from URL
    const url = this.page.url();
    const urlMatch = url.match(/\/p(\d+)\//);
    return urlMatch ? urlMatch[1] : '';
  }

  // ── Name ────────────────────────────────────────────────────

  async extractName(): Promise<string> {
    return this.page.evaluate(() => {
      const h1 = document.querySelector('h1');
      return h1?.textContent?.trim() || '';
    });
  }

  // ── Subtitle ────────────────────────────────────────────────

  /**
   * Extract subtitle / brief summary below the product title.
   * Rozetka shows a short key-feature line under <h1>.
   */
  async extractSubtitle(): Promise<string> {
    return this.page.evaluate(() => {
      // Method 1: Direct sibling/subtitle selectors
      const selectors = [
        '[class*="product-subtitle"]',
        '[class*="subtitle"]',
        '[class*="about__brief"]',
        '[class*="product-about"] > p:first-of-type',
        'h1 + p',
        'h1 ~ p',
      ];
      for (const sel of selectors) {
        const el = document.querySelector(sel);
        const text = el?.textContent?.trim();
        if (text && text.length > 10 && text.length < 500) return text;
      }
      // Method 2: Meta description as subtitle fallback
      const meta = document.querySelector('meta[name="description"]');
      const metaContent = meta?.getAttribute('content') || '';
      if (metaContent.length > 10 && metaContent.length < 500) return metaContent;
      return '';
    });
  }

  // ── Brand ───────────────────────────────────────────────────

  /**
   * Extract brand from product page.
   * Checks multiple sources: spec table, brand link, meta, JSON-LD.
   */
  async extractBrand(): Promise<string> {
    return this.page.evaluate(() => {
      // Method 1: Brand in spec table
      const specRows = document.querySelectorAll('tr, [class*="spec-row"], [class*="characteristics"] li');
      for (const row of specRows) {
        const label = row.querySelector('td:first-child, [class*="label"], [class*="name"]');
        const value = row.querySelector('td:last-child, [class*="value"]');
        const labelText = label?.textContent?.trim().toLowerCase() || '';
        if (labelText === 'бренд' || labelText === 'brand' || labelText === 'виробник') {
          const val = value?.textContent?.trim();
          if (val) return val;
        }
      }

      // Method 2: Brand link/image near title
      const brandLink = document.querySelector(
        '[class*="brand"] a, [class*="brand"] img, [class*="product-brand"] a'
      );
      if (brandLink) {
        const text = brandLink.textContent?.trim();
        if (text && text.length > 1 && text.length < 50) return text;
        const alt = brandLink.getAttribute('alt');
        if (alt) return alt;
        const title = brandLink.getAttribute('title');
        if (title) return title;
      }

      // Method 3: JSON-LD brand
      const scripts = document.querySelectorAll('script[type="application/ld+json"]');
      for (const script of scripts) {
        try {
          const data = JSON.parse(script.textContent || '{}');
          if (data['@type'] === 'Product' && data.brand) {
            return typeof data.brand === 'string' ? data.brand : data.brand.name || '';
          }
        } catch {}
      }

      // Method 4: Meta og:brand
      const metaBrand = document.querySelector('meta[property="product:brand"]');
      return metaBrand?.getAttribute('content') || '';
    });
  }

  // ── Price ───────────────────────────────────────────────────

  /**
   * Extract the current page's price (important for variant pages).
   */
  async extractPrice(): Promise<{ value: number; text: string }> {
    return this.page.evaluate(() => {
      // Method 1: Main price display
      const priceSelectors = [
        '[class*="product-price"] [class*="price"]',
        '[class*="price__big"]',
        '[class*="main-price"]',
        '[class*="price--big"]',
        'p[class*="price"]',
        '[class*="product-prices"] [class*="big"]',
      ];
      for (const sel of priceSelectors) {
        const el = document.querySelector(sel);
        const text = el?.textContent?.trim() || '';
        const match = text.match(/([\d\s]+)\s*₴/);
        if (match) {
          const value = parseInt(match[1].replace(/\s/g, ''), 10);
          if (value > 0) return { value, text };
        }
      }

      // Method 2: Any price-like text
      const allText = document.body.textContent || '';
      const priceMatches = allText.match(/([\d\s]{2,})\s*₴/g);
      if (priceMatches && priceMatches.length > 0) {
        const lastPrice = priceMatches[priceMatches.length - 1];
        const match = lastPrice.match(/([\d\s]+)\s*₴/);
        if (match) {
          const value = parseInt(match[1].replace(/\s/g, ''), 10);
          if (value > 0) return { value, text: lastPrice.trim() };
        }
      }

      // Method 3: JSON-LD price
      const scripts = document.querySelectorAll('script[type="application/ld+json"]');
      for (const script of scripts) {
        try {
          const data = JSON.parse(script.textContent || '{}');
          if (data['@type'] === 'Product' && data.offers) {
            const price = parseFloat(data.offers.price || '0');
            if (price > 0) return { value: price, text: `${price}₴` };
          }
        } catch {}
      }

      return { value: 0, text: '' };
    });
  }

  // ── Gallery ─────────────────────────────────────────────────

  /**
   * Extract full image gallery.
   * Main images: .main-slider__item img with /goods/images/big/ URLs
   * Thumbnails: .thumbnail-button img with /goods/images/medium/ URLs
   */
  async extractGallery(): Promise<{ images: string[]; thumbnails: string[] }> {
    return this.page.evaluate(() => {
      const images: string[] = [];
      const thumbnails: string[] = [];
      const seenImages = new Set<string>();
      const seenThumbs = new Set<string>();

      // Full-size images from main slider
      document.querySelectorAll('.main-slider__item img, .main-slider img').forEach(img => {
        const src = img.getAttribute('src') || '';
        if (src.includes('/goods/images/big/') && !seenImages.has(src)) {
          seenImages.add(src);
          images.push(src);
        }
      });

      // Also check for data-src (lazy loaded)
      document.querySelectorAll('img[data-src*="/goods/images/big/"]').forEach(img => {
        const src = img.getAttribute('data-src') || '';
        if (src && !seenImages.has(src)) {
          seenImages.add(src);
          images.push(src);
        }
      });

      // Fallback: any goods image on page
      if (images.length === 0) {
        document.querySelectorAll('img[src*="rozetka.com.ua/goods/images"]').forEach(img => {
          const src = img.getAttribute('src') || '';
          if (src && !src.includes('tag') && !src.includes('preview') && !seenImages.has(src)) {
            seenImages.add(src);
            images.push(src);
          }
        });
      }

      // Thumbnails
      document.querySelectorAll('.thumbnail-button img, [class*="thumb"] img').forEach(img => {
        const src = img.getAttribute('src') || '';
        if (src.includes('/goods/images/') && !seenThumbs.has(src)) {
          seenThumbs.add(src);
          thumbnails.push(src);
        }
      });

      return { images, thumbnails };
    });
  }

  // ── Breadcrumbs ─────────────────────────────────────────────

  /**
   * Extract breadcrumbs from JSON-LD BreadcrumbList or rz-breadcrumbs element.
   */
  async extractBreadcrumbs(): Promise<Breadcrumb[]> {
    return this.page.evaluate(() => {
      const breadcrumbs: Array<{ name: string; url?: string; position: number }> = [];

      // Method 1: JSON-LD structured data (most reliable)
      const jsonLd = document.querySelector('script[type="application/ld+json"]');
      if (jsonLd) {
        try {
          const data = JSON.parse(jsonLd.textContent || '{}');
          if (data.itemListElement) {
            data.itemListElement.forEach((item: any) => {
              breadcrumbs.push({
                name: item.item?.name || item.name || '',
                url: item.item?.['@id'],
                position: item.position,
              });
            });
            return breadcrumbs;
          }
        } catch {}
      }

      // Method 2: rz-breadcrumbs element
      const rzBread = document.querySelector('rz-breadcrumbs, app-breadcrumbs, [class*="breadcrumb"]');
      if (rzBread) {
        let pos = 1;
        rzBread.querySelectorAll('a').forEach(a => {
          const text = a.textContent?.trim() || '';
          if (text) {
            breadcrumbs.push({
              name: text,
              url: a.getAttribute('href') || undefined,
              position: pos++,
            });
          }
        });
      }

      return breadcrumbs;
    });
  }

  // ── Variants ────────────────────────────────────────────────

  /**
   * Extract product variants from the variant selector section.
   * Rozetka pages have structured selectors with labels like:
   *   "Колір: Cosmic Orange" → color axis
   *   "Вбудована пам'ять: 256 ГБ" → storage axis
   *   "Серія: iPhone 17 Pro Max" → model axis (filtered out)
   */
  async extractVariants(): Promise<ProductVariant[]> {
    const currentUrl = this.page.url();
    const currentPid = currentUrl.match(/\/p(\d+)\//)?.[1] || '';

    // Step 1: Extract variant axes from the selector section
    const axes = await this.page.evaluate(() => {
      const axes: Array<{
        label: string;
        type: 'color' | 'storage' | 'ram' | 'model' | 'other';
        options: Array<{ name: string; url: string; pid: string }>;
      }> = [];

      // Method 1: Check structured Angular components dynamically (universal for all categories)
      const configurators = document.querySelectorAll('rz-var-parameter-option, [class*="var-option"]');
      
      configurators.forEach(el => {
        // Find label
        const labelEl = el.querySelector('.color-black-60, [class*="color-black"]');
        let labelVal = '';
        if (labelEl) {
          labelVal = labelEl.textContent?.trim().replace(/:\s*$/, '') || '';
        } else {
          const p = el.querySelector('p');
          const match = p?.textContent?.trim().match(/^([^:]+):/);
          labelVal = match ? match[1].trim() : '';
        }
        if (!labelVal) return;

        // Find list
        let list = el.querySelector('ul');
        if (!list) {
          const sibling = el.nextElementSibling;
          if (sibling) {
            list = sibling.tagName === 'UL' ? sibling as HTMLUListElement : sibling.querySelector('ul');
          }
        }
        if (!list) return;

        const options: Array<{ name: string; url: string; pid: string }> = [];
        list.querySelectorAll('a[href]').forEach(a => {
          const href = a.getAttribute('href') || '';
          const pid = href.match(/\/p(\d+)\//)?.[1];
          if (!pid) return;
          
          let name = a.textContent?.trim() || a.querySelector('[class*="value"]')?.textContent?.trim() || '';
          
          // Try to extract name from tooltip/title
          if (!name) {
            const title = a.getAttribute('title') || a.querySelector('[rztooltip]')?.getAttribute('rztooltip') || '';
            name = title.trim();
          }
          
          // Try to extract color term from URL slug if name is still empty
          if (!name) {
            const hrefLower = href.toLowerCase();
            const colors = [
              'cosmic-orange', 'deep-blue', 'desert-titanium', 'natural-titanium', 'space-gray', 'space-grey', 'space-black',
              'black', 'white', 'blue', 'red', 'green', 'gold', 'silver', 'purple', 'pink', 'orange', 'titanium',
              'midnight', 'starlight', 'cosmic', 'deep', 'natural', 'slate', 'space', 'graphite', 'rose'
            ];
            const foundColor = colors.find(c => hrefLower.includes(c));
            if (foundColor) {
              name = foundColor.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
            }
          }
          
          const fullUrl = href.startsWith('http') ? href : 'https://rozetka.com.ua' + href;
          options.push({ name: name || pid, url: fullUrl, pid });
        });

        if (options.length === 0) return;

        // Classify axis type from label
        let type: 'color' | 'storage' | 'ram' | 'model' | 'other' = 'other';
        const labelLower = labelVal.toLowerCase();
        if (labelLower.includes('колір') || labelLower.includes('color')) type = 'color';
        else if ((labelLower.includes('пам\'ять') || labelLower.includes('ssd') || labelLower.includes('hdd') || labelLower.includes('накопичувач')) && !labelLower.includes('оперативна')) type = 'storage';
        else if (labelLower.includes('оперативна') || labelLower.includes('ram')) type = 'ram';
        else if (labelLower.includes('серія') || labelLower.includes('модель') || labelLower.includes('series') || labelLower.includes('model')) type = 'model';

        if (!axes.some(a => a.label === labelVal)) {
          axes.push({ label: labelVal, type, options });
        }
      });

      // Method 2: Fallback paragraph search if no axes found (legacy templates)
      if (axes.length === 0) {
        const allParas = document.querySelectorAll('p');
        for (const p of allParas) {
          const text = p.textContent?.trim() || '';
          const labelMatch = text.match(/^(Колір|Вбудована пам.?ять|Обсяг пам.?яті|Оперативна пам.?ять|Обсяг SSD|Об'єм SSD|Серія|Модель|Розмір|Color|Storage|RAM|Series|Model|Size):\s*/i);
          if (!labelMatch) continue;
          
          const labelVal = labelMatch[1];
          let list = p.nextElementSibling;
          if (!list) continue;
          
          if (list.tagName !== 'UL') {
            list = list.querySelector('ul');
          }
          if (!list || list.tagName !== 'UL') continue;

          const options: Array<{ name: string; url: string; pid: string }> = [];
          list.querySelectorAll('a[href]').forEach(a => {
            const href = a.getAttribute('href') || '';
            const pid = href.match(/\/p(\d+)\//)?.[1];
            if (!pid) return;
            
            let name = a.textContent?.trim() || a.querySelector('[class*="value"]')?.textContent?.trim() || '';
            if (!name) {
              const title = a.getAttribute('title') || a.querySelector('[rztooltip]')?.getAttribute('rztooltip') || '';
              name = title.trim();
            }
            
            if (!name) {
              const hrefLower = href.toLowerCase();
              const colors = [
                'cosmic-orange', 'deep-blue', 'desert-titanium', 'natural-titanium', 'space-gray', 'space-grey', 'space-black',
                'black', 'white', 'blue', 'red', 'green', 'gold', 'silver', 'purple', 'pink', 'orange', 'titanium',
                'midnight', 'starlight', 'cosmic', 'deep', 'natural', 'slate', 'space', 'graphite', 'rose'
              ];
              const foundColor = colors.find(c => hrefLower.includes(c));
              if (foundColor) {
                name = foundColor.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
              }
            }
            
            const fullUrl = href.startsWith('http') ? href : 'https://rozetka.com.ua' + href;
            options.push({ name: name || pid, url: fullUrl, pid });
          });

          if (options.length === 0) continue;

          let type: 'color' | 'storage' | 'ram' | 'model' | 'other' = 'other';
          const labelLower = labelVal.toLowerCase();
          if (labelLower.includes('колір') || labelLower.includes('color')) type = 'color';
          else if ((labelLower.includes('пам\'ять') || labelLower.includes('ssd') || labelLower.includes('hdd') || labelLower.includes('накопичувач')) && !labelLower.includes('оперативна')) type = 'storage';
          else if (labelLower.includes('оперативна') || labelLower.includes('ram')) type = 'ram';
          else if (labelLower.includes('серія') || labelLower.includes('модель') || labelLower.includes('series') || labelLower.includes('model')) type = 'model';

          if (!axes.some(a => a.label === labelVal)) {
            axes.push({ label: labelVal, type, options });
          }
        }
      }

      return axes;
    });

    // Step 2: Build variant list from axes, filtering out current page and model variants
    const variants: ProductVariant[] = [];
    const seen = new Set<string>();
    seen.add(currentPid);

    for (const axis of axes) {
      // Skip "series"/"model" axes — those are different products, not variants
      if (axis.type === 'model') continue;

      for (const opt of axis.options) {
        if (seen.has(opt.pid)) continue;
        seen.add(opt.pid);
        variants.push({
          pid: opt.pid,
          url: opt.url,
          name: opt.name.substring(0, 80),
          type: axis.type,
        });
      }
    }

    // Step 3: If no axes found, fall back to link-based detection
    if (axes.length === 0) {
      const fallback = await this.extractVariantsFromLinks();
      return fallback;
    }

    return variants;
  }

  /**
   * Fallback: extract variants from product links on the page.
   * Used when the structured selector section is not found.
   */
  private async extractVariantsFromLinks(): Promise<ProductVariant[]> {
    const { rawVariants, storageSpec } = await this.page.evaluate(() => {
      const currentPid = window.location.href.match(/\/p(\d+)\//)?.[1] || '';
      const rawVariants: Array<{ pid: string; url: string; name: string; type: 'color' | 'storage' | 'model' | 'ram' | 'other' }> = [];
      const seen = new Set<string>();

      document.querySelectorAll('a[href*="/p"]').forEach(a => {
        const href = a.getAttribute('href') || '';
        const pid = href.match(/\/p(\d+)\//)?.[1];
        if (!pid || pid === currentPid || seen.has(pid)) return;
        const text = a.textContent?.trim() || '';
        const title = a.getAttribute('title') || '';
        const img = a.querySelector('img');
        const cls = a.className || '';
        if (cls.includes('service-product') || cls.includes('footer') || cls.includes('tile-image')) return;
        const fullText = (text + ' ' + title).toLowerCase();
        const accessoryWords = ['чохол', 'скло', 'кабель', 'заряд', 'рюкзак', 'монітор', 'портативний', 'monitor', 'backpack', 'case', 'charger', 'cable', 'stand', 'dock', 'hub', 'mouse', 'keyboard', 'миша', 'клавіатур', 'підставка', 'holder', 'sleeve', 'sumka', 'сумка', 'тримач'];
        if (accessoryWords.some(w => fullText.includes(w))) return;

        const currentSlug = window.location.pathname.toLowerCase();
        const variantSlug = (a as HTMLAnchorElement).pathname.toLowerCase();
        const slugParts = currentSlug.split('/').filter((s: string) => s && !s.startsWith('p') && !s.startsWith('ua'));
        const varSlugParts = variantSlug.split('/').filter((s: string) => s && !s.startsWith('p') && !s.startsWith('ua'));
        if (slugParts.length > 0 && varSlugParts.length > 0) {
          const currentFamily = slugParts[0]?.split('-').slice(0, 3).join('-');
          const varFamily = varSlugParts[0]?.split('-').slice(0, 3).join('-');
          const isVariantType = text.match(/^\d+\s*(ГБ|GB|ТБ|TB)$/i) ||
                               text.match(/^(iPhone|Galaxy|MacBook|iPad|Pixel|Redmi|POCO)/i) ||
                               text.match(/^(Black|White|Blue|Red|Green|Gold|Silver|Purple|Pink|Orange|Titanium|Midnight|Starlight|Cosmic|Deep|Natural|Slate|Space|Graphite|Rose)/i);
          if (!isVariantType && currentFamily && varFamily && currentFamily !== varFamily) return;
        }
        seen.add(pid);
        const fullUrl = href.startsWith('http') ? href : 'https://rozetka.com.ua' + href;
        let type: 'color' | 'storage' | 'model' | 'ram' | 'other' = 'other';
        const hasRam = text.match(/(\d+)\s*(ГБ|GB)\s*(RAM|ОЗП)/i);
        const hasTb = text.match(/(\d+)\s*(ТБ|TB)/i);
        const hasGb = text.match(/(\d+)\s*(ГБ|GB)/i);
        if (hasRam) type = 'ram';
        else if (hasTb || hasGb) type = 'storage';
        else if (text.match(/^(iPhone|Galaxy|MacBook|iPad|Pixel|Redmi|POCO)/i)) type = 'model';
        else if (!text && img) type = 'color';
        else if (text.match(/^(Black|White|Blue|Red|Green|Gold|Silver|Purple|Pink|Orange|Titanium|Midnight|Starlight|Cosmic|Deep|Natural|Slate|Space|Graphite|Rose)/i)) type = 'color';
        else {
          const slug = href.toLowerCase();
          if (slug.match(/(black|white|blue|red|green|gold|silver|purple|pink|orange|titanium|midnight|starlight|cosmic|deep|natural|slate|space|graphite|rose)/)) type = 'color';
        }
        rawVariants.push({ pid, url: fullUrl, name: (text || title || pid).substring(0, 80), type });
      });

      const pageText = document.body.textContent || '';
      let storageSpec = '';
      const keywordMatch = pageText.match(/(SSD|HDD|NVMe)\s*(\d+)\s*(ГБ|GB|ТБ|TB)/i);
      if (keywordMatch) {
        storageSpec = `${keywordMatch[2]} ${keywordMatch[3]}`;
      } else {
        const titleEl = document.querySelector('h1');
        const titleText = titleEl?.textContent || '';
        const titleStorageMatch = titleText.match(/(\d+)\s*(ГБ|GB|ТБ|TB)/i);
        if (titleStorageMatch) {
          storageSpec = `${titleStorageMatch[1]} ${titleStorageMatch[2]}`;
        }
      }

      return { rawVariants, storageSpec };
    });

    const variants: ProductVariant[] = rawVariants;

    // Post-process: laptop mode — reclassify GB variants that don't match SSD
    const hasStorageKeyword = storageSpec && (await this.page.evaluate(() => {
      const text = document.body.textContent || '';
      return /(SSD|HDD|NVMe)\s*\d+\s*(ГБ|GB|ТБ|TB)/i.test(text);
    }));

    if (hasStorageKeyword) {
      for (const v of variants) {
        if (v.type !== 'storage') continue;
        const vSize = v.name.match(/(\d+)\s*(ГБ|GB|ТБ|TB)/i);
        if (!vSize) continue;
        const vValue = `${vSize[1]} ${vSize[2]}`;
        if (vValue !== storageSpec && !storageSpec.includes(vSize[1])) {
          v.type = 'ram';
        }
      }
    }

    // Fallback: TB + GB mix → GB is RAM
    const hasTb = variants.some(v => v.type === 'storage' && /\d+\s*(ТБ|TB)/i.test(v.name));
    const hasGb = variants.some(v => v.type === 'storage' && /\d+\s*(ГБ|GB)/i.test(v.name) && !/\d+\s*(ТБ|TB)/i.test(v.name));
    if (hasTb && hasGb) {
      for (const v of variants) {
        if (v.type === 'storage' && /\d+\s*(ГБ|GB)/i.test(v.name) && !/\d+\s*(ТБ|TB)/i.test(v.name)) {
          v.type = 'ram';
        }
      }
    }

    return variants;
  }

  // ── Description ─────────────────────────────────────────────

  /**
   * Extract product description from "Опис" tab or page content.
   */
  async extractDescription(): Promise<string> {
    // Try clicking the description tab first to load content
    try {
      const descTab = await this.page.$('[class*="tab"][class*="about"], button:has-text("Про товар"), button:has-text("Опис")');
      if (descTab) {
        await descTab.click();
        await this.randomDelay(500, 1000);
      }
    } catch {}

    return this.page.evaluate(() => {
      const selectors = [
        '[class*="product-about"] p',
        '[class*="about__brief"]',
        '[class*="description"] p',
        'article p',
        '[class*="about-section"] p',
        '[class*="tab-content"] p',
        '[class*="product-about"]',
      ];

      for (const sel of selectors) {
        const el = document.querySelector(sel);
        const text = el?.textContent?.trim();
        if (text && text.length > 30) return text;
      }

      // Fallback: meta description
      const meta = document.querySelector('meta[name="description"]');
      const metaContent = meta?.getAttribute('content') || '';
      if (metaContent.length > 30) return metaContent;

      return '';
    });
  }

  // ── Specifications ──────────────────────────────────────────

  /**
   * Extract ALL product specifications dynamically from the page.
   * Primary source: <dl> (definition lists) — Rozetka's standard format.
   * Fallbacks: tables, list items, JSON-LD.
   *
   * No hardcoded key filtering — every key-value pair is extracted.
   * Attribute type is inferred from the VALUE pattern.
   */
  async extractSpecifications(): Promise<ProductSpecification[]> {
    // Try clicking the specs tab first
    try {
      const specsTab = await this.page.$('button:has-text("Характеристики"), [class*="tab"]:has-text("Характеристики")');
      if (specsTab) {
        await specsTab.click();
        await this.randomDelay(500, 1000);
      }
    } catch {}

    return this.page.evaluate(() => {
      const specs: Array<{ key: string; value: string; group?: string }> = [];
      const seen = new Set<string>();

      // ── Method 1: <dl> definition lists (Rozetka standard) ──
      document.querySelectorAll('dl').forEach(dl => {
        const dts = dl.querySelectorAll('dt');
        const dds = dl.querySelectorAll('dd');
        for (let i = 0; i < Math.min(dts.length, dds.length); i++) {
          const key = dts[i].textContent?.trim() || '';
          const value = dds[i].textContent?.trim() || '';
          if (key && value) {
            // Deduplicate by key+value (same spec can appear in multiple sections)
            const dedupKey = `${key}::${value.substring(0, 50)}`;
            if (!seen.has(dedupKey)) {
              seen.add(dedupKey);
              specs.push({ key, value });
            }
          }
        }
      });

      if (specs.length > 0) return specs;

      // ── Method 2: Table rows ──
      document.querySelectorAll('table tr').forEach(row => {
        const cells = row.querySelectorAll('td');
        if (cells.length >= 2) {
          const key = cells[0].textContent?.trim() || '';
          const value = cells[1].textContent?.trim() || '';
          if (key && value && !seen.has(key)) {
            seen.add(key);
            specs.push({ key, value });
          }
        }
      });

      if (specs.length > 0) return specs;

      // ── Method 3: JSON-LD additionalProperty ──
      document.querySelectorAll('script[type="application/ld+json"]').forEach(script => {
        try {
          const data = JSON.parse(script.textContent || '{}');
          if (data['@type'] === 'Product' && data.additionalProperty) {
            for (const prop of data.additionalProperty) {
              if (prop.name && prop.value && !seen.has(prop.name)) {
                seen.add(prop.name);
                specs.push({ key: prop.name, value: String(prop.value) });
              }
            }
          }
        } catch {}
      });

      return specs;
    });
  }

  // ── Availability ────────────────────────────────────────────

  /**
   * Extract product availability status.
   */
  async extractAvailability(): Promise<string> {
    return this.page.evaluate(() => {
      const bodyText = document.body.textContent?.toLowerCase() || '';

      // Check for explicit status indicators
      if (document.querySelector('[class*="status--available"], [class*="in-stock"], [class*="available"]')) {
        return 'available';
      }
      if (document.querySelector('[class*="status--unavailable"], [class*="out-of-stock"], [class*="unavailable"]')) {
        return 'out_of_stock';
      }
      if (document.querySelector('[class*="preorder"], [class*="pre-order"]')) {
        return 'preorder';
      }

      // Text-based detection
      if (bodyText.includes('є в наявності') || bodyText.includes('в наявності') || bodyText.includes('in stock')) {
        return 'available';
      }
      if (bodyText.includes('немає в наявності') || bodyText.includes('out of stock') || bodyText.includes('закінчився')) {
        return 'out_of_stock';
      }
      if (bodyText.includes('передзамовлення') || bodyText.includes('preorder')) {
        return 'preorder';
      }

      // JSON-LD availability
      const scripts = document.querySelectorAll('script[type="application/ld+json"]');
      for (const script of scripts) {
        try {
          const data = JSON.parse(script.textContent || '{}');
          if (data['@type'] === 'Product' && data.offers?.availability) {
            const avail = data.offers.availability.toLowerCase();
            if (avail.includes('instock')) return 'available';
            if (avail.includes('outofstock')) return 'out_of_stock';
            if (avail.includes('preorder')) return 'preorder';
          }
        } catch {}
      }

      return '';
    });
  }

  // ── Warranty ────────────────────────────────────────────────

  async extractWarranty(): Promise<string> {
    return this.page.evaluate(() => {
      const text = document.body.textContent || '';
      const match = text.match(/гарантія[:\s]*(\d+\s*(?:місяц(?:ів|я)|рік|роки))/i);
      if (match) return match[1];
      const matchEn = text.match(/warranty[:\s]*(\d+\s*(?:month|year)s?)/i);
      if (matchEn) return matchEn[1];
      return '';
    });
  }

  // ── Seller ──────────────────────────────────────────────────

  async extractSeller(): Promise<string> {
    return this.page.evaluate(() => {
      const sellerEl = document.querySelector(
        '[class*="seller"] a, [class*="seller-name"], [class*="merchant"] a'
      );
      return sellerEl?.textContent?.trim() || '';
    });
  }

  // ── Inline JSON-LD ──────────────────────────────────────────

  /**
   * Extract structured data from JSON-LD for fallback values.
   */
  private async extractInlineJsonLd(): Promise<{
    name?: string;
    description?: string;
    brand?: string;
    price?: number;
  } | null> {
    return this.page.evaluate(() => {
      const scripts = document.querySelectorAll('script[type="application/ld+json"]');
      for (const script of scripts) {
        try {
          const data = JSON.parse(script.textContent || '{}');
          if (data['@type'] === 'Product') {
            return {
              name: data.name,
              description: data.description,
              brand: typeof data.brand === 'string' ? data.brand : data.brand?.name,
              price: data.offers?.price ? parseFloat(data.offers.price) : undefined,
            };
          }
        } catch {}
      }
      return null;
    });
  }

  // ── Category Path Builder ───────────────────────────────────

  /**
   * Build category path from breadcrumbs, excluding product name and store name.
   * Input:  ["Rozetka", "Комп'ютери", "Ноутбуки", "ASUS", "Product Name"]
   * Output: "Комп'ютери > Ноутбуки > ASUS"
   */
  private buildCategoryPath(breadcrumbs: Breadcrumb[]): string {
    const skipNames = new Set([
      'інтернет-магазин rozetka',
      'rozetka',
      'rozetka.com.ua',
    ]);

    const segments = breadcrumbs
      .map(b => b.name)
      .filter(name => {
        if (!name || name.length < 2) return false;
        if (skipNames.has(name.toLowerCase())) return false;
        return true;
      });

    // Remove last segment if it looks like a product name (long text, contains specs)
    if (segments.length > 2) {
      const last = segments[segments.length - 1];
      // Product names are typically long and contain technical specs
      if (last.length > 60 || last.includes('/') || last.match(/\d+.*ГБ|GB|RAM/i)) {
        segments.pop();
      }
    }

    return segments.join(' > ');
  }

  // ── Utilities ───────────────────────────────────────────────

  private randomDelay(min = 1000, max = 2500): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
