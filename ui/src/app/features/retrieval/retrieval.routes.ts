import type { Routes } from '@angular/router';

export const RETRIEVAL_ROUTES: Routes = [
  {
    path: '',
    title: 'Retrieval lab · iChat',
    loadComponent: () => import('./pages/retrieval-page').then((m) => m.RetrievalPage),
  },
];
