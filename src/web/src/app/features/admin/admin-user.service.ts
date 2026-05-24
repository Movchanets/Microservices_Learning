// Admin user service.
// Handles user management operations via Identity.API admin endpoints.
// Uses the BFF pattern - all calls go through /api/identity/users.

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AdminUser, UpdateUserRoleRequest } from './admin.models';

@Injectable({ providedIn: 'root' })
export class AdminUserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/identity/users';

  async getAllUsers(): Promise<AdminUser[]> {
    return firstValueFrom(
      this.http.get<AdminUser[]>(this.baseUrl)
    );
  }

  async getUserById(id: string): Promise<AdminUser> {
    return firstValueFrom(
      this.http.get<AdminUser>(`${this.baseUrl}/${id}`)
    );
  }

  async updateUserRole(id: string, request: UpdateUserRoleRequest): Promise<AdminUser> {
    return firstValueFrom(
      this.http.put<AdminUser>(`${this.baseUrl}/${id}/role`, request)
    );
  }

  async deactivateUser(id: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${id}`)
    );
  }
}
