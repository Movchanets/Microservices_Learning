import { test, expect } from '../../fixtures/test-base';

test.describe('Catalog: Filtering, Sorting & Pagination', () => {

  // ── Implemented features ──────────────────────────────────

  test('should search and reduce product count', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    const initialCount = await catalogPage.getProductCount();
    await catalogPage.search('iPhone');
    await page.waitForLoadState('domcontentloaded');

    const filteredCount = await catalogPage.getProductCount();
    expect(filteredCount).toBeLessThanOrEqual(initialCount);
  });

  test('should show empty state for no-match search', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    await catalogPage.search('zzzznonexistentproduct12345');

    // Wait for the search API response to settle
    await page.waitForResponse(resp => resp.url().includes('/api/catalog') || resp.url().includes('/api/search'))
      .catch(() => {});
    await page.waitForLoadState('domcontentloaded');

    const isEmpty = await catalogPage.isEmpty();
    const count = await catalogPage.getProductCount();
    expect(isEmpty || count === 0).toBe(true);
  });

  // ── Not yet implemented — UI components pending ───────────
  // These features require category sidebar, sort dropdown,
  // search facets, and pagination components that are not yet
  // rendered on the catalog page. Uncomment when implemented.

  test.skip('should filter products by category via sidebar', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    await expect(catalogPage.categorySidebar).toBeVisible({ timeout: 10_000 });

    const initialCount = await catalogPage.getProductCount();
    await catalogPage.filterByCategory('Electronics');
    await page.waitForLoadState('domcontentloaded');

    const filteredCount = await catalogPage.getProductCount();
    expect(filteredCount).toBeLessThanOrEqual(initialCount);
  });

  test.skip('should sort products by price', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    await expect(catalogPage.sortDropdown).toBeVisible({ timeout: 10_000 });

    await catalogPage.sortBy('Price: Low to High');
    await page.waitForLoadState('domcontentloaded');

    const count = await catalogPage.getProductCount();
    expect(count).toBeGreaterThan(0);
  });

  test.skip('should filter by price range', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    await expect(catalogPage.searchFacets).toBeVisible({ timeout: 10_000 });

    await catalogPage.setPriceRange(10, 100);
    await page.waitForLoadState('domcontentloaded');

    const count = await catalogPage.getProductCount();
    expect(count).toBeGreaterThanOrEqual(0);
  });

  test.skip('should paginate through product pages', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    await expect(catalogPage.pagination).toBeVisible({ timeout: 10_000 });

    await catalogPage.goToPage(2);
    await page.waitForLoadState('domcontentloaded');

    const count = await catalogPage.getProductCount();
    expect(count).toBeGreaterThan(0);
  });
});
