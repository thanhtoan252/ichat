import type {
  SearchHitDto,
  SearchRequestDto,
  SearchResultDto,
  SearchStageDto,
} from '@app/core/api/retrieval.dto';
import type { SearchCriteria, SearchHit, SearchResult, SearchStage } from './search.model';

export function toSearchRequestDto(criteria: SearchCriteria): SearchRequestDto {
  return {
    query: criteria.query,
    topK: criteria.topK,
    mode: criteria.mode,
    rewrite: criteria.rewrite,
    applyMmr: criteria.applyMmr,
    expandNeighbors: criteria.expandNeighbors,
    rerank: criteria.rerank,
  };
}

function toSearchHit(dto: SearchHitDto): SearchHit {
  return {
    chunkId: dto.chunkId,
    documentId: dto.documentId,
    headingPath: dto.headingPath,
    snippet: dto.snippet,
    chunkIndex: dto.chunkIndex,
    score: dto.score,
  };
}

function toSearchStage(dto: SearchStageDto): SearchStage {
  return {
    name: dto.name,
    count: dto.count,
    elapsedMs: dto.elapsedMs,
    tsQuery: dto.tsQuery,
    top: dto.top.map(toSearchHit),
  };
}

export function toSearchResult(dto: SearchResultDto): SearchResult {
  const stages: Record<string, SearchStage> = {};

  for (const [name, stage] of Object.entries(dto.stages)) {
    stages[name] = toSearchStage(stage);
  }

  return {
    originalQuery: dto.originalQuery,
    rewrittenQuery: dto.rewrittenQuery,
    stages,
    degraded: dto.degraded,
    elapsedMs: dto.elapsedMs,
  };
}
