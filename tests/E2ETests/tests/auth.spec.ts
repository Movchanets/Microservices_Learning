import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  
  test('should display login page and perform login via data-testid', async ({ page }) => {
    // Navigate to the frontend login page
    await page.goto('/login');

    // Wait for the login form to be visible using testId
    const emailInput = page.getByTestId('email-input');
    const passwordInput = page.getByTestId('password-input');
    const submitBtn = page.getByTestId('login-submit-btn');

    await expect(emailInput).toBeVisible();
    await expect(passwordInput).toBeVisible();
    await expect(submitBtn).toBeVisible();

    // Fill in the form
    await emailInput.fill('buyer@test.com');
    await passwordInput.fill('P@ssw0rd');

    // Click submit
    await submitBtn.click();

    // Assuming the application routes to / upon successful login
    // wait for navigation or check for an element on the home page
    // await expect(page).toHaveURL('/');
  });

});
