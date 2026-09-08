import type { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    title: 'Sign in · iChat',
    loadComponent: () => import('./pages/login-page').then((m) => m.LoginPage),
  },
];
