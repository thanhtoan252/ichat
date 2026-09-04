import type { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/app-shell').then((m) => m.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'chat' },
      {
        path: 'chat',
        title: 'Chat · iChat',
        loadComponent: () => import('./features/chat/chat-page').then((m) => m.ChatPage),
      },
      {
        path: 'chat/:conversationId',
        title: 'Chat · iChat',
        loadComponent: () => import('./features/chat/chat-page').then((m) => m.ChatPage),
      },
      {
        path: 'documents',
        title: 'Knowledge base · iChat',
        loadComponent: () =>
          import('./features/documents/documents-page').then((m) => m.DocumentsPage),
      },
      {
        path: 'retrieval',
        title: 'Retrieval lab · iChat',
        loadComponent: () =>
          import('./features/retrieval/retrieval-page').then((m) => m.RetrievalPage),
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings').then((m) => m.SETTINGS_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
