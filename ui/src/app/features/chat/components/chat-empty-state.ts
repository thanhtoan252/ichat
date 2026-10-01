import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgIcon } from '@ng-icons/core';

/** Presentational. The cold-start screen. */
@Component({
  selector: 'app-chat-empty-state',
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-h-full flex-col items-center justify-center py-12 text-center' },
  templateUrl: './chat-empty-state.html',
})
export class ChatEmptyState {}
