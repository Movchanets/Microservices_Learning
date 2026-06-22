/**
 * Product form component for seller dashboard.
 *
 * Handles both create and edit modes for seller products.
 * Supports per-SKU image galleries with primary image selection.
 * Integrates VariantMatrixBuilder for bulk variant generation.
 *
 * Architecture:
 *   - Form state is managed via Angular signals (not Reactive Forms)
 *   - SKU rows are mutable signal arrays with per-entry image galleries
 *   - Image uploads happen after product/SKU creation (sequential, not parallel)
 *   - Variant matrix builder generates Cartesian product of variant axes
 */

import { Component, ChangeDetectionStrategy, effect, inject, OnInit, OnDestroy, signal, computed, viewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { SellerProductStore } from '../seller-product.store';
import { StoreSettingsStore } from '../store-settings.store';
import { CategoryService, CategoryOption, AttributeDefinition } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { MediaService } from '../../../core/services/media.service';
import { GalleryItem } from '../../catalog/catalog.models';
import { ImageGalleryUploaderComponent, PendingImage } from '../../../shared/components/image-gallery-uploader/image-gallery-uploader';
import { VariantMatrixBuilderComponent, VariantMatrixOutput } from '../variant-matrix-builder/variant-matrix-builder';

// ── Types ──────────────────────────────────────────────────

interface SkuFormEntry {
  id: string;
  /** Real server-side SKU ID (null for new SKUs not yet created). */
  serverSkuId: string | null;
  skuCode: string;
  price: number;
  currency: string;
  images: GalleryItem[];
  pendingUploads: PendingImage[];
  typedAttributes: Record<string, string>;
}

// ── SKU ID generator ───────────────────────────────────────

let skuIdCounter = 0;
function nextSkuId(): string {
  return `sku-${++skuIdCounter}-${Date.now()}`;
}

// ── Component ──────────────────────────────────────────────

@Component({
  selector: 'app-product-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, LucideAngularModule,
    ImageGalleryUploaderComponent, VariantMatrixBuilderComponent,
  ],
  templateUrl: './product-form.html',
})
export class ProductFormComponent implements OnInit, OnDestroy {
  // ── Injected services ────────────────────────────────────
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(SellerProductStore);
  private readonly storeSettingsStore = inject(StoreSettingsStore);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);
  private readonly mediaService = inject(MediaService);

  // ── Child references ─────────────────────────────────────
  variantMatrixBuilder = viewChild(VariantMatrixBuilderComponent);

  // ── Form state ───────────────────────────────────────────
  isEditing = signal(false);
  productId = signal<string | null>(null);
  formPopulated = signal(false);
  formTouched = signal(false);
  formErrors = signal<string[]>([]);

  // ── Product fields ───────────────────────────────────────
  name = signal('');
  description = signal('');
  brand = signal('');
  categoryId = signal('');
  imageUrl = signal('');
  tagsInput = signal('');
  categories = signal<CategoryOption[]>([]);

  // ── Product-level images ─────────────────────────────────
  productImages = signal<GalleryItem[]>([]);
  productPendingUploads = signal<PendingImage[]>([]);

  // ── SKU form entries ─────────────────────────────────────
  skus = signal<SkuFormEntry[]>([this.createEmptySku()]);
  activeSkuTab = signal(0);

  // ── Category variant axes ────────────────────────────────
  variantAxes = signal<AttributeDefinition[]>([]);
  variantAxesLoading = signal(false);

  // ── Bulk variant generation ──────────────────────────────
  showVariantMatrix = signal(false);
  bulkGenerating = signal(false);

  // ── Upload state ─────────────────────────────────────────
  uploading = signal(false);
  uploadError = signal<string | null>(null);
  removingSku = signal(false);

  /** Whether the active SKU tab can be removed (min 1 required). */
  canRemoveSku = computed(() => this.skus().length > 1);

  /** SKU code prefix for bulk generation (derived from product name). */
  skuCodePrefix = computed(() => {
    const words = this.name()
      .replace(/[^a-zA-Z0-9\s]/g, '')
      .split(/\s+/)
      .filter(w => w.length > 0)
      .slice(0, 3)
      .map(w => w.toUpperCase());
    return words.join('-') || 'SKU';
  });

  // ── Lifecycle ────────────────────────────────────────────

  constructor() {
    // Populate form when editing — react to store.selectedProduct changes
    effect(() => {
      const product = this.store.selectedProduct();
      if (product && this.isEditing() && !this.formPopulated()) {
        this.populateFormFromProduct(product);
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

  ngOnDestroy(): void {
    this.revokeAllUrls(this.productPendingUploads());
    for (const sku of this.skus()) {
      this.revokeAllUrls(sku.pendingUploads);
    }
  }

  // ── SKU management ───────────────────────────────────────

  addSkuRow(): void {
    this.skus.update(rows => [...rows, this.createEmptySku()]);
    this.activeSkuTab.set(this.skus().length - 1);
  }

  async removeSkuRow(index: number): Promise<void> {
    if (!this.canRemoveSku()) return;
    const sku = this.skus()[index];

    // If the SKU has a server ID, delete it via API first
    if (sku.serverSkuId && this.productId()) {
      this.removingSku.set(true);
      try {
        const success = await this.store.removeSku(this.productId()!, sku.serverSkuId);
        if (!success) return; // Don't remove from UI if API failed
      } finally {
        this.removingSku.set(false);
      }
    }

    this.skus.update(rows => rows.filter((_, i) => i !== index));
    if (this.activeSkuTab() >= this.skus().length) {
      this.activeSkuTab.set(this.skus().length - 1);
    }
  }

  /** Generic SKU field updater. */
  updateSku<K extends keyof SkuFormEntry>(index: number, field: K, value: SkuFormEntry[K]): void {
    this.skus.update(rows => rows.map((r, i) =>
      i === index ? { ...r, [field]: value } : r
    ));
  }

  /** Update a typed attribute on a SKU entry. */
  updateSkuAttribute(skuIndex: number, key: string, value: string): void {
    this.skus.update(rows => rows.map((r, i) => {
      if (i !== skuIndex) return r;
      return { ...r, typedAttributes: { ...r.typedAttributes, [key]: value } };
    }));
  }

  updateSkuCode(index: number, value: string): void {
    this.updateSku(index, 'skuCode', ProductFormComponent.sanitizeSku(value));
  }

  updateSkuPrice(index: number, value: number): void {
    this.updateSku(index, 'price', value);
  }

  updateSkuCurrency(index: number, value: string): void {
    this.updateSku(index, 'currency', value);
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
    this.updateSku(index, 'skuCode', `${words.join('-')}-${randomSuffix}`);
  }

  // ── Variant matrix ───────────────────────────────────────

  toggleVariantMatrix(): void {
    this.showVariantMatrix.update(v => !v);
  }

  /** Called when the variant matrix builder confirms selections. */
  async onVariantMatrixConfirm(): Promise<void> {
    const builder = this.variantMatrixBuilder();
    if (!builder) return;

    const selection = builder.getSelection();
    if (selection.combinationCount === 0) {
      this.toast.error('Select at least one variant combination');
      return;
    }

    const productId = this.productId();

    // If product exists, bulk-add SKUs directly
    if (productId) {
      await this.bulkAddSkusToProduct(productId, selection);
      return;
    }

    // If creating, store the combinations and let the form submit handle it
    // The combinations will be applied after product creation
    this.pendingBulkSelection.set(selection);
    this.toast.success(`${selection.combinationCount} variants will be created`);
    this.showVariantMatrix.set(false);
  }

  /** Store pending bulk selection for use during create. */
  pendingBulkSelection = signal<VariantMatrixOutput | null>(null);

  private async bulkAddSkusToProduct(productId: string, selection: VariantMatrixOutput): Promise<void> {
    this.bulkGenerating.set(true);
    try {
      const result = await this.store.bulkAddSku(productId, {
        variantCombinations: selection.variantCombinations,
        excludedCombinations: selection.excludedCombinations,
        currency: 'USD',
        skuCodePrefix: this.skuCodePrefix(),
      });

      if (result) {
        this.toast.success(`${result.createdCount} variants created`);
        // Reload product to get updated SKUs
        await this.store.loadProductById(productId);
        this.showVariantMatrix.set(false);
      } else {
        this.toast.error('Failed to generate variants');
      }
    } finally {
      this.bulkGenerating.set(false);
    }
  }

  // ── Product-level image handlers ─────────────────────────

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

  // ── Per-SKU image handlers ───────────────────────────────

  onSkuFilesSelected(skuIndex: number, files: File[]): void {
    const pending = this.createPendingImages(files);
    this.skus.update(rows => rows.map((r, i) =>
      i === skuIndex ? { ...r, pendingUploads: [...r.pendingUploads, ...pending] } : r
    ));
  }

  removeSkuUploaded(skuIndex: number, mediaId: string): void {
    this.mediaService.delete(mediaId).catch(() => {});
    this.skus.update(rows => rows.map((r, i) =>
      i === skuIndex ? { ...r, images: r.images.filter(img => img.id !== mediaId) } : r
    ));
  }

  removeSkuPending(skuIndex: number, pendingId: string): void {
    this.skus.update(rows => rows.map((r, i) =>
      i === skuIndex ? { ...r, pendingUploads: this.removePendingById(r.pendingUploads, pendingId) } : r
    ));
  }

  setSkuPrimary(skuIndex: number, mediaId: string): void {
    this.skus.update(rows => rows.map((r, i) => {
      if (i !== skuIndex) return r;
      return { ...r, images: r.images.map(img => ({ ...img, isPrimary: img.id === mediaId })) };
    }));
  }

  /** CSS class for a SKU tab button (active vs inactive). */
  skuTabClass(index: number): string {
    return index === this.activeSkuTab()
      ? 'px-4 py-2 bg-primary text-white rounded-lg text-sm font-medium whitespace-nowrap cursor-pointer'
      : 'px-4 py-2 bg-muted/10 text-muted-foreground rounded-lg text-sm font-medium hover:bg-muted/20 whitespace-nowrap cursor-pointer';
  }

  // ── Form submission ──────────────────────────────────────

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.formTouched.set(true);

    const errors = this.validateForm();
    if (errors.length > 0) {
      this.formErrors.set(errors);
      return;
    }
    this.formErrors.set([]);

    if (this.isEditing()) {
      await this.updateExistingProduct();
    } else {
      await this.createNewProduct();
    }
  }

  // ── Private helpers ──────────────────────────────────────

  private createEmptySku(): SkuFormEntry {
    return {
      id: nextSkuId(),
      serverSkuId: null,
      skuCode: '',
      price: 0,
      currency: 'USD',
      images: [],
      pendingUploads: [],
      typedAttributes: {},
    };
  }

  private populateFormFromProduct(product: {
    name: string;
    description: string;
    brand?: string | null;
    categoryId: string;
    imageUrl?: string | null;
    tags?: string[] | null;
    skus?: Array<{
      id: string;
      skuCode: string;
      price: number;
      currency: string;
      typedAttributes?: Record<string, string>;
    }> | null;
  }): void {
    this.name.set(product.name);
    this.description.set(product.description);
    this.brand.set(product.brand ?? '');
    this.categoryId.set(product.categoryId);
    this.imageUrl.set(product.imageUrl ?? '');
    this.tagsInput.set(product.tags?.join(', ') ?? '');

    // Populate SKU entries from existing SKUs
    if (product.skus && product.skus.length > 0) {
      this.skus.set(product.skus.map(s => ({
        id: nextSkuId(),
        serverSkuId: s.id,
        skuCode: s.skuCode,
        price: s.price,
        currency: s.currency,
        images: [],
        pendingUploads: [],
        typedAttributes: s.typedAttributes ?? {},
      })));
    }

    this.formPopulated.set(true);

    // Load variant axes for the product's category
    this.loadVariantAxes(product.categoryId);

    // Load galleries (product-level + per-SKU)
    this.loadGalleries(product);
  }

  /** Load product-level and per-SKU galleries from Media API. */
  private async loadGalleries(product: {
    id?: string;
    skus?: Array<{ id: string }> | null;
  }): Promise<void> {
    const productId = product.id ?? this.productId();
    if (!productId) return;

    // Load product-level gallery
    try {
      const productGallery = await this.mediaService.getGallery(productId, 'Product');
      this.productImages.set(productGallery);
    } catch { /* non-critical */ }

    // Load per-SKU galleries in parallel
    if (product.skus && product.skus.length > 0) {
      const galleries = await Promise.all(
        product.skus.map(sku =>
          this.mediaService.getGallery(sku.id, 'SKU').catch(() => [] as GalleryItem[])
        )
      );
      this.skus.update(rows => rows.map((r, i) => ({
        ...r,
        images: galleries[i] ?? [],
      })));
    }
  }

  private async loadCategories(): Promise<void> {
    try {
      const cats = await this.categoryService.getCategories();
      this.categories.set(cats.filter(c => c.isActive));
    } catch { /* non-critical */ }
  }

  /** Loads variant-axis attribute definitions for the selected category. */
  async loadVariantAxes(categoryId: string): Promise<void> {
    if (!categoryId) {
      this.variantAxes.set([]);
      return;
    }
    this.variantAxesLoading.set(true);
    try {
      const attrs = await this.categoryService.getAttributeDefinitions(categoryId, true);
      this.variantAxes.set(attrs.filter(a => a.isVariantAxis && a.target === 'Sku'));
    } catch {
      this.variantAxes.set([]);
    } finally {
      this.variantAxesLoading.set(false);
    }
  }

  private static sanitizeSku(input: string): string {
    return input.toUpperCase()
      .replace(/[^A-Z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .replace(/^-|-$/g, '');
  }

  /** Validate form fields. Returns array of error messages (empty = valid). */
  private validateForm(): string[] {
    const errors: string[] = [];

    if (!this.name().trim()) errors.push('Product name is required.');
    if (!this.categoryId()) errors.push('Category is required.');

    // SKU validation
    const skuEntries = this.skus();
    if (skuEntries.length === 0 && !this.pendingBulkSelection()) {
      errors.push('At least one SKU is required.');
    }

    skuEntries.forEach((sku, i) => {
      const label = `SKU #${i + 1}`;
      if (!sku.skuCode.trim()) errors.push(`${label}: SKU code is required.`);
      if (sku.skuCode.length > 0 && !/^[A-Z0-9][A-Z0-9-]*[A-Z0-9]$/i.test(sku.skuCode))
        errors.push(`${label}: SKU must be alphanumeric with hyphens only.`);
      if (sku.price <= 0) errors.push(`${label}: Price must be greater than zero.`);
    });

    // Duplicate check
    const skuCodes = skuEntries.map(s => s.skuCode.trim().toUpperCase()).filter(Boolean);
    const dupes = skuCodes.filter((c, i) => skuCodes.indexOf(c) !== i);
    if (dupes.length > 0) errors.push(`Duplicate SKU codes: ${[...new Set(dupes)].join(', ')}`);

    return errors;
  }

  /** Update an existing product (edit mode). */
  private async updateExistingProduct(): Promise<void> {
    const success = await this.store.updateProduct(this.productId()!, {
      name: this.name(),
      description: this.description(),
      categoryId: this.categoryId(),
      brand: this.brand() || undefined,
      tags: this.tagsInput()
        ? this.tagsInput().split(',').map(t => t.trim()).filter(t => t.length > 0)
        : undefined,
      imageUrl: this.imageUrl() || undefined,
    });

    if (success) {
      // Upload any pending product images
      await this.uploadPendingImages(this.productPendingUploads(), this.productId()!, 'Product');
      this.productPendingUploads.set([]);

      // Upload pending SKU images
      for (let i = 0; i < this.skus().length; i++) {
        const sku = this.skus()[i];
        if (sku.pendingUploads.length > 0 && sku.serverSkuId) {
          await this.uploadPendingImages(sku.pendingUploads, sku.serverSkuId, 'SKU', this.productId()!);
          this.skus.update(rows => rows.map((r, idx) =>
            idx === i ? { ...r, pendingUploads: [] } : r
          ));
        }
      }

      // Check if we have a pending bulk variant selection
      const bulkSelection = this.pendingBulkSelection();
      if (bulkSelection) {
        await this.bulkAddSkusToProduct(this.productId()!, bulkSelection);
        this.pendingBulkSelection.set(null);
      }

      // Add any new SKUs (without serverSkuId)
      const newSkus = this.skus().filter(s => !s.serverSkuId);
      if (newSkus.length > 0) {
        await this.createSkusForProduct(this.productId()!, newSkus);
      }

      this.toast.success('Product updated');
      this.router.navigate(['/seller/products']);
    } else {
      this.toast.error('Failed to update product');
    }
  }

  /** Create a new product with SKUs and images (create mode). */
  private async createNewProduct(): Promise<void> {
    const storeId = this.storeSettingsStore.settings()?.storeId || '';
    const tags = this.tagsInput()
      ? this.tagsInput().split(',').map(t => t.trim()).filter(t => t.length > 0)
      : [];

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

    // Check if we have a pending bulk selection
    const bulkSelection = this.pendingBulkSelection();
    if (bulkSelection) {
      await this.bulkAddSkusToProduct(product.id, bulkSelection);
      this.pendingBulkSelection.set(null);
    } else {
      // Add individual SKUs
      await this.createSkusForProduct(product.id, this.skus());
    }
  }

  /** Add each SKU to the product and upload its images. */
  private async createSkusForProduct(productId: string, skuEntries: SkuFormEntry[]): Promise<void> {
    const failedSkus: string[] = [];

    for (let i = 0; i < skuEntries.length; i++) {
      const entry = skuEntries[i];
      const sku = await this.store.addSku(productId, {
        skuCode: entry.skuCode,
        price: entry.price,
        currency: entry.currency,
        typedAttributes: Object.keys(entry.typedAttributes).length > 0
          ? entry.typedAttributes
          : undefined,
      });

      if (sku) {
        await this.uploadPendingImages(entry.pendingUploads, sku.id, 'SKU', productId);
      } else {
        failedSkus.push(entry.skuCode);
      }
    }

    if (failedSkus.length === 0) {
      this.toast.success('Product created');
      this.router.navigate(['/seller/products']);
    } else {
      this.toast.error(`Failed to add SKUs: ${failedSkus.join(', ')}. Product was created.`);
      this.router.navigate(['/seller/products', productId, 'edit']);
    }
  }

  // ── Image utilities ──────────────────────────────────────

  private createPendingImages(files: File[]): PendingImage[] {
    return files.map(f => ({
      file: f,
      previewUrl: URL.createObjectURL(f),
      id: `pending-${Date.now()}-${Math.random().toString(36).slice(2)}`,
    }));
  }

  private revokeAllUrls(pending: PendingImage[]): void {
    for (const p of pending) URL.revokeObjectURL(p.previewUrl);
  }

  private removePendingById(pending: PendingImage[], id: string): PendingImage[] {
    const item = pending.find(p => p.id === id);
    if (item) URL.revokeObjectURL(item.previewUrl);
    return pending.filter(p => p.id !== id);
  }

  private async uploadPendingImages(
    pending: PendingImage[],
    targetId: string,
    targetType: 'Product' | 'SKU',
    linkedProductId?: string,
  ): Promise<void> {
    for (let i = 0; i < pending.length; i++) {
      try {
        await this.mediaService.upload(pending[i].file, targetId, targetType, i === 0, linkedProductId);
      } catch {
        // Non-fatal — continue with remaining images
      }
    }
  }
}
