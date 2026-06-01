import { Component, ChangeDetectionStrategy, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CreateReviewRequest, GalleryItem, Sku } from '../catalog.models';
import { AuthStore } from '../../../core/auth/auth.store';
import { ReviewStore } from '../review.store';
import { ProductDetailStore } from './product-detail.store';
import { BuyBoxComponent } from '../components/buy-box/buy-box';
import { FrequentlyBoughtTogetherComponent } from '../components/frequently-bought-together/frequently-bought-together';
import { StockIndicatorComponent } from '../../../shared/components/stock-indicator/stock-indicator';
import { ReviewSummaryComponent } from '../components/review-summary/review-summary';
import { ReviewListComponent } from '../components/review-list/review-list';
import { WriteReviewComponent } from '../components/write-review/write-review';
import { ImageGalleryComponent } from '../components/image-gallery/image-gallery';
import { VariantPickerComponent } from '../components/variant-picker/variant-picker';

@Component({
  selector: 'app-product-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    LucideAngularModule,
    BuyBoxComponent,
    FrequentlyBoughtTogetherComponent,
    StockIndicatorComponent,
    ReviewSummaryComponent,
    ReviewListComponent,
    WriteReviewComponent,
    ImageGalleryComponent,
    VariantPickerComponent,
  ],
  providers: [ProductDetailStore, ReviewStore],
  templateUrl: './product-detail.html',
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected store = inject(ProductDetailStore);
  protected authStore = inject(AuthStore);
  protected reviewStore = inject(ReviewStore);

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
   * Gallery images for the current view.
   * Prefers the selected SKU's image, falls back to product gallery.
   */
  protected galleryImages = computed<GalleryItem[]>(() => {
    const product = this.store.product();
    return product?.gallery ?? [];
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
      this.loadReviews(id);
    }
  }

  /**
   * Called when the user selects a value in the variant picker.
   * Also handles legacy SKU selector (axisKey='_sku', value=skuId).
   */
  onVariantSelected(event: { axisKey: string; value: string }): void {
    if (event.axisKey === '_sku') {
      // Legacy SKU selector — track selected SKU and reload stock
      this.selectedSkuId.set(event.value);
      const product = this.store.product();
      const sku = product?.skus?.find(s => s.id === event.value);
      if (sku) {
        this.store.loadStock(sku.skuCode);
      }
      return;
    }
    this.store.selectVariant(event.axisKey, event.value);
  }

  private loadReviews(productId: string): void {
    this.reviewStore.loadSummary(productId);
    this.reviewStore.loadReviews(productId);
  }

  onBuyNow(): void {
    this.router.navigate(['/checkout']);
  }

  onSortChange(event: Event): void {
    const sort = (event.target as HTMLSelectElement).value;
    this.reviewStore.setSort(this.currentProductId, sort);
  }

  onFilterByRating(rating: number): void {
    const current = this.reviewStore.ratingFilter();
    this.reviewStore.setRatingFilter(this.currentProductId, current === rating ? null : rating);
  }

  onVote(event: { reviewId: string; isHelpful: boolean }): void {
    this.reviewStore.voteReview(this.currentProductId, event.reviewId, event.isHelpful);
  }

  async onSubmitReview(data: CreateReviewRequest): Promise<void> {
    await this.reviewStore.createReview(this.currentProductId, data);
  }

  /**
   * Converts selected variants record to an iterable array for the template.
   * Skips internal keys like '_sku'.
   */
  protected getVariantEntries(selected: Record<string, string>): Array<{ key: string; value: string }> {
    return Object.entries(selected)
      .filter(([key]) => !key.startsWith('_'))
      .map(([key, value]) => ({ key, value }));
  }
}
