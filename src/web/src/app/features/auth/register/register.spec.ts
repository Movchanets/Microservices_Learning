import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Register } from './register';
import { AuthStore } from '../../../core/auth/auth.store';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { LUCIDE_ICONS, LucideIconProvider } from 'lucide-angular';
import { Mail, Lock, Eye, EyeOff, User } from 'lucide-angular';

describe('RegisterComponent', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let authStoreMock: any;

  beforeEach(async () => {
    authStoreMock = {
      register: vi.fn().mockResolvedValue(undefined),
      error: signal(null),
    };

    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideRouter([]),
        {
          provide: LUCIDE_ICONS,
          multi: true,
          useValue: new LucideIconProvider({ Mail, Lock, Eye, EyeOff, User })
        }
      ],
    })
    .overrideProvider(AuthStore, { useValue: authStoreMock })
    .compileComponents();

    fixture = TestBed.createComponent(Register);
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
    expect(component.registerForm.valid).toBe(false);
  });

  it('should require firstName, lastName, email, and password', () => {
    const form = component.registerForm;

    expect(form.controls.firstName.valid).toBe(false);
    expect(form.controls.lastName.valid).toBe(false);
    expect(form.controls.email.valid).toBe(false);
    expect(form.controls.password.valid).toBe(false);

    form.controls.firstName.setValue('John');
    form.controls.lastName.setValue('Doe');
    form.controls.email.setValue('john@example.com');
    form.controls.password.setValue('password');

    expect(form.valid).toBe(true);
  });

  it('submit button should be disabled when form is invalid', () => {
    fixture.detectChanges();
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton.disabled).toBe(true);
  });

  it('submit button should be enabled when form is valid', () => {
    component.registerForm.controls.firstName.setValue('John');
    component.registerForm.controls.lastName.setValue('Doe');
    component.registerForm.controls.email.setValue('john@example.com');
    component.registerForm.controls.password.setValue('password');
    fixture.detectChanges();

    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton.disabled).toBe(false);
  });

  it('calling register on the store when form is valid and submitted', async () => {
    component.registerForm.controls.firstName.setValue('John');
    component.registerForm.controls.lastName.setValue('Doe');
    component.registerForm.controls.email.setValue('john@example.com');
    component.registerForm.controls.password.setValue('password');

    await component.onSubmit();

    expect(authStoreMock.register).toHaveBeenCalledWith({
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      password: 'password'
    });
  });

  it('should set isSubmitting to true and false during submission', async () => {
    component.registerForm.controls.firstName.setValue('John');
    component.registerForm.controls.lastName.setValue('Doe');
    component.registerForm.controls.email.setValue('john@example.com');
    component.registerForm.controls.password.setValue('password');

    // Defer the resolution to check the intermediate state
    let resolveRegister: any;
    const registerPromise = new Promise(resolve => { resolveRegister = resolve; });
    authStoreMock.register.mockReturnValue(registerPromise);

    const submitPromise = component.onSubmit();

    expect(component.isSubmitting()).toBe(true);

    resolveRegister();
    await submitPromise;

    expect(component.isSubmitting()).toBe(false);
  });
});
