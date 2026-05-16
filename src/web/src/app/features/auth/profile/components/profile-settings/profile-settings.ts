import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ProfileStore } from '../../profile.store';
import { AuthStore } from '../../../../../core/auth/auth.store';
import { LucideAngularModule, Save, Key, User, Mail, Lock } from 'lucide-angular';

@Component({
  selector: 'app-profile-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  template: `
    <div class="space-y-8 animate-in fade-in duration-300">
      <div *ngIf="profileStore.successMessage()" class="bg-green-500/10 text-green-600 p-4 rounded-xl border border-green-500/20">
        {{ profileStore.successMessage() }}
      </div>
      
      <div *ngIf="profileStore.error()" class="bg-red-500/10 text-red-500 p-4 rounded-xl border border-red-500/20">
        {{ profileStore.error() }}
      </div>

      <!-- Update Profile Section -->
      <section class="bg-card border border-border rounded-2xl p-6 shadow-sm">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center">
            <lucide-icon [name]="UserIcon" class="w-5 h-5 text-primary"></lucide-icon>
          </div>
          <h2 class="text-xl font-bold font-lexend text-foreground">Profile Information</h2>
        </div>
        
        <form [formGroup]="profileForm" (ngSubmit)="onUpdateProfile()" class="space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-2">
              <label class="text-sm font-medium text-foreground">First Name</label>
              <div class="relative">
                <lucide-icon [name]="UserIcon" class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"></lucide-icon>
                <input 
                  type="text" 
                  formControlName="firstName"
                  class="w-full bg-background border border-input rounded-xl py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground"
                >
              </div>
            </div>
            
            <div class="space-y-2">
              <label class="text-sm font-medium text-foreground">Last Name</label>
              <div class="relative">
                <lucide-icon [name]="UserIcon" class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"></lucide-icon>
                <input 
                  type="text" 
                  formControlName="lastName"
                  class="w-full bg-background border border-input rounded-xl py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground"
                >
              </div>
            </div>
          </div>
          
          <div class="space-y-2">
            <label class="text-sm font-medium text-foreground">Email</label>
            <div class="relative">
              <lucide-icon [name]="MailIcon" class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"></lucide-icon>
              <input 
                type="email" 
                formControlName="email"
                class="w-full bg-background border border-input rounded-xl py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground"
              >
            </div>
          </div>
          
          <div class="flex justify-end pt-2">
            <button 
              type="submit" 
              [disabled]="profileForm.invalid || profileStore.updating()"
              class="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-2 rounded-xl font-medium hover:bg-primary/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <lucide-icon [name]="SaveIcon" class="w-4 h-4"></lucide-icon>
              <span *ngIf="profileStore.updating()">Saving...</span>
              <span *ngIf="!profileStore.updating()">Save Changes</span>
            </button>
          </div>
        </form>
      </section>

      <!-- Change Password Section -->
      <section class="bg-card border border-border rounded-2xl p-6 shadow-sm">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-10 h-10 rounded-lg bg-orange-500/10 flex items-center justify-center">
            <lucide-icon [name]="KeyIcon" class="w-5 h-5 text-orange-500"></lucide-icon>
          </div>
          <h2 class="text-xl font-bold font-lexend text-foreground">Change Password</h2>
        </div>
        
        <form [formGroup]="passwordForm" (ngSubmit)="onChangePassword()" class="space-y-4">
          <div class="space-y-2">
            <label class="text-sm font-medium text-foreground">Current Password</label>
            <div class="relative">
              <lucide-icon [name]="LockIcon" class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"></lucide-icon>
              <input 
                type="password" 
                formControlName="currentPassword"
                class="w-full bg-background border border-input rounded-xl py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground"
              >
            </div>
          </div>
          
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-2">
              <label class="text-sm font-medium text-foreground">New Password</label>
              <div class="relative">
                <lucide-icon [name]="LockIcon" class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"></lucide-icon>
                <input 
                  type="password" 
                  formControlName="newPassword"
                  class="w-full bg-background border border-input rounded-xl py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground"
                >
              </div>
            </div>
            
            <div class="space-y-2">
              <label class="text-sm font-medium text-foreground">Confirm New Password</label>
              <div class="relative">
                <lucide-icon [name]="LockIcon" class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"></lucide-icon>
                <input 
                  type="password" 
                  formControlName="confirmPassword"
                  class="w-full bg-background border border-input rounded-xl py-2 pl-10 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-foreground"
                >
              </div>
            </div>
          </div>
          
          <div class="flex justify-end pt-2">
            <button 
              type="submit" 
              [disabled]="passwordForm.invalid || profileStore.changingPassword()"
              class="flex items-center gap-2 bg-foreground text-background px-6 py-2 rounded-xl font-medium hover:bg-foreground/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <lucide-icon [name]="KeyIcon" class="w-4 h-4"></lucide-icon>
              <span *ngIf="profileStore.changingPassword()">Updating...</span>
              <span *ngIf="!profileStore.changingPassword()">Update Password</span>
            </button>
          </div>
        </form>
      </section>
    </div>
  `
})
export class ProfileSettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  readonly profileStore = inject(ProfileStore);
  private authStore = inject(AuthStore);

  readonly UserIcon = User;
  readonly MailIcon = Mail;
  readonly SaveIcon = Save;
  readonly KeyIcon = Key;
  readonly LockIcon = Lock;

  profileForm = this.fb.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]]
  });

  passwordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  });

  ngOnInit() {
    this.profileStore.clearMessages();
    const user = this.authStore.user();
    if (user) {
      this.profileForm.patchValue({
        firstName: user.firstName,
        lastName: user.lastName,
        email: user.email
      });
    }
  }

  async onUpdateProfile() {
    if (this.profileForm.invalid) return;
    const user = this.authStore.user();
    if (!user?.id) return;

    await this.profileStore.updateProfile(user.id, this.profileForm.value as any);
  }

  async onChangePassword() {
    if (this.passwordForm.invalid) return;
    if (this.passwordForm.value.newPassword !== this.passwordForm.value.confirmPassword) {
      // Basic validation for matching passwords
      alert("New passwords do not match.");
      return;
    }
    
    await this.profileStore.changePassword(this.passwordForm.value as any);
    if (!this.profileStore.error()) {
      this.passwordForm.reset();
    }
  }
}