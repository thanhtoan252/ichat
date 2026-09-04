import { Injectable, computed, inject, signal } from '@angular/core';
import { toast } from 'ngx-sonner';
import { RetrievalApi } from '@app/core/api/retrieval.api';
import { describeHttpError } from '@app/core/api/http-error';
import { toSearchRequestDto, toSearchResult } from './search.mapper';
import type { SearchCriteria, SearchResult, SearchStage } from '../model/search.model';

const DEFAULT_CRITERIA: SearchCriteria = {
  query: '',
  topK: 8,
  mode: 'Hybrid',
  rewrite: true,
  applyMmr: true,
  expandNeighbors: true,
  rerank: true,
};

/**
 * The order the pipeline actually runs in; `stages` is a map, so the UI supplies the order.
 *
 * These names are a published contract with the API — they must match
 * `RetrievalStageName` on the backend exactly. A name that does not match still renders,
 * because unknown stages are appended at the end, which is precisely how a typo here hides:
 * nothing breaks, the stages just stop reading in pipeline order.
 */
const STAGE_ORDER: readonly string[] = [
  'vector',
  'fulltext',
  'trigram',
  'fused',
  'afterMmr',
  'reranked',
  'final',
];

@Injectable()
export class RetrievalStore {
  private readonly api = inject(RetrievalApi);

  private readonly criteriaSignal = signal<SearchCriteria>(DEFAULT_CRITERIA);
  private readonly resultSignal = signal<SearchResult | null>(null);
  private readonly runningSignal = signal(false);
  private readonly expandedSignal = signal<string | null>(null);

  public readonly criteria = this.criteriaSignal.asReadonly();
  public readonly result = this.resultSignal.asReadonly();
  public readonly running = this.runningSignal.asReadonly();
  public readonly expanded = this.expandedSignal.asReadonly();

  public readonly canRun = computed(
    () => !this.runningSignal() && this.criteriaSignal().query.trim().length > 0,
  );

  /** `stages` arrives as a map; render it in pipeline order and drop stages that did not run. */
  public readonly orderedStages = computed<readonly SearchStage[]>(() => {
    const stages = this.resultSignal()?.stages ?? {};
    const known = STAGE_ORDER.map((name) => stages[name]).filter(
      (stage): stage is SearchStage => stage !== undefined,
    );
    const extra = Object.entries(stages)
      .filter(([name]) => !STAGE_ORDER.includes(name))
      .map(([, stage]) => stage);

    return [...known, ...extra];
  });

  public setCriteria(criteria: SearchCriteria): void {
    this.criteriaSignal.set(criteria);
  }

  public toggleStage(name: string): void {
    this.expandedSignal.update((current) => (current === name ? null : name));
  }

  public async run(): Promise<void> {
    if (!this.canRun()) {
      return;
    }

    const criteria = this.criteriaSignal();
    this.runningSignal.set(true);

    try {
      const request = toSearchRequestDto({ ...criteria, query: criteria.query.trim() });
      const result = await this.api.search(request);

      this.resultSignal.set(toSearchResult(result));
      // The final stage is what actually reached the model, so open that one first.
      this.expandedSignal.set('final');
    } catch (error) {
      toast.error(describeHttpError(error, 'The search failed.'));
    } finally {
      this.runningSignal.set(false);
    }
  }
}
