import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import { render, screen } from '@testing-library/angular';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { ConversationsApi } from '@app/core/api/conversations.api';
import { provideSpartanHlm } from '@app/ui/utils';
import type { ChatMessage } from '@app/core/api/api.models';
import { APP_ICONS } from '@app/core/icons';
import { ChatStore } from './chat.store';
import { ChatPage } from './chat-page';

/**
 * The test DOM implements neither API. `matchMedia` only picks the sources panel's start
 * state, and `scrollTo` only pins the thread to the newest turn — neither is what these
 * tests assert, but both throw from lifecycle code if left missing.
 */
beforeAll(() => {
  Element.prototype.scrollTo ??= (() => undefined) as Element['scrollTo'];

  window.matchMedia ??= ((query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => false,
    }) as unknown as MediaQueryList) as typeof window.matchMedia;
});

vi.mock('ngx-sonner', () => ({
  toast: Object.assign(vi.fn(), {
    error: vi.fn(),
    success: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  }),
}));

const MESSAGES: ChatMessage[] = [
  {
    id: 'm-1',
    role: 'User',
    content: 'Câu hỏi cũ',
    rewrittenQuery: null,
    provider: null,
    model: null,
    inputTokens: null,
    outputTokens: null,
    latencyMs: null,
    retrievalMs: null,
    createdAt: '2026-08-29T07:16:56Z',
    citations: [],
  },
];

function page(items: readonly ChatMessage[] = []) {
  return { items, page: 1, pageSize: 200, totalCount: items.length };
}

async function renderPage(conversationId: string | undefined) {
  const api: Partial<ConversationsApi> = {
    list: vi.fn().mockResolvedValue(page()),
    messages: vi.fn().mockResolvedValue(page(MESSAGES)),
  };

  const result = await render(ChatPage, {
    inputs: { conversationId },
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      provideIcons(APP_ICONS),
      provideSpartanHlm(),
      { provide: ConversationsApi, useValue: api },
    ],
  });

  await result.fixture.whenStable();

  return result;
}

describe('ChatPage routing', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows the thread of the routed conversation', async () => {
    const { fixture } = await renderPage('c-1');
    const store = fixture.debugElement.injector.get(ChatStore);

    expect(store.activeId()).toBe('c-1');
    expect(screen.getByText('Câu hỏi cũ')).toBeTruthy();
  });

  it('falls back to the empty state when navigating back to /chat', async () => {
    const { fixture, rerender } = await renderPage('c-1');
    const store = fixture.debugElement.injector.get(ChatStore);

    // Leaving the conversation: the URL no longer names one.
    await rerender({ inputs: { conversationId: undefined } });
    await fixture.whenStable();

    expect(store.activeId()).toBeNull();
    expect(store.isEmptyThread()).toBe(true);
    expect(screen.queryByText('Câu hỏi cũ')).toBeNull();
    expect(screen.getByText('Ask your documents')).toBeTruthy();
  });
});
