import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { API_BASE_URL, API_V1 } from './api.config';
import type { PagedResponse } from '@app/shared/util/api-envelope.model';
import type {
  DocumentChunk,
  DocumentStatus,
  DocumentSummary,
  UploadDocumentResult,
} from './api.models';

@Injectable({ providedIn: 'root' })
export class DocumentsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private get root(): string {
    return `${this.baseUrl}${API_V1}/documents`;
  }

  list(page = 1, pageSize = 20, status?: DocumentStatus): Promise<PagedResponse<DocumentSummary>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (status) {
      params = params.set('status', status);
    }

    return firstValueFrom(this.http.get<PagedResponse<DocumentSummary>>(this.root, { params }));
  }

  byId(id: string): Promise<DocumentSummary> {
    return firstValueFrom(this.http.get<DocumentSummary>(`${this.root}/${id}`));
  }

  chunks(id: string, page = 1, pageSize = 50): Promise<PagedResponse<DocumentChunk>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);

    return firstValueFrom(
      this.http.get<PagedResponse<DocumentChunk>>(`${this.root}/${id}/chunks`, { params }),
    );
  }

  /** Returns the raw event stream so the caller can render upload progress. */
  upload(
    file: File,
    title?: string,
  ): Observable<import('@angular/common/http').HttpEvent<UploadDocumentResult>> {
    const form = new FormData();
    form.append('file', file, file.name);

    if (title) {
      form.append('title', title);
    }

    return this.http.post<UploadDocumentResult>(this.root, form, {
      reportProgress: true,
      observe: 'events',
    });
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.root}/${id}`));
  }
}
