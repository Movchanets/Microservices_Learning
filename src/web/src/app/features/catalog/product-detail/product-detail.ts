import { Component, ChangeDetectionStrategy, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { Sku } from '../catalog.models';
import { AuthStore } from '../../../core/auth/auth.store';
import { ProductDetailStore } from './product-detail.store';
import { BuyBoxComponent } from '../components/buy-box/buy-box';
import { StockIndicatorComponent } from '../../../shared/components/stock-indicator/stock-indicator';
import { ImageGalleryComponent } from '../components/image-gallery/image-gallery';
import { VariantPickerComponent } from '../components/variant-picker/variant-picker';

@Component({
  selector: 'app-product-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    LucideAngularModule,
    BuyBoxComponent,
    StockIndicatorComponent,
    ImageGalleryComponent,
    VariantPickerComponent,
  ],
  providers: [ProductDetailStore],
  templateUrl: './product-detail.html',
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected store = inject(ProductDetailStore);
  protected authStore = inject(AuthStore);

  private currentProductId = '';

  /**
   * Tracks the selected SKU ID for legacy (non-variant-matrix) products.
   * Set when user clicks a SKU button in the flat list.
   */
  private selectedSkuId = signal<string | null>(null);

  /**
   * The currently selected SKU.
   * Uses store's selectedVariantSku for variant-matrix products,
   * falls back to selectedSkuId / first SKU for legacy products.
   */
  protected selectedSku = computed<Sku | null>(() => {
    // Variant picker active — delegate to store
    if (this.store.hasVariantPicker()) {
      return this.store.selectedVariantSku();
    }

    // Legacy: use selectedSkuId or first SKU
    const product = this.store.product();
    if (!product?.skus?.length) return null;
    const selectedId = this.selectedSkuId();
    if (selectedId) {
      return product.skus.find(s => s.id === selectedId) ?? product.skus[0] ?? null;
    }
    return product.skus[0] ?? null;
  });

  protected hasMultipleSkus = computed(() => {
    const product = this.store.product();
    return (product?.skus?.length ?? 0) > 1;
  });

  /**
   * Variant breadcrumb text, e.g. "Gold · 512GB".
   * Derived from selected variant entries.
   */
  protected variantBreadcrumbText = computed(() => {
    const selected = this.store.selectedVariants();
    const entries = Object.entries(selected).filter(([key]) => !key.startsWith('_'));
    if (entries.length === 0) return null;
    return entries.map(([, value]) => value).join(' · ');
  });

  protected fallbackImageUrl = computed(() => {
    const product = this.store.product();
    const sku = this.selectedSku();
    return sku?.imageUrl ?? product?.imageUrl ?? null;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.currentProductId = id;
      this.store.loadProduct(id);
      this.store.loadVariantMatrix(id);
    }
  }

  /**
   * Called when the user selects a value in the variant picker.
   * Also handles legacy SKU selector (axisKey='_sku', value=skuId).
   */
  onVariantSelected(event: { axisKey: string; value: string }): void {
    if (event.axisKey === '_sku') {
      // Legacy SKU selector — track selected SKU, reload stock + gallery
      this.selectedSkuId.set(event.value);
      const product = this.store.product();
      const sku = product?.skus?.find(s => s.id === event.value);
      if (sku) {
        this.store.loadStock(sku.skuCode);
        this.store.loadSkuGallery(sku.id);
      }
      return;
    }
    this.store.selectVariant(event.axisKey, event.value);
  }

  onBuyNow(): void {
    this.router.navigate(['/checkout']);
  }
}
