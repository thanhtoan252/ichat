import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { HlmButton } from '@app/ui/button';
import type { StatusFilter } from '../data/document.model';

/** Presentational. Ingestion-status filter pills. */
@Component({
  selector: 'app-status-filter',
  imports: [HlmButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-wrap items-center gap-1.5' },
  templateUrl: './status-filter.html',
})
export class StatusFilterBar {
  public readonly filters = input.required<readonly StatusFilter[]>();
  public readonly active = input.required<StatusFilter>();

  public readonly filterChange = output<StatusFilter>();
}
