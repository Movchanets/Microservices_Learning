import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthStore } from '../../../core/auth/auth.store';

import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule],
  templateUrl: './register.html',
})
export class Register {
  private fb = inject(FormBuilder);
  authStore = inject(AuthStore);

  isSubmitting = signal(false);
  showPassword = signal(false);

  togglePassword() {
    this.showPassword.update((v) => !v);
  }

  registerForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$/),
    ]],
  });

  async onSubmit() {
    if (this.registerForm.valid) {
      this.isSubmitting.set(true);
      try {
        await this.authStore.register(this.registerForm.getRawValue());
      } finally {
        this.isSubmitting.set(false);
      }
    }
  }
}
