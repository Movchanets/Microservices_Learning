// Store settings component.
// Manages store configuration (name, description, logo).
// Shows verification status, creation date, and rejection reason.

import { Component, ChangeDetectionStrategy, computed, effect, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { StoreSettingsStore } from '../store-settings.store';

@Component({
  selector: 'app-store-settings',

  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LucideAngularModule],
  template: `
    <div class="bg-card rounded-3xl border border-border p-6 max-w-2xl">
      <h2 class="text-xl font-bold font-lexend mb-6" i18n>Store Settings</h2>

      @if (store.loading()) {
        <div class="flex justify-center p-8">
          <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
        </div>
      } @else if (store.error()) {
        <div class="p-4 bg-red-500/10 text-red-500 rounded-xl mb-4">{{ store.error() }}</div>
        <button (click)="store.loadSettings()"
                class="px-4 py-2 bg-primary text-white rounded-xl text-sm">
          Retry
        </button>
      } @else if (!store.hasSettings()) {
        <div class="text-center py-8">
          <p class="text-muted mb-4">You don't have a store yet.</p>
          <p class="text-sm text-muted mb-6">Create a store to start selling products.</p>
          <div class="space-y-3">
            <input #storeNameInput [value]="storeName()" (input)="storeName.set(storeNameInput.value)"
                   placeholder="Store Name"
                   data-testid="store-name-input"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" />
            @if (storeName().length > 0 && storeName().trim().length < 2) {
              <p class="text-xs text-red-500 mt-1" aria-live="polite">Store name must be at least 2 characters.</p>
            }
            <textarea #storeDescInput [value]="storeDesc()" (input)="storeDesc.set(storeDescInput.value)"
                      placeholder="Store Description"
                      rows="3"
                      class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"></textarea>
            @if (storeDesc().length > 0 && storeDesc().trim().length < 10) {
              <p class="text-xs text-red-500 mt-1" aria-live="polite">Description must be at least 10 characters.</p>
            }
            <button (click)="onCreate()"
                    [disabled]="createDisabled()"
                    class="px-6 py-2.5 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors cursor-pointer disabled:opacity-50">
              Create Store
            </button>
          </div>
        </div>
      } @else {
        <form (submit)="onSave($event)" class="space-y-5">
          <!-- Verification Status -->
          @if (store.settings()?.verificationStatus) {
            <div class="p-4 rounded-xl" [class]="statusBannerClass()">
              <div class="flex items-center gap-3">
                <lucide-icon [name]="statusIcon()" class="w-5 h-5"></lucide-icon>
                <div>
                  <p class="font-medium text-sm">{{ statusTitle() }}</p>
                  <p class="text-xs opacity-80">{{ statusMessage() }}</p>
                </div>
              </div>
            </div>
          }

          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Store Name</label>
            <input #nameEditInput [value]="storeName()" (input)="storeName.set(nameEditInput.value)"
                   data-testid="store-name-input"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" />
            @if (storeName().length > 0 && storeName().trim().length < 2) {
              <p class="text-xs text-red-500 mt-1" aria-live="polite">Store name must be at least 2 characters.</p>
            }
          </div>
          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Description</label>
            <textarea #descEditInput [value]="storeDesc()" (input)="storeDesc.set(descEditInput.value)"
                      rows="3"
                      class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"></textarea>
            @if (storeDesc().length > 0 && storeDesc().trim().length < 10) {
              <p class="text-xs text-red-500 mt-1" aria-live="polite">Description must be at least 10 characters.</p>
            }
          </div>
          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Logo URL</label>
            <input #logoUrlInput [value]="logoUrl()" (input)="logoUrl.set(logoUrlInput.value)"
                   placeholder="https://example.com/logo.png"
                   data-testid="logo-url-input"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" />
            @if (logoUrl()) {
              <div class="mt-2 flex items-center gap-3">
                <img [src]="logoUrl()" alt="Store logo" class="w-12 h-12 rounded-xl object-cover border border-border" />
                <span class="text-xs text-muted">Logo preview</span>
              </div>
            }
          </div>

          <!-- Store metadata -->
          @if (store.settings()?.createdAt) {
            <div class="text-xs text-muted pt-2">
              <span i18n>Created</span>: {{ store.settings()?.createdAt | date:'mediumDate' }}
              @if (store.settings()?.verifiedAt) {
                <span class="ml-3" i18n>Verified</span>: {{ store.settings()?.verifiedAt | date:'mediumDate' }}
              }
            </div>
          }

          <div class="flex items-center gap-3">
            <button type="submit"
                    [disabled]="saveDisabled()"
                    class="px-6 py-2.5 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors cursor-pointer disabled:opacity-50">
              <span i18n>Save Changes</span>
            </button>
            @if (logoUrl() !== (store.settings()?.logoUrl || '')) {
              <button type="button"
                      (click)="onSaveLogo()"
                      [disabled]="store.loading()"
                      class="px-6 py-2.5 bg-muted/20 text-foreground rounded-xl font-medium hover:bg-muted/30 transition-colors cursor-pointer disabled:opacity-50">
                <span i18n>Update Logo</span>
              </button>
            }
          </div>
        </form>
      }
    </div>
  `
})
export class StoreSettingsComponent implements OnInit {
  readonly store = inject(StoreSettingsStore);
  storeName = signal('');
  storeDesc = signal('');
  logoUrl = signal('');

