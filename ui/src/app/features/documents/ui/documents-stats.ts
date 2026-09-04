import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface DocumentStat {
  readonly label: string;
  readonly value: number;
}

/** Presentational. The four counters above the table. */
@Component({
  selector: 'app-documents-stats',
  imports: [DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'grid grid-cols-2 gap-3 sm:grid-cols-4' },
  templateUrl: './documents-stats.html',
})
export class DocumentsStats {
  public readonly stats = input.required<readonly DocumentStat[]>();
}
