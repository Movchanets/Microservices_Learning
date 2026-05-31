import { test as base, type Page, type TestFixture } from '@playwright/test';
import { allure } from 'allure-playwright';
import { LoginPage } from '../pages/login.page';
import { RegisterPage } from '../pages/register.page';
import { HomePage } from '../pages/home.page';
import { CatalogPage } from '../pages/catalog.page';
import { ProductDetailPage } from '../pages/product-detail.page';
import { CartPage } from '../pages/cart.page';
import { CheckoutPage } from '../pages/checkout.page';
import { OrdersPage } from '../pages/orders.page';
import { OrderDetailPage } from '../pages/order-detail.page';
import { SellerDashboardPage } from '../pages/seller-dashboard.page';
import { SellerProductsPage } from '../pages/seller-products.page';
import { SellerOrdersPage } from '../pages/seller-orders.page';
import { StoreSettingsPage } from '../pages/store-settings.page';
import { StorePagePage } from '../pages/store-page.page';
import { AdminPage } from '../pages/admin.page';
import { AdminStoreDetailPage } from '../pages/admin-store-detail.page';
import { ProfilePage } from '../pages/profile.page';
import { ProfileHubPage } from '../pages/profile-hub.page';
import { InventoryPage } from '../pages/inventory.page';
import { HeaderComponent } from '../components/header.component';
import { FooterComponent } from '../components/footer.component';
import { NotFoundComponent } from '../components/not-found.component';

type MyFixtures = {
  // ── Page Objects ────────────────────────────────────────
  loginPage: LoginPage;
  registerPage: RegisterPage;
  homePage: HomePage;
  catalogPage: CatalogPage;
  productDetailPage: ProductDetailPage;
  cartPage: CartPage;
  checkoutPage: CheckoutPage;
  ordersPage: OrdersPage;
  orderDetailPage: OrderDetailPage;
  sellerDashboardPage: SellerDashboardPage;
  sellerProductsPage: SellerProductsPage;
  sellerOrdersPage: SellerOrdersPage;
  storeSettingsPage: StoreSettingsPage;
  storePagePage: StorePagePage;
  adminPage: AdminPage;
  adminStoreDetailPage: AdminStoreDetailPage;
  profilePage: ProfilePage;
  profileHubPage: ProfileHubPage;
  inventoryPage: InventoryPage;
  // ── Component Objects ───────────────────────────────────
  header: HeaderComponent;
  footer: FooterComponent;
  notFoundComponent: NotFoundComponent;
};

/**
 * Factory: creates a fixture that instantiates a POM/Component from { page }.
 * Returns a single-key object suitable for spreading into base.extend().
 */
function pageFixture<T>(Ctor: new (page: Page) => T, key: string) {
  const fixture: TestFixture<T, { page: Page }> = async ({ page }, use) => {
    await use(new Ctor(page));
  };
  return { [key]: fixture } as Record<string, TestFixture<T, { page: Page }>>;
}

export const test = base.extend<MyFixtures>({
  // ── Page Objects ────────────────────────────────────────
  ...pageFixture(LoginPage, 'loginPage'),
  ...pageFixture(RegisterPage, 'registerPage'),
  ...pageFixture(HomePage, 'homePage'),
  ...pageFixture(CatalogPage, 'catalogPage'),
  ...pageFixture(ProductDetailPage, 'productDetailPage'),
  ...pageFixture(CartPage, 'cartPage'),
  ...pageFixture(CheckoutPage, 'checkoutPage'),
  ...pageFixture(OrdersPage, 'ordersPage'),
  ...pageFixture(OrderDetailPage, 'orderDetailPage'),
  ...pageFixture(SellerDashboardPage, 'sellerDashboardPage'),
  ...pageFixture(SellerProductsPage, 'sellerProductsPage'),
  ...pageFixture(SellerOrdersPage, 'sellerOrdersPage'),
  ...pageFixture(StoreSettingsPage, 'storeSettingsPage'),
  ...pageFixture(StorePagePage, 'storePagePage'),
  ...pageFixture(AdminPage, 'adminPage'),
  ...pageFixture(AdminStoreDetailPage, 'adminStoreDetailPage'),
  ...pageFixture(ProfilePage, 'profilePage'),
  ...pageFixture(ProfileHubPage, 'profileHubPage'),
  ...pageFixture(InventoryPage, 'inventoryPage'),

  // ── Component Objects ───────────────────────────────────
  ...pageFixture(HeaderComponent, 'header'),
  ...pageFixture(FooterComponent, 'footer'),
  ...pageFixture(NotFoundComponent, 'notFoundComponent'),

  // ── Override page fixture: Allure metadata + console error tracking ──
  page: async ({ page }, use, testInfo) => {
    allure.epic('Marketplace E2E');
    allure.feature(testInfo.project.name);
    allure.parameter('baseUrl', testInfo.project.use.baseURL || 'http://localhost:4201');
    allure.parameter('browser', testInfo.project.name);
    allure.parameter('ci', process.env.CI ? 'true' : 'false');

    const consoleErrors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await use(page);

    if (testInfo.status !== testInfo.expectedStatus && consoleErrors.length > 0) {
      await testInfo.attach('console-errors', {
        body: consoleErrors.join('\n'),
        contentType: 'text/plain',
      });
    }
  },
});

export { expect } from '@playwright/test';
