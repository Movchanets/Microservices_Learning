import { test as base } from '@playwright/test';
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
  homePage: async ({ page }, use) => {
    await use(new HomePage(page));
  },
  catalogPage: async ({ page }, use) => {
    await use(new CatalogPage(page));
  },
  productDetailPage: async ({ page }, use) => {
    await use(new ProductDetailPage(page));
  },
  productDetailEnhancedPage: async ({ page }, use) => {
    await use(new ProductDetailEnhancedPage(page));
  },
  cartPage: async ({ page }, use) => {
    await use(new CartPage(page));
  },
  checkoutPage: async ({ page }, use) => {
    await use(new CheckoutPage(page));
  },
  checkoutEnhancedPage: async ({ page }, use) => {
    await use(new CheckoutEnhancedPage(page));
  },
  ordersPage: async ({ page }, use) => {
    await use(new OrdersPage(page));
  },
  orderDetailPage: async ({ page }, use) => {
    await use(new OrderDetailPage(page));
  },
  orderDetailEnhancedPage: async ({ page }, use) => {
    await use(new OrderDetailEnhancedPage(page));
  },
  sellerDashboardPage: async ({ page }, use) => {
    await use(new SellerDashboardPage(page));
  },
  sellerProductsPage: async ({ page }, use) => {
    await use(new SellerProductsPage(page));
  },
  sellerOrdersPage: async ({ page }, use) => {
    await use(new SellerOrdersPage(page));
  },
  storeSettingsPage: async ({ page }, use) => {
    await use(new StoreSettingsPage(page));
  },
  storePagePage: async ({ page }, use) => {
    await use(new StorePagePage(page));
  },
  adminPage: async ({ page }, use) => {
    await use(new AdminPage(page));
  },
  adminStoreDetailPage: async ({ page }, use) => {
    await use(new AdminStoreDetailPage(page));
  },
  profilePage: async ({ page }, use) => {
    await use(new ProfilePage(page));
  },
  profileHubPage: async ({ page }, use) => {
    await use(new ProfileHubPage(page));
  },
  inventoryPage: async ({ page }, use) => {
    await use(new InventoryPage(page));
  },
  header: async ({ page }, use) => {
    await use(new HeaderComponent(page));
  },
  footer: async ({ page }, use) => {
    await use(new FooterComponent(page));
  },
  cartDrawer: async ({ page }, use) => {
    await use(new CartDrawerComponent(page));
  },
  searchBar: async ({ page }, use) => {
    await use(new SearchBarComponent(page));
  },
  megaMenu: async ({ page }, use) => {
    await use(new MegaMenuComponent(page));
  },
  reviewSummary: async ({ page }, use) => {
    await use(new ReviewSummaryComponent(page));
  },
  reviewList: async ({ page }, use) => {
    await use(new ReviewListComponent(page));
  },
  writeReview: async ({ page }, use) => {
    await use(new WriteReviewComponent(page));
  },
  toastContainer: async ({ page }, use) => {
    await use(new ToastContainerComponent(page));
  },
  notFoundComponent: async ({ page }, use) => {
    await use(new NotFoundComponent(page));
  },
  pagination: async ({ page }, use) => {
    await use(new PaginationComponent(page));
  },
  categorySidebar: async ({ page }, use) => {
    await use(new CategorySidebarComponent(page));
  },
});

export { expect } from '@playwright/test';
