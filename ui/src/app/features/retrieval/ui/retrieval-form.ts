import { FormsModule } from '@angular/forms';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import { HlmInput } from '@app/ui/input';
import { HlmSeparator } from '@app/ui/separator';
import { HlmSwitch } from '@app/ui/switch';
import { HlmTooltip } from '@app/ui/tooltip';
import type { SearchCriteria, SearchMode } from '../model/search.model';

const MODES: readonly SearchMode[] = ['Hybrid', 'Vector', 'FullText', 'Trigram'];

type ToggleKey = 'rewrite' | 'applyMmr' | 'expandNeighbors' | 'rerank';

interface Toggle {
  readonly key: ToggleKey;
  readonly label: string;
  readonly hint: string;
}

const TOGGLES: readonly Toggle[] = [
  { key: 'rewrite', label: 'Rewrite', hint: 'Rewrite the question into a standalone form first' },
  { key: 'applyMmr', label: 'MMR', hint: 'Drop near-duplicate chunks before assembling context' },
  { key: 'expandNeighbors', label: 'Neighbors', hint: 'Pull in the chunks adjacent to each hit' },
  { key: 'rerank', label: 'Rerank', hint: 'Re-score the surviving candidates' },
];

/**
 * Presentational. Takes the whole criteria object and emits a new one on every edit,
 * rather than exposing one input/output pair per control.
 */
@Component({
  selector: 'app-retrieval-form',
  imports: [FormsModule, NgIcon, HlmButton, HlmInput, HlmSeparator, HlmSwitch, HlmTooltip],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'bg-card block rounded-xl border p-4' },
  templateUrl: './retrieval-form.html',
})
export class RetrievalForm {
  public readonly criteria = input.required<SearchCriteria>();
  public readonly running = input(false);
  public readonly canRun = input(false);

  public readonly criteriaChange = output<SearchCriteria>();
  public readonly run = output<void>();

  protected readonly modes = MODES;
  protected readonly toggles = TOGGLES;

  protected isEnabled(key: ToggleKey): boolean {
    return this.criteria()[key];
  }

  protected setToggle(key: ToggleKey, value: boolean): void {
    this.patch({ [key]: value } as Partial<SearchCriteria>);
  }

  protected patch(change: Partial<SearchCriteria>): void {
    this.criteriaChange.emit({ ...this.criteria(), ...change });
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.run.emit();
  }
}
