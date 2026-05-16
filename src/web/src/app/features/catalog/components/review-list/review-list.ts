import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { Review } from '../../catalog.models';

@Component({
  selector: 'app-review-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LucideAngularModule],
  template: `
    <div class="flex flex-col gap-6">
      @for (review of reviews(); track review.id) {
        <div class="p-6 bg-card/40 backdrop-blur-sm border border-border rounded-2xl">
          <!-- Header -->
          <div class="flex items-start justify-between mb-3">
            <div class="flex flex-col gap-1">
              <div class="flex items-center gap-2">
                <span class="font-semibold text-foreground">{{ review.userName }}</span>
                @if (review.isVerifiedPurchase) {
                  <span class="px-2 py-0.5 bg-green-500/10 text-green-500 text-xs font-medium rounded-full border border-green-500/20">
                    Verified Purchase
                  </span>
                }
              </div>
              <div class="flex items-center gap-1">
                @for (star of getStars(review.rating); track $index) {
                  <lucide-icon
                    name="Star"
                    [class]="star ? 'w-4 h-4 text-yellow-400 fill-yellow-400' : 'w-4 h-4 text-muted'"
                  ></lucide-icon>
                }
                <span class="text-xs text-muted-foreground ml-2">
                  {{ review.createdAt | date:'mediumDate' }}
                </span>
              </div>
            </div>
          </div>

          <!-- Title & Text -->
          <h4 class="font-semibold text-foreground mb-2">{{ review.title }}</h4>
          <p class="text-muted-foreground leading-relaxed mb-4">{{ review.text }}</p>

          <!-- Photos -->
          @if (review.photoUrls.length > 0) {
            <div class="flex gap-2 mb-4 overflow-x-auto">
              @for (photo of review.photoUrls; track photo) {
                <img
                  [src]="photo"
                  alt="Review photo"
                  class="w-20 h-20 object-cover rounded-lg border border-border"
                />
              }
            </div>
          }

          <!-- Seller Response -->
          @if (review.sellerResponse) {
            <div class="mt-4 p-4 bg-muted/10 border-l-4 border-primary rounded-r-lg">
              <div class="flex items-center gap-2 mb-1">
                <lucide-icon name="Store" class="w-4 h-4 text-primary"></lucide-icon>
                <span class="text-sm font-semibold text-foreground">Seller Response</span>
                @if (review.sellerResponseDate) {
                  <span class="text-xs text-muted-foreground">
                    {{ review.sellerResponseDate | date:'mediumDate' }}
                  </span>
                }
              </div>
              <p class="text-sm text-muted-foreground">{{ review.sellerResponse }}</p>
            </div>
          }

          <!-- Helpful Votes -->
          <div class="flex items-center gap-4 mt-4 pt-4 border-t border-border">
            <span class="text-sm text-muted-foreground">Was this helpful?</span>
            <button
              (click)="vote.emit({ reviewId: review.id, isHelpful: true })"
              class="flex items-center gap-1 text-sm text-muted-foreground hover:text-green-500 transition-colors"
            >
              <lucide-icon name="ThumbsUp" class="w-4 h-4"></lucide-icon>
              {{ review.helpfulCount }}
            </button>
            <button
              (click)="vote.emit({ reviewId: review.id, isHelpful: false })"
              class="flex items-center gap-1 text-sm text-muted-foreground hover:text-red-500 transition-colors"
            >
              <lucide-icon name="ThumbsDown" class="w-4 h-4"></lucide-icon>
              {{ review.notHelpfulCount }}
            </button>
          </div>
        </div>
      } @empty {
        <div class="py-12 text-center text-muted-foreground">
          <lucide-icon name="MessageSquare" class="w-12 h-12 mx-auto mb-3 opacity-30"></lucide-icon>
          <p>No reviews yet. Be the first to review this product!</p>
        </div>
      }
    </div>
  `,
})
export class ReviewListComponent {
  reviews = input.required<Review[]>();
  vote = output<{ reviewId: string; isHelpful: boolean }>();

  protected getStars(rating: number): boolean[] {
    return Array.from({ length: 5 }, (_, i) => i < rating);
  }
}
