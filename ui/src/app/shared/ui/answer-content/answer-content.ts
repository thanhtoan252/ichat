import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  inject,
  input,
  output,
} from '@angular/core';
import { renderAnswer } from '@app/shared/util/markdown';

/**
 * Renders one assistant answer and reports clicks on its `[n]` citation chips.
 *
 * The chips are plain DOM created by the renderer rather than Angular components: an
 * answer arrives as a markdown string and re-renders on every streamed token, so
 * building a component tree per token would cost far more than one `innerHTML` swap.
 * Clicks are caught by delegation on the host.
 */
@Component({
  selector: 'app-answer-content',
  // No template on purpose: the rendered fragment is swapped in imperatively below.
  template: '',
  host: {
    class: 'prose-answer block',
    '(click)': 'onClick($event)',
  },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnswerContent {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  public readonly content = input.required<string>();
  public readonly sourceCount = input<number>(0);
  public readonly streaming = input<boolean>(false);

  public readonly citationClick = output<number>();

  constructor() {
    effect(() => {
      const fragment = renderAnswer(this.content(), {
        sourceCount: this.sourceCount() || undefined,
        streaming: this.streaming(),
      });

      this.host.nativeElement.replaceChildren(fragment);
    });
  }

  protected onClick(event: MouseEvent): void {
    const target = (event.target as HTMLElement | null)?.closest<HTMLElement>(
      '[data-citation-marker]',
    );

    const marker = target?.dataset['citationMarker'];

    if (marker) {
      event.preventDefault();
      this.citationClick.emit(Number(marker));
    }
  }
}
