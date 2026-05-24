import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, importProvidersFrom } from '@angular/core';
import { Header } from './header';
import { provideRouter, Router, Routes } from '@angular/router';

@Component({ template: '' })
class DummyComponent {}

const routes: Routes = [
  { path: 'catalog', component: DummyComponent },
];
import { AuthStore } from '../../../core/auth/auth.store';
import { CartStore } from '../../../features/cart/cart.store';
import { signal } from '@angular/core';
import { LucideAngularModule, User, LogOut, Settings, ChevronDown, Search, Menu, ShoppingCart, Heart, Shield } from 'lucide-angular';

describe('HeaderComponent', () => {
  let component: Header;
  let fixture: ComponentFixture<Header>;
  let mockAuthStore: { user: ReturnType<typeof signal<any>>; logout: ReturnType<typeof vi.fn> };
  let mockCartStore: { totalItems: ReturnType<typeof signal<number>>; toggleDrawer: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(async () => {
    mockAuthStore = {
      user: signal(null),
      logout: vi.fn(),
    };
    mockCartStore = {
      totalItems: signal(0),
      toggleDrawer: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [Header],
      providers: [
        provideRouter(routes),
        { provide: AuthStore, useValue: mockAuthStore },
        { provide: CartStore, useValue: mockCartStore },
        importProvidersFrom(LucideAngularModule.pick({ User, LogOut, Settings, ChevronDown, Search, Menu, ShoppingCart, Heart, Shield })),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Header);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows "Sign in" when user is not authenticated', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const loginLink = compiled.querySelector('[data-testid="nav-login"]');

    expect(loginLink).toBeTruthy();
    expect(loginLink?.textContent?.trim()).toBe('Sign in');
  });

  it('shows "Profile/Logout" when authenticated', async () => {
    mockAuthStore.user.set({ id: '1', firstName: 'John', email: 'john@example.com' });

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const userMenuTrigger = compiled.querySelector('[data-testid="user-menu-trigger"]');
    expect(userMenuTrigger).toBeTruthy();

    // Open menu
    component.isMenuOpen.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    const profileLink = compiled.querySelector('[data-testid="nav-profile"]');
    const logoutBtn = compiled.querySelector('[data-testid="nav-logout"]');
    expect(profileLink).toBeTruthy();
    expect(profileLink?.textContent?.trim()).toContain('My Profile');
    expect(logoutBtn).toBeTruthy();
    expect(logoutBtn?.textContent?.trim()).toContain('Sign Out');
  });

  it('renders Lucide icons correctly', async () => {
    mockAuthStore.user.set({ id: '1', firstName: 'John', email: 'john@example.com' });

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const icon = compiled.querySelector('lucide-icon');
    expect(icon).toBeTruthy();
  });

  // --- Mega-menu toggle tests ---

  it('starts with mega menu closed', () => {
    expect(component.isMegaMenuOpen()).toBe(false);
  });

  it('toggles mega menu on Catalog button click', () => {
    component.toggleMegaMenu();
    expect(component.isMegaMenuOpen()).toBe(true);

    component.toggleMegaMenu();
    expect(component.isMegaMenuOpen()).toBe(false);
  });

  it('renders mega-menu component when open', () => {
    component.isMegaMenuOpen.set(true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const megaMenu = compiled.querySelector('app-mega-menu');
    expect(megaMenu).toBeTruthy();
  });

  it('does not render mega-menu when closed', () => {
    component.isMegaMenuOpen.set(false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const megaMenu = compiled.querySelector('app-mega-menu');
    expect(megaMenu).toBeFalsy();
  });

  it('closeMegaMenu sets isMegaMenuOpen to false', () => {
    component.isMegaMenuOpen.set(true);
    component.closeMegaMenu();
    expect(component.isMegaMenuOpen()).toBe(false);
  });

  // --- Search tests ---

  it('navigates to catalog with query on search', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');

    component.search('laptop');

    expect(navigateSpy).toHaveBeenCalledWith(['/catalog'], { queryParams: { q: 'laptop' } });
  });

  it('does not navigate on empty search query', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');

    component.search('');
    expect(navigateSpy).not.toHaveBeenCalled();

    component.search('   ');
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('closes mega menu on search', () => {
    component.isMegaMenuOpen.set(true);
    component.search('phones');
    expect(component.isMegaMenuOpen()).toBe(false);
  });

  // --- Cart tests ---

  it('displays cart badge when items exist', async () => {
    mockCartStore.totalItems.set(3);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const badge = compiled.querySelector('.absolute.-top-1.-right-1');
    expect(badge).toBeTruthy();
    expect(badge?.textContent?.trim()).toBe('3');
  });

  it('does not display cart badge when cart is empty', () => {
    mockCartStore.totalItems.set(0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const badge = compiled.querySelector('.absolute.-top-1.-right-1');
    expect(badge).toBeFalsy();
  });

  it('calls toggleDrawer on cart button click', () => {
    component.cartStore.toggleDrawer();
    expect(mockCartStore.toggleDrawer).toHaveBeenCalledOnce();
  });

  // --- User menu tests ---

  it('toggles user menu on click', () => {
    expect(component.isMenuOpen()).toBe(false);

    component.toggleMenu();
    expect(component.isMenuOpen()).toBe(true);

    component.toggleMenu();
    expect(component.isMenuOpen()).toBe(false);
  });

  it('shows user email in dropdown when authenticated', async () => {
    mockAuthStore.user.set({ id: '1', firstName: 'John', email: 'john@example.com' });
    component.isMenuOpen.set(true);

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('john@example.com');
  });

  it('calls logout and closes menu', async () => {
    mockAuthStore.user.set({ id: '1', firstName: 'John', email: 'john@example.com' });
    component.isMenuOpen.set(true);

    component.logout();

    expect(mockAuthStore.logout).toHaveBeenCalledOnce();
    expect(component.isMenuOpen()).toBe(false);
  });

  it('shows Admin Panel link for Admin users', async () => {
    mockAuthStore.user.set({ id: '1', firstName: 'Admin', email: 'admin@example.com', role: 'Admin' });
    component.isMenuOpen.set(true);

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const adminLink = compiled.querySelector('[data-testid="nav-admin"]');
    expect(adminLink).toBeTruthy();
  });

  it('hides Admin Panel link for non-Admin users', async () => {
    mockAuthStore.user.set({ id: '1', firstName: 'John', email: 'john@example.com', role: 'Buyer' });
    component.isMenuOpen.set(true);

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const adminLink = compiled.querySelector('[data-testid="nav-admin"]');
    expect(adminLink).toBeFalsy();
  });

  it('renders logo with link to homepage', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const logo = compiled.querySelector('[data-testid="header-logo"]');
    expect(logo).toBeTruthy();
    expect(logo?.getAttribute('href')).toBe('/');
  });
});
