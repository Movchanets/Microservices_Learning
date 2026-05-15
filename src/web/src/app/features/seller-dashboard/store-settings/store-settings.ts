// Store settings component.
// Manages store configuration (name, description, contact email).
// Currently uses stubbed data until Phase 6 (StoreManagement.API) is built.

import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
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
      } @else if (store.hasSettings()) {
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
          <div>
            <label class="block text-sm font-medium mb-1.5" i18n>Contact Email</label>
            <input type="email" [value]="contactEmail()" (input)="contactEmail.set($any($event.target).value)"
                   class="w-full px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none" />
          </div>
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

  ngOnInit(): void {
    this.store.loadSettings();
  }

  async onSave(event: Event): Promise<void> {
    event.preventDefault();
    await this.store.updateSettings({
      storeName: this.storeName(),
      description: this.storeDesc(),
      contactEmail: this.contactEmail(),
    });
  }
}
