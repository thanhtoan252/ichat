import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { LowerCasePipe } from '@angular/common';
import { HlmSkeleton } from '@app/ui/skeleton';
import { ConfirmDialog } from '@app/shared/ui/confirm-dialog/confirm-dialog';
import { DocumentsEmpty } from '../ui/documents-empty';
import { DocumentsStats, type DocumentStat } from '../ui/documents-stats';
import { DocumentsTable } from '../ui/documents-table';
import { DocumentsToolbar } from '../ui/documents-toolbar';
import { StatusFilterBar } from '../ui/status-filter';
import { UploadProgressBar } from '../ui/upload-progress';
import { DocumentsStore } from '../data-access/documents.store';
import type { DocumentSummary, StatusFilter } from '../model/document.model';

const FILTERS: readonly StatusFilter[] = ['All', 'Indexed', 'Processing', 'Pending', 'Failed'];

/** Container. Connects the documents store to the presentational pieces. */
@Component({
  selector: 'app-documents-page',
  providers: [DocumentsStore],
  imports: [
    LowerCasePipe,
    HlmSkeleton,
    ConfirmDialog,
    DocumentsEmpty,
    DocumentsStats,
    DocumentsTable,
    DocumentsToolbar,
    StatusFilterBar,
    UploadProgressBar,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  templateUrl: './documents-page.html',
})
export class DocumentsPage {
  protected readonly store = inject(DocumentsStore);
  protected readonly filters = FILTERS;
  protected readonly skeletonRows = [0, 1, 2, 3];
  protected readonly dragging = signal(false);

  // Both of these destroy work: one drops a document and its chunks, the other rebuilds
  // every embedding in the corpus. Neither should happen on a single stray click.
  protected readonly pendingRemoval = signal<DocumentSummary | null>(null);
  protected readonly reindexRequested = signal(false);

  protected readonly removalDescription = computed(() => {
    const document = this.pendingRemoval();

    return document
      ? `${document.title} and its ${document.chunkCount} indexed chunks are removed. Answers will stop citing it.`
      : '';
  });

  protected readonly reindexDescription = computed(
    () =>
      `All ${this.store.total()} documents are chunked and embedded again. This costs embedding calls and takes a while.`,
  );

  protected readonly stats = computed<readonly DocumentStat[]>(() => {
    const counts = this.store.counts();

    return [
      { label: 'Documents', value: this.store.total() },
      { label: 'Indexed', value: counts.indexed },
      { label: 'In progress', value: counts.working },
      { label: 'Chunks', value: counts.chunks },
    ];
  });

  constructor() {
    void this.store.load();
    inject(DestroyRef).onDestroy(() => this.store.stopPolling());
  }

  protected confirmRemove(document: DocumentSummary): void {
    this.pendingRemoval.set(document);
  }

  protected async remove(): Promise<void> {
    const document = this.pendingRemoval();

    this.pendingRemoval.set(null);

    if (document) {
      await this.store.remove(document);
    }
  }

  protected async reindex(): Promise<void> {
    this.reindexRequested.set(false);
    await this.store.reindexAll();
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
  }

  protected async onDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    this.dragging.set(false);

    await this.upload(Array.from(event.dataTransfer?.files ?? []));
  }

  /** Sequential on purpose: the API queues each upload, and a burst only adds contention. */
  protected async upload(files: readonly File[]): Promise<void> {
    for (const file of files) {
      await this.store.uploadFile(file);
    }
  }
}
