/**
 * Mirrors the response contracts of IChat.Api v1. ASP.NET Core serialises with the
 * camelCase policy and `JsonStringEnumConverter`, so enums arrive as their names.
 */

// ---------------------------------------------------------------- conversations

export interface Conversation {
  readonly id: string;
  readonly title: string;
  readonly userId: string | null;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface Citation {
  readonly chunkId: string;
  readonly markerIndex: number;
  readonly score: number;
  readonly documentId: string;
  readonly documentTitle: string;
  readonly headingPath: string | null;
}

/** Serialised by the API's default `JsonStringEnumConverter`, so the wire values are PascalCase. */
export type MessageRole = 'User' | 'Assistant' | 'System';

export interface ChatMessage {
  readonly id: string;
  readonly role: MessageRole;
  readonly content: string;
  readonly rewrittenQuery: string | null;
  readonly provider: string | null;
  readonly model: string | null;
  readonly inputTokens: number | null;
  readonly outputTokens: number | null;
  readonly latencyMs: number | null;
  readonly retrievalMs: number | null;
  readonly createdAt: string;
  readonly citations: readonly Citation[];
}

export interface CreateConversationRequest {
  readonly title?: string;
  readonly userId?: string;
}

export interface SendMessageRequest {
  readonly content: string;
  readonly model?: string;
}

// -------------------------------------------------------------- chat SSE stream

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

// ------------------------------------------------------------------- documents

export type DocumentStatus = 'Pending' | 'Processing' | 'Indexed' | 'Failed';

export interface DocumentSummary {
  readonly id: string;
  readonly title: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeInBytes: number;
  readonly status: DocumentStatus;
  readonly errorMessage: string | null;
  readonly chunkCount: number;
  readonly createdAt: string;
  readonly indexedAt: string | null;
}

export interface DocumentChunk {
  readonly id: string;
  readonly chunkIndex: number;
  readonly content: string;
  readonly headingPath: string | null;
  readonly embeddedText: string;
  readonly tokenCount: number;
  readonly embeddingModel: string;
  readonly embeddingDimensions: number;
  readonly metadata: string;
}

export interface UploadDocumentResult {
  readonly id: string;
  readonly title: string;
  readonly status: DocumentStatus;
}

// ----------------------------------------------------------------------- admin

export interface ReindexResult {
  readonly documentCount: number;
}
