// Product form component.
// Handles both create and edit modes for seller products.
// Supports per-SKU image galleries with primary image selection.

import { Component, ChangeDetectionStrategy, effect, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { SellerProductStore } from '../seller-product.store';
import { StoreSettingsStore } from '../store-settings.store';
import { CategoryService, CategoryOption } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { MediaService } from '../../../core/services/media.service';
import { GalleryItem } from '../../catalog/catalog.models';
import { ImageGalleryUploaderComponent, PendingImage } from '../../../shared/components/image-gallery-uploader/image-gallery-uploader';

interface SkuFormEntry {
  id: string;
  skuCode: string;
  price: number;
  currency: string;
  images: GalleryItem[];
  pendingUploads: PendingImage[];
}

let skuIdCounter = 0;
function nextSkuId(): string {
  return `sku-${++skuIdCounter}-${Date.now()}`;
}

@Component({
  selector: 'app-product-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule, DecimalPipe, ImageGalleryUploaderComponent],
  templateUrl: './product-form.html',
})
export class ProductFormComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(SellerProductStore);
  private readonly storeSettingsStore = inject(StoreSettingsStore);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);
  private readonly mediaService = inject(MediaService);

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

  // SKU form entries — each SKU has its own image gallery
  skus = signal<SkuFormEntry[]>([{
    id: nextSkuId(), skuCode: '', price: 0, currency: 'USD', images: [], pendingUploads: []
  }]);

  // Product-level images
  productImages = signal<GalleryItem[]>([]);
  productPendingUploads = signal<PendingImage[]>([]);

  // Existing SKUs for edit mode
  existingSkus = signal<{ skuCode: string; price: number; currency: string; images: GalleryItem[] }[]>([]);

  // Global state
  uploading = signal(false);
  uploadError = signal<string | null>(null);
  activeSkuTab = signal<number>(0);

  constructor() {
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
            images: [],
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
    } catch { /* non-critical */ }
  }

  // ── SKU management ────────────────────────────────────────

  addSkuRow(): void {
    this.skus.update(rows => [...rows, {
      id: nextSkuId(), skuCode: '', price: 0, currency: 'USD', images: [], pendingUploads: []
    }]);
    this.activeSkuTab.set(this.skus().length - 1);
  }

  removeSkuRow(index: number): void {
    if (this.skus().length <= 1) return;
    this.skus.update(rows => rows.filter((_, i) => i !== index));
    if (this.activeSkuTab() >= this.skus().length) {
      this.activeSkuTab.set(this.skus().length - 1);
    }
  }

  updateSkuCode(index: number, value: string): void {
    this.skus.update(rows => rows.map((r, i) =>
      i === index ? { ...r, skuCode: ProductFormComponent.sanitizeSku(value) } : r));
  }

  updateSkuPrice(index: number, value: number): void {
    this.skus.update(rows => rows.map((r, i) =>
      i === index ? { ...r, price: value } : r));
  }

  updateSkuCurrency(index: number, value: string): void {
    this.skus.update(rows => rows.map((r, i) =>
      i === index ? { ...r, currency: value } : r));
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
    this.skus.update(rows => rows.map((r, i) =>
      i === index ? { ...r, skuCode } : r));
  }

  private static sanitizeSku(input: string): string {
    return input.toUpperCase()
      .replace(/[^A-Z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .replace(/^-|-$/g, '');
  }

  // ── Image management (shared helpers) ─────────────────────

  ngOnDestroy(): void {
    this.revokeAllPendingUrls(this.productPendingUploads());
    for (const sku of this.skus()) {
      this.revokeAllPendingUrls(sku.pendingUploads);
    }
  }

  private createPendingImages(files: File[]): PendingImage[] {
    return files.map(f => ({
      file: f,
      previewUrl: URL.createObjectURL(f),
      id: `pending-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    }));
  }

  private revokeAllPendingUrls(pending: PendingImage[]): void {
    for (const p of pending) URL.revokeObjectURL(p.previewUrl);
  }

  private removePendingById(pending: PendingImage[], id: string): PendingImage[] {
    const item = pending.find(p => p.id === id);
    if (item) URL.revokeObjectURL(item.previewUrl);
    return pending.filter(p => p.id !== id);
  }

  // Product-level images
  onProductFilesSelected(files: File[]): void {
    this.productPendingUploads.update(p => [...p, ...this.createPendingImages(files)]);
  }

  removeProductUploaded(mediaId: string): void {
    this.mediaService.delete(mediaId).catch(() => {});
    this.productImages.update(imgs => imgs.filter(i => i.id !== mediaId));
  }

  removeProductPending(pendingId: string): void {
    this.productPendingUploads.update(p => this.removePendingById(p, pendingId));
  }

  setProductPrimary(mediaId: string): void {
    const pid = this.productId();
    if (pid) {
      this.mediaService.setPrimary(pid, 'Product', mediaId).catch(() => {});
    }
    this.productImages.update(imgs =>
      imgs.map(i => ({ ...i, isPrimary: i.id === mediaId })));
  }

  // Per-SKU images
  onSkuFilesSelected(skuIndex: number, files: File[]): void {
    const pending = this.createPendingImages(files);
    this.skus.update(rows => rows.map((r, i) =>
      i === skuIndex ? { ...r, pendingUploads: [...r.pendingUploads, ...pending] } : r));
  }

  removeSkuUploaded(skuIndex: number, mediaId: string): void {
    this.mediaService.delete(mediaId).catch(() => {});
    this.skus.update(rows => rows.map((r, i) =>
      i === skuIndex ? { ...r, images: r.images.filter(img => img.id !== mediaId) } : r));
  }

  removeSkuPending(skuIndex: number, pendingId: string): void {
    this.skus.update(rows => rows.map((r, i) =>
      i === skuIndex ? { ...r, pendingUploads: this.removePendingById(r.pendingUploads, pendingId) } : r));
  }

  setSkuPrimary(skuIndex: number, mediaId: string): void {
    this.skus.update(rows => rows.map((r, i) => {
      if (i !== skuIndex) return r;
      return { ...r, images: r.images.map(img => ({ ...img, isPrimary: img.id === mediaId })) };
    }));
  }

  // ── Form submission ───────────────────────────────────────

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.formTouched.set(true);

    const errors: string[] = [];
    if (!this.name().trim()) errors.push('Product name is required.');
    if (!this.categoryId()) errors.push('Category is required.');

    if (!this.isEditing()) {
      const skuEntries = this.skus();
      if (skuEntries.length === 0) errors.push('At least one SKU is required.');
      skuEntries.forEach((sku, i) => {
        if (!sku.skuCode.trim()) errors.push(`SKU #${i + 1}: SKU code is required.`);
        if (!/^[A-Z0-9][A-Z0-9-]*[A-Z0-9]$/i.test(sku.skuCode) && sku.skuCode.length > 0)
          errors.push(`SKU #${i + 1}: SKU must be alphanumeric with hyphens only.`);
        if (sku.price <= 0) errors.push(`SKU #${i + 1}: Price must be greater than zero.`);
      });
      const skuCodes = skuEntries.map(s => s.skuCode.trim().toUpperCase()).filter(Boolean);
      const dupes = skuCodes.filter((c, i) => skuCodes.indexOf(c) !== i);
      if (dupes.length > 0) errors.push(`Duplicate SKU codes: ${[...new Set(dupes)].join(', ')}`);
    }

    if (errors.length > 0) { this.formErrors.set(errors); return; }
    this.formErrors.set([]);

    const tags = this.tagsInput()
      ? this.tagsInput().split(',').map(t => t.trim()).filter(t => t.length > 0)
      : [];

    if (this.isEditing()) {
      const success = await this.store.updateProduct(this.productId()!, {
        name: this.name(), description: this.description(),
        categoryId: this.categoryId(), imageUrl: this.imageUrl() || undefined,
      });
      if (success) {
        this.toast.success('Product updated');
        this.router.navigate(['/seller/products']);
      } else {
        this.toast.error('Failed to update product');
      }
    } else {
      const storeId = this.storeSettingsStore.settings()?.storeId || '';
      const product = await this.store.createProduct({
        name: this.name(), description: this.description(),
        brand: this.brand() || undefined, categoryId: this.categoryId(),
        storeId, tags, imageUrl: this.imageUrl() || undefined,
      });

      if (!product) {
        this.toast.error('Failed to create product');
        return;
      }

      // Upload product-level images
      await this.uploadPendingImages(this.productPendingUploads(), product.id, 'Product');
      this.productPendingUploads.set([]);

      // Add SKUs and upload per-SKU images
      const failedSkus: string[] = [];
      for (let i = 0; i < this.skus().length; i++) {
        const skuEntry = this.skus()[i];
        const sku = await this.store.addSku(product.id, {
          skuCode: skuEntry.skuCode,
          price: skuEntry.price,
          currency: skuEntry.currency,
        });

        if (sku) {
          // Upload SKU-level images
          await this.uploadPendingImages(skuEntry.pendingUploads, sku.id, 'SKU');
          this.skus.update(rows => rows.map((r, idx) =>
            idx === i ? { ...r, pendingUploads: [] } : r));
        } else {
          failedSkus.push(skuEntry.skuCode);
        }
      }

      if (failedSkus.length === 0) {
        this.toast.success('Product created');
        this.router.navigate(['/seller/products']);
      } else {
        this.toast.error(`Failed to add SKUs: ${failedSkus.join(', ')}. Product was created.`);
        this.router.navigate(['/seller/products', product.id, 'edit']);
      }
    }
  }

  private async uploadPendingImages(
    pending: PendingImage[],
    targetId: string,
    targetType: 'Product' | 'SKU',
  ): Promise<void> {
    for (let i = 0; i < pending.length; i++) {
      try {
        await this.mediaService.upload(pending[i].file, targetId, targetType, i === 0);
      } catch {
        // Non-fatal
      }
    }
  }
}
