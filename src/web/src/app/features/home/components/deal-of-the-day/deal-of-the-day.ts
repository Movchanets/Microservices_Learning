import { Component, ChangeDetectionStrategy, input, signal, OnInit, OnDestroy, computed } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ProductListItem } from '../../../catalog/catalog.models';

@Component({
  selector: 'app-deal-of-the-day',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, RouterLink, LucideAngularModule],
  template: `
    @if (product(); as p) {
      <div class="p-6 md:p-8 bg-gradient-to-r from-red-500/10 via-orange-500/10 to-yellow-500/10
                  border border-red-500/20 rounded-2xl">
        <div class="flex flex-col md:flex-row items-center gap-6">
          <!-- Product Info -->
          <div class="flex-1 text-center md:text-left">
            <div class="flex items-center gap-2 justify-center md:justify-start mb-2">
              <lucide-icon name="Zap" class="w-5 h-5 text-orange-500"></lucide-icon>
              <span class="text-sm font-bold text-orange-500 uppercase tracking-wider">Deal of the Day</span>
            </div>
            <h3 class="text-xl font-bold text-foreground font-lexend mb-2">{{ p.name }}</h3>
            <div class="flex items-center gap-3 justify-center md:justify-start mb-4">
              <span class="text-2xl font-bold text-foreground">
                {{ p.price | currency: p.currency : 'symbol' : '1.2-2' }}
              </span>
              <span class="text-lg text-muted-foreground line-through">
                {{ originalPrice() | currency: p.currency : 'symbol' : '1.2-2' }}
              </span>
              <span class="px-2 py-0.5 bg-red-500/20 text-red-400 text-sm font-bold rounded">
                -{{ discountPercent() }}%
              </span>
            </div>

            <!-- Countdown -->
            <div class="flex items-center gap-3 justify-center md:justify-start mb-4">
              <span class="text-sm text-muted-foreground">Ends in:</span>
              <div class="flex items-center gap-1">
                <span class="px-2 py-1 bg-foreground/10 rounded text-sm font-mono font-bold text-foreground">
                  {{ hours() }}
                </span>
                <span class="text-muted-foreground">:</span>
                <span class="px-2 py-1 bg-foreground/10 rounded text-sm font-mono font-bold text-foreground">
                  {{ minutes() }}
                </span>
                <span class="text-muted-foreground">:</span>
                <span class="px-2 py-1 bg-foreground/10 rounded text-sm font-mono font-bold text-foreground">
                  {{ seconds() }}
                </span>
              </div>
            </div>

            <!-- Progress Bar -->
            <div class="mb-4">
              <div class="flex items-center justify-between text-xs text-muted-foreground mb-1">
                <span>{{ claimedPercent() }}% claimed</span>
                <span>Limited stock</span>
              </div>
              <div class="h-2 bg-muted/20 rounded-full overflow-hidden">
                <div
                  class="h-full bg-gradient-to-r from-red-500 to-orange-500 rounded-full transition-all"
                  [style.width.%]="claimedPercent()"
                ></div>
              </div>
            </div>

            <a
              [routerLink]="['/catalog', p.id]"
              class="inline-flex items-center gap-2 px-6 py-3 bg-orange-500 text-white font-semibold
                     rounded-xl hover:bg-orange-600 active:scale-[0.98] transition-all"
            >
              <lucide-icon name="Zap" class="w-5 h-5"></lucide-icon>
              View Deal
            </a>
          </div>

          <!-- Product Image -->
          <div class="flex-none w-48 h-48 flex items-center justify-center bg-muted/10 rounded-2xl">
            @if (p.imageUrl) {
              <img [src]="p.imageUrl" [alt]="p.name" class="w-full h-full object-cover rounded-2xl" />
            } @else {
              <lucide-icon name="Package" class="w-16 h-16 text-muted opacity-30"></lucide-icon>
            }
          </div>
        </div>
      </div>
    }
  `,
})
export class DealOfTheDayComponent implements OnInit, OnDestroy {
  product = input<ProductListItem | null>(null);

  private intervalId: ReturnType<typeof setInterval> | null = null;
  private endTime = new Date();
  timeLeft = signal({ hours: 0, minutes: 0, seconds: 0 });

  hours = computed(() => String(this.timeLeft().hours).padStart(2, '0'));
  minutes = computed(() => String(this.timeLeft().minutes).padStart(2, '0'));
  seconds = computed(() => String(this.timeLeft().seconds).padStart(2, '0'));

  originalPrice = computed(() => {
    const p = this.product();
    return p ? p.price * 1.3 : 0; // 30% markup for "original" price
  });

  discountPercent = computed(() => {
    const p = this.product();
    if (!p) return 0;
    return Math.round(((this.originalPrice() - p.price) / this.originalPrice()) * 100);
  });

  claimedPercent = signal(67); // Mock percentage

  ngOnInit(): void {
    // Set deal to end at midnight
    this.endTime = new Date();
    this.endTime.setHours(23, 59, 59, 999);
    this.updateTime();
    this.intervalId = setInterval(() => this.updateTime(), 1000);
  }

  ngOnDestroy(): void {
    if (this.intervalId) clearInterval(this.intervalId);
  }

  private updateTime(): void {
    const now = new Date();
    const diff = Math.max(0, this.endTime.getTime() - now.getTime());
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);
    this.timeLeft.set({ hours, minutes, seconds });
  }
}
