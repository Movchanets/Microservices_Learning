import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Review, ReviewSummary, CreateReviewRequest, PagedResult } from './catalog.models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private http = inject(HttpClient);

  getReviews(
    productId: string,
    page = 1,
    pageSize = 10,
    sort = 'helpful',
    rating?: number,
    photoOnly?: boolean,
  ): Promise<PagedResult<Review>> {
    let params: Record<string, string | number | boolean> = { page, pageSize, sort };
    if (rating) params['rating'] = rating;
    if (photoOnly) params['photoOnly'] = true;

    return firstValueFrom(
      this.http.get<PagedResult<Review>>(
        `/api/catalog/products/${productId}/reviews`,
        { params },
      ),
    );
  }

  getReviewSummary(productId: string): Promise<ReviewSummary> {
    return firstValueFrom(
      this.http.get<ReviewSummary>(`/api/catalog/products/${productId}/reviews/summary`),
    );
  }

  createReview(productId: string, data: CreateReviewRequest): Promise<Review> {
    return firstValueFrom(
      this.http.post<Review>(`/api/catalog/products/${productId}/reviews`, data),
    );
  }

  voteReview(reviewId: string, isHelpful: boolean): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`/api/catalog/products/reviews/${reviewId}/vote`, { isHelpful }),
    );
  }
}
