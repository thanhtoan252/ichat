export type SearchMode = 'Hybrid' | 'Vector' | 'FullText' | 'Trigram';

/** Everything the form controls, as one value so the form has a single input/output pair. */
export interface SearchCriteria {
  readonly query: string;
  readonly topK: number;
  readonly mode: SearchMode;
  readonly rewrite: boolean;
  readonly applyMmr: boolean;
  readonly expandNeighbors: boolean;
  readonly rerank: boolean;
}

export interface SearchHit {
  readonly chunkId: string;
  readonly documentId: string;
  readonly headingPath: string | null;
  readonly snippet: string;
  readonly chunkIndex: number;
  readonly score: number;
}

export interface SearchStage {
  readonly name: string;
  readonly count: number;
  readonly elapsedMs: number;
  readonly tsQuery: string | null;
  readonly top: readonly SearchHit[];
}

export interface SearchResult {
  readonly originalQuery: string;
  readonly rewrittenQuery: string;
  /** Keyed by stage name: vector, fulltext, trigram, fused, afterMmr, reranked, final. */
  readonly stages: Readonly<Record<string, SearchStage>>;
  readonly degraded: boolean;
  readonly elapsedMs: number;
}
