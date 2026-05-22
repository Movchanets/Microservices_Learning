import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  LoginCredentials,
  RegisterCredentials,
  User,
  UpdateProfileRequest,
  ChangePasswordRequest,
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/bff';
  private readonly authBaseUrl = '/bff/auth';

  login(credentials: LoginCredentials): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.authBaseUrl}/login`, credentials));
  }

  register(credentials: RegisterCredentials): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.authBaseUrl}/register`, credentials));
  }

  logout(): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.authBaseUrl}/logout`, {}));
  }

  getUser(): Promise<User> {
    return firstValueFrom(this.http.get<User>(`${this.baseUrl}/user`));
  }

  ensureCsrf(): Promise<void> {
    return firstValueFrom(this.http.get<void>(`${this.baseUrl}/csrf`));
  }

  forgotPassword(email: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.authBaseUrl}/forgot-password`, { email }));
  }

  updateProfile(id: string, request: Partial<UpdateProfileRequest>): Promise<void> {
    return firstValueFrom(this.http.put<void>(`/api/identity/users/${id}/profile`, request));
  }

  changePassword(request: ChangePasswordRequest): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/identity/auth/change-password`, request));
  }
}
