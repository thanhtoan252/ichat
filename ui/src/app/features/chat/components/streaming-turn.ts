import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmAlert, HlmAlertDescription, HlmAlertTitle } from '@app/ui/alert';
import { HlmBubble, HlmBubbleContent } from '@app/ui/bubble';
import { HlmButton } from '@app/ui/button';
import { HlmMarker, HlmMarkerContent, HlmMarkerIcon } from '@app/ui/marker';
import { HlmMessage, HlmMessageContent } from '@app/ui/message';
import { HlmSkeleton } from '@app/ui/skeleton';
import { AnswerContent } from '@app/shared/markdown/answer-content';
import type { PendingTurn } from '../chat.store';

const STAGE_LABELS: Readonly<Record<string, string>> = {
  rewriting: 'Rewriting the question',
  retrieving: 'Searching the knowledge base',
  generating: 'Composing the answer',
};

/**
 * The turn currently streaming: the question, the pipeline stage, and the answer as it
 * arrives. Naming the stage matters here — retrieval runs before the first token, and
 * without it the several seconds of silence look like a hang.
 */
@Component({
  selector: 'app-streaming-turn',
  imports: [
    NgIcon,
    HlmAlert,
    HlmAlertDescription,
    HlmAlertTitle,
    HlmBubble,
    HlmBubbleContent,
    HlmButton,
    HlmMarker,
    HlmMarkerContent,
    HlmMarkerIcon,
    HlmMessage,
    HlmMessageContent,
    HlmSkeleton,
    AnswerContent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './streaming-turn.html',
})
export class StreamingTurn {
  public readonly turn = input.required<PendingTurn>();

  public readonly citationClick = output<number>();
  public readonly retry = output<void>();
  public readonly dismiss = output<void>();

  protected readonly sourceCount = computed(() => this.turn().sources.length);

  protected readonly stageLabel = computed(() => {
    const stage = this.turn().stage;

    if (!stage) {
      return this.turn().text.length > 0 ? null : 'Working on it';
    }

    return STAGE_LABELS[stage] ?? stage;
  });
}
