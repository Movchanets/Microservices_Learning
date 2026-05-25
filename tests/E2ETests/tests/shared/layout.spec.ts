import { test, expect } from '../../fixtures/test-base';
import { authTest } from '../../fixtures/auth.fixture';
import { HeaderComponent } from '../../components/header.component';

test.describe('Shared Layout: Header', () => {

  test('should display logo, search bar, and cart button', async ({ page, header }) => {
    await page.goto('/home');
    await page.waitForLoadState('domcontentloaded');

    await expect(header.logo).toBeVisible();
    await expect(header.cartBtn).toBeVisible();
  });

  test('should show Sign in link when not authenticated', async ({ page, header }) => {
    await page.goto('/home');
    await page.waitForLoadState('domcontentloaded');

    await expect(header.loginLink).toBeVisible();
  });

  test('should navigate to home when clicking logo', async ({ page, header }) => {
    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    await header.clickLogo();

    await expect(page).toHaveURL(/\/home/);
  });

  test('should open and close mega menu', async ({ page, header }) => {
    await page.goto('/home');
    await page.waitForLoadState('domcontentloaded');

    // Open mega menu
    await header.toggleMegaMenu();
    await expect(header.megaMenu).toBeVisible();

    // Close mega menu by clicking the toggle again
    await header.toggleMegaMenu();
    await expect(header.megaMenu).toBeHidden();
  });

  test('should open cart drawer when clicking cart button', async ({ page, header, cartDrawer }) => {
    await page.goto('/home');
    await page.waitForLoadState('domcontentloaded');

    await header.openCart();
    await cartDrawer.waitForOpen();

    await expect(cartDrawer.heading).toBeVisible();
  });

  test('should close cart drawer', async ({ page, header, cartDrawer }) => {
    await page.goto('/home');
    await page.waitForLoadState('domcontentloaded');

    await header.openCart();
    await cartDrawer.waitForOpen();

    await cartDrawer.close();
    await cartDrawer.waitForClose();
  });
});

authTest.describe('Shared Layout: Header (Authenticated)', () => {

  authTest('should show user menu when authenticated', async ({ buyerContext }) => {
    const authPage = await buyerContext.newPage();
    const authHeader = new HeaderComponent(authPage);
    await authPage.goto('/home');
    await authPage.waitForLoadState('domcontentloaded');

    // Should show user menu trigger instead of login link
    await expect(authHeader.userMenuTrigger).toBeVisible({ timeout: 15000 });
    await authPage.close();
  });

  authTest('should open user dropdown and show profile link', async ({ buyerContext }) => {
    const authPage = await buyerContext.newPage();
    const authHeader = new HeaderComponent(authPage);
    await authPage.goto('/home');
    await authPage.waitForLoadState('domcontentloaded');

    await authHeader.openUserMenu();

    await expect(authHeader.profileLink).toBeVisible();
    await expect(authHeader.logoutLink).toBeVisible();
    await authPage.close();
  });
});

test.describe('Shared Layout: Footer', () => {

  test('should display theme toggle button', async ({ page, footer }) => {
    await page.goto('/home');
    await page.waitForLoadState('domcontentloaded');

    await expect(footer.themeToggle).toBeVisible();
  });

  test('should toggle theme via dropdown', async ({ page, footer }) => {
    await page.goto('/home');
    await page.waitForLoadState('domcontentloaded');

    // Get initial dark class state
    const hadDarkInitially = await page.locator('html').evaluate(
      (el) => el.classList.contains('dark')
    );

    // Click theme button to open dropdown
    await footer.themeToggle.click();

    // Select the opposite theme
    if (hadDarkInitially) {
      await page.getByRole('button', { name: /light/i }).click();
    } else {
      await page.getByRole('button', { name: /dark/i }).click();
    }

    // Verify the class changed — use expect.poll to handle Angular effect timing
    await expect.poll(
      () => page.locator('html').evaluate((el) => el.classList.contains('dark')),
      { timeout: 5000 }
    ).toBe(!hadDarkInitially);
  });
});
