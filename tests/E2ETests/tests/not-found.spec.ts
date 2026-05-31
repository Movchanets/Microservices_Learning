import { test, expect } from '../fixtures/test-base';

test.describe('404 Not Found Page', () => {

  test('should display 404 heading for unknown routes', async ({ page, notFoundComponent }) => {
    await test.step('Navigate to unknown route', async () => {
      await page.goto('/this-page-does-not-exist-12345');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify 404 heading and message', async () => {
      await expect(notFoundComponent.heading404).toBeVisible();
      await expect(notFoundComponent.messageText).toBeVisible();
    });
  });

  test('should have a working Go Home link', async ({ page, notFoundComponent }) => {
    await test.step('Navigate to unknown route', async () => {
      await page.goto('/nonexistent-page');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify Go Home link and click it', async () => {
      await expect(notFoundComponent.goHomeLink).toBeVisible();
      await notFoundComponent.goHome();
    });

    await test.step('Verify navigation to home page', async () => {
      await expect(page).toHaveURL(/\/home/);
    });
  });
});
