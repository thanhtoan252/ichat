import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import { HlmSeparator } from '@app/ui/separator';
import { HlmSidebarTrigger } from '@app/ui/sidebar';

/** Presentational. Thread title plus the sources toggle. */
@Component({
  selector: 'app-chat-header',
  imports: [NgIcon, HlmButton, HlmSeparator, HlmSidebarTrigger],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex h-14 shrink-0 items-center gap-2 border-b px-3 sm:px-4' },
  templateUrl: './chat-header.html',
})
export class ChatHeader {
  public readonly title = input.required<string>();
  public readonly sourceCount = input(0);
  public readonly sourcesOpen = input(false);

  public readonly toggleSources = output<void>();
}
