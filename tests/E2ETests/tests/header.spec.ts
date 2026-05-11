import { test, expect } from '../fixtures/test-base';

test.describe('Header: Navigation & Auth State', () => {
  
  test('should show user dropdown after login', async ({ loginPage, registerPage, header, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    const email = `header_user_${randomId}@test.com`;
    const password = "P@ssw0rd123!";
    
    // 1. Register
    await registerPage.goto('/auth/register');
    await registerPage.register("Header", "Tester", email, password);
    
    // 2. Verify redirect to catalog
    await expect(page).toHaveURL(/(\/catalog|\/auth\/login)$/);
    
    // 3. If redirected to login, perform login
    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, password);
    }
    
    // 4. Verify user menu trigger is visible
    await expect(header.userMenuTrigger).toBeVisible();
    await expect(header.userMenuTrigger).toContainText("Header");
    
    // 5. Open menu and check links
    await header.openUserMenu();
    await expect(header.profileLink).toBeVisible();
    await expect(header.logoutLink).toBeVisible();
  });

  test('should navigate to profile page via header dropdown', async ({ loginPage, registerPage, header, profilePage, page }) => {
    // This test assumes a user is logged in. For simplicity, we'll register a new one.
    const randomId = Math.random().toString(36).substring(7);
    const email = `profile_nav_${randomId}@test.com`;
    
    await registerPage.goto('/auth/register');
    await registerPage.register("Nav", "Tester", email, "P@ssw0rd123!");
    
    // Wait for redirect to avoid race condition
    await expect(page).toHaveURL(/(\/catalog|\/auth\/login)$/);
    
    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, "P@ssw0rd123!");
    }

    // Navigate to profile
    await header.clickProfile();
    await expect(page).toHaveURL('/profile');
    
    // Verify profile content
    await profilePage.expectUserDetails("Nav", "Tester", email);
  });

  test('should logout successfully via header dropdown', async ({ registerPage, loginPage, header, page }) => {
    const email = `logout_user_${Math.random().toString(36).substring(7)}@test.com`;
    
    await registerPage.goto('/auth/register');
    await registerPage.register("Logout", "User", email, "P@ssw0rd123!");
    
    // Wait for redirect to avoid race condition
    await expect(page).toHaveURL(/(\/catalog|\/auth\/login)$/);
    
    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, "P@ssw0rd123!");
    }

    // Logout
    await header.logout();
    
    // Verify redirect to login
    await expect(page).toHaveURL(/\/auth\/login/);
    
    // Verify user menu trigger is NOT visible
    await expect(header.userMenuTrigger).not.toBeVisible();
    await expect(header.loginLink).toBeVisible();
  });

});
