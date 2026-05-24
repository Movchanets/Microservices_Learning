import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const STORAGE_KEY = 'recentlyViewed';
const MAX_ITEMS = 20;

@Injectable({ providedIn: 'root' })
export class RecentlyViewedService {
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);

  readonly recentlyViewed = signal<string[]>([]);

  constructor() {
    if (this.isBrowser) {
      this.loadFromStorage();

      effect(() => {
        const ids = this.recentlyViewed();
        localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
      });
    }
  }

  trackView(productId: string): void {
    const current = this.recentlyViewed();
    const filtered = current.filter(id => id !== productId);
    this.recentlyViewed.set([productId, ...filtered].slice(0, MAX_ITEMS));
  }

  clear(): void {
    this.recentlyViewed.set([]);
  }

  private loadFromStorage(): void {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const ids = JSON.parse(stored) as string[];
        this.recentlyViewed.set(ids.slice(0, MAX_ITEMS));
      }
    } catch {
      // Ignore corrupt data
    }
  }
}
