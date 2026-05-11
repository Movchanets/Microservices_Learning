import { test as base } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { RegisterPage } from '../pages/register.page';
import { ForgotPasswordPage } from '../pages/forgot-password.page';
import { CatalogPage } from '../pages/catalog.page';
//import { CheckoutPage } from '../pages/checkout.page';
import { HeaderComponent } from '../components/header.component';
import { FooterComponent } from '../components/footer.component';

import { ProfilePage } from '../pages/profile.page';

type MyFixtures = {
  loginPage: LoginPage;
  registerPage: RegisterPage;
  forgotPasswordPage: ForgotPasswordPage;
  catalogPage: CatalogPage;
  profilePage: ProfilePage;
 // checkoutPage: CheckoutPage;
  header: HeaderComponent;
  footer: FooterComponent;
};

export const test = base.extend<MyFixtures>({
  loginPage: async ({ page }, use) => {
    await use(new LoginPage(page));
  },
  registerPage: async ({ page }, use) => {
    await use(new RegisterPage(page));
  },
  forgotPasswordPage: async ({ page }, use) => {
    await use(new ForgotPasswordPage(page));
  },
  catalogPage: async ({ page }, use) => {
    await use(new CatalogPage(page));
  },
  profilePage: async ({ page }, use) => {
    await use(new ProfilePage(page));
  },
  // checkoutPage: async ({ page }, use) => {
  //   await use(new CheckoutPage(page));
  // },
  header: async ({ page }, use) => {
    await use(new HeaderComponent(page));
  },
  footer: async ({ page }, use) => {
    await use(new FooterComponent(page));
  },
});

export { expect } from '@playwright/test';
