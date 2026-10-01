import type { Routes } from '@angular/router';

export const CHAT_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Chat · iChat',
    loadComponent: () => import('./pages/chat-page').then((m) => m.ChatPage),
  },
  {
    path: ':conversationId',
    title: 'Chat · iChat',
    loadComponent: () => import('./pages/chat-page').then((m) => m.ChatPage),
  },
];
