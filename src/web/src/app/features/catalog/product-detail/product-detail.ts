import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CreateReviewRequest } from '../catalog.models';
import { AuthStore } from '../../../core/auth/auth.store';
import { ReviewStore } from '../review.store';
import { ProductDetailStore } from './product-detail.store';
import { BuyBoxComponent } from '../components/buy-box/buy-box';
import { FrequentlyBoughtTogetherComponent } from '../components/frequently-bought-together/frequently-bought-together';
import { StockIndicatorComponent } from '../../../shared/components/stock-indicator/stock-indicator';
import { ReviewSummaryComponent } from '../components/review-summary/review-summary';
import { ReviewListComponent } from '../components/review-list/review-list';
import { WriteReviewComponent } from '../components/write-review/write-review';

// TODO: Add product variant selector (color, size) when Catalog supports variants.
//       Ref: plans/future_design/product_details.md — "Advanced Product Variations Selector"

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

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.currentProductId = id;
      this.store.loadProduct(id);
      this.loadReviews(id);
    } else {
      // Store has no setError method, but loading will stop
      // and the template handles the null product case
    }
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
}
