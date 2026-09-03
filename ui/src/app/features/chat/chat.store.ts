import { computed, inject, Injectable, signal } from '@angular/core';
import { toast } from 'ngx-sonner';
import { ConversationsApi } from '@app/core/api/conversations.api';
import { describeHttpError } from '@app/core/api/http-error';
import type {
  ChatMessage,
  ChatStage,
  Citation,
  Conversation,
  DonePayload,
  SourceView,
} from '@app/core/api/api.models';

/** Whether the panel holds the live retrieved context or citations read back from history. */
export type SourceKind = 'retrieved' | 'cited';

interface SourceSelection {
  readonly sources: readonly SourceView[];
  readonly kind: SourceKind;
}

/** The turn currently in flight. Absent between turns. */
export interface PendingTurn {
  readonly question: string;
  readonly stage: ChatStage | string | null;
  readonly sources: readonly SourceView[];
  readonly text: string;
  readonly failure: string | null;
}

const EMPTY_TURN: PendingTurn = {
  question: '',
  stage: null,
  sources: [],
  text: '',
  failure: null,
};

/**
 * Single owner of the chat screen's state.
 *
 * Deltas are buffered and flushed on an animation frame rather than written straight
 * to the signal: the answer is re-parsed as markdown on every change, and a fast
 * provider emits tokens far more often than the screen refreshes.
 */
