import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule],
  templateUrl: './forgot-password.html',
})
export class ForgotPassword {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  isSubmitting = signal(false);
  isSuccess = signal(false);

  forgotForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  async onSubmit() {
    if (this.forgotForm.valid) {
      this.isSubmitting.set(true);
      try {
        const { email } = this.forgotForm.getRawValue();
        await this.authService.forgotPassword(email);
        this.isSuccess.set(true);
      } catch (error) {
        console.error('Forgot password request failed', error);
        // Rationale: We don't show specific errors to avoid email enumeration,
        // but we might want to handle network errors or similar in a real app.
      } finally {
        this.isSubmitting.set(false);
      }
    }
  }
}
