// Product form component.
// Handles both create and edit modes for seller products.
// Uses signals for form state, submits to SellerProductStore.
// Supports category selection, tags, and image URL.

import { Component, ChangeDetectionStrategy, effect, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { SellerProductStore } from '../seller-product.store';
import { StoreSettingsStore } from '../store-settings.store';
import { CategoryService, CategoryOption } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-product-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './product-form.html',
})
export class ProductFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(SellerProductStore);
  private readonly storeSettingsStore = inject(StoreSettingsStore);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);

  isEditing = signal(false);
  productId = signal<string | null>(null);
  formPopulated = signal(false);
  name = signal('');
  sku = signal('');
  description = signal('');
  price = signal(0);
  currency = signal('USD');
  categoryId = signal('');
  imageUrl = signal('');
  tagsInput = signal('');
  categories = signal<CategoryOption[]>([]);
  formErrors = signal<string[]>([]);
  formTouched = signal(false);

  constructor() {
    // Populate form fields when editing — fires once when selectedProduct loads
    effect(() => {
      const product = this.store.selectedProduct();
      if (product && this.isEditing() && !this.formPopulated()) {
        this.name.set(product.name);
        this.sku.set(product.sku);
        this.description.set(product.description);
        this.price.set(product.price);
        this.currency.set(product.currency);
        this.categoryId.set(product.categoryId);
        this.imageUrl.set(product.imageUrl ?? '');
        this.tagsInput.set(product.tags?.join(', ') ?? '');
        this.formPopulated.set(true);
      }
    });
  }

  ngOnInit(): void {
    this.loadCategories();

    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEditing.set(true);
      this.productId.set(id);
      this.store.loadProductById(id);
    }
  }

  private async loadCategories(): Promise<void> {
    try {
      const cats = await this.categoryService.getCategories();
      this.categories.set(cats.filter(c => c.isActive));
    } catch {
      // Non-critical — form still works without categories
    }
  }

  onSkuInput(rawValue: string): void {
    this.sku.set(ProductFormComponent.sanitizeSku(rawValue));
  }

  generateSku(): void {
    const productName = this.name().trim();
    if (!productName) return;

    // Take first 3 words, uppercase, join with hyphens, append random 4-digit
    const words = productName
      .replace(/[^a-zA-Z0-9\s]/g, '')
      .split(/\s+/)
      .filter(w => w.length > 0)
      .slice(0, 3)
      .map(w => w.toUpperCase());

    if (words.length === 0) return;

    const randomSuffix = Math.floor(1000 + Math.random() * 9000);
    this.sku.set(`${words.join('-')}-${randomSuffix}`);
  }

  /**
   * Sanitize SKU to valid format: alphanumeric and hyphens only.
   * Replaces spaces with hyphens, strips invalid chars, collapses repeats.
   */
  private static sanitizeSku(input: string): string {
    return input
      .toUpperCase()
      .replace(/[^A-Z0-9\s-]/g, '')   // keep only letters, digits, spaces, hyphens
      .replace(/\s+/g, '-')            // spaces → hyphens
      .replace(/-+/g, '-')             // collapse multiple hyphens
      .replace(/^-|-$/g, '');           // trim leading/trailing hyphens
  }

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.formTouched.set(true);

    // Validate required fields
    const errors: string[] = [];
    if (!this.name().trim()) errors.push('Product name is required.');
    if (!this.sku().trim()) errors.push('SKU is required.');
    if (!this.isEditing() && !/^[A-Z0-9][A-Z0-9-]*[A-Z0-9]$/i.test(this.sku()) && this.sku().length > 0) {
      errors.push('SKU must be alphanumeric with hyphens only.');
    }
    if (this.price() <= 0) errors.push('Price must be greater than zero.');
    if (!this.categoryId()) errors.push('Category is required.');
    if (this.imageUrl() && !/^https?:\/\/.+/.test(this.imageUrl())) {
      errors.push('Image URL must be a valid URL starting with http:// or https://.');
    }
    if (errors.length > 0) {
      this.formErrors.set(errors);
      return;
    }
    this.formErrors.set([]);

    // Safety net: ensure SKU is valid even if pasted
    this.sku.set(ProductFormComponent.sanitizeSku(this.sku()));

    const tags = this.tagsInput()
      ? this.tagsInput().split(',').map(t => t.trim()).filter(t => t.length > 0)
      : [];

    if (this.isEditing()) {
      const product = this.store.selectedProduct();
      const priceChanged = product && this.price() !== product.price;

      const success = await this.store.updateProduct(
        this.productId()!,
        {
          name: this.name(),
          description: this.description(),
          categoryId: this.categoryId(),
          imageUrl: this.imageUrl() || undefined,
        },
        // Pass new price if changed — store will call changePrice endpoint
        priceChanged ? this.price() : undefined,
        priceChanged ? this.currency() : undefined,
      );
      if (success) {
        this.toast.success('Product updated');
        this.router.navigate(['/seller/products']);
      } else {
        this.toast.error('Failed to update product');
      }
    } else {
      const storeId = this.storeSettingsStore.settings()?.storeId || '';
      const success = await this.store.createProduct({
        name: this.name(),
        sku: this.sku(),
        description: this.description(),
        price: this.price(),
        currency: this.currency(),
        categoryId: this.categoryId(),
        storeId,
        tags,
        imageUrl: this.imageUrl() || undefined,
      });
      if (success) {
        this.toast.success('Product created');
        this.router.navigate(['/seller/products']);
      } else {
        this.toast.error('Failed to create product');
      }
    }
  }
}