  readonly statusBannerClass = computed(() => {
    const status = this.store.settings()?.verificationStatus;
    switch (status) {
      case 'Pending': return 'bg-yellow-500/10 border border-yellow-500/20 text-yellow-600';
      case 'Verified': return 'bg-green-500/10 border border-green-500/20 text-green-600';
      case 'Rejected': return 'bg-red-500/10 border border-red-500/20 text-red-600';
      default: return '';
    }
  });

  readonly statusIcon = computed(() => {
    const status = this.store.settings()?.verificationStatus;
    switch (status) {
      case 'Pending': return 'Clock';
      case 'Verified': return 'CheckCircle';
      case 'Rejected': return 'XCircle';
      default: return 'Store';
    }
  });

  readonly statusTitle = computed(() => {
    const status = this.store.settings()?.verificationStatus;
    switch (status) {
      case 'Pending': return 'Verification Pending';
      case 'Verified': return 'Store Verified';
      case 'Rejected': return 'Verification Rejected';
      default: return '';
    }
  });

  readonly statusMessage = computed(() => {
    const settings = this.store.settings();
    switch (settings?.verificationStatus) {
      case 'Pending': return 'Your store is being reviewed by our team. This usually takes 1-2 business days.';
      case 'Verified': return 'Your store is verified and live on the marketplace.';
      case 'Rejected': return settings.rejectionReason || 'Your store was not approved. Please update your store details and try again.';
      default: return '';
    }
  });

  constructor() {
    // Populate form fields when store settings load
    effect(() => {
      const settings = this.store.settings();
      if (settings) {
        this.storeName.set(settings.storeName);
        this.storeDesc.set(settings.description);
        this.logoUrl.set(settings.logoUrl ?? '');
      }
    });
  }

  ngOnInit(): void {
    this.store.loadSettings();
  }

  private isValidStoreName(name: string): boolean {
    return name.trim().length >= 2 && name.trim().length <= 200;
  }

  private isValidDescription(desc: string): boolean {
    return desc.trim().length >= 10 && desc.trim().length <= 2000;
  }

  createDisabled(): boolean {
    return !this.isValidStoreName(this.storeName())
      || !this.isValidDescription(this.storeDesc())
      || this.store.loading();
  }

  saveDisabled(): boolean {
    return !this.isValidStoreName(this.storeName())
      || !this.isValidDescription(this.storeDesc())
      || this.store.loading();
  }

  async onCreate(): Promise<void> {
    if (!this.isValidStoreName(this.storeName()) || !this.isValidDescription(this.storeDesc())) return;
    await this.store.createStore(this.storeName().trim(), this.storeDesc().trim());
  }

  async onSave(event: Event): Promise<void> {
    event.preventDefault();
    if (!this.isValidStoreName(this.storeName()) || !this.isValidDescription(this.storeDesc())) return;
    await this.store.updateSettings(this.storeName().trim(), this.storeDesc().trim());
  }

  async onSaveLogo(): Promise<void> {
    await this.store.setLogo(this.logoUrl());
  }
}
