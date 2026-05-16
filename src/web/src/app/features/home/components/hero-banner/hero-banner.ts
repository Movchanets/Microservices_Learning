import { Component, ChangeDetectionStrategy, signal, OnInit, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

interface Banner {
  id: number;
  title: string;
  subtitle: string;
  imageUrl: string;
  link: string;
  cta: string;
}

@Component({
  selector: 'app-hero-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <div class="relative w-full h-[400px] md:h-[500px] overflow-hidden rounded-3xl bg-muted/20">
      @for (banner of banners(); track banner.id) {
        <div
          class="absolute inset-0 transition-opacity duration-700"
          [class.opacity-100]="banner.id === currentIndex()"
          [class.opacity-0]="banner.id !== currentIndex()"
        >
          <!-- Background gradient (placeholder for image) -->
          <div
            class="absolute inset-0 bg-gradient-to-r from-primary/20 via-primary/10 to-transparent"
          ></div>

          <!-- Content -->
          <div class="relative h-full flex items-center p-8 md:p-16">
            <div class="max-w-lg">
              <h2 class="text-3xl md:text-5xl font-bold text-foreground font-lexend mb-4 leading-tight">
                {{ banner.title }}
              </h2>
              <p class="text-lg text-muted-foreground mb-6">
                {{ banner.subtitle }}
              </p>
              <a
                [routerLink]="banner.link"
                class="inline-flex items-center gap-2 px-6 py-3 bg-primary text-white font-semibold rounded-xl
                       hover:bg-secondary active:scale-[0.98] transition-all shadow-lg shadow-primary/20"
              >
                {{ banner.cta }}
                <lucide-icon name="ArrowRight" class="w-5 h-5"></lucide-icon>
              </a>
            </div>
          </div>
        </div>
      }

      <!-- Navigation Arrows -->
      <button
        (click)="prev()"
        class="absolute left-4 top-1/2 -translate-y-1/2 p-2 bg-background/60 backdrop-blur-sm
               rounded-full text-foreground hover:bg-background/80 transition-colors"
        aria-label="Previous slide"
      >
        <lucide-icon name="ChevronLeft" class="w-6 h-6"></lucide-icon>
      </button>
      <button
        (click)="next()"
        class="absolute right-4 top-1/2 -translate-y-1/2 p-2 bg-background/60 backdrop-blur-sm
               rounded-full text-foreground hover:bg-background/80 transition-colors"
        aria-label="Next slide"
      >
        <lucide-icon name="ChevronRight" class="w-6 h-6"></lucide-icon>
      </button>

      <!-- Dots -->
      <div class="absolute bottom-4 left-1/2 -translate-x-1/2 flex items-center gap-2">
        @for (banner of banners(); track banner.id) {
          <button
            (click)="goTo(banner.id)"
            [class]="banner.id === currentIndex()
              ? 'w-8 h-2 bg-primary rounded-full transition-all'
              : 'w-2 h-2 bg-foreground/40 rounded-full transition-all'"
            [attr.aria-label]="'Go to slide ' + (banner.id + 1)"
          ></button>
        }
      </div>
    </div>
  `,
})
export class HeroBannerComponent implements OnInit, OnDestroy {
  currentIndex = signal(0);
  private intervalId: ReturnType<typeof setInterval> | null = null;

  banners = signal<Banner[]>([
    {
      id: 0,
      title: 'Summer Collection 2026',
      subtitle: 'Discover the latest trends with up to 40% off on selected items.',
      imageUrl: '',
      link: '/catalog',
      cta: 'Shop Now',
    },
    {
      id: 1,
      title: 'Tech Essentials',
      subtitle: 'Everything you need for your home office setup.',
      imageUrl: '',
      link: '/catalog',
      cta: 'Explore',
    },
    {
      id: 2,
      title: 'Free Shipping on Orders $50+',
      subtitle: 'Limited time offer. No code needed.',
      imageUrl: '',
      link: '/catalog',
      cta: 'Learn More',
    },
  ]);

  ngOnInit(): void {
    this.intervalId = setInterval(() => this.next(), 5000);
  }

  ngOnDestroy(): void {
    if (this.intervalId) clearInterval(this.intervalId);
  }

  next(): void {
    const total = this.banners().length;
    this.currentIndex.update(i => (i + 1) % total);
  }

  prev(): void {
    const total = this.banners().length;
    this.currentIndex.update(i => (i - 1 + total) % total);
  }

  goTo(index: number): void {
    this.currentIndex.set(index);
  }
}
