import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Header } from './header';
import { provideRouter } from '@angular/router';
import { AuthStore } from '../../../core/auth/auth.store';
import { signal } from '@angular/core';

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
        { provide: AuthStore, useValue: mockAuthStore }
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Header);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows "Login/Register" when user is not authenticated', () => {
    const authStore = TestBed.inject(AuthStore);
    authStore.user = signal(null) as any;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const loginLink = compiled.querySelector('[data-testid="nav-login"]');
    const registerLink = compiled.querySelector('[data-testid="nav-register"]');

    expect(loginLink).toBeTruthy();
    expect(loginLink?.textContent?.trim()).toBe('Sign in');
    expect(registerLink).toBeTruthy();
    expect(registerLink?.textContent?.trim()).toBe('Get Started');
  });

  it('shows "Profile/Logout" and user name when authenticated', async () => {
    const authStore = TestBed.inject(AuthStore);
    // Use an unwrapped signal update instead of replacing the entire signal instance
    // which bypasses Angular's change detection tracking since `component.user`
    // references `authStore.user`.
    (authStore.user as any).set({ id: '1', firstName: 'John', email: 'john@example.com' });

    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const userMenuTrigger = compiled.querySelector('[data-testid="user-menu-trigger"]');
    expect(userMenuTrigger).toBeTruthy();
    expect(userMenuTrigger?.textContent?.trim()).toContain('John');

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
