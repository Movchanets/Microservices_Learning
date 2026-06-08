import { type Browser } from "playwright";
import * as fs from "fs/promises";
import * as path from "path";
import * as crypto from "crypto";
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

// ── Target Data Contracts ──────────────────────────────────────

export interface Category {
  id: string;
  parentId: string | null;
  name: string;
  url: string;
}

export interface AttributeDefinition {
  categoryId: string;
  name: string;
  possibleValues: string[];
}

export interface BaseProduct {
  externalId: string;
  categoryId: string;
  title: string;
  description: string;
  brand: string;
}

export interface ProductVariant {
  productExternalId: string;
  sku: string;
  price: number;
  currency: string;
  inStock: boolean;
  images: string[];
  attributes: Record<string, string>;
}

export interface CatalogData {
  categories: Category[];
  attributeDefinitions: AttributeDefinition[];
  baseProducts: BaseProduct[];
  productVariants: ProductVariant[];
}

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
  books: {
    name: "Books",
    url: "https://rozetka.com.ua/ua/hudojestvennaya-literatura/c4326593/",
    storeName: "Book Store",
    categoryName: "Books",
    tags: ["books", "fiction", "literature"],
  },
};

// ── Constants ──────────────────────────────────────────────────

const PROJECT_ROOT = path.resolve(import.meta.dirname, "../../../..");
const DATA_DIR = path.join(PROJECT_ROOT, "src/Tools/Seeder.App/Data");
const IMAGES_DIR = path.join(DATA_DIR, "Images");
const CATALOG_JSON = path.join(DATA_DIR, "catalog.json");

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
  
  private categories = new Map<string, Category>();
  private attributeDefinitions = new Map<string, AttributeDefinition>();
  private baseProducts = new Map<string, BaseProduct>();
  private productVariants = new Map<string, ProductVariant>();
  
  private existingSkus = new Set<string>();

  constructor() {
    this.imgDownloader = new ImageDownloader(IMAGES_DIR);
  }

  async init() {
    log("Initializing...");
    await fs.mkdir(DATA_DIR, { recursive: true });
    await fs.mkdir(IMAGES_DIR, { recursive: true });
    try {
      const data: CatalogData = JSON.parse(await fs.readFile(CATALOG_JSON, "utf-8"));
      
      data.categories.forEach(c => this.categories.set(c.id, c));
      data.attributeDefinitions.forEach(a => this.attributeDefinitions.set(`${a.categoryId}_${a.name}`, a));
      data.baseProducts.forEach(p => this.baseProducts.set(p.externalId, p));
      data.productVariants.forEach(v => {
        this.productVariants.set(v.sku, v);
        this.existingSkus.add(v.sku);
      });
      
      log(`Loaded ${this.baseProducts.size} existing products and ${this.productVariants.size} variants`);
    } catch {
      // File doesn't exist or is invalid
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
  ): Promise<{ tiles: ProductTile[]; categoryFilters: string[]; dynamicCategoryName: string }> {
    log("Phase 1: Collecting URLs...");
    const ctx = await this.newCtx();
    const page = await ctx.newPage();
    const catPage = new RozetkaCategoryPage(page);
    try {
      await catPage.goto(catUrl, categoryKey);
      await delay(1500, 2500);

      const categoryFilters = await catPage.extractSidebarFilters();
      log(`  Extracted ${categoryFilters.length} category filters.`);

      const dynamicCategoryName = await catPage.extractCategoryName();

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
      return { tiles: tiles.slice(0, limit), categoryFilters, dynamicCategoryName };
    } finally {
      await page.close();
      await ctx.close();
    }
  }



  // ── Phase 2: Scrape full product ─────────────────────────────

  private async scrapeProduct(
    tile: ProductTile,
    cat: { name: string; url: string } & CategoryConfig,
    categoryId: string,
    categoryFilters: string[],
  ): Promise<boolean> {
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
      if (this.existingSkus.has(generateSku(finalCode))) return false;

      // Log rich data we extracted
      log(`  📝 ${details.name.substring(0, 60)}`);

      // ── Create BaseProduct ──────────────────────────────────
      const baseProductId = crypto.createHash("sha256").update(details.name + cat.url).digest("hex");
      
      let baseBrand = details.brand || tile.brand || "Unknown";
      
      const baseProduct: BaseProduct = {
        externalId: baseProductId,
        categoryId: categoryId,
        title: details.name || tile.title,
        description: details.description || "",
        brand: baseBrand.trim()
      };
      this.baseProducts.set(baseProductId, baseProduct);

      const imgs =
        details.images.length > 0
          ? details.images
          : tile.imgSrc
            ? [tile.imgSrc]
            : [];
      const slug = buildImageFolderName(details.name, finalCode);
      const local = await this.imgDownloader.downloadMultiple(imgs, slug, 10);

      const scrapedVariants: Array<{
        pid: string;
        skuCode: string;
        name: string;
        price: number;
        images: string[];
        specifications: ProductSpecification[];
      }> = [];
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
      const filterKeys = new Set(categoryFilters.map((f) => mapFilterNameToKey(f)));
      
      scrapedVariants.forEach((v) => {
        const attributesMap: Record<string, string> = {};
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

            if (!attributesMap[specKey]) {
              attributesMap[specKey] = val;
            }

            if (specKey === "brand") {
              hasBrand = true;
            }
          }
        }

        // Ensure brand is present
        if (!hasBrand && (filterKeys.has("brand") || ["brand"].includes("brand"))) {
          attributesMap["brand"] = baseBrand.trim();
        }

        // ── Create ProductVariant and AttributeDefinitions ────────────────
        const variant: ProductVariant = {
          productExternalId: baseProductId,
          sku: v.skuCode,
          price: v.price,
          currency: "UAH",
          inStock: v.price > 0,
          images: v.images,
          attributes: attributesMap
        };

        this.productVariants.set(variant.sku, variant);
        this.existingSkus.add(variant.sku);

        // Register all attributes as possible options
        for (const [attrName, attrVal] of Object.entries(attributesMap)) {
          const key = `${categoryId}_${attrName}`;
          let def = this.attributeDefinitions.get(key);
          if (!def) {
            def = { categoryId: categoryId, name: attrName, possibleValues: [] };
            this.attributeDefinitions.set(key, def);
          }
          if (!def.possibleValues.includes(attrVal)) {
            def.possibleValues.push(attrVal);
          }
        }
      });

      return true;
    } finally {
      await page.close();
      await ctx.close();
    }
  }

  // ── Main scrape entry ────────────────────────────────────────

  async scrape(opts: { category?: string; url?: string; limit: number }) {
    let targetUrl = opts.url || "";
    let catConfig = {
      name: "Custom Category",
      url: targetUrl,
      storeName: "Tech Store",
      categoryName: "Electronics",
      tags: ["electronics"],
    };

    if (opts.category) {
      const predefined = CATEGORIES[opts.category];
      if (predefined) {
        catConfig = { ...predefined };
        if (!targetUrl) targetUrl = predefined.url;
      } else if (!targetUrl) {
        throw new Error(`Unknown category: ${opts.category} and no URL provided.`);
      }
    } else if (!targetUrl) {
      // Default to laptops if nothing is specified
      const predefined = CATEGORIES["laptops"];
      catConfig = { ...predefined };
      targetUrl = predefined.url;
    }

    log(`Scraping category URL: ${targetUrl}`);

    const { tiles: urls, categoryFilters, dynamicCategoryName } = await this.collectUrls(
      opts.limit,
      targetUrl,
      opts.category || "",
    );

    let finalCategoryName = catConfig.name;
    if (dynamicCategoryName && (!opts.category || !CATEGORIES[opts.category])) {
      finalCategoryName = dynamicCategoryName;
      catConfig.tags = [slugify(dynamicCategoryName).replace(/-/g, "")];
    }

    const categoryId = slugify(finalCategoryName);
    if (!this.categories.has(categoryId)) {
      this.categories.set(categoryId, {
        id: categoryId,
        parentId: null,
        name: finalCategoryName,
        url: targetUrl
      });
    }

    log(`Phase 2: Scraping ${urls.length} products under category: ${finalCategoryName}...`);

    let addedCount = 0;
    for (let i = 0; i < urls.length; i++) {
      log(`\n[${i + 1}/${urls.length}] ${urls[i].title.substring(0, 50)}`);
      const success = await this.scrapeProduct(urls[i], catConfig, categoryId, categoryFilters);
      if (success) {
        addedCount++;
      }
      await delay(3000, 6000);
    }

    if (addedCount > 0) {
      const outputData: CatalogData = {
        categories: Array.from(this.categories.values()),
        attributeDefinitions: Array.from(this.attributeDefinitions.values()),
        baseProducts: Array.from(this.baseProducts.values()),
        productVariants: Array.from(this.productVariants.values())
      };

      await fs.writeFile(CATALOG_JSON, JSON.stringify(outputData, null, 2));
      log(`\n✅ ${addedCount} new products added!`);
      log(`Total DB stats: ${outputData.categories.length} categories, ${outputData.attributeDefinitions.length} attributes, ${outputData.baseProducts.length} products, ${outputData.productVariants.length} variants`);
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
  .version("2.2")
  .option(
    "-c, --category <c>",
    "Category key (optional): laptops|phones|tablets|headphones",
    "",
  )
  .option("-u, --url <url>", "Category URL to scrape directly", "")
  .option("-l, --limit <n>", "Max products to scrape", "10")
  .action(async (opts) => {
    const s = new RozetkaScraper();
    try {
      await s.init();
      await s.scrape({
        category: opts.category,
        url: opts.url,
        limit: parseInt(opts.limit),
      });
    } catch (e) {
      log(`Fatal: ${e}`, "error");
      process.exit(1);
    } finally {
      await s.cleanup();
    }
  })
  .parse();
