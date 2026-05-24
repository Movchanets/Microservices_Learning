// Store verification component for admin panel.
// Shows pending store applications with approve/reject actions.

import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AdminStore } from '../admin.store';
import { AdminStore as AdminStoreModel } from '../admin.models';

@Component({
  selector: 'app-store-verification',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, LucideAngularModule],
  template: `
    <div>
      @if (store.loading()) {
        <div class="flex justify-center p-12">
          <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
        </div>
      } @else if (store.error()) {
        <div class="p-4 bg-red-500/10 text-red-500 rounded-xl">
          {{ store.error() }}
        </div>
      } @else if (!store.hasPendingStores()) {
        <div class="text-center py-16 bg-card rounded-3xl border border-border">
          <lucide-icon name="CheckCircle" class="w-16 h-16 mx-auto mb-4 text-green-500/30"></lucide-icon>
          <p class="text-xl font-medium text-foreground mb-2">All caught up!</p>
          <p class="text-muted">No pending store verifications</p>
        </div>
      } @else {
        <div class="space-y-4">
          @for (storeItem of store.pendingStores(); track storeItem.id) {
            <div class="bg-card rounded-2xl border border-border p-6"
                    data-testid="store-verification-card">
              <div class="flex items-start justify-between">
                <div class="flex-1">
                  <div class="flex items-center gap-3 mb-2">
                    <div class="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center">
                      <lucide-icon name="Store" class="w-5 h-5 text-primary"></lucide-icon>
                    </div>
                    <div>
                      <h3 class="font-semibold text-foreground">{{ storeItem.name }}</h3>
                      <p class="text-xs text-muted font-mono">{{ storeItem.sellerId }}</p>
                    </div>
                  </div>
                  <p class="text-sm text-muted mt-2 line-clamp-2">{{ storeItem.description }}</p>
                  <p class="text-xs text-muted mt-2">
                    Applied {{ storeItem.createdAt | date:'medium' }}
                  </p>
                </div>

                <div class="flex gap-2 ml-4">
                  <button
                    (click)="onApprove(storeItem)"
                    class="px-4 py-2 rounded-xl bg-green-500/10 text-green-500 hover:bg-green-500/20 transition-colors text-sm font-medium">
                    <span class="flex items-center gap-1.5">
                      <lucide-icon name="Check" class="w-4 h-4"></lucide-icon>
                      <span>Approve</span>
                    </span>
                  </button>
                  <button
                    (click)="onReject(storeItem)"
                    class="px-4 py-2 rounded-xl bg-red-500/10 text-red-500 hover:bg-red-500/20 transition-colors text-sm font-medium">
                    <span class="flex items-center gap-1.5">
                      <lucide-icon name="X" class="w-4 h-4"></lucide-icon>
                      <span>Reject</span>
                    </span>
                  </button>
                  <a [routerLink]="['/admin/stores', storeItem.id]"
                     class="px-4 py-2 rounded-xl bg-muted/10 text-muted hover:bg-muted/20 transition-colors text-sm font-medium">
                    Details
                  </a>
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class StoreVerificationComponent implements OnInit {
  readonly store = inject(AdminStore);

  ngOnInit(): void {
    this.store.loadPendingStores();
  }

  onApprove(storeItem: AdminStoreModel): void {
    this.store.verifyStore(storeItem.id, { isApproved: true });
  }

  onReject(storeItem: AdminStoreModel): void {
    const reason = prompt('Reason for rejection:');
    if (reason) {
      this.store.verifyStore(storeItem.id, { isApproved: false, reason });
    }
  }
}
