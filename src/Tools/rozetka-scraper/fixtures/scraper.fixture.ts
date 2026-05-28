/**
 * Rozetka Scraper Fixture
 * 
 * Provides a pre-configured Playwright browser context with anti-bot evasion.
 * Can be used in both standalone scripts and Playwright tests.
 */

import { type Browser, type BrowserContext, chromium } from 'playwright';

export interface ScraperConfig {
  userAgent: string;
  viewport: { width: number; height: number };
  locale: string;
  timezone: string;
}

const DEFAULT_CONFIG: ScraperConfig = {
  userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
  viewport: { width: 1920, height: 1080 },
  locale: 'uk-UA',
  timezone: 'Europe/Kiev',
};

/**
 * Create a browser context with anti-bot evasion
 */
export async function createScraperContext(
  browser: Browser,
  config: Partial<ScraperConfig> = {}
): Promise<BrowserContext> {
  const cfg = { ...DEFAULT_CONFIG, ...config };

  const ctx = await browser.newContext({
    userAgent: cfg.userAgent,
    viewport: cfg.viewport,
    locale: cfg.locale,
    timezoneId: cfg.timezone,
    extraHTTPHeaders: {
      'Accept-Language': 'uk-UA,uk;q=0.9,en;q=0.7',
    },
  });

  // Stealth: hide webdriver
  await ctx.addInitScript(() => {
    Object.defineProperty(navigator, 'webdriver', { get: () => false });
  });

  return ctx;
}

/**
 * Launch browser and create a scraper context
 */
export async function launchScraper(config: Partial<ScraperConfig> = {}) {
  const browser = await chromium.launch({
    headless: true,
    args: ['--disable-blink-features=AutomationControlled'],
  });

  const context = await createScraperContext(browser, config);

  return { browser, context };
}
