import { type Browser } from "playwright";
import * as fs from "fs/promises";
import * as path from "path";
import { program } from "commander";
import {
  RozetkaCategoryPage,
  type ProductTile,
} from "../pages/rozetka-category.page";
import { RozetkaProductPage, type ProductSpecification } from "../pages/rozetka-product.page";
import { ImageDownloader } from "../utils/image-downloader";
import {
  generateSku,
  slugify,
  mapFilterNameToKey,
  inferAttributeType,
  normalizeColor,
  normalizeNumber,
  normalizeList,
  type CategoryConfig,
} from "../utils/rozetka-transformer";
import { createScraperContext, launchScraper } from "../fixtures/scraper.fixture";
import {
  classifyAttributes,
  type VariantDetail,
} from "../utils/attribute-classifier";

// ── Category Configs ───────────────────────────────────────────

const CATEGORIES: Record<
  string,
  { name: string; url: string } & CategoryConfig
> = {
  laptops: {
    name: "Laptops",
    url: "https://rozetka.com.ua/ua/notebooks/c80004/",
    storeName: "Tech Store",
    categoryName: "Electronics",
    tags: ["laptop", "notebook", "computer"],
  },
  phones: {
    name: "Smartphones",
    url: "https://rozetka.com.ua/ua/mobile-phones/c80003/",
    storeName: "Tech Store",
    categoryName: "Electronics",
    tags: ["smartphone", "phone", "mobile"],
  },
  tablets: {
    name: "Tablets",
    url: "https://rozetka.com.ua/ua/tablets/c130309/",
    storeName: "Tech Store",
    categoryName: "Electronics",
    tags: ["tablet", "ipad"],
  },
  headphones: {
    name: "Headphones",
    url: "https://rozetka.com.ua/ua/headphones/c80027/",
    storeName: "Tech Store",
    categoryName: "Electronics",
    tags: ["headphones", "audio", "wireless"],
  },
};

// ── Constants ──────────────────────────────────────────────────

const PROJECT_ROOT = path.resolve(import.meta.dirname, "../../../..");
const DATA_DIR = path.join(PROJECT_ROOT, "src/Tools/Seeder.App/Data");
const IMAGES_DIR = path.join(DATA_DIR, "Images");
const PRODUCTS_JSON = path.join(DATA_DIR, "products-v2.json");

// ── Logging ────────────────────────────────────────────────────

function log(msg: string, lvl: "info" | "warn" | "error" = "info") {
  const icon = lvl === "error" ? "❌" : lvl === "warn" ? "⚠️" : "ℹ️";
  console.log(`${icon} [${new Date().toISOString()}] ${msg}`);
}

function delay(min = 2000, max = 4000) {
  return new Promise((r) =>
    setTimeout(r, Math.floor(Math.random() * (max - min + 1)) + min),
  );
}

function buildImageFolderName(name: string, skuCode: string): string {
  // Clean generic prefix
  let clean = name.replace(/^(Мобільний телефон|Ноутбук|Планшет|Навушники|Камера|Колонка)\s+/i, '');
  // Remove parentheses (like model codes)
  clean = clean.replace(/\([^)]*\)/g, '');
  
  // Clean up extra spaces
  clean = clean.trim();

  // Slugify the cleaned product/variant name
  const nameSlug = slugify(clean);
  
  // Normalize the SKU to be safe for filenames
  const safeSku = skuCode.replace(/[^a-zA-Z0-9-]/g, '-').toLowerCase();

  // Combine them
  const combined = `${nameSlug}-${safeSku}`;
  
  return combined.substring(0, 95);
}

// ── Scraper Class ──────────────────────────────────────────────

class RozetkaScraper {
  private browser: Browser | null = null;
  private imgDownloader: ImageDownloader;
  private existing: any[] = [];
  private existingSkus = new Set<string>();

  constructor() {
    this.imgDownloader = new ImageDownloader(IMAGES_DIR);
  }

  async init() {
    log("Initializing...");
    await fs.mkdir(DATA_DIR, { recursive: true });
    await fs.mkdir(IMAGES_DIR, { recursive: true });
    try {
      this.existing = JSON.parse(await fs.readFile(PRODUCTS_JSON, "utf-8"));
      this.existingSkus = new Set(
        this.existing.flatMap((p) => p.variants?.map((v: any) => v.sku) || []),
      );
      log(`Loaded ${this.existing.length} existing products`);
    } catch {
      this.existing = [];
      this.existingSkus = new Set();
    }
    const scraper = await launchScraper();
    this.browser = scraper.browser;
    log("Browser ready");
  }

  private async newCtx() {
    return createScraperContext(this.browser!);
  }

  // ── Phase 1: Collect product URLs from category listing ──────

