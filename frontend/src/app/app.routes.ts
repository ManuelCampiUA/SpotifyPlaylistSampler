import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'canvas',
    loadComponent: () => import('./features/canvas/canvas.component').then((m) => m.CanvasComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
