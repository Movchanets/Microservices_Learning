/**
 * Rozetka Scraper Fixtures
 * 
 * Provides Playwright browser context with anti-bot evasion,
 * page objects, and configuration for the Rozetka scraper.
 */

import { test as base, Browser, BrowserContext, Page } from '@playwright/test';
import { RozetkaCategoryPage } from '../pages/rozetka-category.page';
import { RozetkaProductPage } from '../pages/rozetka-product.page';

// ============================================================================
// Types
// ============================================================================

export interface ScraperConfig {
  userAgent: string;
  viewport: { width: number; height: number };
  locale: string;
  timezone: string;
  minDelay: number;
  maxDelay: number;
}

export interface RozetkaFixtures {
  scraperConfig: ScraperConfig;
  browserContext: BrowserContext;
  categoryPage: RozetkaCategoryPage;
  productPage: RozetkaProductPage;
}

// ============================================================================
// Default Config
// ============================================================================

const DEFAULT_CONFIG: ScraperConfig = {
  userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36',
  viewport: { width: 1920, height: 1080 },
  locale: 'uk-UA',
  timezone: 'Europe/Kiev',
  minDelay: 1000,
  maxDelay: 2500,
};

// ============================================================================
// Fixture Definition
// ============================================================================

export const test = base.extend<RozetkaFixtures>({
  // Config fixture with defaults (can be overridden)
  scraperConfig: [DEFAULT_CONFIG, { option: true }],

  // Browser context with anti-bot evasion
  browserContext: async ({ browser, scraperConfig }, use) => {
    const context = await browser.newContext({
      userAgent: scraperConfig.userAgent,
      viewport: scraperConfig.viewport,
      locale: scraperConfig.locale,
      timezoneId: scraperConfig.timezone,
      extraHTTPHeaders: {
        'Accept-Language': 'uk-UA,uk;q=0.9,en;q=0.7',
      },
    });

    // Stealth: hide webdriver property
    await context.addInitScript(() => {
      Object.defineProperty(navigator, 'webdriver', { get: () => false });
    });

    await use(context);
    await context.close();
  },

  // Category page object
  categoryPage: async ({ browserContext }, use) => {
    const page = await browserContext.newPage();
    await use(new RozetkaCategoryPage(page));
    await page.close();
  },

  // Product page object
  productPage: async ({ browserContext }, use) => {
    const page = await browserContext.newPage();
    await use(new RozetkaProductPage(page));
    await page.close();
  },
});

export { expect } from '@playwright/test';
