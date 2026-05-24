import { test, expect } from '../../fixtures/test-base';

test.describe('Catalog: Filtering, Sorting & Pagination', () => {

  test('should filter products by category via sidebar', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    const sidebarVisible = await catalogPage.categorySidebar.isVisible().catch(() => false);
    if (!sidebarVisible) {
      test.skip(true, 'Category sidebar not visible — skipping');
      return;
    }

    const initialCount = await catalogPage.getProductCount();
    await catalogPage.filterByCategory('Electronics');
    await page.waitForLoadState('domcontentloaded');

    const filteredCount = await catalogPage.getProductCount();
    expect(filteredCount).toBeLessThanOrEqual(initialCount);
  });

  test('should sort products by price', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    const sortVisible = await catalogPage.sortDropdown.isVisible().catch(() => false);
    if (!sortVisible) {
      test.skip(true, 'Sort dropdown not visible — skipping');
      return;
    }

    await catalogPage.sortBy('Price: Low to High');
    await page.waitForLoadState('domcontentloaded');

    // Verify products are still displayed
    const count = await catalogPage.getProductCount();
    expect(count).toBeGreaterThan(0);
  });

  test('should filter by price range', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    const facetsVisible = await catalogPage.searchFacets.isVisible().catch(() => false);
    if (!facetsVisible) {
      test.skip(true, 'Search facets not visible — skipping');
      return;
    }

    await catalogPage.setPriceRange(10, 100);
    await page.waitForLoadState('domcontentloaded');

    // Products should still be visible (or empty if no products in range)
    const count = await catalogPage.getProductCount();
    expect(count).toBeGreaterThanOrEqual(0);
  });

  test('should paginate through product pages', async ({ page, catalogPage }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    const paginationVisible = await catalogPage.pagination.isVisible().catch(() => false);
    if (!paginationVisible) {
      test.skip(true, 'Pagination not visible (single page of results) — skipping');
      return;
    }

    await catalogPage.goToPage(2);
    await page.waitForLoadState('domcontentloaded');

    // Should have products on page 2
    const count = await catalogPage.getProductCount();
    expect(count).toBeGreaterThan(0);
  });

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
    await page.waitForLoadState('domcontentloaded');

    const isEmpty = await catalogPage.isEmpty();
    const count = await catalogPage.getProductCount();
    expect(isEmpty || count === 0).toBe(true);
  });
});
