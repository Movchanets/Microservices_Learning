// Product form component.
// Handles both create and edit modes for seller products.
// Uses signals for form state, submits to SellerProductStore.
// Supports category selection, tags, image URL, and multiple SKUs.

import { Component, ChangeDetectionStrategy, effect, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { SellerProductStore } from '../seller-product.store';
import { StoreSettingsStore } from '../store-settings.store';
import { CategoryService, CategoryOption } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { AddSkuRequest } from '../seller.models';

interface SkuFormEntry {
  id: string;
  skuCode: string;
  price: number;
  currency: string;
}

let skuIdCounter = 0;
function nextSkuId(): string {
  return `sku-${++skuIdCounter}-${Date.now()}`;
}

@Component({
  selector: 'app-product-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule, DecimalPipe],
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
  description = signal('');
  brand = signal('');
  categoryId = signal('');
  imageUrl = signal('');
  tagsInput = signal('');
  categories = signal<CategoryOption[]>([]);
  formErrors = signal<string[]>([]);
  formTouched = signal(false);

  // SKU form entries for create mode
  skus = signal<SkuFormEntry[]>([{ id: nextSkuId(), skuCode: '', price: 0, currency: 'USD' }]);

  // Existing SKUs for edit mode
  existingSkus = signal<{ skuCode: string; price: number; currency: string }[]>([]);

  constructor() {
    // Populate form fields when editing — fires once when selectedProduct loads
    effect(() => {
      const product = this.store.selectedProduct();
      if (product && this.isEditing() && !this.formPopulated()) {
        this.name.set(product.name);
        this.description.set(product.description);
        this.brand.set(product.brand ?? '');
        this.categoryId.set(product.categoryId);
        this.imageUrl.set(product.imageUrl ?? '');
        this.tagsInput.set(product.tags?.join(', ') ?? '');
        this.existingSkus.set(
          product.skus?.map(s => ({
            skuCode: s.skuCode,
            price: s.price,
            currency: s.currency,
          })) ?? []
        );
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

  addSkuRow(): void {
    this.skus.update(rows => [...rows, { id: nextSkuId(), skuCode: '', price: 0, currency: 'USD' }]);
  }

  removeSkuRow(index: number): void {
    if (this.skus().length <= 1) return; // Always keep at least one SKU row
    this.skus.update(rows => rows.filter((_, i) => i !== index));
  }

  updateSkuCode(index: number, value: string): void {
    this.skus.update(rows => rows.map((r, i) => i === index ? { ...r, skuCode: ProductFormComponent.sanitizeSku(value) } : r));
  }

  updateSkuPrice(index: number, value: number): void {
    this.skus.update(rows => rows.map((r, i) => i === index ? { ...r, price: value } : r));
  }

  updateSkuCurrency(index: number, value: string): void {
    this.skus.update(rows => rows.map((r, i) => i === index ? { ...r, currency: value } : r));
  }

  generateSkuFor(index: number): void {
    const productName = this.name().trim();
    if (!productName) return;

    const words = productName
      .replace(/[^a-zA-Z0-9\s]/g, '')
      .split(/\s+/)
      .filter(w => w.length > 0)
      .slice(0, 3)
      .map(w => w.toUpperCase());

    if (words.length === 0) return;

    const randomSuffix = Math.floor(1000 + Math.random() * 9000);
    const skuCode = `${words.join('-')}-${randomSuffix}`;
    this.skus.update(rows => rows.map((r, i) => i === index ? { ...r, skuCode } : r));
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
    if (!this.categoryId()) errors.push('Category is required.');
    if (this.imageUrl() && !/^https?:\/\/.+/.test(this.imageUrl())) {
      errors.push('Image URL must be a valid URL starting with http:// or https://.');
    }

    // Validate SKUs in create mode
    if (!this.isEditing()) {
      const skuEntries = this.skus();
      if (skuEntries.length === 0) {
        errors.push('At least one SKU is required.');
      }
      skuEntries.forEach((sku, i) => {
        if (!sku.skuCode.trim()) errors.push(`SKU #${i + 1}: SKU code is required.`);
        if (!/^[A-Z0-9][A-Z0-9-]*[A-Z0-9]$/i.test(sku.skuCode) && sku.skuCode.length > 0) {
          errors.push(`SKU #${i + 1}: SKU must be alphanumeric with hyphens only.`);
        }
        if (sku.price <= 0) errors.push(`SKU #${i + 1}: Price must be greater than zero.`);
      });

      // Check for duplicate SKU codes
      const skuCodes = skuEntries.map(s => s.skuCode.trim().toUpperCase()).filter(Boolean);
      const duplicates = skuCodes.filter((code, i) => skuCodes.indexOf(code) !== i);
      if (duplicates.length > 0) {
        errors.push(`Duplicate SKU codes: ${[...new Set(duplicates)].join(', ')}`);
      }
    }

    if (errors.length > 0) {
      this.formErrors.set(errors);
      return;
    }
    this.formErrors.set([]);

    const tags = this.tagsInput()
      ? this.tagsInput().split(',').map(t => t.trim()).filter(t => t.length > 0)
      : [];

    if (this.isEditing()) {
      const success = await this.store.updateProduct(
        this.productId()!,
        {
          name: this.name(),
          description: this.description(),
          categoryId: this.categoryId(),
          imageUrl: this.imageUrl() || undefined,
        },
      );
      if (success) {
        this.toast.success('Product updated');
        this.router.navigate(['/seller/products']);
      } else {
        this.toast.error('Failed to update product');
      }
    } else {
      const storeId = this.storeSettingsStore.settings()?.storeId || '';
      const product = await this.store.createProduct({
        name: this.name(),
        description: this.description(),
        brand: this.brand() || undefined,
        categoryId: this.categoryId(),
        storeId,
        tags,
        imageUrl: this.imageUrl() || undefined,
      });

      if (product) {
        // Add each SKU to the newly created product
        const failedSkus: string[] = [];
        for (const skuEntry of this.skus()) {
          const skuRequest: AddSkuRequest = {
            skuCode: skuEntry.skuCode,
            price: skuEntry.price,
            currency: skuEntry.currency,
          };
          const sku = await this.store.addSku(product.id, skuRequest);
          if (!sku) {
            failedSkus.push(skuEntry.skuCode);
          }
        }

        if (failedSkus.length === 0) {
          this.toast.success('Product created');
          this.router.navigate(['/seller/products']);
        } else {
          // Stay on form — show which SKUs failed, user can retry
          this.toast.error(`Failed to add SKUs: ${failedSkus.join(', ')}. Product was created — you can add missing SKUs from the edit page.`);
          this.router.navigate(['/seller/products', product.id, 'edit']);
        }
      } else {
        this.toast.error('Failed to create product');
      }
    }
  }
}
