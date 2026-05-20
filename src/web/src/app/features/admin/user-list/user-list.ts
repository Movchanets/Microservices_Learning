// User list component for admin panel.
// Displays a table of all users with role management and deactivation.

import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { AdminStore } from '../admin.store';
import { AdminUser } from '../admin.models';

@Component({
  selector: 'app-user-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LucideAngularModule],
  template: `
    <div class="bg-card rounded-3xl border border-border overflow-hidden">
      @if (store.loading()) {
        <div class="flex justify-center p-12">
          <div class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"></div>
        </div>
      } @else if (store.error()) {
        <div class="p-4 m-4 bg-red-500/10 text-red-500 rounded-xl">
          {{ store.error() }}
        </div>
      } @else if (!store.hasUsers()) {
        <div class="text-center py-12 text-muted">
          <lucide-icon name="Users" class="w-12 h-12 mx-auto mb-3 opacity-30"></lucide-icon>
          <p>No users found</p>
        </div>
      } @else {
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-border text-left text-muted">
                <th class="p-4 font-medium">Name</th>
                <th class="p-4 font-medium">Email</th>
                <th class="p-4 font-medium">Role</th>
                <th class="p-4 font-medium">Joined</th>
                <th class="p-4 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (user of store.users(); track user.id) {
                <tr class="border-b border-border/50 hover:bg-muted/5 transition-colors">
                  <td class="p-4 font-medium text-foreground">
                    {{ user.firstName }} {{ user.lastName }}
                  </td>
                  <td class="p-4 text-muted">{{ user.email }}</td>
                  <td class="p-4">
                    <span [class]="roleBadgeClass(user.role)">{{ user.role }}</span>
                  </td>
                  <td class="p-4 text-muted">{{ user.createdAt | date:'short' }}</td>
                  <td class="p-4">
                    <div class="flex gap-2">
                      <select
                        [value]="user.role"
                        (change)="onRoleChange(user, $event)"
                        class="px-2 py-1 text-xs rounded-lg bg-background border border-border focus:outline-none focus:ring-2 focus:ring-primary">
                        <option value="Buyer">Buyer</option>
                        <option value="Seller">Seller</option>
                        <option value="Admin">Admin</option>
                      </select>
                      <button
                        (click)="onDeactivate(user)"
                        class="p-1.5 rounded-lg hover:bg-red-500/10 text-muted hover:text-red-500 transition-colors"
                        aria-label="Deactivate user">
                        <lucide-icon name="UserX" class="w-4 h-4"></lucide-icon>
                      </button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class UserListComponent implements OnInit {
  readonly store = inject(AdminStore);

  ngOnInit(): void {
    this.store.loadUsers();
  }

  roleBadgeClass(role: string): string {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold';
    const variants: Record<string, string> = {
      Admin: `${base} bg-purple-500/10 text-purple-500`,
      Seller: `${base} bg-blue-500/10 text-blue-500`,
      Buyer: `${base} bg-green-500/10 text-green-500`,
    };
    return variants[role] || base;
  }

  onRoleChange(user: AdminUser, event: Event): void {
    const role = (event.target as HTMLSelectElement).value as 'Buyer' | 'Seller' | 'Admin';
    this.store.updateUserRole(user.id, role);
  }

  onDeactivate(user: AdminUser): void {
    if (confirm(`Deactivate ${user.firstName} ${user.lastName}?`)) {
      this.store.deactivateUser(user.id);
    }
  }
}
