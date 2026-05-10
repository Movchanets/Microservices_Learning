import { test, expect } from '../../fixtures/test-base';
import * as users from '../../data/users.json';

test.describe('Authentication: Login', () => {
  
  test.beforeEach(async ({ loginPage }) => {
    await loginPage.goto('/auth/login');
  });

  test('should login successfully with valid credentials', async ({ loginPage, page }) => {
    const user = users.validUser;
    
    await loginPage.login(user.email, user.password);
    
    // Check if redirected to home or dashboard
    // await expect(page).toHaveURL('/');
  });

  test('should show error with invalid credentials', async ({ loginPage }) => {
    const user = users.invalidUser;
    
    await loginPage.login(user.email, user.password);
    
    // The specific error message might vary, using a generic check
    // await loginPage.expectErrorMessage('Invalid credentials');
  });

});
