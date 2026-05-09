import { Component } from '@angular/core';

@Component({
  selector: 'app-catalog',
  standalone: true,
  template: `
    <div class="container mx-auto p-8">
      <h1 class="text-3xl font-bold mb-4">Product Catalog</h1>
      <p class="text-muted">Explore our wide range of products.</p>
    </div>
  `
})
export class CatalogComponent {}
