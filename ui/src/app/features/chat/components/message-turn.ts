import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmBadge } from '@app/ui/badge';
import { HlmBubble, HlmBubbleContent } from '@app/ui/bubble';
import { HlmButton } from '@app/ui/button';
import { HlmMessage, HlmMessageContent, HlmMessageFooter } from '@app/ui/message';
import { HlmTooltip } from '@app/ui/tooltip';
import type { ChatMessage } from '../data/chat.model';
import { AnswerContent } from '@app/shared/ui/answer-content/answer-content';

/**
 * Presentational. One persisted message. User turns render as a bubble; assistant turns render as
 * markdown with citation chips plus the provider/latency footer, because for a RAG
 * answer *where it came from* is part of the answer.
 */
@Component({
  selector: 'app-message-turn',
  imports: [
    DatePipe,
    NgIcon,
    HlmBadge,
    HlmBubble,
    HlmBubbleContent,
    HlmButton,
    HlmMessage,
    HlmMessageContent,
    HlmMessageFooter,
    HlmTooltip,
    AnswerContent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './message-turn.html',
})
export class MessageTurn {
  public readonly message = input.required<ChatMessage>();

  /** The marker, and the answer it was clicked in — the panel needs both. */
  public readonly citationClick = output<{ markerIndex: number; messageId: string }>();
  public readonly copyRequest = output<string>();

  protected readonly citations = computed(() =>
    [...this.message().citations].sort((a, b) => a.markerIndex - b.markerIndex),
  );

  /**
   * The highest marker the API verified for this turn. Markers above it are left as
   * plain text, matching what the server was willing to attribute.
   */
  protected readonly citationCeiling = computed(() =>
    this.citations().reduce((highest, citation) => Math.max(highest, citation.markerIndex), 0),
  );

  protected readonly meta = computed(() => {
    const message = this.message();

    if (!message.model && message.latencyMs === null) {
      return null;
    }

    const parts: string[] = [];

    if (message.provider) {
      parts.push(message.provider);
    }

    if (message.model) {
      parts.push(message.model);
    }

    if (message.latencyMs !== null) {
      parts.push(`${(message.latencyMs / 1000).toFixed(1)}s`);
    }

    if (message.retrievalMs !== null) {
      parts.push(`retrieval ${message.retrievalMs}ms`);
    }

    if (message.inputTokens !== null || message.outputTokens !== null) {
      parts.push(`${message.inputTokens ?? 0} in / ${message.outputTokens ?? 0} out tokens`);
    }

    return {
      short: message.model ?? `${((message.latencyMs ?? 0) / 1000).toFixed(1)}s`,
      long: parts.join(' · '),
    };
  });
}
