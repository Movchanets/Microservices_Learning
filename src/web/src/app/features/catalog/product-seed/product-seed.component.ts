import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CatalogService } from '../catalog.service';
import { LucideAngularModule } from 'lucide-angular';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-product-seed',
  standalone: true,
  imports: [LucideAngularModule, CommonModule],
  template: `
    <div class="p-6">
      <h2 class="text-2xl font-bold mb-4">Development Data Seeder</h2>
      <button
        (click)="seedData()"
        [disabled]="isSeeding()"
        class="bg-primary hover:bg-secondary text-white px-4 py-2 rounded">
        {{ isSeeding() ? 'Seeding...' : 'Seed Data' }}
      </button>

      @if (resultMessage()) {
        <div class="mt-4 p-4 rounded" [class.bg-green-100]="!error()" [class.bg-red-100]="error()">
          {{ resultMessage() }}
        </div>
      }
    </div>
  `
})
export class ProductSeedComponent {
  private http = inject(HttpClient);

  isSeeding = signal(false);
  resultMessage = signal('');
  error = signal(false);

  async seedData() {
    this.isSeeding.set(true);
    this.error.set(false);
    this.resultMessage.set('');

    try {
      // 1. Create a category
      const category: any = await firstValueFrom(this.http.post('/api/catalog/categories', {
        name: 'Electronics',
        description: 'Electronic devices',
        sortOrder: 1
      }));

      // 2. Create products
      const products = [
        {
          name: 'iPhone 15 Pro',
          description: 'Latest Apple smartphone',
          price: 999.99,
          currency: 'USD',
          sku: 'PHONE-IPHONE-15-PRO',
          categoryId: category.id,
          sellerId: '00000000-0000-0000-0000-000000000001',
          tags: ['apple', 'smartphone']
        },
        {
          name: 'MacBook Pro M3',
          description: 'Powerful laptop for professionals',
          price: 1999.99,
          currency: 'USD',
          sku: 'LAPTOP-MACBOOK-PRO-M3',
          categoryId: category.id,
          sellerId: '00000000-0000-0000-0000-000000000001',
          tags: ['apple', 'laptop']
        }
      ];

      for (const p of products) {
        await firstValueFrom(this.http.post('/api/catalog/products', p));
      }

      this.resultMessage.set('Successfully seeded category and products!');
    } catch (err: any) {
      this.error.set(true);
      this.resultMessage.set('Failed to seed data: ' + (err.message || 'Unknown error'));
      console.error(err);
    } finally {
      this.isSeeding.set(false);
    }
  }
}
