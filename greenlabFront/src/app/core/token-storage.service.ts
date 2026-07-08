import { Injectable, signal } from '@angular/core';
import { LoginResponse } from '../models/api.types';

const TOKEN_KEY = 'greenlab_token';
const USER_KEY = 'greenlab_user';

export interface StoredUser {
  email: string;
  fullName: string;
  role: string;
}

/** API may return camelCase or PascalCase depending on server config. */
function normalizeLoginResponse(res: LoginResponse & Record<string, unknown>): {
  token: string;
  email: string;
  fullName: string;
  role: string;
} {
  const token = (res.token ?? res['Token']) as string | undefined;
  const email = (res.email ?? res['Email']) as string | undefined;
  const fullName = (res.fullName ?? res['FullName']) as string | undefined;
  const role = (res.role ?? res['Role']) as string | undefined;
  return {
    token: token ?? '',
    email: email ?? '',
    fullName: fullName ?? '',
    role: role ?? '',
  };
}

function normalizeStoredUser(raw: Record<string, unknown>): StoredUser | null {
  const email = (raw['email'] ?? raw['Email']) as string | undefined;
  const fullName = (raw['fullName'] ?? raw['FullName']) as string | undefined;
  const role = (raw['role'] ?? raw['Role']) as string | undefined;
  if (!email && !fullName && !role) return null;
  return {
    email: email ?? '',
    fullName: fullName ?? '',
    role: role ?? '',
  };
}

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  readonly user = signal<StoredUser | null>(this.readUser());

  getToken(): string | null {
    const t = localStorage.getItem(TOKEN_KEY);
    if (!t || t === 'undefined' || t === 'null') return null;
    return t;
  }

  hasToken(): boolean {
    return !!this.getToken();
  }

  setSession(res: LoginResponse): void {
    const n = normalizeLoginResponse(res as LoginResponse & Record<string, unknown>);
    if (!n.token) return;
    localStorage.setItem(TOKEN_KEY, n.token);
    const u: StoredUser = { email: n.email, fullName: n.fullName, role: n.role };
    localStorage.setItem(USER_KEY, JSON.stringify(u));
    this.user.set(u);
  }

  clear(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.user.set(null);
  }

  readUser(): StoredUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      const parsed = JSON.parse(raw) as Record<string, unknown>;
      const u = normalizeStoredUser(parsed);
      if (u && (!parsed['role'] && parsed['Role'])) {
        localStorage.setItem(USER_KEY, JSON.stringify(u));
      }
      return u;
    } catch {
      return null;
    }
  }
}
