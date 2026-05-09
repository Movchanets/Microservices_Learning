import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { LoginCredentials, RegisterCredentials, User } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = '/bff';

  login(credentials: LoginCredentials): Promise<void> {
    return firstValueFrom(this.http.post<void>('/api/identity/auth/login', credentials));
  }

  register(credentials: RegisterCredentials): Promise<void> {
    return firstValueFrom(this.http.post<void>('/api/identity/auth/register', credentials));
  }

  logout(): Promise<void> {
    return firstValueFrom(this.http.get<void>(`${this.baseUrl}/logout`));
  }

  getUser(): Promise<User> {
    return firstValueFrom(this.http.get<User>(`${this.baseUrl}/user`));
  }
}
