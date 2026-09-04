import type { Routes } from '@angular/router';

export const DOCUMENTS_ROUTES: Routes = [
  {
    path: '',
    title: 'Knowledge base · iChat',
    loadComponent: () => import('./documents-page').then((m) => m.DocumentsPage),
  },
];
