import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-background flex items-center justify-center">
      <div class="text-center px-6">
        <h1 class="text-8xl font-bold text-foreground font-lexend mb-4">404</h1>
        <p class="text-xl text-muted mb-8">Page not found</p>
        <a
          routerLink="/"
          class="px-6 py-3 rounded-xl bg-primary text-white font-medium hover:opacity-90 transition-opacity">
          Go Home
        </a>
      </div>
    </div>
  `
})
export class NotFoundComponent {}
