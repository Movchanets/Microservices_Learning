// Admin dashboard page component.
// Main container for the admin panel with stats overview and tab navigation.
// Loads users, stores, and pending verifications on init.

import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AdminStore } from '../admin.store';
import { StatsCardComponent } from '../components/stats-card/stats-card';

@Component({
  selector: 'app-admin-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterOutlet, LucideAngularModule, StatsCardComponent],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-6xl">
        <h1 class="text-3xl font-bold text-foreground font-lexend mb-8">Admin Panel</h1>

        <!-- Stats Overview -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <app-stats-card
            label="Total Users"
            [value]="store.users().length"
            icon="Users" />
          <app-stats-card
            label="Total Stores"
            [value]="store.stores().length"
            icon="Store" />
          <app-stats-card
            label="Pending Verifications"
            [value]="store.pendingCount()"
            icon="Clock" />
          <app-stats-card
            label="Sellers"
            [value]="store.sellerUsers().length"
            icon="ShoppingBag" />
        </div>

        <!-- Navigation Tabs -->
        <nav class="flex gap-1 mb-6 bg-muted/10 rounded-xl p-1 w-fit">
          <a routerLink="/admin/users"
             class="px-4 py-2 rounded-lg text-sm font-medium hover:bg-background transition-colors"
             routerLinkActive="bg-background text-foreground shadow-sm">
            Users
          </a>
          <a routerLink="/admin/verifications"
             class="px-4 py-2 rounded-lg text-sm font-medium hover:bg-background transition-colors"
             routerLinkActive="bg-background text-foreground shadow-sm">
            Verifications
            @if (store.pendingCount() > 0) {
              <span class="ml-1.5 px-1.5 py-0.5 text-xs rounded-full bg-primary text-white">
                {{ store.pendingCount() }}
              </span>
            }
          </a>
          <a routerLink="/admin/stores"
             class="px-4 py-2 rounded-lg text-sm font-medium hover:bg-background transition-colors"
             routerLinkActive="bg-background text-foreground shadow-sm">
            All Stores
          </a>
        </nav>

        <!-- Content -->
        <router-outlet />
      </div>
    </div>
  `
})
export class AdminPageComponent implements OnInit {
  readonly store = inject(AdminStore);

  ngOnInit(): void {
    this.store.loadUsers();
    this.store.loadStores();
    this.store.loadPendingStores();
  }
}
