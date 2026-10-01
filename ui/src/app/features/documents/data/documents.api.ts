import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { API_BASE_URL, API_V1 } from '@app/core/api/api.config';
import type { PagedResponse } from '@app/shared/util/api-envelope.model';
import type {
  DocumentChunkDto,
  DocumentStatusDto,
  DocumentSummaryDto,
  UploadDocumentResultDto,
} from './documents.dto';

@Injectable({ providedIn: 'root' })
export class DocumentsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private get root(): string {
    return `${this.baseUrl}${API_V1}/documents`;
  }

  list(
    offset = 0,
    limit = 20,
    status?: DocumentStatusDto,
  ): Promise<PagedResponse<DocumentSummaryDto>> {
    let params = new HttpParams().set('offset', offset).set('limit', limit);

    if (status) {
      params = params.set('status', status);
    }

    return firstValueFrom(
      this.http.get<PagedResponse<DocumentSummaryDto>>(this.root, { params }),
    );
  }

  byId(id: string): Promise<DocumentSummaryDto> {
    return firstValueFrom(this.http.get<DocumentSummaryDto>(`${this.root}/${id}`));
  }

  chunks(id: string, offset = 0, limit = 50): Promise<PagedResponse<DocumentChunkDto>> {
    const params = new HttpParams().set('offset', offset).set('limit', limit);

    return firstValueFrom(
      this.http.get<PagedResponse<DocumentChunkDto>>(`${this.root}/${id}/chunks`, { params }),
    );
  }

  /** Returns the raw event stream so the caller can render upload progress. */
  upload(
    file: File,
    title?: string,
  ): Observable<import('@angular/common/http').HttpEvent<UploadDocumentResultDto>> {
    const form = new FormData();
    form.append('file', file, file.name);

    if (title) {
      form.append('title', title);
    }

    return this.http.post<UploadDocumentResultDto>(this.root, form, {
      reportProgress: true,
      observe: 'events',
    });
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.root}/${id}`));
  }
}
