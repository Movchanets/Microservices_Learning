/**
 * Rozetka Categories Tree Scraper
 * 
 * Scrapes the category hierarchy from Rozetka.com.ua.
 * Outputs categories.json for the Seeder.App.
 * 
 * Usage:
 *   npx tsx scripts/scrape-categories.ts
 *   npx tsx scripts/scrape-categories.ts --depth 2
 */

import { chromium } from 'playwright';
import * as fs from 'fs/promises';
import * as path from 'path';
import { program } from 'commander';
import { RozetkaCategoriesPage, type CategoryNode } from '../pages/rozetka-categories.page';

// Paths
const PROJECT_ROOT = path.resolve(import.meta.dirname, '../../../..');
const DATA_DIR = path.join(PROJECT_ROOT, 'src/Tools/Seeder.App/Data');
const CATEGORIES_JSON = path.join(DATA_DIR, 'rozetka-categories.json');

function log(msg: string): void {
  console.log(`ℹ️ [${new Date().toISOString()}] ${msg}`);
}

async function main(depth: number) {
  log(`Scraping Rozetka categories (depth: ${depth})...`);

  const browser = await chromium.launch({
    headless: true,
    args: ['--disable-blink-features=AutomationControlled'],
  });

  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    locale: 'uk-UA',
    timezoneId: 'Europe/Kiev',
  });

  await ctx.addInitScript(() => {
    Object.defineProperty(navigator, 'webdriver', { get: () => false });
  });

  const page = await ctx.newPage();
  const categoriesPage = new RozetkaCategoriesPage(page);

  log('Fetching top-level categories...');
  const tree = await categoriesPage.buildCategoryTree(depth);

  log(`Found ${tree.length} top-level categories`);

  // Flatten for JSON output
  const flat = categoriesPage.flattenTree(tree);
  log(`Total categories (flat): ${flat.length}`);

  // Save tree structure
  await fs.mkdir(DATA_DIR, { recursive: true });
  await fs.writeFile(CATEGORIES_JSON, JSON.stringify(tree, null, 2));
  log(`Saved tree to ${CATEGORIES_JSON}`);

  // Also save flat version
  const flatPath = path.join(DATA_DIR, 'rozetka-categories-flat.json');
  await fs.writeFile(flatPath, JSON.stringify(flat, null, 2));
  log(`Saved flat list to ${flatPath}`);

  // Print summary
  console.log('\n=== Category Tree ===');
  for (const cat of tree) {
    console.log(`📁 ${cat.name} (${cat.id})`);
    for (const sub of cat.children.slice(0, 5)) {
      console.log(`  └─ ${sub.name} (${sub.id})`);
    }
    if (cat.children.length > 5) {
      console.log(`  └─ ... and ${cat.children.length - 5} more`);
    }
  }

  await browser.close();
  log('Done!');
}

program
  .name('scrape-categories')
  .description('Scrape Rozetka category tree')
  .option('-d, --depth <n>', 'Tree depth to scrape', '1')
  .action((opts) => main(parseInt(opts.depth, 10)));

program.parse();