@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly api = inject(ConversationsApi);

  private readonly conversationsSignal = signal<readonly Conversation[]>([]);
  private readonly activeIdSignal = signal<string | null>(null);
  private readonly messagesSignal = signal<readonly ChatMessage[]>([]);
  private readonly pendingSignal = signal<PendingTurn | null>(null);
  private readonly loadingConversationsSignal = signal(false);
  private readonly loadingMessagesSignal = signal(false);
  private readonly lastTurnSignal = signal<DonePayload | null>(null);
  private readonly lastSourcesSignal = signal<readonly SourceView[]>([]);
  private readonly lastFailedQuestionSignal = signal<string | null>(null);
  /** Assistant message whose sources the panel shows. Null means "the newest answer". */
  private readonly focusedMessageIdSignal = signal<string | null>(null);
  /** Which message the streamed passages belong to, so they are not shown for another. */
  private readonly streamedSourcesOwnerSignal = signal<string | null>(null);
  private readonly conversationsErrorSignal = signal<string | null>(null);

  private abortController: AbortController | null = null;
  private deltaBuffer = '';
  private flushHandle: number | null = null;

  public readonly conversations = this.conversationsSignal.asReadonly();
  public readonly activeId = this.activeIdSignal.asReadonly();
  public readonly messages = this.messagesSignal.asReadonly();
  public readonly pending = this.pendingSignal.asReadonly();
  public readonly loadingConversations = this.loadingConversationsSignal.asReadonly();
  public readonly loadingMessages = this.loadingMessagesSignal.asReadonly();
  public readonly lastTurn = this.lastTurnSignal.asReadonly();
  /** The question of the last turn that failed, so it can be sent again. */
  public readonly lastFailedQuestion = this.lastFailedQuestionSignal.asReadonly();
  /** Set when the conversation list could not be loaded, to tell "empty" from "unreachable". */
  public readonly conversationsError = this.conversationsErrorSignal.asReadonly();

  public readonly streaming = computed(() => this.pendingSignal() !== null);

  public readonly activeConversation = computed(() => {
    const id = this.activeIdSignal();

    return id ? (this.conversationsSignal().find((item) => item.id === id) ?? null) : null;
  });

  public readonly isEmptyThread = computed(
    () => this.messagesSignal().length === 0 && this.pendingSignal() === null,
  );

  /**
   * Sources of the turn on screen: the streaming one while it runs, the one that just
   * finished afterwards, and — for a thread that was merely opened — the citations the
   * last answer was persisted with.
   *
   * That last fallback is what keeps a reopened conversation honest. Without it the
   * panel is empty on every reload and the `[n]` markers become dead links, which is
   * exactly the claim this UI makes and must keep.
   */
  private readonly sourceSelection = computed<SourceSelection>(() => {
    const pending = this.pendingSignal();

    if (pending && pending.sources.length > 0) {
      return { sources: pending.sources, kind: 'retrieved' };
    }

    const messages = this.messagesSignal();
    const focusedId = this.focusedMessageIdSignal();
    const focused =
      (focusedId === null ? undefined : messages.find((item) => item.id === focusedId)) ??
      lastAnswer(messages);

    if (!focused) {
      return { sources: this.lastSourcesSignal(), kind: 'retrieved' };
    }

    // A settled turn always shows what it cited, whether it streamed a second ago or was
    // read back from history. Showing the whole retrieved set for the fresh one made the
    // panel change size, title and content the moment the page was reloaded, which reads
    // as a rendering fault rather than as two different kinds of information.
    //
    // The passages are still in memory for the turn that just streamed, so they are
    // matched back onto the citations by chunk; history simply has none to match.
    const retained =
      this.streamedSourcesOwnerSignal() === focused.id ? this.lastSourcesSignal() : [];

    return { sources: toSourceViews(focused.citations, retained), kind: 'cited' };
  });

  public readonly visibleSources = computed<readonly SourceView[]>(
    () => this.sourceSelection().sources,
  );

  /**
   * What the panel is actually showing: everything retrieval put in context, or just the
   * citations recovered from a stored answer.
   */
  public readonly visibleSourceKind = computed<SourceKind>(() => this.sourceSelection().kind);

  /** Points the panel at one answer, so a marker in an older turn resolves to its own sources. */
  public focusTurn(messageId: string): void {
    this.focusedMessageIdSignal.set(messageId);
  }

  public async loadConversations(): Promise<void> {
    this.loadingConversationsSignal.set(true);

    try {
      const page = await this.api.list(1, 50);

      this.conversationsSignal.set(page.items);
      this.conversationsErrorSignal.set(null);
    } catch (error) {
      const message = describeHttpError(error, 'Could not load conversations.');

      // Without this the sidebar would claim "No conversations yet" when the real
      // problem is that the API never answered.
      this.conversationsErrorSignal.set(message);
      toast.error(message);
    } finally {
      this.loadingConversationsSignal.set(false);
    }
  }

  public async openConversation(id: string): Promise<void> {
    if (this.activeIdSignal() === id) {
      return;
    }

    this.cancelStream();
    this.activeIdSignal.set(id);
    this.messagesSignal.set([]);
    this.lastTurnSignal.set(null);
    this.resetSources();
    this.loadingMessagesSignal.set(true);

    try {
      const page = await this.api.messages(id, 1, 200);

      // Guard against a fast second click: only paint what is still the active thread.
      if (this.activeIdSignal() === id) {
        this.messagesSignal.set(page.items);
      }
    } catch (error) {
      toast.error(describeHttpError(error, 'Could not load this conversation.'));
    } finally {
      this.loadingMessagesSignal.set(false);
    }
  }

  /**
   * Returns the screen to a blank thread, leaving the sidebar history untouched.
   *
   * Navigating to `/chat` from an open conversation has to land here. The store is
   * root-provided, so without this the previous thread stays on screen under a URL that
   * no longer names it — and worse, `activeId` stays set, so the next question would be
   * appended to the conversation the reader believes they just left.
   */
  public closeConversation(): void {
    this.cancelStream();
    this.activeIdSignal.set(null);
    this.messagesSignal.set([]);
    this.lastTurnSignal.set(null);
    this.lastFailedQuestionSignal.set(null);
    this.resetSources();
  }

  public async startConversation(title?: string): Promise<Conversation | null> {
    try {
      const conversation = await this.api.create(title ? { title } : {});

      this.conversationsSignal.update((list) => [conversation, ...list]);
      this.cancelStream();
      this.activeIdSignal.set(conversation.id);
      this.messagesSignal.set([]);
      this.lastTurnSignal.set(null);
      this.resetSources();

      return conversation;
    } catch (error) {
      toast.error(describeHttpError(error, 'Could not start a conversation.'));

      return null;
    }
  }

  /**
   * Sends a question and consumes the SSE stream.
   *
   * When no conversation is open one is created first, so the composer works from a
   * cold start without the caller sequencing two calls.
   */
  public async send(question: string, model?: string): Promise<void> {
    const trimmed = question.trim();

    if (trimmed.length === 0 || this.streaming()) {
      return;
    }

    this.lastFailedQuestionSignal.set(null);

    // Deliberately before the pending turn is set: starting a conversation cancels any
    // stream in flight, which would wipe a turn set up here.
    const conversationId =
      this.activeIdSignal() ?? (await this.startConversation(titleFrom(trimmed)))?.id;

    if (!conversationId) {
      // Creating the conversation is itself a network call. Failing it used to return
      // silently, leaving the reader with a cleared composer and no trace of the
      // question; now the failed turn stays on screen with a way to send it again.
      this.failPending(trimmed, 'Could not start a conversation. Is the API reachable?');

      return;
    }

    this.abortController = new AbortController();
    this.pendingSignal.set({ ...EMPTY_TURN, question: trimmed });
    this.lastTurnSignal.set(null);
    this.resetSources();

    try {
      const stream = this.api.streamAnswer(
        conversationId,
        model ? { content: trimmed, model } : { content: trimmed },
        this.abortController.signal,
      );

      for await (const event of stream) {
        switch (event.type) {
          case 'status':
            this.flushDeltas();
            this.patchPending({ stage: event.data.stage });
            break;

          case 'sources':
            this.flushDeltas();
            this.patchPending({ sources: event.data.sources });
            this.lastSourcesSignal.set(event.data.sources);
            break;

          case 'delta':
            this.queueDelta(event.data.text);
            break;

          case 'done':
            this.flushDeltas();
            this.commitTurn(trimmed, event.data);
            break;

          case 'error':
            this.flushDeltas();
            this.failPending(trimmed, event.data.message);
            toast.error(event.data.message);
            break;
        }
      }
    } catch (error) {
      if (!isAbort(error)) {
        const message = error instanceof Error ? error.message : 'The answer stream failed.';

        this.failPending(trimmed, message);
        toast.error(message);
      }
    } finally {
      this.abortController = null;

      // A turn that ended without a `done` frame keeps whatever text arrived so the
      // reader can see how far it got; only a clean finish clears the pending turn.
      if (this.pendingSignal()?.failure === null) {
        this.pendingSignal.set(null);
      }
    }
  }

  /** Stops the current stream. The server persists the partial answer as `[interrupted]`. */
  public stop(): void {
    this.abortController?.abort();
    this.flushDeltas();

    const pending = this.pendingSignal();

    if (pending && pending.text.trim().length > 0) {
      // Keep the retrieved passages attached to the interrupted answer: they are what
      // the partial text was built from, and the reader still has to be able to check it.
      this.streamedSourcesOwnerSignal.set(
        this.appendLocalTurn(pending.question, pending.text, null),
      );
      this.focusedMessageIdSignal.set(null);
    }

    this.pendingSignal.set(null);
  }

  public dismissFailure(): void {
    this.pendingSignal.set(null);
    this.lastFailedQuestionSignal.set(null);
  }

  /** Sends the last failed question again, from the error shown in the thread. */
  public async retry(): Promise<void> {
    const question = this.lastFailedQuestionSignal();

    if (!question) {
      return;
    }

    this.pendingSignal.set(null);
    this.lastFailedQuestionSignal.set(null);

    await this.send(question);
  }

  /** Leaves the failed turn on screen, carrying its question and the reason. */
  private failPending(question: string, message: string): void {
    this.pendingSignal.set({
      ...EMPTY_TURN,
      question,
      text: this.pendingSignal()?.text ?? '',
      sources: this.pendingSignal()?.sources ?? [],
      failure: message,
    });
    this.lastFailedQuestionSignal.set(question);
  }

  public async renameLocally(id: string, title: string): Promise<void> {
    this.conversationsSignal.update((list) =>
      list.map((item) => (item.id === id ? { ...item, title } : item)),
    );
  }

  private commitTurn(question: string, done: DonePayload): void {
    const pending = this.pendingSignal();

    this.streamedSourcesOwnerSignal.set(this.appendLocalTurn(question, pending?.text ?? '', done));
    this.focusedMessageIdSignal.set(null);
    this.lastTurnSignal.set(done);
    this.pendingSignal.set(null);
    this.touchConversation(this.activeIdSignal(), question);

    if (done.degraded) {
      toast.warning('Answered in degraded mode — part of the retrieval pipeline was skipped.');
    }

    if (done.interrupted) {
      toast.info('The answer was cut short and saved as-is.');
    }
  }

  /**
   * Appends the finished turn without refetching. The user message has no server id
   * until the thread is reopened, so it carries a client-side one.
   */
  private appendLocalTurn(question: string, answer: string, done: DonePayload | null): string {
    const now = new Date().toISOString();

    const userMessage: ChatMessage = {
      id: `local-${crypto.randomUUID()}`,
      role: 'User',
      content: question,
      rewrittenQuery: null,
      provider: null,
      model: null,
      inputTokens: null,
      outputTokens: null,
      latencyMs: null,
      retrievalMs: null,
      createdAt: now,
      citations: [],
    };

    const assistantMessage: ChatMessage = {
      id: done?.messageId ?? `local-${crypto.randomUUID()}`,
      role: 'Assistant',
      content: answer,
      rewrittenQuery: null,
      provider: done?.provider ?? null,
      model: done?.model ?? null,
      inputTokens: done?.inputTokens ?? null,
      outputTokens: done?.outputTokens ?? null,
      latencyMs: done ? Number(done.latencyMs) : null,
      retrievalMs: done ? Number(done.retrievalMs) : null,
      createdAt: now,
      citations: done?.citations ?? [],
    };

    this.messagesSignal.update((list) => [...list, userMessage, assistantMessage]);

    return assistantMessage.id;
  }

  private resetSources(): void {
    this.lastSourcesSignal.set([]);
    this.streamedSourcesOwnerSignal.set(null);
    this.focusedMessageIdSignal.set(null);
  }

  private touchConversation(id: string | null, question: string): void {
    if (!id) {
      return;
    }

    const now = new Date().toISOString();

    this.conversationsSignal.update((list) => {
      const index = list.findIndex((item) => item.id === id);

      if (index === -1) {
        return list;
      }

      const current = list[index];
      // The API names an untitled thread after its first question; mirror that locally
      // so the sidebar does not sit on "New conversation" until the next reload.
      const title = isPlaceholderTitle(current.title) ? titleFrom(question) : current.title;
      const updated = { ...current, title, updatedAt: now };

      return [updated, ...list.slice(0, index), ...list.slice(index + 1)];
    });
  }

  private patchPending(patch: Partial<PendingTurn>): void {
    this.pendingSignal.update((current) => (current ? { ...current, ...patch } : current));
  }

  private queueDelta(text: string): void {
    this.deltaBuffer += text;

    if (this.flushHandle !== null) {
      return;
    }

    this.flushHandle = requestAnimationFrame(() => {
      this.flushHandle = null;
      this.flushDeltas();
    });
  }

  private flushDeltas(): void {
    if (this.flushHandle !== null) {
      cancelAnimationFrame(this.flushHandle);
      this.flushHandle = null;
    }

    if (this.deltaBuffer.length === 0) {
      return;
    }

    const chunk = this.deltaBuffer;
    this.deltaBuffer = '';

    this.pendingSignal.update((current) =>
      current ? { ...current, text: current.text + chunk } : current,
    );
  }

  private cancelStream(): void {
    this.abortController?.abort();
    this.abortController = null;
    this.deltaBuffer = '';

    if (this.flushHandle !== null) {
      cancelAnimationFrame(this.flushHandle);
      this.flushHandle = null;
    }

    this.pendingSignal.set(null);
  }
}

