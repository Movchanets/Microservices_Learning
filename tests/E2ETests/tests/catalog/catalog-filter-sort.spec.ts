import { test, expect } from '../../fixtures/test-base';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Catalog: Filtering, Sorting & Pagination', () => {

  // ── Implemented features ──────────────────────────────────

  test('should search and reduce product count', async ({ page, catalogPage }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    let initialCount: number;
    await test.step('Record initial product count', async () => {
      initialCount = await catalogPage.getProductCount();
    });

    await test.step('Search for a product', async () => {
      await catalogPage.search('iPhone');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify filtered product count', async () => {
      const filteredCount = await catalogPage.getProductCount();
      expect(filteredCount).toBeLessThanOrEqual(initialCount!);
    });
  });

  test('should show empty state for no-match search', async ({ page, catalogPage }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Search for nonexistent product', async () => {
      await catalogPage.search('zzzznonexistentproduct12345');
    });

    await test.step('Wait for search results to load', async () => {
      // Wait for loading skeleton to disappear (API response received)
      await catalogPage.loadingSkeleton.waitFor({ state: 'hidden', timeout: 15_000 }).catch(() => {});
      // Give Angular time to render the empty state after API response
      await page.waitForTimeout(1000);
    });

    await test.step('Verify empty state is shown', async () => {
      await expect(catalogPage.emptyState).toBeVisible({ timeout: TIMEOUTS.api });
    });
  });

  // ── Not yet implemented — UI components pending ───────────
  // These features require category sidebar, sort dropdown,
  // search facets, and pagination components that are not yet
  // rendered on the catalog page. Uncomment when implemented.

  test.skip('should filter products by category via sidebar', async ({ page, catalogPage }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Verify category sidebar is visible', async () => {
      await expect(catalogPage.categorySidebar).toBeVisible({ timeout: TIMEOUTS.element });
    });

    let initialCount: number;
    await test.step('Record initial product count', async () => {
      initialCount = await catalogPage.getProductCount();
    });

    await test.step('Filter by Electronics category', async () => {
      await catalogPage.filterByCategory('Electronics');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify filtered product count', async () => {
      const filteredCount = await catalogPage.getProductCount();
      expect(filteredCount).toBeLessThanOrEqual(initialCount!);
    });
  });

  test.skip('should sort products by price', async ({ page, catalogPage }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Verify sort dropdown is visible', async () => {
      await expect(catalogPage.sortDropdown).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Sort by price low to high', async () => {
      await catalogPage.sortBy('Price: Low to High');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify products are still displayed', async () => {
      const count = await catalogPage.getProductCount();
      expect(count).toBeGreaterThan(0);
    });
  });

  test.skip('should filter by price range', async ({ page, catalogPage }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Verify search facets are visible', async () => {
      await expect(catalogPage.searchFacets).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Set price range filter', async () => {
      await catalogPage.setPriceRange(10, 100);
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify products are displayed', async () => {
      const count = await catalogPage.getProductCount();
      expect(count).toBeGreaterThanOrEqual(0);
    });
  });

  test.skip('should paginate through product pages', async ({ page, catalogPage }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Verify pagination is visible', async () => {
      await expect(catalogPage.pagination).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Navigate to page 2', async () => {
      await catalogPage.goToPage(2);
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify products are displayed on page 2', async () => {
      const count = await catalogPage.getProductCount();
      expect(count).toBeGreaterThan(0);
    });
  });
});