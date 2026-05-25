import { test, expect } from '../fixtures/test-base';

test.describe('404 Not Found Page', () => {

  test('should display 404 heading for unknown routes', async ({ page, notFoundComponent }) => {
    await page.goto('/this-page-does-not-exist-12345');
    await page.waitForLoadState('domcontentloaded');

    await expect(notFoundComponent.heading404).toBeVisible();
    await expect(notFoundComponent.messageText).toBeVisible();
  });

  test('should have a working Go Home link', async ({ page, notFoundComponent }) => {
    await page.goto('/nonexistent-page');
    await page.waitForLoadState('domcontentloaded');

    await expect(notFoundComponent.goHomeLink).toBeVisible();
    await notFoundComponent.goHome();

    await expect(page).toHaveURL(/\/home/);
  });
});