/** The newest answer that cited anything — what the panel falls back to. */
function lastAnswer(messages: readonly ChatMessage[]): ChatMessage | undefined {
  for (let index = messages.length - 1; index >= 0; index--) {
    const message = messages[index];

    if (message.role === 'Assistant' && message.citations.length > 0) {
      return message;
    }
  }

  return undefined;
}

/**
 * Rebuilds source cards from the citations stored with an answer, restoring each
 * passage from `retained` where the turn's retrieved chunks are still in memory.
 *
 * The API persists what each marker points at but not the passage behind it, so a card
 * recovered from history carries no snippet and `SourcesPanel` omits that line rather
 * than showing a blank.
 */
function toSourceViews(
  citations: readonly Citation[],
  retained: readonly SourceView[],
): readonly SourceView[] {
  const passages = new Map(retained.map((source) => [source.chunkId, source.snippet]));

  return [...citations]
    .sort((a, b) => a.markerIndex - b.markerIndex)
    .map((citation) => ({
      index: citation.markerIndex,
      chunkId: citation.chunkId,
      documentId: citation.documentId,
      documentTitle: citation.documentTitle,
      headingPath: citation.headingPath,
      snippet: passages.get(citation.chunkId) ?? '',
      score: citation.score,
    }));
}

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}

function isPlaceholderTitle(title: string): boolean {
  return (
    title.trim().length === 0 || /^(new conversation|cuộc trò chuyện mới)$/i.test(title.trim())
  );
}

function titleFrom(question: string): string {
  const condensed = question.replace(/\s+/g, ' ').trim();

  return condensed.length <= 60 ? condensed : `${condensed.slice(0, 57)}…`;
}
