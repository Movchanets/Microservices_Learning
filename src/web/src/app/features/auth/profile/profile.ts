import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthStore } from '../../../core/auth/auth.store';
import { LucideAngularModule, User, Mail, Shield, LogOut } from 'lucide-angular';

@Component({
  selector: 'app-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="min-h-[calc(100vh-80px)] flex items-center justify-center p-4">
      <div
        class="w-full max-w-2xl bg-card/80 backdrop-blur-xl rounded-2xl shadow-2xl p-8 border border-border"
      >
        <div class="flex flex-col items-center mb-8">
          <div
            class="w-24 h-24 rounded-full bg-primary/10 flex items-center justify-center mb-4 border border-primary/20 shadow-inner"
          >
            <lucide-icon [name]="UserIcon" class="w-12 h-12 text-primary"></lucide-icon>
          </div>
          <h1 class="text-3xl font-bold text-foreground font-lexend">
            {{ user()?.firstName }} {{ user()?.lastName }}
          </h1>
          <p class="text-muted-foreground font-medium">{{ user()?.role }}</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
          <div
            class="p-6 rounded-xl bg-card/40 border border-border/50 flex items-start gap-4 transition-all hover:bg-card/60"
          >
            <div
              class="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center shrink-0"
            >
              <lucide-icon [name]="MailIcon" class="w-5 h-5 text-primary"></lucide-icon>
            </div>
            <div>
              <p class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Email Address
              </p>
              <p class="text-foreground font-medium">{{ user()?.email }}</p>
            </div>
          </div>

          <div
            class="p-6 rounded-xl bg-card/40 border border-border/50 flex items-start gap-4 transition-all hover:bg-card/60"
          >
            <div
              class="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center shrink-0"
            >
              <lucide-icon [name]="ShieldIcon" class="w-5 h-5 text-primary"></lucide-icon>
            </div>
            <div>
              <p class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Account Role
              </p>
              <p class="text-foreground font-medium">{{ user()?.role }}</p>
            </div>
          </div>
        </div>

        <div class="flex justify-center border-t border-border pt-8">
          <button
            (click)="onLogout()"
            class="flex items-center gap-2 px-8 py-3 rounded-xl bg-red-500/10 text-red-500 font-semibold transition-all hover:bg-red-500 hover:text-white group"
            data-testid="profile-logout-btn"
          >
            <lucide-icon
              [name]="LogOutIcon"
              class="w-5 h-5 group-hover:scale-110 transition-transform"
            ></lucide-icon>
            Sign Out
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
    `,
  ],
})
export class ProfileComponent {
  private authStore = inject(AuthStore);

  user = this.authStore.user;

  readonly UserIcon = User;
  readonly MailIcon = Mail;
  readonly ShieldIcon = Shield;
  readonly LogOutIcon = LogOut;

  onLogout() {
    this.authStore.logout();
  }
}
