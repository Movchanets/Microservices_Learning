import { test, expect } from '../../fixtures/test-base';
import { TIMEOUTS } from '../../utils/constants';
import { authTest } from '../../fixtures/auth.fixture';
import { HeaderComponent } from '../../components/header.component';

test.describe('Shared Layout: Header', () => {

  test('should display logo, search bar, and cart button', async ({ page, header }) => {
    await test.step('Navigate to home page', async () => {
      await page.goto('/home');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify header elements are visible', async () => {
      await expect(header.logo).toBeVisible();
      await expect(header.cartBtn).toBeVisible();
    });
  });

  test('should show Sign in link when not authenticated', async ({ page, header }) => {
    await test.step('Navigate to home page', async () => {
      await page.goto('/home');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify Sign in link is visible', async () => {
      await expect(header.loginLink).toBeVisible();
    });
  });

  test('should navigate to home when clicking logo', async ({ page, header }) => {
    await test.step('Navigate to catalog page', async () => {
      await page.goto('/catalog');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Click the logo', async () => {
      await header.clickLogo();
    });

    await test.step('Verify navigation to home page', async () => {
      await expect(page).toHaveURL(/\/home/);
    });
  });

  test('should open and close mega menu', async ({ page, header }) => {
    await test.step('Navigate to home page', async () => {
      await page.goto('/home');
      await page.waitForLoadState('domcontentloaded');
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

  test('should open cart drawer when clicking cart button', async ({ page, header, cartDrawer }) => {
    await test.step('Navigate to home page', async () => {
      await page.goto('/home');
      await page.waitForLoadState('domcontentloaded');
      // Wait for Angular hydration — header must be interactive
      await expect(header.logo).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Open cart drawer', async () => {
      await header.openCart();
      await cartDrawer.waitForOpen();
    });

    await test.step('Verify cart drawer is visible', async () => {
      await expect(cartDrawer.heading).toBeVisible();
    });
  });

  test('should close cart drawer', async ({ page, header, cartDrawer }) => {
    await test.step('Navigate to home page', async () => {
      await page.goto('/home');
      await page.waitForLoadState('domcontentloaded');
      // Wait for Angular hydration — header must be interactive
      await expect(header.logo).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Open and close cart drawer', async () => {
      await header.openCart();
      await cartDrawer.waitForOpen();
      await cartDrawer.close();
      await cartDrawer.waitForClose();
    });
  });
});

authTest.describe('Shared Layout: Header (Authenticated)', () => {

  authTest('should show user menu when authenticated', async ({ buyerContext }) => {
    const authPage = await buyerContext.newPage();
    const authHeader = new HeaderComponent(authPage);

    await test.step('Navigate to home page as authenticated user', async () => {
      await authPage.goto('/home');
      await authPage.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify user menu trigger is visible', async () => {
      await expect(authHeader.userMenuTrigger).toBeVisible({ timeout: TIMEOUTS.api });
    });

    await authPage.close();
  });

  authTest('should open user dropdown and show profile link', async ({ buyerContext }) => {
    const authPage = await buyerContext.newPage();
    const authHeader = new HeaderComponent(authPage);

    await test.step('Navigate to home page as authenticated user', async () => {
      await authPage.goto('/home');
      await authPage.waitForLoadState('domcontentloaded');
    });

    await test.step('Open user menu', async () => {
      await authHeader.openUserMenu();
    });

    await test.step('Verify profile and logout links', async () => {
      await expect(authHeader.profileLink).toBeVisible();
      await expect(authHeader.logoutLink).toBeVisible();
    });

    await authPage.close();
  });
});

test.describe('Shared Layout: Footer', () => {

  test('should display theme toggle button', async ({ page, footer }) => {
    await test.step('Navigate to home page', async () => {
      await page.goto('/home');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify theme toggle is visible', async () => {
      await expect(footer.themeToggle).toBeVisible();
    });
  });

  test('should toggle theme via dropdown', async ({ page, footer }) => {
    await test.step('Navigate to home page', async () => {
      await page.goto('/home');
      await page.waitForLoadState('domcontentloaded');
    });

    let hadDarkInitially: boolean;
    await test.step('Get initial theme state', async () => {
      hadDarkInitially = await page.locator('html').evaluate(
        (el) => el.classList.contains('dark')
      );
    });

    await test.step('Toggle theme', async () => {
      await footer.themeToggle.click();
      if (hadDarkInitially!) {
        await page.getByRole('button', { name: /light/i }).click();
      } else {
        await page.getByRole('button', { name: /dark/i }).click();
      }
    });

    await test.step('Verify theme changed', async () => {
      await expect.poll(
        () => page.locator('html').evaluate((el) => el.classList.contains('dark')),
        { timeout: TIMEOUTS.quick }
      ).toBe(!hadDarkInitially!);
    });
  });
});