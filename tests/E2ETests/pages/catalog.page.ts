import { Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class CatalogPage extends BasePage {
  readonly searchInput: Locator;
  readonly productCards: Locator;
  readonly catalogContainer: Locator;
  readonly catalogTitle: Locator;

  constructor(page: any) {
    super(page);
    this.searchInput = page.getByTestId('search-input');
    this.productCards = page.getByTestId(/product-card-.*/);
    this.catalogContainer = page.getByTestId('catalog-container');
    this.catalogTitle = page.getByTestId('catalog-title');
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }

  async getProductCard(id: string): Promise<Locator> {
    return this.page.getByTestId(`product-card-${id}`);
  }
}
