import { test, expect } from '../fixtures/test-base';

test.describe('404 Not Found Page', () => {

  test('should display 404 heading for unknown routes', async ({ page }) => {
    await page.goto('/this-page-does-not-exist-12345');
    await page.waitForLoadState('domcontentloaded');

    await expect(page.getByRole('heading', { name: '404' })).toBeVisible();
    await expect(page.getByText('Page not found')).toBeVisible();
  });

  test('should have a working Go Home link', async ({ page }) => {
    await page.goto('/nonexistent-page');
    await page.waitForLoadState('domcontentloaded');

    const goHomeLink = page.getByRole('link', { name: /go home/i });
    await expect(goHomeLink).toBeVisible();
    await goHomeLink.click();

    await expect(page).toHaveURL(/\/home/);
  });
});
