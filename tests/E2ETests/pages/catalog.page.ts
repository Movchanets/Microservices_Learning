import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { CategorySidebarComponent } from '../components/category-sidebar.component';
import { PaginationComponent } from '../components/pagination.component';

/**
 * Page object for `/catalog` — product listing with search, filtering, sorting, and pagination.
 */
export class CatalogPage extends BasePage {
  // ── Components ──────────────────────────────────────────
  readonly sidebar: CategorySidebarComponent;
  readonly pagination: PaginationComponent;

  // ── Search ──────────────────────────────────────────────
  readonly searchInput: Locator;
  readonly emptyState: Locator;
  readonly loadingSkeleton: Locator;

  // ── Product Grid ────────────────────────────────────────
  readonly productCards: Locator;
  readonly catalogContainer: Locator;
  readonly catalogTitle: Locator;

  // ── Filtering & Sorting ─────────────────────────────────
  readonly sortDropdown: Locator;
  readonly searchFacets: Locator;
  readonly priceMinInput: Locator;
  readonly priceMaxInput: Locator;
  readonly inStockCheckbox: Locator;
  readonly productCount: Locator;

  constructor(page: Page) {
    super(page);

    // Components
    this.sidebar = new CategorySidebarComponent(page);
    this.pagination = new PaginationComponent(page);

    // Search
    this.searchInput = page.getByTestId('search-input');
    this.emptyState = page.getByText(/no products found|no results|no items found|nothing found/i);
    this.loadingSkeleton = page.locator('.animate-pulse');

    // Product Grid
    this.productCards = page.getByTestId(/product-card-.*/);
    this.catalogContainer = page.getByTestId('catalog-container');
    this.catalogTitle = page.getByTestId('catalog-title');

    // Filtering & Sorting
    this.sortDropdown = page.locator('select').filter({ hasText: /sort|relevance|price|name/i })
      .or(page.getByRole('combobox'));
    this.searchFacets = page.locator('app-search-facets');
    this.priceMinInput = this.searchFacets.getByPlaceholder(/min/i);
    this.priceMaxInput = this.searchFacets.getByPlaceholder(/max/i);
    this.inStockCheckbox = this.searchFacets.getByRole('checkbox');
    this.productCount = page.getByText(/\d+ product/i);
  }

  get url(): string {
    return '/catalog';
  }


  // ── Actions ─────────────────────────────────────────────

  /** Type a query character-by-character (reliable for Angular ngModelChange) and wait for results. */
  async search(query: string) {
    await this.searchInput.clear();
    await this.searchInput.type(query, { delay: 10 });
  }

  /** Select a sort option from the dropdown (e.g. "Price: Low to High"). */
  async sortBy(option: string) {
    await this.sortDropdown.selectOption({ label: option });
  }

  /** Click a category button in the sidebar. */
  async filterByCategory(name: string) {
    await this.sidebar.selectCategory(name);
  }

  /** Fill min/max price inputs and trigger the filter via Tab. */
  async setPriceRange(min: number, max: number) {
    await this.priceMinInput.fill(String(min));
    await this.priceMaxInput.fill(String(max));
    await this.priceMaxInput.press('Tab');
  }

  /** Toggle the "In Stock Only" checkbox. */
  async toggleInStockOnly() {
    await this.inStockCheckbox.click();
  }

  /** Click a specific pagination page number. */
  async goToPage(n: number) {
    await this.pagination.goToPage(n);
  }

  // ── Queries ─────────────────────────────────────────────

  /** Get a single product card by its data-testid id. */
  async getProductCard(id: string): Promise<Locator> {
    return this.page.getByTestId(`product-card-${id}`);
  }

  /** Return the total number of visible product cards. */
  async getProductCount(): Promise<number> {
    return this.productCards.count();
  }

  /** True if the loading skeleton is visible (API call in progress). */
  async isLoading(): Promise<boolean> {
    return this.loadingSkeleton.isVisible();
  }

  /** True if the empty-state message is visible (no results). */
  async isEmpty(): Promise<boolean> {
    return this.emptyState.isVisible();
  }
}
