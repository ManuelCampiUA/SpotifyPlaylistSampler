import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'auth/callback',
    loadComponent: () =>
      import('./features/callback/callback.component').then((m) => m.CallbackComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'canvas',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/canvas/canvas.component').then((m) => m.CanvasComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
