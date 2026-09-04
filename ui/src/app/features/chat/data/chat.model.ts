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
