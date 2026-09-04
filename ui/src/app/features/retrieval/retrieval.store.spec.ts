import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { RetrievalApi } from '@app/shared/data-access/retrieval.api';
import type { SearchResult, SearchStage } from '@app/core/api/api.models';
import { RetrievalStore } from './retrieval.store';

vi.mock('ngx-sonner', () => ({
  toast: Object.assign(vi.fn(), {
    error: vi.fn(),
    success: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  }),
}));

function stage(name: string): SearchStage {
  return { name, count: 1, elapsedMs: 1, tsQuery: null, top: [] };
}

/**
 * The keys the API actually emits (`RetrievalStageName` on the backend), deliberately
 * given to the store out of order — `stages` is a map, so arrival order proves nothing.
 */
function result(...names: readonly string[]): SearchResult {
  return {
    originalQuery: 'q',
    rewrittenQuery: 'q',
    stages: Object.fromEntries(names.map((name) => [name, stage(name)])),
    degraded: false,
    elapsedMs: 1,
  };
}

function makeStore(search: SearchResult): RetrievalStore {
  TestBed.configureTestingModule({
    providers: [
      RetrievalStore,
      { provide: RetrievalApi, useValue: { search: vi.fn().mockResolvedValue(search) } },
    ],
  });

  return TestBed.inject(RetrievalStore);
}

describe('RetrievalStore.orderedStages', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('renders the stages in pipeline order, not in the order the map happened to arrive', async () => {
    const store = makeStore(
      result('final', 'fused', 'vector', 'afterMmr', 'trigram', 'fulltext', 'reranked'),
    );

    store.setCriteria({ ...store.criteria(), query: 'what is the default timeout?' });
    await store.run();

    // Every name here has to match RetrievalStageName on the backend. When one drifts,
    // the stage still renders — it is just appended after `final`, which reads as the
    // pipeline running in the wrong order.
    expect(store.orderedStages().map((s) => s.name)).toEqual([
      'vector',
      'fulltext',
      'trigram',
      'fused',
      'afterMmr',
      'reranked',
      'final',
    ]);
  });

  it('keeps a stage the UI does not know about instead of dropping it', async () => {
    const store = makeStore(result('vector', 'somethingNew', 'final'));

    store.setCriteria({ ...store.criteria(), query: 'q' });
    await store.run();

    expect(store.orderedStages().map((s) => s.name)).toEqual(['vector', 'final', 'somethingNew']);
  });
});
