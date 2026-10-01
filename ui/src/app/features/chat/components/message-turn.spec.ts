import { provideZonelessChangeDetection } from '@angular/core';
import { render, screen } from '@testing-library/angular';
import { describe, expect, it } from 'vitest';
import type { ChatMessage } from '../data/chat.model';
import { MessageTurn } from './message-turn';

/**
 * The API serialises `MessageRole` with the default `JsonStringEnumConverter`, so the
 * wire values are `User`/`Assistant`, not lowercase. These fixtures deliberately use the
 * exact strings the endpoint returns.
 */
function message(overrides: Partial<ChatMessage> = {}): ChatMessage {
  return {
    id: 'm1',
    role: 'User',
    content: 'What does the knowledge base say about setup?',
    rewrittenQuery: null,
    provider: null,
    model: null,
    inputTokens: null,
    outputTokens: null,
    latencyMs: null,
    retrievalMs: null,
    createdAt: '2026-08-29T07:16:56Z',
    citations: [],
    ...overrides,
  };
}

async function renderTurn(value: ChatMessage) {
  const { fixture } = await render(MessageTurn, {
    inputs: { message: value },
    providers: [provideZonelessChangeDetection()],
  });

  await fixture.whenStable();

  return fixture.nativeElement as HTMLElement;
}

describe('MessageTurn', () => {
  it('renders a user turn as an end-aligned bubble', async () => {
    const host = await renderTurn(message());

    expect(host.querySelector('[data-slot="bubble"]')).not.toBeNull();
    expect(host.querySelector('[data-slot="message"]')?.getAttribute('data-align')).toBe('end');
  });

  it('renders an assistant turn as answer content with a copy action', async () => {
    const host = await renderTurn(
      message({
        id: 'm2',
        role: 'Assistant',
        content: 'No matching passages.',
        model: 'gpt-4o-mini',
      }),
    );

    expect(host.querySelector('[data-slot="bubble"]')).toBeNull();
    expect(screen.getByLabelText('Copy answer')).toBeTruthy();
  });
});
