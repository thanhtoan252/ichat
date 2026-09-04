import type { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/app-shell').then((m) => m.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'chat' },
      {
        path: 'chat',
        loadChildren: () => import('./features/chat').then((m) => m.CHAT_ROUTES),
      },
      {
        path: 'documents',
        loadChildren: () => import('./features/documents').then((m) => m.DOCUMENTS_ROUTES),
      },
      {
        path: 'retrieval',
        loadChildren: () => import('./features/retrieval').then((m) => m.RETRIEVAL_ROUTES),
      },
      {
        path: 'settings',
        loadChildren: () =>
          import('./features/settings/settings.routes').then((m) => m.SETTINGS_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
