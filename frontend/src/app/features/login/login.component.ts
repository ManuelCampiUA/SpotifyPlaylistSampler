import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="login-page">
      <div class="login-card">
        <div class="login-icon">
          <mat-icon>library_music</mat-icon>
        </div>
        <h1>Playlist Sampler</h1>
        <p>Accedi con il tuo account Spotify per analizzare le tue playlist e costruire il tuo albero musicale.</p>

        @if (errorMessage) {
          <div class="login-error">
            <mat-icon>error_outline</mat-icon>
            <span>{{ errorMessage }}</span>
          </div>
        }

        <button mat-flat-button class="spotify-btn" (click)="login()">
          <svg class="spotify-logo" viewBox="0 0 24 24" width="22" height="22">
            <path fill="currentColor" d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.359-.66.48-1.021.24-2.82-1.74-6.36-2.101-10.561-1.141-.418.122-.779-.179-.899-.539-.12-.421.18-.78.54-.9 4.56-1.021 8.52-.6 11.64 1.32.42.18.479.659.301 1.02zm1.44-3.3c-.301.42-.841.6-1.262.3-3.239-1.98-8.159-2.58-11.939-1.38-.479.12-1.02-.12-1.14-.6-.12-.48.12-1.021.6-1.141C9.6 9.9 15 10.561 18.72 12.84c.361.181.54.78.241 1.2zm.12-3.36C15.24 8.4 8.82 8.16 5.16 9.301c-.6.179-1.2-.181-1.38-.721-.18-.601.18-1.2.72-1.381 4.26-1.26 11.28-1.02 15.721 1.621.539.3.719 1.02.419 1.56-.299.421-1.02.599-1.559.3z"/>
          </svg>
          Accedi con Spotify
        </button>
      </div>
    </div>
  `,
  styles: [`
    .login-page {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: #000;
    }

    .login-card {
      text-align: center;
      padding: 3rem 2.5rem;
      max-width: 420px;
      width: 100%;
    }

    .login-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: rgba(76, 175, 80, 0.12);
      border: 1px solid rgba(76, 175, 80, 0.25);
      margin-bottom: 1.5rem;

      mat-icon {
        font-size: 42px;
        width: 42px;
        height: 42px;
        color: #4caf50;
      }
    }

    h1 {
      margin: 0 0 0.75rem;
      font-size: 2rem;
      font-weight: 700;
      background: linear-gradient(135deg, #4caf50, #80e27e);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    p {
      color: #9e9e9e;
      font-size: 0.95rem;
      line-height: 1.6;
      margin: 0 0 2rem;
    }

    .login-error {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      margin-bottom: 1.5rem;
      background: rgba(211, 47, 47, 0.1);
      border: 1px solid rgba(211, 47, 47, 0.3);
      border-radius: 8px;
      color: #ef5350;
      font-size: 0.85rem;

      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .spotify-btn {
      background: #1ed760 !important;
      color: #000 !important;
      font-size: 1rem;
      font-weight: 600;
      padding: 0 2rem !important;
      height: 52px;
      border-radius: 26px !important;
      gap: 0.5rem;

      &:hover { background: #1fdf64 !important; }
    }

    .spotify-logo { flex-shrink: 0; }
  `],
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  errorMessage: string | null = null;

  constructor() {
    const error = this.route.snapshot.queryParamMap.get('error');
    if (error === 'access_denied') {
      this.errorMessage = 'Accesso negato. Riprova.';
    } else if (error) {
      this.errorMessage = 'Errore durante il login. Riprova.';
    }
  }

  login(): void {
    this.authService.login();
  }
}
