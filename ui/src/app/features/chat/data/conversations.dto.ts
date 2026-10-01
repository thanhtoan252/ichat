/**
 * Wire types for `/conversations`. ASP.NET Core serialises with the camelCase policy
 * and `JsonStringEnumConverter`, so enums arrive as their names; `conversations.mapper.ts`
 * maps these onto the feature's own domain model.
 */

export interface ConversationDto {
  readonly id: string;
  readonly title: string;
  readonly userId: string;
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface CitationDto {
  readonly chunkId: string;
  readonly markerIndex: number;
  readonly score: number;
  readonly documentId: string;
  readonly documentTitle: string;
  readonly headingPath: string | null;
}

export type MessageRoleDto = 'User' | 'Assistant' | 'System';

export interface ChatMessageDto {
  readonly id: string;
  readonly role: MessageRoleDto;
  readonly content: string;
  readonly rewrittenQuery: string | null;
  readonly provider: string | null;
  readonly model: string | null;
  readonly inputTokens: number | null;
  readonly outputTokens: number | null;
  readonly latencyMs: number | null;
  readonly retrievalMs: number | null;
  readonly createdAt: string;
  readonly citations: readonly CitationDto[];
}

export interface CreateConversationRequestDto {
  readonly title?: string;
}

export interface SendMessageRequestDto {
  readonly content: string;
  readonly model?: string;
}
