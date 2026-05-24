import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class CatalogPage extends BasePage {
  readonly searchInput: Locator;
  readonly productCards: Locator;
  readonly catalogContainer: Locator;
  readonly catalogTitle: Locator;

  // Filtering & Sorting
  readonly categorySidebar: Locator;
  readonly sortDropdown: Locator;
  readonly priceMinInput: Locator;
  readonly priceMaxInput: Locator;
  readonly inStockCheckbox: Locator;
  readonly searchFacets: Locator;
  readonly productCount: Locator;
  readonly emptyState: Locator;
  readonly loadingSkeleton: Locator;

  // Pagination
  readonly pagination: Locator;
  readonly paginationPrev: Locator;
  readonly paginationNext: Locator;

  constructor(page: Page) {
    super(page);
    this.searchInput = page.getByTestId('search-input');
    this.productCards = page.getByTestId(/product-card-.*/);
    this.catalogContainer = page.getByTestId('catalog-container');
    this.catalogTitle = page.getByTestId('catalog-title');

    // Filtering & Sorting
    this.categorySidebar = page.locator('app-category-sidebar');
    this.sortDropdown = page.locator('select').filter({ hasText: /sort|relevance|price|name/i })
      .or(page.getByRole('combobox'));
    this.searchFacets = page.locator('app-search-facets');
    this.priceMinInput = this.searchFacets.getByPlaceholder(/min/i);
    this.priceMaxInput = this.searchFacets.getByPlaceholder(/max/i);
    this.inStockCheckbox = this.searchFacets.getByRole('checkbox');
    this.productCount = page.getByText(/\d+ product/i);
    this.emptyState = page.getByText(/no products found|no results|no items found|nothing found/i);
    this.loadingSkeleton = page.locator('.animate-pulse');

    // Pagination
    this.pagination = page.locator('nav[aria-label="Pagination"]');
    this.paginationPrev = this.pagination.locator('button').first();
    this.paginationNext = this.pagination.locator('button').last();
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }

  async getProductCard(id: string): Promise<Locator> {
    return this.page.getByTestId(`product-card-${id}`);
  }

  async getProductCount(): Promise<number> {
    return this.productCards.count();
  }

  async sortBy(option: string) {
    await this.sortDropdown.selectOption({ label: option });
  }

  async filterByCategory(name: string) {
    const btn = this.categorySidebar.getByRole('button', { name });
    await btn.click();
  }

  async setPriceRange(min: number, max: number) {
    await this.priceMinInput.fill(String(min));
    await this.priceMaxInput.fill(String(max));
    // Trigger filter by pressing Tab or Enter
    await this.priceMaxInput.press('Tab');
  }

  async toggleInStockOnly() {
    await this.inStockCheckbox.click();
  }

  async goToPage(n: number) {
    const pageBtn = this.pagination.locator('button', { hasText: String(n) });
    await pageBtn.click();
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSkeleton.isVisible();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyState.isVisible();
  }
}
