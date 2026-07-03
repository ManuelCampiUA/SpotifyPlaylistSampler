import { Injectable, computed, signal } from '@angular/core';
import { UserInfo } from '../models/auth.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'spotify_jwt';

  readonly currentUser = signal<UserInfo | null>(null);
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  constructor() {
    this.loadUserFromToken();
  }

  login(): void {
    window.location.href = `${environment.apiUrl}/api/auth/login`;
  }

  handleCallback(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.loadUserFromToken();
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUser.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isTokenValid(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  private loadUserFromToken(): void {
    const token = this.getToken();
    if (!token) {
      this.currentUser.set(null);
      return;
    }

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));

      if (payload.exp * 1000 <= Date.now()) {
        this.logout();
        return;
      }

      this.currentUser.set({
        spotifyId: payload.sub,
        displayName: payload.name,
        email: payload.email,
        imageUrl: payload.picture || undefined,
      });
    } catch {
      this.logout();
    }
  }
}
