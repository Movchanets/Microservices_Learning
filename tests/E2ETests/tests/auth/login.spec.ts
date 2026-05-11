import { test, expect } from '../../fixtures/test-base';
import * as users from '../../data/users.json';

test.describe('Authentication: Login', () => {
  
  test('should login successfully with newly registered user', async ({ registerPage, loginPage, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    const email = `user_${randomId}@test.com`;
    const password = "P@ssw0rd123!";
    
    // 1. Register a new random user
    await registerPage.goto('/auth/register');
    await registerPage.register("Test", "User", email, password);
    
    // Check if there's an error message visible
    const errorAlert = page.getByRole('alert');
    if (await errorAlert.isVisible()) {
      const errorText = await errorAlert.innerText();
      console.error(`Registration failed with error: ${errorText}`);
    }
    
    // Wait for registration to complete and redirect (either to catalog or login)
    await expect(page).toHaveURL(/(\/catalog|\/auth\/login)$/);
    
    // If we are at login page, it means we weren't auto-logged in (which is fine for this test)
    const currentUrl = page.url();
    if (currentUrl.includes('/auth/login')) {
      console.log('Registration successful, redirected to login page.');
    } else {
      console.log('Registration successful, auto-logged in to home page.');
    }
    
    // 2. Clear session to test login
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    
    // 3. Login with the newly created user
    await loginPage.goto('/auth/login');
    await loginPage.login(email, password);
    
    // Check if redirected to catalog
    await expect(page).toHaveURL('/catalog');
  });

  test('should show error with invalid credentials', async ({ loginPage }) => {
    await loginPage.goto('/auth/login');
    await loginPage.login('nonexistent@test.com', 'WrongPassword123!');
    
    // Verify error message is shown
    await loginPage.expectErrorMessage('Invalid credentials');
  });

});
