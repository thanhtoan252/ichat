import type { ChatMessageDto, CitationDto, ConversationDto, MessageRoleDto } from './conversations.dto';
import type { ChatMessage, Citation, Conversation, MessageRole } from './chat.model';

function toMessageRole(dto: MessageRoleDto): MessageRole {
  return dto;
}

export function toCitation(dto: CitationDto): Citation {
  return {
    chunkId: dto.chunkId,
    markerIndex: dto.markerIndex,
    score: dto.score,
    documentId: dto.documentId,
    documentTitle: dto.documentTitle,
    headingPath: dto.headingPath,
  };
}

export function toConversation(dto: ConversationDto): Conversation {
  return {
    id: dto.id,
    title: dto.title,
    userId: dto.userId,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
  };
}

export function toChatMessage(dto: ChatMessageDto): ChatMessage {
  return {
    id: dto.id,
    role: toMessageRole(dto.role),
    content: dto.content,
    rewrittenQuery: dto.rewrittenQuery,
    provider: dto.provider,
    model: dto.model,
    inputTokens: dto.inputTokens,
    outputTokens: dto.outputTokens,
    latencyMs: dto.latencyMs,
    retrievalMs: dto.retrievalMs,
    createdAt: dto.createdAt,
    citations: dto.citations.map(toCitation),
  };
}
