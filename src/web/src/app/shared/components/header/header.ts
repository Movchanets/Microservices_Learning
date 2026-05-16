import { Component, inject, signal } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthStore } from '../../../core/auth/auth.store';
import { CartStore } from '../../../features/cart/cart.store';
import { LucideAngularModule, User, LogOut, Settings, ChevronDown, Search, Menu, ShoppingCart, Heart, Globe } from 'lucide-angular';
import { MegaMenu } from '../mega-menu/mega-menu';
import { SearchBarComponent } from '../search-bar/search-bar';

@Component({
  selector: 'app-header',
  imports: [RouterLink, CommonModule, LucideAngularModule, MegaMenu, SearchBarComponent],
  standalone: true,
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  private authStore = inject(AuthStore);
  private router = inject(Router);
  cartStore = inject(CartStore);

  user = this.authStore.user;
  isMenuOpen = signal(false);
  isMegaMenuOpen = signal(false);

  readonly UserIcon = User;
  readonly LogOutIcon = LogOut;
  readonly SettingsIcon = Settings;
  readonly ChevronIcon = ChevronDown;
  readonly SearchIcon = Search;
  readonly MenuIcon = Menu;
  readonly CartIcon = ShoppingCart;
  readonly HeartIcon = Heart;
  readonly GlobeIcon = Globe;

  toggleMenu() {
    this.isMenuOpen.update((v) => !v);
  }

  toggleMegaMenu() {
    this.isMegaMenuOpen.update((v) => !v);
  }

  closeMegaMenu() {
    this.isMegaMenuOpen.set(false);
  }

  logout() {
    this.authStore.logout();
    this.isMenuOpen.set(false);
  }

  search(query: string) {
    const q = query.trim();
    if (q) {
      this.router.navigate(['/catalog'], { queryParams: { q } });
      this.closeMegaMenu();
    }
  }
}
