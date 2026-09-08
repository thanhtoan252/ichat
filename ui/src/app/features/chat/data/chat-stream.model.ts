import type { Citation } from './chat.model';

/**
 * The chat SSE stream's frame types. These are transient — consumed inline in
 * ChatStore's `for await` loop and never rendered as a persisted resource — so unlike
 * Conversation/ChatMessage they get no separate DTO: the wire shape and the shape the
 * rest of the feature works with are the same type.
 */

/** The four stages ChatService emits before the first token. */
export type ChatStage = 'rewriting' | 'retrieving' | 'generating';

export interface StatusPayload {
  readonly stage: ChatStage | (string & {});
}

export interface SourceView {
  readonly index: number;
  readonly chunkId: string;
  readonly documentId: string;
  readonly documentTitle: string;
  readonly headingPath: string | null;
  readonly snippet: string;
  readonly score: number;
}

export interface SourcesPayload {
  readonly sources: readonly SourceView[];
}

export interface DeltaPayload {
  readonly text: string;
}

export interface DonePayload {
  readonly messageId: string;
  readonly citations: readonly Citation[];
  readonly provider: string | null;
  readonly model: string | null;
  readonly inputTokens: number | null;
  readonly outputTokens: number | null;
  readonly latencyMs: number;
  readonly retrievalMs: number;
  readonly degraded: boolean;
  readonly interrupted: boolean;
}

export interface ErrorPayload {
  readonly code: string;
  readonly message: string;
}

/**
 * Discriminated union of the SSE frames. The API answers business failures with an
 * `error` event under HTTP 200, so the stream — not the status code — is the source
 * of truth for whether a turn succeeded.
 */
export type ChatStreamEvent =
  | { readonly type: 'status'; readonly data: StatusPayload }
  | { readonly type: 'sources'; readonly data: SourcesPayload }
  | { readonly type: 'delta'; readonly data: DeltaPayload }
  | { readonly type: 'done'; readonly data: DonePayload }
  | { readonly type: 'error'; readonly data: ErrorPayload };

/** Whether the panel holds the live retrieved context or citations read back from history. */
export type SourceKind = 'retrieved' | 'cited';

/** The turn currently in flight. Absent between turns. */
export interface PendingTurn {
  readonly question: string;
  readonly stage: ChatStage | string | null;
  readonly sources: readonly SourceView[];
  readonly text: string;
  readonly failure: string | null;
}
