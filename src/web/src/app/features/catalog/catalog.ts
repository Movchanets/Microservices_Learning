import { Component } from '@angular/core';

@Component({
  selector: 'app-catalog',
  standalone: true,
  template: `
    <div class="container mx-auto p-8" data-testid="catalog-container">
      <h1 class="text-3xl font-bold mb-4" data-testid="catalog-title">Product Catalog</h1>
      <p class="text-muted">Explore our wide range of products.</p>
    </div>
  `
})
export class CatalogComponent {}
