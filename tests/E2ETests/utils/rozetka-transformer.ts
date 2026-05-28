/**
 * Rozetka Data Transformer
 * 
 * Transforms scraped Rozetka product data into Seeder.App format.
 * Handles SKU generation, price parsing, and slug creation.
 */

import { ProductTile } from '../pages/rozetka-category.page';
import { ProductDetails } from '../pages/rozetka-product.page';

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
  Sku: string;
  Tags: string[];
  ImageUrl: string;
  InitialStock: number;
}

export interface CategoryConfig {
  storeName: string;
  categoryName: string;
  tags: string[];
}

// ============================================================================
// Utilities
// ============================================================================

/**
 * Generate URL-friendly slug from text
 */
export function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')
    .replace(/[\s_]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .substring(0, 80);
}

/**
 * Generate SKU from Rozetka product ID
 */
export function generateSku(articleId: string): string {
  const id = articleId.replace('p', '');
  return `ROZ-${id}`;
}

/**
 * Parse price string (e.g., "39 999₴ 37 999₴") to number
 * Takes the last price (usually the sale price)
 */
export function parsePrice(priceStr: string): number {
  const prices = priceStr.match(/[\d\s]+(?=₴)/g);
  if (!prices || prices.length === 0) return 0;
  const lastPrice = prices[prices.length - 1];
  return parseInt(lastPrice.replace(/\s/g, ''), 10) || 0;
}

/**
 * Convert scraped data to Seeder format
 */
export function toSeederProduct(
  tile: ProductTile,
  details: ProductDetails | null,
  localImages: string[],
  config: CategoryConfig
): SeederProduct {
  const title = details?.description ? 
    tile.title : // Use original title if no better description
    tile.title;
  
  const description = details?.description || `${tile.title} — купити на Rozetka`;

  return {
    StoreName: config.storeName,
    CategoryName: config.categoryName,
    Name: tile.title,
    Description: description,
    Price: parsePrice(tile.priceText),
    Currency: 'UAH',
    Sku: generateSku(tile.articleId),
    Tags: [...config.tags, tile.brand.toLowerCase()].filter(Boolean),
    ImageUrl: localImages[0] || tile.imgSrc || '',
    InitialStock: Math.floor(Math.random() * 90) + 10,
  };
}
