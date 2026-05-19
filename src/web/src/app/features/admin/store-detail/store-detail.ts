// Store detail component for admin panel.
// Shows full store information with verification decision actions.

import { Component, ChangeDetectionStrategy, inject, OnInit, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AdminStore } from '../admin.store';

@Component({
  selector: 'app-admin-store-detail',
  standalone: true,
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
      } @else if (store.selectedStore(); as storeItem) {
        <div class="mb-6">
          <a routerLink="/admin/verifications" class="text-muted hover:text-foreground transition-colors flex items-center gap-2 mb-4">
            <lucide-icon name="ChevronLeft" class="w-4 h-4"></lucide-icon>
            <span>Back to Verifications</span>
          </a>
          <div class="flex items-center justify-between">
            <h2 class="text-xl font-bold font-lexend text-foreground">{{ storeItem.name }}</h2>
            <span [class]="statusBadgeClass(storeItem.verificationStatus)">
              {{ storeItem.verificationStatus }}
            </span>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Store Info -->
          <div class="bg-card rounded-3xl border border-border p-6">
            <h3 class="text-lg font-semibold font-lexend mb-4">Store Information</h3>
            <dl class="space-y-3 text-sm">
              <div>
                <dt class="text-muted">Store ID</dt>
                <dd class="font-mono">{{ storeItem.id }}</dd>
              </div>
              <div>
                <dt class="text-muted">Seller ID</dt>
                <dd class="font-mono">{{ storeItem.sellerId }}</dd>
              </div>
              <div>
                <dt class="text-muted">Description</dt>
                <dd class="text-foreground">{{ storeItem.description }}</dd>
              </div>
              <div>
                <dt class="text-muted">Applied On</dt>
                <dd>{{ storeItem.createdAt | date:'medium' }}</dd>
              </div>
              @if (storeItem.verifiedAt) {
                <div>
                  <dt class="text-muted">Verified On</dt>
                  <dd>{{ storeItem.verifiedAt | date:'medium' }}</dd>
                </div>
              }
              @if (storeItem.rejectionReason) {
                <div>
                  <dt class="text-muted">Rejection Reason</dt>
                  <dd class="text-red-500">{{ storeItem.rejectionReason }}</dd>
                </div>
              }
            </dl>
          </div>

          <!-- Actions -->
          @if (storeItem.verificationStatus === 'Pending') {
            <div class="bg-card rounded-3xl border border-border p-6">
              <h3 class="text-lg font-semibold font-lexend mb-4">Verification Decision</h3>
              <div class="space-y-3">
                <button
                  (click)="onApprove()"
                  class="w-full px-4 py-3 rounded-xl bg-green-500/10 text-green-500 hover:bg-green-500/20 transition-colors font-medium">
                  <span class="flex items-center justify-center gap-2">
                    <lucide-icon name="Check" class="w-5 h-5"></lucide-icon>
                    <span>Approve Store</span>
                  </span>
                </button>
                <button
                  (click)="onReject()"
                  class="w-full px-4 py-3 rounded-xl bg-red-500/10 text-red-500 hover:bg-red-500/20 transition-colors font-medium">
                  <span class="flex items-center justify-center gap-2">
                    <lucide-icon name="X" class="w-5 h-5"></lucide-icon>
                    <span>Reject Store</span>
                  </span>
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class StoreDetailComponent implements OnInit {
  readonly store = inject(AdminStore);
  storeId = input.required<string>();

  ngOnInit(): void {
    this.store.loadStoreById(this.storeId());
  }

  statusBadgeClass(status: string): string {
    const base = 'inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold';
    const variants: Record<string, string> = {
      Pending: `${base} bg-yellow-500/10 text-yellow-500`,
      Verified: `${base} bg-green-500/10 text-green-500`,
      Rejected: `${base} bg-red-500/10 text-red-500`,
    };
    return variants[status] || base;
  }

  onApprove(): void {
    this.store.verifyStore(this.storeId(), { isApproved: true });
  }

  onReject(): void {
    const reason = prompt('Reason for rejection:');
    if (reason) {
      this.store.verifyStore(this.storeId(), { isApproved: false, reason });
    }
  }
}
