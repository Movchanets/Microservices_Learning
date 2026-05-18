import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Cart: Add to Cart Flow', () => {

  test('should add product to cart from catalog', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const addToCartBtn = page.getByRole('button', { name: /add to cart/i }).first();
    await expect(addToCartBtn).toBeVisible({ timeout: 10000 });
    await addToCartBtn.click();
    await expect(page.getByTestId('cart-badge')).toBeVisible();
    await page.close();
  });

  test('should add product from detail page', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.getByTestId(/product-card-.*/).first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('networkidle');

    const addBtn = page.getByRole('button', { name: /add to cart/i });
    await expect(addBtn).toBeVisible({ timeout: 10000 });
    await addBtn.click();
    await expect(page.getByTestId('cart-badge')).toBeVisible();
    await page.close();
  });

  test('should display cart page with items', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    await page.goto('/cart');
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: 'Your Cart' })).toBeVisible();
    await page.close();
  });
});
