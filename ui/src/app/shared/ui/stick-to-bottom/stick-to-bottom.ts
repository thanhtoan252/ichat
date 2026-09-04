import { Directive, ElementRef, effect, inject, input, signal, untracked } from '@angular/core';

/**
 * Keeps a scroll container pinned to its newest content — but only while the reader is
 * already at the bottom.
 *
 * Following unconditionally is the single most annoying thing a chat UI can do: it yanks
 * the viewport away from the passage someone scrolled back to read. The `follow` input is
 * whatever value means "content changed"; reading it inside the effect is what schedules
 * the scroll.
 */
@Directive({
  selector: '[appStickToBottom]',
  exportAs: 'stickToBottom',
  host: { '(scroll)': 'onScroll()' },
})
export class StickToBottom {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Distance from the bottom, in pixels, still counted as "at the bottom". */
  public readonly threshold = input(64);

  /** Re-pins to the bottom whenever this changes. */
  public readonly follow = input<unknown>(null);

  private readonly atBottomSignal = signal(true);

  public readonly atBottom = this.atBottomSignal.asReadonly();

  constructor() {
    effect(() => {
      this.follow();

      if (untracked(this.atBottomSignal)) {
        // The DOM has not been updated yet when the effect runs.
        queueMicrotask(() => this.scrollToBottom('auto'));
      }
    });
  }

  public scrollToBottom(behavior: ScrollBehavior = 'smooth'): void {
    const element = this.host.nativeElement;

    element.scrollTo({ top: element.scrollHeight, behavior });
    this.atBottomSignal.set(true);
  }

  /** Call after content is replaced wholesale (a different thread) to re-arm following. */
  public reset(): void {
    this.atBottomSignal.set(true);
  }

  protected onScroll(): void {
    const element = this.host.nativeElement;
    const distance = element.scrollHeight - element.scrollTop - element.clientHeight;

    this.atBottomSignal.set(distance < this.threshold());
  }
}
