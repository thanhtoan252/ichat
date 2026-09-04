import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { HlmSkeleton } from '@app/ui/skeleton';
import type { ChatMessage } from '../model/chat.model';
import type { PendingTurn } from '../model/chat-stream.model';
import { MessageTurn } from './message-turn';
import { StreamingTurn } from './streaming-turn';

/** Presentational. The transcript: settled turns, then the one still streaming. */
@Component({
  selector: 'app-chat-thread',
  imports: [HlmSkeleton, MessageTurn, StreamingTurn],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  templateUrl: './chat-thread.html',
})
export class ChatThread {
  public readonly messages = input.required<readonly ChatMessage[]>();
  public readonly pending = input<PendingTurn | null>(null);
  public readonly loading = input(false);

  public readonly citationSelect = output<{ markerIndex: number; messageId: string | null }>();
  public readonly copyRequest = output<string>();
  public readonly retryFailure = output<void>();
  public readonly dismissFailure = output<void>();

  protected readonly skeletonRows = [0, 1];
}
