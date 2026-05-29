import { test as base, type Page, type TestFixture } from '@playwright/test';
import { allure } from 'allure-playwright';
import { LoginPage } from '../pages/login.page';
import { RegisterPage } from '../pages/register.page';
import { ForgotPasswordPage } from '../pages/forgot-password.page';
import { HomePage } from '../pages/home.page';
import { CatalogPage } from '../pages/catalog.page';
import { ProductDetailPage } from '../pages/product-detail.page';
import { ProductDetailEnhancedPage } from '../pages/product-detail-enhanced.page';
import { CartPage } from '../pages/cart.page';
import { CheckoutPage } from '../pages/checkout.page';
import { CheckoutEnhancedPage } from '../pages/checkout-enhanced.page';
import { OrdersPage } from '../pages/orders.page';
import { OrderDetailPage } from '../pages/order-detail.page';
import { OrderDetailEnhancedPage } from '../pages/order-detail-enhanced.page';
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
import { CartDrawerComponent } from '../components/cart-drawer.component';
import { SearchBarComponent } from '../components/search-bar.component';
import { MegaMenuComponent } from '../components/mega-menu.component';
import { ReviewSummaryComponent } from '../components/review-summary.component';
import { ReviewListComponent } from '../components/review-list.component';
import { WriteReviewComponent } from '../components/write-review.component';
import { ToastContainerComponent } from '../components/toast-container.component';
import { NotFoundComponent } from '../components/not-found.component';
import { PaginationComponent } from '../components/pagination.component';
import { CategorySidebarComponent } from '../components/category-sidebar.component';

type MyFixtures = {
  loginPage: LoginPage;
  registerPage: RegisterPage;
  forgotPasswordPage: ForgotPasswordPage;
  homePage: HomePage;
  catalogPage: CatalogPage;
  productDetailPage: ProductDetailPage;
  productDetailEnhancedPage: ProductDetailEnhancedPage;
  cartPage: CartPage;
  checkoutPage: CheckoutPage;
  checkoutEnhancedPage: CheckoutEnhancedPage;
  ordersPage: OrdersPage;
  orderDetailPage: OrderDetailPage;
  orderDetailEnhancedPage: OrderDetailEnhancedPage;
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
  header: HeaderComponent;
  footer: FooterComponent;
  cartDrawer: CartDrawerComponent;
  searchBar: SearchBarComponent;
  megaMenu: MegaMenuComponent;
  reviewSummary: ReviewSummaryComponent;
  reviewList: ReviewListComponent;
  writeReview: WriteReviewComponent;
  toastContainer: ToastContainerComponent;
  notFoundComponent: NotFoundComponent;
  pagination: PaginationComponent;
  categorySidebar: CategorySidebarComponent;
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
  // --- Page Objects ---
  ...pageFixture(LoginPage, 'loginPage'),
  ...pageFixture(RegisterPage, 'registerPage'),
  ...pageFixture(ForgotPasswordPage, 'forgotPasswordPage'),
  ...pageFixture(HomePage, 'homePage'),
  ...pageFixture(CatalogPage, 'catalogPage'),
  ...pageFixture(ProductDetailPage, 'productDetailPage'),
  ...pageFixture(ProductDetailEnhancedPage, 'productDetailEnhancedPage'),
  ...pageFixture(CartPage, 'cartPage'),
  ...pageFixture(CheckoutPage, 'checkoutPage'),
  ...pageFixture(CheckoutEnhancedPage, 'checkoutEnhancedPage'),
  ...pageFixture(OrdersPage, 'ordersPage'),
  ...pageFixture(OrderDetailPage, 'orderDetailPage'),
  ...pageFixture(OrderDetailEnhancedPage, 'orderDetailEnhancedPage'),
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

  // --- Component Objects ---
  ...pageFixture(HeaderComponent, 'header'),
  ...pageFixture(FooterComponent, 'footer'),
  ...pageFixture(CartDrawerComponent, 'cartDrawer'),
  ...pageFixture(SearchBarComponent, 'searchBar'),
  ...pageFixture(MegaMenuComponent, 'megaMenu'),
  ...pageFixture(ReviewSummaryComponent, 'reviewSummary'),
  ...pageFixture(ReviewListComponent, 'reviewList'),
  ...pageFixture(WriteReviewComponent, 'writeReview'),
  ...pageFixture(ToastContainerComponent, 'toastContainer'),
  ...pageFixture(NotFoundComponent, 'notFoundComponent'),
  ...pageFixture(PaginationComponent, 'pagination'),
  ...pageFixture(CategorySidebarComponent, 'categorySidebar'),

  // --- Override page fixture: Allure metadata + console error tracking ---
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
