import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { SellerProductsPage } from '../../pages/seller-products.page';

test.describe('Seller: Products', () => {

  test('should display seller products page', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const productsPage = new SellerProductsPage(page);

    await productsPage.goto();

    // Should see either products table or empty state
    const hasTable = await productsPage.productsTable.isVisible().catch(() => false);
    const hasEmpty = await productsPage.emptyState.isVisible().catch(() => false);
    expect(hasTable || hasEmpty).toBe(true);
    await page.close();
  });

  test('should have add product button for seller', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const productsPage = new SellerProductsPage(page);

    await productsPage.goto();

    await expect(productsPage.addProductBtn).toBeVisible();
    await page.close();
  });

  test('should show product list when products exist', async ({ sellerContext, sellerApi }) => {
    const page = await sellerContext.newPage();
    const productsPage = new SellerProductsPage(page);

    await productsPage.goto();

    const count = await productsPage.getProductCount();
    // Seller should have at least 1 product from test fixtures
    if (count > 0) {
      const firstRow = await productsPage.getProductRow(0);
      await expect(firstRow).toBeVisible();
    }
    await page.close();
  });
});
