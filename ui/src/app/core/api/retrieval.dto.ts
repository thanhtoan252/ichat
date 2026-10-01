/**
 * Wire types for the endpoints on the shared `RetrievalApi` client. ASP.NET Core
 * serialises with the camelCase policy, so these mirror the JSON exactly; each
 * consuming feature maps its slice onto its own domain model in its `data-access/`.
 */

// ---------------------------------------------------------------------- providers

export interface ProviderInfoDto {
  readonly kind: string;
  readonly provider: string;
  readonly model: string;
  readonly available: boolean;
  readonly reason: string | null;
}

export interface ProviderCatalogDto {
  readonly chat: ProviderInfoDto;
  readonly utilityChat: ProviderInfoDto;
  readonly embedding: ProviderInfoDto;
  readonly allowedChatModels: readonly string[];
}

// ------------------------------------------------------------------- search

export type SearchModeDto = 'Hybrid' | 'Vector' | 'FullText' | 'Trigram';

export interface SearchHistoryTurnDto {
  readonly role: string;
  readonly content: string;
}

export interface SearchRequestDto {
  readonly query: string;
  readonly topK?: number;
  readonly mode?: SearchModeDto;
  readonly rewrite?: boolean;
  readonly history?: readonly SearchHistoryTurnDto[];
  readonly applyMmr?: boolean;
  readonly expandNeighbors?: boolean;
  readonly rerank?: boolean;
}

export interface SearchHitDto {
  readonly chunkId: string;
  readonly documentId: string;
  readonly headingPath: string | null;
  readonly snippet: string;
  readonly chunkIndex: number;
  readonly score: number;
}

export interface SearchStageDto {
  readonly name: string;
  readonly count: number;
  readonly elapsedMs: number;
  readonly tsQuery: string | null;
  readonly top: readonly SearchHitDto[];
}

export interface SearchResultDto {
  readonly originalQuery: string;
  readonly rewrittenQuery: string;
  /** Keyed by stage name: vector, fulltext, trigram, fused, afterMmr, reranked, final. */
  readonly stages: Readonly<Record<string, SearchStageDto>>;
  readonly degraded: boolean;
  readonly elapsedMs: number;
}

// ----------------------------------------------------------------------- admin

export interface ReindexResultDto {
  readonly documentCount: number;
}
