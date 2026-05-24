/**
 * Test data file loader.
 * Loads JSON data files from the data/ directory, mirroring Seeder.App's LoadJsonAsync.
 *
 * Usage:
 *   import { loadTestDataFile, loadProducts, loadUsers } from '../utils/data-loader';
 *
 *   const products = await loadProducts();
 *   const users = await loadUsers();
 */

import * as fs from 'fs';
import * as path from 'path';

const DATA_DIR = path.resolve(__dirname, '..', 'data');

function loadJson<T>(fileName: string): T {
  const filePath = path.join(DATA_DIR, fileName);
  if (!fs.existsSync(filePath)) {
    throw new Error(`Data file not found: ${filePath}`);
  }
  const raw = fs.readFileSync(filePath, 'utf-8');
  return JSON.parse(raw) as T;
}

// ── Types (mirror the JSON structure) ──

export interface UserData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface UsersFile {
  admin: UserData;
  buyer: UserData;
  seller: UserData;
  sellerAlt: UserData;
}

export interface CategoryData {
  name: string;
  description: string;
}

export interface StoreData {
  name: string;
  sellerEmail: string;
  description: string;
}

export interface ProductData {
  storeName: string;
  categoryName: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  sku: string;
  tags: string[];
  imageUrl: string;
  initialStock: number;
}

// ── Loaders ──

export function loadUsers(): UsersFile {
  return loadJson<UsersFile>('users.json');
}

export function loadCategories(): CategoryData[] {
  return loadJson<CategoryData[]>('categories.json');
}

export function loadStores(): StoreData[] {
  return loadJson<StoreData[]>('stores.json');
}

export function loadProducts(): ProductData[] {
  return loadJson<ProductData[]>('products.json');
}

/** Get a specific user by role */
export function getUserByRole(role: 'admin' | 'buyer' | 'seller' | 'sellerAlt'): UserData {
  const users = loadUsers();
  return users[role];
}

/** Get products for a specific store */
export function getProductsByStore(storeName: string): ProductData[] {
  return loadProducts().filter(p => p.storeName === storeName);
}

/** Get products for a specific category */
export function getProductsByCategory(categoryName: string): ProductData[] {
  return loadProducts().filter(p => p.categoryName === categoryName);
}