  private async collectUrls(
    limit: number,
    catUrl: string,
    categoryKey: string,
  ): Promise<{ tiles: ProductTile[]; categoryFilters: string[] }> {
    log("Phase 1: Collecting URLs...");
    const ctx = await this.newCtx();
    const page = await ctx.newPage();
    const catPage = new RozetkaCategoryPage(page);
    try {
      await catPage.goto(catUrl, categoryKey);
      await delay(1500, 2500);

      const categoryFilters = await catPage.extractSidebarFilters();
      log(`  Extracted ${categoryFilters.length} category filters.`);

      const tiles: ProductTile[] = [];
      let pg = 1;
      while (tiles.length < limit && pg <= 5) {
        log(`  Page ${pg}...`);
        const all = await catPage.extractProductTiles();
        const fresh = all.filter(
          (t) =>
            !this.existingSkus.has(generateSku(t.articleId.replace("p", ""))),
        );
        tiles.push(...fresh.slice(0, limit - tiles.length));
        log(`  ${all.length} tiles, ${tiles.length} new`);
        if (tiles.length >= limit || !(await catPage.nextPage())) break;
        pg++;
      }
      return { tiles: tiles.slice(0, limit), categoryFilters };
    } finally {
      await page.close();
      await ctx.close();
    }
  }



  // ── Phase 2: Scrape full product ─────────────────────────────

