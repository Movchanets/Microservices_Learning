import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule],
  templateUrl: './forgot-password.html'
})
export class ForgotPassword {
  private fb = inject(FormBuilder);
  
  isSubmitting = signal(false);
  isSuccess = signal(false);

  forgotForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  async onSubmit() {
    if (this.forgotForm.valid) {
      this.isSubmitting.set(true);
      try {
        // mock API call
        await new Promise(resolve => setTimeout(resolve, 1000));
        this.isSuccess.set(true);
      } finally {
        this.isSubmitting.set(false);
      }
    }
  }
}
