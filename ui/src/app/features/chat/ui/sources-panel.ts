import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  viewChildren,
} from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmBadge } from '@app/ui/badge';
import { HlmButton } from '@app/ui/button';
import { HlmSeparator } from '@app/ui/separator';
import type { SourceKind, SourceView } from '../model/chat-stream.model';

/**
 * The context the answer was built from, one card per source.
 *
 * The panel is the other half of a cited answer: `[3]` in the text is only meaningful
 * if the reader can see the passage behind it, so clicking a marker scrolls the
 * matching card into view and highlights it.
 *
 * Wide enough and it sits beside the thread; below that it slides over as a drawer.
 * It cannot simply be hidden on narrow screens — the citation markers and the header
 * toggle stay on offer there, and a control that opens nothing is worse than no panel.
 */
@Component({
  selector: 'app-sources-panel',
  imports: [NgIcon, HlmBadge, HlmButton, HlmSeparator],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class:
      'bg-card fixed inset-y-0 end-0 z-40 flex h-full w-[22rem] max-w-[85vw] shrink-0 flex-col border-s shadow-xl lg:static lg:z-auto lg:max-w-none lg:shadow-none xl:w-[26rem]',
    '(keydown.escape)': 'closeRequested.emit()',
    tabindex: '-1',
  },
  templateUrl: './sources-panel.html',
})
export class SourcesPanel {
  private readonly cards = viewChildren<ElementRef<HTMLElement>>('card');

  public readonly sources = input.required<readonly SourceView[]>();
  /**
   * Which set is on screen. A reopened thread can only offer the citations the API
   * stored, so the panel says so instead of looking like a shorter version of itself.
   */
  public readonly kind = input<SourceKind>('retrieved');
  /** Marker index to reveal, set when the reader clicks a citation chip. */
  public readonly highlighted = input<number | null>(null);

  public readonly closeRequested = output<void>();

  /** Set only for a thread read back from history, where the API stored no passages. */
  protected readonly passagesUnavailable = computed(
    () => this.sources().length > 0 && this.sources().every((source) => !source.snippet),
  );

  constructor() {
    effect(() => {
      const index = this.highlighted();

      if (index === null) {
        return;
      }

      const card = this.cards().find(
        (item) => item.nativeElement.dataset['sourceIndex'] === String(index),
      );

      card?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
  }
}
