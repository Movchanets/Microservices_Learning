// Store settings component.
// Manages store configuration (name, description).
// Loads real store data from StoreManagement.API.

import { Component, ChangeDetectionStrategy, effect, inject, OnInit, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { StoreSettingsStore } from '../store-settings.store';

@Component({
  selector: 'app-store-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="bg-card/60 backdrop-blur-sm rounded-3xl border border-border p-6 max-w-2xl">
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
            <input [value]="storeName()" (input)="storeName.set($any($event.target).value)"
                   placeholder="Store Name"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" />
            <textarea [value]="storeDesc()" (input)="storeDesc.set($any($event.target).value)"
                      placeholder="Store Description"
                      rows="3"
                      class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"></textarea>
            <button (click)="onCreate()"
                    [disabled]="!storeName() || !storeDesc()"
                    class="px-6 py-2.5 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors cursor-pointer disabled:opacity-50">
              Create Store
            </button>
          </div>
        </div>
      } @else {
        <form (submit)="onSave($event)" class="space-y-5">
          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Store Name</label>
            <input [value]="storeName()" (input)="storeName.set($any($event.target).value)"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Description</label>
            <textarea [value]="storeDesc()" (input)="storeDesc.set($any($event.target).value)"
                      rows="3"
                      class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none resize-none"></textarea>
          </div>
          @if (store.settings()?.verificationStatus) {
            <div>
              <label class="block text-sm font-medium mb-1.5" i18n>Status</label>
              <span [class]="statusClass()" class="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold">
                {{ store.settings()?.verificationStatus }}
              </span>
            </div>
          }
          <button type="submit"
                  class="px-6 py-2.5 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors cursor-pointer">
            <span i18n>Save Changes</span>
          </button>
        </form>
      }
    </div>
  `
})
export class StoreSettingsComponent implements OnInit {
  readonly store = inject(StoreSettingsStore);
  storeName = signal('');
  storeDesc = signal('');
  contactEmail = signal('');

  constructor() {
    // Populate form fields when store settings load
    effect(() => {
      const settings = this.store.settings();
      if (settings) {
        this.storeName.set(settings.storeName);
        this.storeDesc.set(settings.description);
        this.contactEmail.set(settings.contactEmail);
      }
    });
  }

  ngOnInit(): void {
    this.store.loadSettings();
  }

  async onCreate(): Promise<void> {
    await this.store.createStore(this.storeName(), this.storeDesc());
  }

  async onSave(event: Event): Promise<void> {
    event.preventDefault();
    await this.store.updateSettings(this.storeName(), this.storeDesc());
  }

  statusClass(): string {
    const status = this.store.settings()?.verificationStatus;
    const base = 'inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold';
    const variants: Record<string, string> = {
      Pending: `${base} bg-yellow-500/10 text-yellow-500`,
      Verified: `${base} bg-green-500/10 text-green-500`,
      Rejected: `${base} bg-red-500/10 text-red-500`,
    };
    return variants[status || ''] || base;
  }
}
