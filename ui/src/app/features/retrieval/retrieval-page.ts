import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { HlmSkeleton } from '@app/ui/skeleton';
import { RetrievalEmpty } from './components/retrieval-empty';
import { RetrievalForm } from './components/retrieval-form';
import { RetrievalToolbar } from './components/retrieval-toolbar';
import { StageList } from './components/stage-list';
import { RetrievalStore } from './retrieval.store';

/**
 * Container. A window onto the hybrid search pipeline.
 *
 * This exists because most retrieval debugging is finding the stage where a chunk fell
 * out — not editing the prompt.
 */
@Component({
  selector: 'app-retrieval-page',
  providers: [RetrievalStore],
  imports: [HlmSkeleton, RetrievalEmpty, RetrievalForm, RetrievalToolbar, StageList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  templateUrl: './retrieval-page.html',
})
export class RetrievalPage {
  protected readonly store = inject(RetrievalStore);
  protected readonly skeletonRows = [0, 1, 2, 3, 4];
}
