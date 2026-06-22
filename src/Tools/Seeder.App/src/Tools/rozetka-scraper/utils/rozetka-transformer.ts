/**
 * Rozetka Data Transformer
 * 
 * Converts scraped Rozetka data into Seeder.App format.
 */

import type { ProductTile } from '../pages/rozetka-category.page';
import type { ProductDetails, Breadcrumb, ProductVariant } from '../pages/rozetka-product.page';

// ============================================================================
// Types
// ============================================================================

export interface SeederProduct {
  StoreName: string;
  CategoryName: string;
  Name: string;
  Description: string;
  Price: number;
  Currency: string;
  Sku: string;           // Rozetka SKU code (e.g., "ROZ-528975609")
  RozetkaCode: string;   // Original Rozetka code (e.g., "528975609")
  Tags: string[];
  ImageUrl: string;       // Local path to first image
  Gallery: string[];      // All local image paths
  Breadcrumbs: Breadcrumb[];
  CategoryPath: string;   // "Комп'ютери та ноутбуки > Ноутбуки"
  InitialStock: number;
  VariantAxes?: Record<string, string[]>;
  Variants: Array<{
    RozetkaCode: string;
    Name: string;
    Type: string;
    Price: number;
    ImageUrl: string;
    Gallery: string[];
    Attributes?: Record<string, string>;
  }>;
}

export interface CategoryConfig {
  storeName: string;
  categoryName: string;
  tags: string[];
}

// ============================================================================
// Utilities
// ============================================================================

export function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')
    .replace(/[\s_]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .substring(0, 80);
}

export function generateSku(rozetkaCode: string): string {
  return `ROZ-${rozetkaCode}`;
}

export function parsePrice(priceStr: string): number {
  const prices = priceStr.match(/[\d\s]+(?=₴)/g);
  if (!prices || prices.length === 0) return 0;
  const lastPrice = prices[prices.length - 1];
  return parseInt(lastPrice.replace(/\s/g, ''), 10) || 0;
}

// ============================================================================
// Attribute Extraction
// ============================================================================

/**
 * Extract structured attributes from a variant name.
 * Handles Ukrainian and English patterns for storage, color, etc.
 */
export function extractVariantAttributes(variantName: string): Record<string, string> {
  const attrs: Record<string, string> = {};
  const lower = variantName.toLowerCase();

  // Storage: "1 ТБ", "256 ГБ", "512 GB", "1 TB"
  const storageMatch = lower.match(/(\d+)\s*(гб|тб|gb|tb)/);
  if (storageMatch) {
    const size = storageMatch[1];
    const unit = storageMatch[2].toUpperCase();
    const normalizedUnit = (unit === 'ГБ' || unit === 'GB') ? 'GB' : 'TB';
    attrs['storage'] = `${size} ${normalizedUnit}`;
  }

  // RAM: "16 ГБ RAM", "32GB RAM"
  const ramMatch = lower.match(/(\d+)\s*(гб|gb)\s*(ram|озп)/);
  if (ramMatch) {
    attrs['ram'] = `${ramMatch[1]} GB`;
  }

  // Color detection
  const colorPatterns: Record<string, string> = {
    'чорний': 'Black', 'чорна': 'Black', 'чорне': 'Black', 'black': 'Black',
    'білий': 'White', 'біла': 'White', 'біле': 'White', 'white': 'White',
    'сірий': 'Gray', 'сіра': 'Gray', 'сіре': 'Gray', 'gray': 'Gray', 'grey': 'Gray',
    'червоний': 'Red', 'червона': 'Red', 'red': 'Red',
    'синій': 'Blue', 'синя': 'Blue', 'blue': 'Blue',
    'зелений': 'Green', 'зелена': 'Green', 'green': 'Green',
    'рожевий': 'Pink', 'рожева': 'Pink', 'pink': 'Pink',
    'золотий': 'Gold', 'золота': 'Gold', 'gold': 'Gold',
    'срібний': 'Silver', 'срібна': 'Silver', 'silver': 'Silver',
    'titanium': 'Titanium',
    'midnight': 'Midnight', 'starlight': 'Starlight',
    'cosmic': 'Cosmic', 'desert': 'Desert',
    'polar blue': 'Polar Blue', 'mint green': 'Mint Green',
    'off white': 'Off White', 'graphite': 'Graphite',
    'luna grey': 'Luna Grey', 'light silver': 'Light Silver',
    'citrus': 'Citrus',
  };

  for (const [pattern, colorName] of Object.entries(colorPatterns)) {
    if (lower.includes(pattern)) {
      attrs['color'] = colorName;
      break;
    }
  }

  return attrs;
}

/**
 * Build VariantAxes dictionary from all variant attributes.
 */
export function buildVariantAxes(
  variants: Array<{ Attributes?: Record<string, string> }>
): Record<string, string[]> {
  const axes: Record<string, string[]> = {};

  for (const v of variants) {
    if (!v.Attributes) continue;
    for (const [key, val] of Object.entries(v.Attributes)) {
      if (!axes[key]) axes[key] = [];
      if (!axes[key].includes(val)) axes[key].push(val);
    }
  }

  // Only return if there are actual axes
  return Object.keys(axes).length > 0 ? axes : {};
}

// ============================================================================
// Main Transform
// ============================================================================

/**
 * Convert scraped Rozetka data to Seeder format
 */
export function toSeederProduct(
  tile: ProductTile,
  details: ProductDetails,
  localImages: string[],
  config: CategoryConfig
): SeederProduct {
  // Use breadcrumbs to determine better category if available
  const categoryFromBreadcrumbs = details.breadcrumbs
    .filter(b => b.name && b.name !== 'Інтернет-магазин Rozetka')
    .map(b => b.name);

  // Determine category name from breadcrumbs (skip first "Rozetka" and last "product name")
  const categoryPath = categoryFromBreadcrumbs.length > 2
    ? categoryFromBreadcrumbs.slice(1, -1).join(' > ')
    : config.categoryName;

  // Build variants with structured attributes
  const variants = (details.variants || []).map(v => {
    const attrs = extractVariantAttributes(v.name);
    return {
      RozetkaCode: v.pid,
      Name: v.name,
      Type: v.type,
      Price: parsePrice(tile.priceText),
      ImageUrl: '',
      Gallery: [] as string[],
      Attributes: Object.keys(attrs).length > 0 ? attrs : undefined,
    };
  });

  // Build VariantAxes from variant attributes
  const variantAxes = buildVariantAxes(variants);

  const product: SeederProduct = {
    StoreName: config.storeName,
    CategoryName: categoryPath || config.categoryName,
    Name: tile.title,
    Description: details.description || `${tile.title} — купити на Rozetka`,
    Price: parsePrice(tile.priceText),
    Currency: 'UAH',
    Sku: generateSku(details.sku),
    RozetkaCode: details.sku,
    Tags: [
      ...config.tags,
      tile.brand.toLowerCase(),
      // Add breadcrumb categories as tags
      ...categoryFromBreadcrumbs
        .filter(b => b.length < 30 && b.length > 2)
        .map(b => b.toLowerCase()),
    ].filter(Boolean),
    ImageUrl: localImages[0] || tile.imgSrc || '',
    Gallery: localImages,
    Breadcrumbs: details.breadcrumbs,
    CategoryPath: details.categoryPath,
    InitialStock: Math.floor(Math.random() * 90) + 10,
    Variants: variants,
  };

  // Only add VariantAxes if non-empty
  if (Object.keys(variantAxes).length > 0) {
    product.VariantAxes = variantAxes;
  }

  return product;
}
