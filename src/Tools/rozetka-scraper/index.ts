/**
 * Rozetka Scraper - Public API
 */

// Page Objects
export { RozetkaCategoryPage, type ProductTile } from './pages/rozetka-category.page';
export { RozetkaProductPage, type ProductDetails, type Breadcrumb, type ProductVariant, type ProductSpecification } from './pages/rozetka-product.page';
export { RozetkaCategoriesPage, type CategoryNode } from './pages/rozetka-categories.page';

// Utilities
export { ImageDownloader } from './utils/image-downloader';
export {
  generateSku, slugify, parsePrice,
  normalizeColor, inferAttributeType, buildTypedAttributes, buildVariantAxes,
  extractVariantAttributes, mapFilterNameToKey, slugifyTransliterated,
  normalizeNumber, normalizeList,
  type CategoryConfig, type TypedAttribute, type AttributeType,
} from './utils/rozetka-transformer';

// Fixtures
export { createScraperContext, launchScraper, type ScraperConfig } from './fixtures/scraper.fixture';
