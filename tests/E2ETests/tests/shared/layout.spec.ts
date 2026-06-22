import { test, expect } from '../../fixtures/test-base';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Shared Layout: Header', () => {

  test('should display logo, search bar, and cart button', async ({ homePage, header }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await homePage.waitForPageLoad();
    });

    await test.step('Verify header elements are visible', async () => {
      await expect(header.logo).toBeVisible();
      await expect(header.cartBtn).toBeVisible();
    });
  });

  test('should show Sign in link when not authenticated', async ({ homePage, header }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await homePage.waitForPageLoad();
    });

    await test.step('Verify Sign in link is visible', async () => {
      await expect(header.loginLink).toBeVisible();
    });
  });

  test('should navigate to home when clicking logo', async ({ page, catalogPage, header }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto();
      await catalogPage.waitForPageLoad();
    });

    await test.step('Click the logo', async () => {
      await header.clickLogo();
    });

    await test.step('Verify navigation to home page', async () => {
      await expect(page).toHaveURL(/\/home/);
    });
  });

  test('should open and close mega menu', async ({ homePage, header }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await homePage.waitForPageLoad();
    });

    await test.step('Open mega menu', async () => {
      await header.toggleMegaMenu();
      await expect(header.megaMenu).toBeVisible();
    });

    await test.step('Close mega menu', async () => {
      await header.toggleMegaMenu();
      await expect(header.megaMenu).toBeHidden();
    });
  });

  test('should open cart drawer when clicking cart button', async ({ homePage, header }) => {
    await test.step('Navigate to home page and wait for hydration', async () => {
      await homePage.goto();
      await homePage.page.waitForLoadState('networkidle');
      await expect(header.logo).toBeVisible({ timeout: TIMEOUTS.element });
      await expect(header.cartBtn).toBeEnabled({ timeout: TIMEOUTS.element });
    });

    await test.step('Open cart drawer', async () => {
      await header.openCart();
      await homePage.cartDrawer.waitForOpen();
    });

    await test.step('Verify cart drawer is visible', async () => {
      await expect(homePage.cartDrawer.heading).toBeVisible();
    });
  });

  test('should close cart drawer', async ({ homePage, header }) => {
    await test.step('Navigate to home page and wait for hydration', async () => {
      await homePage.goto();
      await homePage.page.waitForLoadState('networkidle');
      await expect(header.logo).toBeVisible({ timeout: TIMEOUTS.element });
      await expect(header.cartBtn).toBeEnabled({ timeout: TIMEOUTS.element });
    });

    await test.step('Open and close cart drawer', async () => {
      await header.openCart();
      await homePage.cartDrawer.waitForOpen();
      await homePage.cartDrawer.close();
      await homePage.cartDrawer.waitForClose();
    });
  });
});
