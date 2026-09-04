import { HttpEventType } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { toast } from 'ngx-sonner';
import { DocumentsApi } from '../data/documents.api';
import { RetrievalApi } from '@app/core/api/retrieval.api';
import { describeHttpError } from '@app/core/api/http-error';
import { toDocumentSummary, toReindexResult } from '../data/documents.mapper';
import type { DocumentSummary, StatusFilter, UploadProgress } from '../data/document.model';

/** Ingestion is asynchronous, so the list is re-polled while anything is still working. */
const POLL_INTERVAL_MS = 3000;

@Injectable()
export class DocumentsStore {
  private readonly api = inject(DocumentsApi);
  private readonly admin = inject(RetrievalApi);

  private readonly documentsSignal = signal<readonly DocumentSummary[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly filterSignal = signal<StatusFilter>('All');
  private readonly uploadSignal = signal<UploadProgress | null>(null);
  private readonly totalSignal = signal(0);

  private pollHandle: ReturnType<typeof setTimeout> | null = null;

  /** Every document the page holds, before the status pills narrow it. */
  public readonly allDocuments = this.documentsSignal.asReadonly();
  public readonly loading = this.loadingSignal.asReadonly();
  public readonly filter = this.filterSignal.asReadonly();
  public readonly upload = this.uploadSignal.asReadonly();
  public readonly total = this.totalSignal.asReadonly();

  /**
   * The rows the table shows. Filtering happens here rather than on the server so the
   * counters above the table keep describing the whole corpus: refetching per pill made
   * "Documents 1 / Indexed 0" appear the moment someone clicked `Failed`, which reads as
   * a corpus that just emptied itself.
   */
  public readonly documents = computed<readonly DocumentSummary[]>(() => {
    const filter = this.filterSignal();
    const documents = this.documentsSignal();

    return filter === 'All' ? documents : documents.filter((item) => item.status === filter);
  });

  public readonly counts = computed(() => {
    const documents = this.documentsSignal();

    return {
      indexed: documents.filter((item) => item.status === 'Indexed').length,
      working: documents.filter((item) => item.status === 'Pending' || item.status === 'Processing')
        .length,
      failed: documents.filter((item) => item.status === 'Failed').length,
      chunks: documents.reduce((sum, item) => sum + item.chunkCount, 0),
    };
  });

  public readonly hasWorkInFlight = computed(() => this.counts().working > 0);

  public async load(): Promise<void> {
    this.loadingSignal.set(true);

    try {
      const page = await this.api.list(1, 100);

      this.documentsSignal.set(page.items.map(toDocumentSummary));
      this.totalSignal.set(page.totalCount);
      this.schedulePoll();
    } catch (error) {
      toast.error(describeHttpError(error, 'Could not load documents.'));
    } finally {
      this.loadingSignal.set(false);
    }
  }

  public setFilter(filter: StatusFilter): void {
    this.filterSignal.set(filter);
  }

  public uploadFile(file: File, title?: string): Promise<void> {
    this.uploadSignal.set({ fileName: file.name, percent: 0 });

    return new Promise((resolve) => {
      this.api.upload(file, title).subscribe({
        next: (event) => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            this.uploadSignal.set({
              fileName: file.name,
              percent: Math.round((event.loaded / event.total) * 100),
            });
          }

          if (event.type === HttpEventType.Response) {
            this.uploadSignal.set(null);
            toast.success(`${file.name} queued for ingestion.`);
            void this.load();
            resolve();
          }
        },
        error: (error: unknown) => {
          this.uploadSignal.set(null);
          toast.error(describeHttpError(error, `Could not upload ${file.name}.`));
          resolve();
        },
      });
    });
  }

  public async remove(document: DocumentSummary): Promise<void> {
    // Optimistic: the row disappears at once and comes back if the delete fails.
    const snapshot = this.documentsSignal();

    this.documentsSignal.update((list) => list.filter((item) => item.id !== document.id));

    try {
      await this.api.delete(document.id);
      toast.success(`${document.title} deleted.`);
    } catch (error) {
      this.documentsSignal.set(snapshot);
      toast.error(describeHttpError(error, 'Could not delete the document.'));
    }
  }

  public async reindexAll(): Promise<void> {
    try {
      const result = toReindexResult(await this.admin.reindex());

      toast.success(`Reindexing ${result.documentCount} documents.`);
      await this.load();
    } catch (error) {
      toast.error(describeHttpError(error, 'Could not start a reindex.'));
    }
  }

  public stopPolling(): void {
    if (this.pollHandle !== null) {
      clearTimeout(this.pollHandle);
      this.pollHandle = null;
    }
  }

  private schedulePoll(): void {
    this.stopPolling();

    if (!this.hasWorkInFlight()) {
      return;
    }

    this.pollHandle = setTimeout(() => void this.load(), POLL_INTERVAL_MS);
  }
}
