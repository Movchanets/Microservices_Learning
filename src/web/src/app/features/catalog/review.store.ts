import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { ReviewService } from './review.service';
import { Review, ReviewSummary, CreateReviewRequest } from './catalog.models';

interface ReviewState {
  reviews: Review[];
  summary: ReviewSummary | null;
  totalCount: number;
  page: number;
  pageSize: number;
  sort: string;
  ratingFilter: number | null;
  loading: boolean;
  submitting: boolean;
  error: string | null;
}

const initialState: ReviewState = {
  reviews: [],
  summary: null,
  totalCount: 0,
  page: 1,
  pageSize: 10,
  sort: 'helpful',
  ratingFilter: null,
  loading: false,
  submitting: false,
  error: null,
};

export const ReviewStore = signalStore(
  withState<ReviewState>(initialState),
  withMethods((store, reviewService = inject(ReviewService)) => {
    return {
      async loadReviews(productId: string): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          const result = await reviewService.getReviews(
            productId,
            store.page(),
            store.pageSize(),
            store.sort(),
            store.ratingFilter() ?? undefined,
          );
          patchState(store, {
            reviews: result.items,
            totalCount: result.totalCount,
            loading: false,
          });
        } catch {
          patchState(store, { error: 'Failed to load reviews', loading: false });
        }
      },

      async loadSummary(productId: string): Promise<void> {
        try {
          const summary = await reviewService.getReviewSummary(productId);
          patchState(store, { summary });
        } catch {
          // Non-critical; silently fail
        }
      },

      async createReview(productId: string, data: CreateReviewRequest): Promise<boolean> {
        patchState(store, { submitting: true, error: null });
        try {
          await reviewService.createReview(productId, data);
          patchState(store, { submitting: false, page: 1 });
          // Reload reviews and summary
          await this.loadReviews(productId);
          await this.loadSummary(productId);
          return true;
        } catch {
          patchState(store, { submitting: false, error: 'Failed to submit review' });
          return false;
        }
      },

      async voteReview(productId: string, reviewId: string, isHelpful: boolean): Promise<void> {
        try {
          await reviewService.voteReview(reviewId, isHelpful);
          // Reload to get updated counts
          await this.loadReviews(productId);
        } catch {
          // Non-critical; silently fail
        }
      },

      setSort(productId: string, sort: string): void {
        patchState(store, { sort, page: 1 });
        this.loadReviews(productId);
      },

      setRatingFilter(productId: string, rating: number | null): void {
        patchState(store, { ratingFilter: rating, page: 1 });
        this.loadReviews(productId);
      },

      goToPage(productId: string, page: number): void {
        patchState(store, { page });
        this.loadReviews(productId);
      },
    };
  }),
);
