import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { LoginResponse } from '../models/api.types';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = '/api';

  readonly user;

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
    private readonly tokens: TokenStorageService
  ) {
    this.user = this.tokens.user;
  }

  isLoggedIn(): boolean {
    return this.tokens.hasToken();
  }

  getToken(): string | null {
    return this.tokens.getToken();
  }

  isAdmin(): boolean {
    const role = this.tokens.user()?.role?.trim();
    return role === 'Admin' || role?.toLowerCase() === 'admin';
  }

  isStaff(): boolean {
    const r = this.tokens.user()?.role;
    return r === 'Teacher' || r === 'TeacherAssistant';
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.api}/auth/login`, { email, password })
      .pipe(tap((res) => this.tokens.setSession(res)));
  }

  logout(): void {
    this.tokens.clear();
    void this.router.navigate(['/login']);
  }
}
