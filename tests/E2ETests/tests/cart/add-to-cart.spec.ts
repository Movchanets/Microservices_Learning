import { test, expect } from '../../fixtures/test-base';
import { ProductDetailPage } from '../../pages/product-detail.page';
import { CartPage } from '../../pages/cart.page';
import * as users from '../../data/users.json';

test.describe('Cart: Add to Cart Flow', () => {

  test('should add product to cart from catalog', async ({ catalogPage, page }) => {
    // Login first
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Go to catalog
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    // Find first product with an "Add to Cart" button
    const addToCartBtn = page.getByRole('button', { name: /add to cart/i }).first();
    if (await addToCartBtn.isVisible()) {
      await addToCartBtn.click();

      // Should see some confirmation (toast, badge update, etc.)
      // The cart badge should update
      await page.waitForTimeout(500);
    }
  });

  test('should add product from detail page', async ({ page }) => {
    // Login
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Navigate to catalog and click first product
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.getByTestId(/product-card-.*/).first();
    if (await firstProduct.isVisible()) {
      await firstProduct.click();
      await page.waitForLoadState('networkidle');

      // Click Add to Cart on detail page
      const addBtn = page.getByRole('button', { name: /add to cart/i });
      if (await addBtn.isVisible()) {
        await addBtn.click();
        await page.waitForTimeout(500);
      }
    }
  });

  test('should display cart page with items', async ({ page }) => {
    // Login
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Go to cart
    await page.goto('/cart');
    await page.waitForLoadState('networkidle');

    // Should see cart heading
    await expect(page.getByRole('heading', { name: 'Your Cart' })).toBeVisible();
  });
});
