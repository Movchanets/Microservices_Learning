import { Component } from '@angular/core';

@Component({
  selector: 'app-catalog',
  standalone: true,
  template: `
    <div class="min-h-screen bg-background p-8 pt-12">
      <div class="container mx-auto">
        <header class="mb-12">
          <h1 class="text-4xl font-bold text-foreground font-lexend mb-2" data-testid="catalog-title">Explore Catalog</h1>
          <p class="text-muted text-lg max-w-2xl">Discover premium microservices and tools built for scale. Quality-vetted by our enterprise experts.</p>
        </header>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-8">
          @for (i of [1, 2, 3, 4, 5, 6, 7, 8]; track i) {
            <div class="bg-card/40 backdrop-blur-sm border border-border rounded-2xl p-6 shadow-sm animate-pulse">
              <div class="w-full aspect-video bg-muted/20 rounded-xl mb-4"></div>
              <div class="h-6 bg-muted/20 rounded-md w-3/4 mb-3"></div>
              <div class="h-4 bg-muted/20 rounded-md w-full mb-2"></div>
              <div class="h-4 bg-muted/20 rounded-md w-5/6 mb-6"></div>
              <div class="h-10 bg-muted/20 rounded-xl w-full"></div>
            </div>
          }
        </div>
      </div>
    </div>
  `
})
export class CatalogComponent {}
