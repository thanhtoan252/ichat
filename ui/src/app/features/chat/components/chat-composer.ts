import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import { HlmKbd, HlmKbdGroup } from '@app/ui/kbd';

const MAX_ROWS_HEIGHT = 220;

/**
 * The question box.
 *
 * Enter sends and Shift+Enter inserts a newline — the convention every chat client
 * shares, and worth the explicit key handling because a multi-line question is common
 * when quoting a document.
 */
@Component({
  selector: 'app-chat-composer',
  imports: [NgIcon, HlmButton, HlmKbd, HlmKbdGroup],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './chat-composer.html',
})
export class ChatComposer {
  private readonly input = viewChild.required<ElementRef<HTMLTextAreaElement>>('input');

  public readonly streaming = input<boolean>(false);
  public readonly disabled = input<boolean>(false);
  public readonly placeholder = input<string>('Ask anything about your documents…');

  public readonly send = output<string>();
  public readonly stop = output<void>();

  protected readonly draft = signal('');

  protected readonly canSend = computed(
    () => !this.disabled() && !this.streaming() && this.draft().trim().length > 0,
  );

  constructor() {
    // `field-sizing-content` does the growing; this only caps it so a pasted page of
    // text cannot push the thread off screen.
    effect(() => {
      this.draft();
      const element = this.input().nativeElement;

      element.style.overflowY = element.scrollHeight > MAX_ROWS_HEIGHT ? 'auto' : 'hidden';
    });
  }

  public focus(): void {
    this.input().nativeElement.focus();
  }

  protected onInput(event: Event): void {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  protected onKeydown(event: KeyboardEvent): void {
    // `isComposing` guards IME input: Enter commits a candidate there, it does not send.
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      this.submit();
    }
  }

  protected submit(): void {
    if (!this.canSend()) {
      return;
    }

    this.send.emit(this.draft().trim());
    this.draft.set('');
    this.input().nativeElement.value = '';
  }
}
