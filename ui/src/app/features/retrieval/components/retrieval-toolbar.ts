import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmBadge } from '@app/ui/badge';
import { HlmSeparator } from '@app/ui/separator';
import { HlmSidebarTrigger } from '@app/ui/sidebar';
import { HlmTooltip } from '@app/ui/tooltip';

/** Presentational. Header showing how the last trace went. */
@Component({
  selector: 'app-retrieval-toolbar',
  imports: [NgIcon, HlmBadge, HlmSeparator, HlmSidebarTrigger, HlmTooltip],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex h-14 shrink-0 items-center gap-2 border-b px-3 sm:px-4' },
  templateUrl: './retrieval-toolbar.html',
})
export class RetrievalToolbar {
  public readonly elapsedMs = input<number | null>(null);
  public readonly degraded = input(false);
}
