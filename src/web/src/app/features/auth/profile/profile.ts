import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ProfileSidebarComponent } from './components/profile-sidebar/profile-sidebar';

@Component({
  selector: 'app-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterModule, ProfileSidebarComponent],
  template: `
    <div class="min-h-[calc(100vh-80px)] max-w-7xl mx-auto p-4 md:p-8">
      <div class="grid grid-cols-1 md:grid-cols-12 gap-8">
        
        <!-- Sidebar Navigation -->
        <aside class="md:col-span-4 lg:col-span-3">
          <app-profile-sidebar></app-profile-sidebar>
        </aside>

        <!-- Main Content Area -->
        <main class="md:col-span-8 lg:col-span-9">
          <router-outlet></router-outlet>
        </main>
        
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
    `,
  ],
})
export class ProfileComponent {}