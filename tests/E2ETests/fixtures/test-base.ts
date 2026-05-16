import { test as base } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { RegisterPage } from '../pages/register.page';
import { ForgotPasswordPage } from '../pages/forgot-password.page';
import { CatalogPage } from '../pages/catalog.page';
import { ProductDetailPage } from '../pages/product-detail.page';
import { CartPage } from '../pages/cart.page';
import { CheckoutPage } from '../pages/checkout.page';
import { OrdersPage } from '../pages/orders.page';
import { OrderDetailPage } from '../pages/order-detail.page';
import { SellerDashboardPage } from '../pages/seller-dashboard.page';
import { StoreSettingsPage } from '../pages/store-settings.page';
import { AdminPage } from '../pages/admin.page';
import { ProfilePage } from '../pages/profile.page';
import { HeaderComponent } from '../components/header.component';
import { FooterComponent } from '../components/footer.component';

type MyFixtures = {
  loginPage: LoginPage;
  registerPage: RegisterPage;
  forgotPasswordPage: ForgotPasswordPage;
  catalogPage: CatalogPage;
  productDetailPage: ProductDetailPage;
  cartPage: CartPage;
  checkoutPage: CheckoutPage;
  ordersPage: OrdersPage;
  orderDetailPage: OrderDetailPage;
  sellerDashboardPage: SellerDashboardPage;
  storeSettingsPage: StoreSettingsPage;
  adminPage: AdminPage;
  profilePage: ProfilePage;
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
  productDetailPage: async ({ page }, use) => {
    await use(new ProductDetailPage(page));
  },
  cartPage: async ({ page }, use) => {
    await use(new CartPage(page));
  },
  checkoutPage: async ({ page }, use) => {
    await use(new CheckoutPage(page));
  },
  ordersPage: async ({ page }, use) => {
    await use(new OrdersPage(page));
  },
  orderDetailPage: async ({ page }, use) => {
    await use(new OrderDetailPage(page));
  },
  sellerDashboardPage: async ({ page }, use) => {
    await use(new SellerDashboardPage(page));
  },
  storeSettingsPage: async ({ page }, use) => {
    await use(new StoreSettingsPage(page));
  },
  adminPage: async ({ page }, use) => {
    await use(new AdminPage(page));
  },
  profilePage: async ({ page }, use) => {
    await use(new ProfilePage(page));
  },
  header: async ({ page }, use) => {
    await use(new HeaderComponent(page));
  },
  footer: async ({ page }, use) => {
    await use(new FooterComponent(page));
  },
});

export { expect } from '@playwright/test';
