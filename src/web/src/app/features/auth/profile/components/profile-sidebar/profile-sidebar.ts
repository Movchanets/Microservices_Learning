import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthStore } from '../../../../../core/auth/auth.store';
import { LucideAngularModule, User, ShoppingBag, Settings, LogOut } from 'lucide-angular';

@Component({
  selector: 'app-profile-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  template: `
    <div class="bg-card/80 backdrop-blur-xl border border-border rounded-2xl p-6 shadow-sm sticky top-24">
      <div class="flex flex-col items-center mb-8 pb-8 border-b border-border text-center">
        <div class="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center mb-4 border border-primary/20 shadow-inner">
          <lucide-icon [name]="UserIcon" class="w-10 h-10 text-primary"></lucide-icon>
        </div>
        <h2 class="text-xl font-bold text-foreground font-lexend">
          {{ user()?.firstName }} {{ user()?.lastName }}
        </h2>
        <p class="text-muted-foreground text-sm">{{ user()?.email }}</p>
        <span class="mt-3 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary/10 text-primary border border-primary/20">
          {{ user()?.role }}
        </span>
      </div>

      <nav class="space-y-2">
        <a 
          routerLink="orders" 
          routerLinkActive="bg-primary text-primary-foreground font-medium"
          class="flex items-center gap-3 px-4 py-3 rounded-xl text-muted-foreground hover:bg-primary/10 hover:text-primary transition-all group"
        >
          <lucide-icon [name]="ShoppingBagIcon" class="w-5 h-5 group-[.active]:text-primary-foreground"></lucide-icon>
          <span>My Orders</span>
        </a>
        
        <a 
          routerLink="settings" 
          routerLinkActive="bg-primary text-primary-foreground font-medium"
          class="flex items-center gap-3 px-4 py-3 rounded-xl text-muted-foreground hover:bg-primary/10 hover:text-primary transition-all group"
        >
          <lucide-icon [name]="SettingsIcon" class="w-5 h-5 group-[.active]:text-primary-foreground"></lucide-icon>
          <span>Settings</span>
        </a>
      </nav>

      <div class="mt-8 pt-6 border-t border-border">
        <button 
          (click)="onLogout()"
          class="flex items-center gap-3 px-4 py-3 w-full rounded-xl text-red-500 hover:bg-red-500/10 transition-all font-medium"
        >
          <lucide-icon [name]="LogOutIcon" class="w-5 h-5"></lucide-icon>
          <span>Sign Out</span>
        </button>
      </div>
    </div>
  `
})
export class ProfileSidebarComponent {
  private authStore = inject(AuthStore);
  user = this.authStore.user;

  readonly UserIcon = User;
  readonly ShoppingBagIcon = ShoppingBag;
  readonly SettingsIcon = Settings;
  readonly LogOutIcon = LogOut;

  onLogout() {
    this.authStore.logout();
  }
}