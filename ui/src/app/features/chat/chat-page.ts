import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { toast } from 'ngx-sonner';
import { HlmButton } from '@app/ui/button';
import { StickToBottom } from '@app/shared/scroll/stick-to-bottom';
import { ChatComposer } from './components/chat-composer';
import { ChatEmptyState } from './components/chat-empty-state';
import { ChatHeader } from './components/chat-header';
import { ChatThread } from './components/chat-thread';
import { SourcesPanel } from './components/sources-panel';
import { ChatStore } from './chat.store';

/** Matches the `lg` breakpoint the sources panel docks at. */
function matchesWideViewport(): boolean {
  return typeof window !== 'undefined' && window.matchMedia('(min-width: 64rem)').matches;
}

/**
 * Container. Owns the routed conversation and wires the store to the presentational
 * pieces below it; every child here takes inputs and emits outputs, nothing more.
 */
@Component({
  selector: 'app-chat-page',
  imports: [
    NgIcon,
    HlmButton,
    StickToBottom,
    ChatComposer,
    ChatEmptyState,
    ChatHeader,
    ChatThread,
    SourcesPanel,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-h-0 flex-1' },
  templateUrl: './chat-page.html',
})
export class ChatPage {
  private readonly router = inject(Router);
  private readonly scroll = viewChild(StickToBottom);
  private readonly composer = viewChild(ChatComposer);

  /** Bound from the route by `withComponentInputBinding`. */
  public readonly conversationId = input<string | undefined>(undefined);

  protected readonly store = inject(ChatStore);

  // Docked beside the thread on a wide screen, so it may start open there. Narrower it
  // is a drawer over the answer, and opening one uninvited is not the reader's choice.
  protected readonly sourcesOpen = signal(matchesWideViewport());
  protected readonly highlightedSource = signal<number | null>(null);

  constructor() {
    void this.store.loadConversations();

    effect(() => {
      const id = this.conversationId();

      if (id) {
        void this.store.openConversation(id);
      } else {
        // `/chat` with no id is a fresh thread, not "keep showing the last one".
        this.store.closeConversation();
      }
    });
  }

  /**
   * Any change to this re-pins the scroll container to the newest content. A computed,
   * not a method: a method would produce a new array on every check and scroll forever.
   */
  protected readonly followToken = computed(() => [
    this.store.messages(),
    this.store.pending()?.text,
  ]);

  protected async ask(question: string): Promise<void> {
    const hadConversation = this.store.activeId() !== null;

    this.scroll()?.reset();
    await this.store.send(question);

    // A cold start creates the conversation inside the store; reflect it in the URL so
    // the thread is linkable and a reload lands back on it.
    const id = this.store.activeId();

    if (!hadConversation && id) {
      await this.router.navigate(['/chat', id], { replaceUrl: true });
    }

    this.composer()?.focus();
  }

  protected async copy(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      toast.success('Answer copied.');
    } catch {
      toast.error('The browser blocked clipboard access.');
    }
  }

  protected revealSource(event: { markerIndex: number; messageId: string | null }): void {
    // A marker means "source 2 *of this answer*". Point the panel at that turn first,
    // or a click in an older answer would highlight the newest answer's second source.
    if (event.messageId) {
      this.store.focusTurn(event.messageId);
    }

    this.sourcesOpen.set(true);
    // Re-set through null so clicking the same marker twice scrolls again.
    this.highlightedSource.set(null);
    queueMicrotask(() => this.highlightedSource.set(event.markerIndex));
  }

  protected toggleSources(): void {
    this.sourcesOpen.update((open) => !open);
  }
}
