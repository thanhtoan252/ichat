import type { Routes } from '@angular/router';

export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    title: 'Settings · iChat',
    loadComponent: () => import('./settings-page').then((m) => m.SettingsPage),
  },
];
