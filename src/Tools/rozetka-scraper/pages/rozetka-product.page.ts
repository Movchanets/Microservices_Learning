/**
 * Rozetka Product Detail Page Object
 * 
 * Extracts full product details from Rozetka product pages:
 * - SKU/Article number (Код товару)
 * - Full image gallery (big/ URLs, not medium/)
 * - Breadcrumbs (from rz-breadcrumbs or JSON-LD)
 * - Description
 */

import { Page } from 'playwright';

export interface Breadcrumb {
  name: string;
  url?: string;
  position: number;
}

export interface ProductVariant {
  pid: string;
  url: string;
  name: string;
  type: 'color' | 'storage' | 'model' | 'other';
}

export interface ProductDetails {
  sku: string;              // Rozetka SKU code (e.g., "528975609")
  description: string;
  images: string[];          // Full-size image URLs (/goods/images/big/)
  thumbnails: string[];      // Thumbnail URLs (/goods/images/medium/)
  breadcrumbs: Breadcrumb[]; // Category hierarchy
  categoryPath: string;
  variants: ProductVariant[];
}

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
   * Extract all product details from the page
   */
  async extractDetails(): Promise<ProductDetails> {
    const [sku, gallery, breadcrumbs, variants] = await Promise.all([
      this.extractSku(),
      this.extractGallery(),
      this.extractBreadcrumbs(),
      this.extractVariants(),
    ]);

    const description = await this.extractDescription();
    const categoryPath = breadcrumbs.map(b => b.name).filter(Boolean).join(' > ');

    return {
      sku,
      description,
      images: gallery.images,
      thumbnails: gallery.thumbnails,
      breadcrumbs,
      categoryPath,
      variants,
    };
  }

  /**
   * Extract SKU/article number
   * Pattern: <span class="ms-auto color-black-60">Код:  528975609</span>
   */
  async extractSku(): Promise<string> {
    // Method 1: Look for "Код:" text in spans
    const fromDom = await this.page.evaluate(() => {
      const spans = document.querySelectorAll('span, div, p');
      for (const el of spans) {
        const t = el.textContent?.trim() || '';
        const match = t.match(/^Код:\s*(\d+)$/);
        if (match) return match[1];
      }
      return '';
    });

    if (fromDom) return fromDom;

    // Method 2: Extract from URL (fallback)
    const url = this.page.url();
    const urlMatch = url.match(/\/p(\d+)\//);
    return urlMatch ? urlMatch[1] : '';
  }

  /**
   * Extract full image gallery
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

  /**
   * Extract breadcrumbs from rz-breadcrumbs element or JSON-LD
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

  /**
   * Extract product variants (color, storage, model)
   * Variants are links to /p{id}/ with different product IDs
   */
  async extractVariants(): Promise<ProductVariant[]> {
    return this.page.evaluate(() => {
      const currentPid = window.location.href.match(/\/p(\d+)\//)?.[1] || '';
      const variants: Array<{ pid: string; url: string; name: string; type: 'color' | 'storage' | 'model' | 'other' }> = [];
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
        // Filter out accessories, bags, monitors, cases, cables, chargers
        const accessoryWords = ['чохол', 'скло', 'кабель', 'заряд', 'рюкзак', 'монітор', 'портативний', 'monitor', 'backpack', 'case', 'charger', 'cable', 'stand', 'dock', 'hub', 'mouse', 'keyboard', 'миша', 'клавіатур', 'підставка', 'holder', 'sleeve', 'sumka', 'сумка', 'тримач'];
        if (accessoryWords.some(w => fullText.includes(w))) return;
        
        // Only accept variants that share a similar URL slug pattern
        // True variants have similar product names in the URL
        const currentSlug = window.location.pathname.toLowerCase();
        const variantSlug = href.toLowerCase();
        // Extract product family from URL (e.g., 'iphone-17-pro-max' from both variants)
        const slugParts = currentSlug.split('/').filter(s => s && !s.startsWith('p') && !s.startsWith('ua'));
        const varSlugParts = variantSlug.split('/').filter(s => s && !s.startsWith('p') && !s.startsWith('ua'));
        // If slugs are completely different, it's likely not a variant
        if (slugParts.length > 0 && varSlugParts.length > 0) {
          const currentFamily = slugParts[0]?.split('-').slice(0, 3).join('-');
          const varFamily = varSlugParts[0]?.split('-').slice(0, 3).join('-');
          // Allow if family matches, or if it's a color/storage variant
          const isVariantType = text.match(/^\d+\s*(ГБ|GB|ТБ|TB)$/i) || 
                               text.match(/^(iPhone|Galaxy|MacBook|iPad|Pixel|Redmi|POCO)/i) ||
                               text.match(/^(Black|White|Blue|Red|Green|Gold|Silver|Purple|Pink|Orange|Titanium|Midnight|Starlight|Cosmic|Deep|Natural|Slate|Space|Graphite|Rose)/i);
          if (!isVariantType && currentFamily && varFamily && currentFamily !== varFamily) return;
        }
        seen.add(pid);
        const fullUrl = href.startsWith('http') ? href : 'https://rozetka.com.ua' + href;
        let type: 'color' | 'storage' | 'model' | 'other' = 'other';
        if (text.match(/^\d+\s*(ГБ|GB|ТБ|TB)$/i)) type = 'storage';
        else if (text.match(/^(iPhone|Galaxy|MacBook|iPad|Pixel|Redmi|POCO)/i)) type = 'model';
        else if (!text && img) type = 'color';
        else if (text.match(/^(Black|White|Blue|Red|Green|Gold|Silver|Purple|Pink|Orange|Titanium|Midnight|Starlight|Cosmic|Deep|Natural|Slate|Space|Graphite|Rose)/i)) type = 'color';
        else {
          const slug = href.toLowerCase();
          if (slug.match(/(black|white|blue|red|green|gold|silver|purple|pink|orange|titanium|midnight|starlight|cosmic|deep|natural|slate|space|graphite|rose)/)) type = 'color';
        }
        variants.push({ pid, url: fullUrl, name: (text || title || pid).substring(0, 80), type });
      });

      return variants;
    });
  }

  /**
   * Extract product description
   */
  async extractDescription(): Promise<string> {
    return this.page.evaluate(() => {
      const selectors = [
        '[class*="product-about"] p',
        '[class*="about__brief"]',
        '[class*="description"] p',
        'article p',
      ];

      for (const sel of selectors) {
        const el = document.querySelector(sel);
        const text = el?.textContent?.trim();
        if (text && text.length > 20) return text;
      }

      return '';
    });
  }

  private randomDelay(min = 1000, max = 2500): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
