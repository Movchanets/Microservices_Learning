// Login component unit tests.
// Verifies the login form renders correctly, handles user input,
// submits credentials to AuthStore, and displays validation errors.

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Login } from './login';
import { AuthStore } from '../../../core/auth/auth.store';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { LUCIDE_ICONS, LucideIconProvider } from 'lucide-angular';
import { Mail, Lock, Eye, EyeOff, LogIn } from 'lucide-angular';

describe('LoginComponent', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let authStoreMock: any;

  beforeEach(async () => {
    authStoreMock = {
      login: vi.fn().mockResolvedValue(undefined),
      error: signal(null),
    };

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        {
          provide: LUCIDE_ICONS,
          multi: true,
          useValue: new LucideIconProvider({ Mail, Lock, Eye, EyeOff, LogIn })
        }
      ],
    })
    .overrideProvider(AuthStore, { useValue: authStoreMock })
    .compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have invalid form initially', () => {
    expect(component.loginForm.valid).toBe(false);
  });

  it('should have required email and valid email format', () => {
    const emailControl = component.loginForm.controls.email;
    expect(emailControl.valid).toBe(false);
    expect(emailControl.errors?.['required']).toBeTruthy();

    emailControl.setValue('invalid-email');
    expect(emailControl.errors?.['email']).toBeTruthy();

    emailControl.setValue('test@example.com');
    expect(emailControl.valid).toBe(true);
  });

  it('submit button should be disabled when form is invalid', () => {
    fixture.detectChanges();
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton.disabled).toBe(true);
  });

  it('submit button should be enabled when form is valid', () => {
    component.loginForm.controls.email.setValue('test@example.com');
    component.loginForm.controls.password.setValue('password');
    fixture.detectChanges();

    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton.disabled).toBe(false);
  });

  it('calling login on the store when form is valid and submitted', async () => {
    component.loginForm.controls.email.setValue('test@example.com');
    component.loginForm.controls.password.setValue('password');

    await component.onSubmit();

    expect(authStoreMock.login).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password'
    });
  });

  it('should set isSubmitting to true and false during submission', async () => {
    component.loginForm.controls.email.setValue('test@example.com');
    component.loginForm.controls.password.setValue('password');

    // Defer the resolution to check the intermediate state
    let resolveLogin: any;
    const loginPromise = new Promise(resolve => { resolveLogin = resolve; });
    authStoreMock.login.mockReturnValue(loginPromise);

    const submitPromise = component.onSubmit();

    expect(component.isSubmitting()).toBe(true);

    resolveLogin();
    await submitPromise;

    expect(component.isSubmitting()).toBe(false);
  });
});