  private async scrapeProduct(
    tile: ProductTile,
    cat: { name: string; url: string } & CategoryConfig,
    categoryFilters: string[],
  ): Promise<any | null> {
    const code = tile.articleId.replace("p", "");
    if (this.existingSkus.has(generateSku(code))) return null;
    log(`  ${tile.title.substring(0, 50)}...`);

    const ctx = await this.newCtx();
    const page = await ctx.newPage();
    const pom = new RozetkaProductPage(page);
    try {
      await pom.goto(tile.url);
      const details = await pom.extractDetails();
      const finalCode = details.sku || code;
      if (this.existingSkus.has(generateSku(finalCode))) return null;

      // Log rich data we extracted
      log(`  📝 ${details.name.substring(0, 60)}`);

      const imgs =
        details.images.length > 0
          ? details.images
          : tile.imgSrc
            ? [tile.imgSrc]
            : [];
      const slug = buildImageFolderName(details.name, finalCode);
      const local = await this.imgDownloader.downloadMultiple(imgs, slug, 10);

      const scrapedVariants: VariantDetail[] = [];
      const visitedPids = new Set<string>();
      visitedPids.add(finalCode);

      const queue: Array<{ url: string; name: string }> = [];
      for (const v of details.variants) {
        queue.push({ url: v.url, name: v.name });
      }

      // Include the base product itself as one of the variants
      scrapedVariants.push({
        pid: finalCode,
        skuCode: generateSku(finalCode),
        name: details.name,
        price: details.price,
        images: local,
        specifications: details.specifications,
      });

      if (queue.length > 0) {
        log(`  🔀 Discovering complete variant matrix recursively...`);
        let scrapeLimit = 15; // safety limit to prevent infinite loops or getting blocked
        
        while (queue.length > 0 && scrapeLimit > 0) {
          const current = queue.shift()!;
          const currentPidMatch = current.url.match(/\/p(\d+)\//);
          const currentPid = currentPidMatch ? currentPidMatch[1] : '';

          if (!currentPid || visitedPids.has(currentPid)) continue;
          visitedPids.add(currentPid);
          scrapeLimit--;

          log(`    🔄 Scraping variant page: ${current.name} (${current.url})`);
          
          const ctx = await this.newCtx();
          const page = await ctx.newPage();
          const pom = new RozetkaProductPage(page);
          
          try {
            await pom.goto(current.url);
            const [price, gallery, specs, name, sku, subVariants] = await Promise.all([
              pom.extractPrice(),
              pom.extractGallery(),
              pom.extractSpecifications(),
              pom.extractName(),
              pom.extractSku(),
              pom.extractVariants(),
            ]);

            const variantSlug = buildImageFolderName(name, sku || currentPid);
            const imgs = gallery.images.length > 0 ? gallery.images : [];
            const localImgs = imgs.length > 0
              ? await this.imgDownloader.downloadMultiple(imgs, variantSlug, 10)
              : [];

            scrapedVariants.push({
              pid: sku || currentPid,
              skuCode: generateSku(sku || currentPid),
              name,
              price: price.value,
              images: localImgs,
              specifications: specs,
            });

            if (price.value > 0) {
              log(`      💰 ${name}: ${price.value}₴ (${localImgs.length} images)`);
            }

            // Discover and add any sub-variants linked from this variant page
            for (const sv of subVariants) {
              const svPidMatch = sv.url.match(/\/p(\d+)\//);
              const svPid = svPidMatch ? svPidMatch[1] : '';
              if (svPid && !visitedPids.has(svPid) && !queue.some((q) => q.url.includes(`/p${svPid}/`))) {
                queue.push({ url: sv.url, name: sv.name });
              }
            }
          } catch (e) {
            log(`      Failed to scrape variant details: ${e}`, "warn");
          } finally {
            await page.close();
            await ctx.close();
          }

          await delay(2000, 4000);
        }
      }

      // Map all raw specifications of each variant using dynamic categoryFilters.
      // This translates raw Ukrainian specification keys to matched category filter keys
      // and normalizes their values (e.g. brand, storage, ram, color).
      const filterKeys = new Set(categoryFilters.map((f) => mapFilterNameToKey(f)));
      
      const mappedVariants = scrapedVariants.map((v) => {
        const mappedSpecs: ProductSpecification[] = [];
        let hasBrand = false;

        for (const spec of v.specifications) {
          const specKey = mapFilterNameToKey(spec.key);
          
          // Match against dynamic category filters or standard/required fields
          if (filterKeys.has(specKey) || ["brand", "color", "storage", "ram"].includes(specKey)) {
            let val = spec.value.trim();
            const type = inferAttributeType(spec.key, spec.value);
            
            // Normalize values based on inferred type
            if (type === "color") {
              val = normalizeColor(val);
            } else if (type === "number") {
              val = normalizeNumber(val);
            } else if (type === "list") {
              val = normalizeList(val);
            }

            // Avoid duplicate keys in specifications
            if (!mappedSpecs.some((s) => s.key === specKey)) {
              mappedSpecs.push({
                key: specKey,
                value: val,
              });
            }

            if (specKey === "brand") {
              hasBrand = true;
            }
          }
        }

        // Ensure brand is present if available in product details or fallback tile brand
        if (!hasBrand && (filterKeys.has("brand") || ["brand"].includes("brand"))) {
          const brandVal = details.brand || tile.brand;
          if (brandVal) {
            mappedSpecs.push({
              key: "brand",
              value: brandVal.trim(),
            });
          }
        }

        return {
          ...v,
          specifications: mappedSpecs,
        };
      });

      // Classify attributes using the mapped specifications
      const { commonAttributes, variantAttributes } =
        classifyAttributes(mappedVariants);

      // Build expected Output JSON Schema
      const productOutput = {
        productName: details.name || tile.title,
        categoryName: cat.name,
        categoryFilters,
        commonAttributes,
        variants: mappedVariants.map((v) => ({
          sku: v.skuCode,
          price: v.price,
          attributes: variantAttributes[v.pid] || {},
          galleryUrls: v.images,
        })),
      };

      return productOutput;
    } finally {
      await page.close();
      await ctx.close();
    }
  }

  // ── Main scrape entry ────────────────────────────────────────

  async scrape(category: string, limit: number) {
    const cat = CATEGORIES[category];
    if (!cat) throw new Error(`Unknown category: ${category}`);
    log(`Scraping ${cat.name} (limit: ${limit})`);

    const { tiles: urls, categoryFilters } = await this.collectUrls(
      limit,
      cat.url,
      category,
    );
    log(`Phase 2: Scraping ${urls.length} products...`);

    const added: any[] = [];
    for (let i = 0; i < urls.length; i++) {
      log(`\n[${i + 1}/${urls.length}] ${urls[i].title.substring(0, 50)}`);
      const p = await this.scrapeProduct(urls[i], cat, categoryFilters);
      if (p) {
        added.push(p);
        this.existing.push(p);
        p.variants.forEach((v: any) => this.existingSkus.add(v.sku));
      }
      await delay(3000, 6000);
    }

    if (added.length) {
      await fs.writeFile(PRODUCTS_JSON, JSON.stringify(this.existing, null, 2));
      log(
        `\n✅ ${added.length} new products added (total: ${this.existing.length})`,
      );
    } else {
      log("No new products found");
    }
    log("Done!");
  }

  async cleanup() {
    await this.browser?.close();
    log("Browser closed");
  }
}

// ── CLI ────────────────────────────────────────────────────────

program
  .name("rozetka-scraper")
  .version("2.1")
  .option(
    "-c, --category <c>",
    "Category: laptops|phones|tablets|headphones",
    "laptops",
  )
  .option("-l, --limit <n>", "Max products to scrape", "10")
  .action(async (opts) => {
    const s = new RozetkaScraper();
    try {
      await s.init();
      await s.scrape(opts.category, parseInt(opts.limit));
    } catch (e) {
      log(`Fatal: ${e}`, "error");
      process.exit(1);
    } finally {
      await s.cleanup();
    }
  })
  .parse();
