import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmBadge } from '@app/ui/badge';
import type { SearchStage } from '../model/search.model';

/**
 * Presentational. One expandable row per pipeline stage.
 *
 * A zero count is styled as destructive on purpose: seeing `fulltext: 0` next to the
 * tsquery that produced it is the fastest route to the usual retrieval bug.
 */
@Component({
  selector: 'app-stage-list',
  imports: [DecimalPipe, NgIcon, HlmBadge],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-2' },
  templateUrl: './stage-list.html',
})
export class StageList {
  public readonly stages = input.required<readonly SearchStage[]>();
  public readonly expanded = input<string | null>(null);

  public readonly stageToggle = output<string>();
}
