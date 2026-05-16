import { ComponentFixture, TestBed } from '@angular/core/testing';
import { importProvidersFrom } from '@angular/core';
import { Header } from './header';
import { provideRouter } from '@angular/router';
import { AuthStore } from '../../../core/auth/auth.store';
import { signal } from '@angular/core';
import { LucideAngularModule, User, LogOut, Settings, ChevronDown, Search, Menu, ShoppingCart, Heart, Globe, Clock, Package } from 'lucide-angular';

describe('HeaderComponent', () => {
  let component: Header;
  let fixture: ComponentFixture<Header>;

  beforeEach(async () => {
    // Mock AuthStore
    const mockAuthStore = {
      user: signal(null),
      logout: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [Header],
      providers: [
        provideRouter([]),
        { provide: AuthStore, useValue: mockAuthStore },
        importProvidersFrom(LucideAngularModule.pick({ User, LogOut, Settings, ChevronDown, Search, Menu, ShoppingCart, Heart, Globe, Clock, Package })),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Header);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows "Sign in" when user is not authenticated', () => {
    const authStore = TestBed.inject(AuthStore);
    authStore.user = signal(null) as any;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const loginLink = compiled.querySelector('[data-testid="nav-login"]');

    expect(loginLink).toBeTruthy();
    expect(loginLink?.textContent?.trim()).toBe('Sign in');
  });

  it('shows "Profile/Logout" when authenticated', async () => {
    const authStore = TestBed.inject(AuthStore);
    (authStore.user as any).set({ id: '1', firstName: 'John', email: 'john@example.com' });

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
    const authStore = TestBed.inject(AuthStore);
    (authStore.user as any).set({ id: '1', firstName: 'John', email: 'john@example.com' });

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const icon = compiled.querySelector('lucide-icon');
    expect(icon).toBeTruthy();
  });
});
