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
  Variants: Array<{ RozetkaCode: string; Name: string; Type: string; Price: number; ImageUrl: string; Gallery: string[] }>;
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

  return {
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
    Variants: (details.variants || []).map(v => ({
      RozetkaCode: v.pid,
      Name: v.name,
      Type: v.type,
      Price: parsePrice(tile.priceText),
      ImageUrl: '',
      Gallery: [],
    })),
  };
}
