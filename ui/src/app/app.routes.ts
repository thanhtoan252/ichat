import type { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  // ORDER IS LOAD-BEARING. Both this route and the auth one below sit at the empty path,
  // and the router takes the first whose children match the rest of the URL. The shell
  // has to come first: with auth first, `/` matched it, found no empty-path child to
  // render and stopped there — a blank page that never reached a guard.
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell').then((m) => m.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'chat' },
      {
        path: 'chat',
        loadChildren: () => import('./features/chat/chat.routes').then((m) => m.CHAT_ROUTES),
      },
      {
        path: 'documents',
        loadChildren: () =>
          import('./features/documents/documents.routes').then((m) => m.DOCUMENTS_ROUTES),
      },
      {
        path: 'retrieval',
        canActivate: [adminGuard],
        loadChildren: () =>
          import('./features/retrieval/retrieval.routes').then((m) => m.RETRIEVAL_ROUTES),
      },
      {
        path: 'settings',
        loadChildren: () =>
          import('./features/settings/settings.routes').then((m) => m.SETTINGS_ROUTES),
      },
    ],
  },
  // Sign-in sits outside the shell: no sidebar, no conversation history, nothing that
  // would need a session the reader does not have yet. Reached by backtracking out of
  // the shell route, which owns no `login` child.
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
