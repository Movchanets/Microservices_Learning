import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthStore } from '../../../core/auth/auth.store';
import { LucideAngularModule, User, LogOut, Settings, ChevronDown } from 'lucide-angular';

@Component({
  selector: 'app-header',
  imports: [RouterLink, CommonModule, LucideAngularModule],
  standalone: true,
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  private authStore = inject(AuthStore);

  user = this.authStore.user;
  isMenuOpen = signal(false);

  readonly UserIcon = User;
  readonly LogOutIcon = LogOut;
  readonly SettingsIcon = Settings;
  readonly ChevronIcon = ChevronDown;

  toggleMenu() {
    this.isMenuOpen.update((v) => !v);
  }

  logout() {
    this.authStore.logout();
    this.isMenuOpen.set(false);
  }
}
