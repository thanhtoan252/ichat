import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ConversationsApi } from '../data/conversations.api';
import type { ConversationDto } from '../data/conversations.dto';
import type { ChatStreamEvent } from '../data/chat-stream.model';
import { ChatStore } from './chat.store';

vi.mock('ngx-sonner', () => ({
  toast: Object.assign(vi.fn(), {
    error: vi.fn(),
    success: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  }),
}));

const CONVERSATION: ConversationDto = {
  id: 'c-1',
  title: 'New conversation',
  userId: null,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

function stream(...events: ChatStreamEvent[]) {
  return async function* () {
    for (const event of events) {
      yield event;
    }
  };
}

function makeStore(api: Partial<ConversationsApi>): ChatStore {
  TestBed.configureTestingModule({
    providers: [ChatStore, { provide: ConversationsApi, useValue: api }],
  });

  return TestBed.inject(ChatStore);
}

describe('ChatStore.send', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('reports a failure when the conversation cannot be created', async () => {
    const store = makeStore({
      create: vi.fn().mockRejectedValue(new Error('offline')),
      streamAnswer: vi.fn(),
    });

    await store.send('Câu hỏi của tôi');

    // The turn has to stay on screen: the question was already cleared from the
    // composer, so losing it here loses the user's typing outright.
    expect(store.pending()).not.toBeNull();
    expect(store.pending()?.question).toBe('Câu hỏi của tôi');
    expect(store.pending()?.failure).toBeTruthy();
  });

  it('keeps the failed question retrievable so it can be retried', async () => {
    const store = makeStore({
      create: vi.fn().mockRejectedValue(new Error('offline')),
      streamAnswer: vi.fn(),
    });

    await store.send('Xin chào');

    expect(store.lastFailedQuestion()).toBe('Xin chào');
  });

  it('surfaces an error event from the stream', async () => {
    const store = makeStore({
      create: vi.fn().mockResolvedValue(CONVERSATION),
      streamAnswer: stream({
        type: 'error',
        data: { code: 'rate_limited', message: 'Too many requests.' },
      }) as unknown as ConversationsApi['streamAnswer'],
    });

    await store.send('Câu hỏi');

    expect(store.pending()?.failure).toBe('Too many requests.');
  });

  it('surfaces a transport failure', async () => {
    const store = makeStore({
      create: vi.fn().mockResolvedValue(CONVERSATION),
      streamAnswer: (() => {
        throw new Error('Cannot reach the API.');
      }) as unknown as ConversationsApi['streamAnswer'],
    });

    await store.send('Câu hỏi');

    expect(store.pending()?.failure).toContain('Cannot reach the API.');
  });

  it('commits the turn when the stream completes', async () => {
    const store = makeStore({
      create: vi.fn().mockResolvedValue(CONVERSATION),
      streamAnswer: stream(
        { type: 'delta', data: { text: 'Câu trả lời' } },
        {
          type: 'done',
          data: {
            messageId: 'm-1',
            citations: [],
            provider: 'OpenAI',
            model: 'gpt-4o-mini',
            inputTokens: 10,
            outputTokens: 5,
            latencyMs: 900,
            retrievalMs: 120,
            degraded: false,
            interrupted: false,
          },
        },
      ) as unknown as ConversationsApi['streamAnswer'],
    });

    await store.send('Câu hỏi');

    expect(store.pending()).toBeNull();
    expect(store.messages()).toHaveLength(2);
    expect(store.messages()[1].content).toBe('Câu trả lời');
  });
});

describe('ChatStore.closeConversation', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('returns to an empty thread so /chat does not keep showing the last conversation', async () => {
    const store = makeStore({
      create: vi.fn().mockResolvedValue(CONVERSATION),
      streamAnswer: stream(
        { type: 'delta', data: { text: 'Câu trả lời' } },
        {
          type: 'done',
          data: {
            messageId: 'm-1',
            citations: [],
            provider: 'OpenAI',
            model: 'gpt-4o-mini',
            inputTokens: 10,
            outputTokens: 5,
            latencyMs: 900,
            retrievalMs: 120,
            degraded: false,
            interrupted: false,
          },
        },
      ) as unknown as ConversationsApi['streamAnswer'],
    });

    await store.send('Câu hỏi');
    expect(store.messages()).toHaveLength(2);

    store.closeConversation();

    expect(store.isEmptyThread()).toBe(true);
    expect(store.messages()).toEqual([]);
    // The next question must start its own conversation, not append to the one just left.
    expect(store.activeId()).toBeNull();
    expect(store.visibleSources()).toEqual([]);
  });

  it('keeps the sidebar history, which is not part of the open thread', async () => {
    const store = makeStore({
      list: vi
        .fn()
        .mockResolvedValue({ items: [CONVERSATION], page: 1, pageSize: 50, totalCount: 1 }),
      create: vi.fn(),
      streamAnswer: vi.fn(),
    });

    await store.loadConversations();
    store.closeConversation();

    expect(store.conversations()).toHaveLength(1);
  });
});
