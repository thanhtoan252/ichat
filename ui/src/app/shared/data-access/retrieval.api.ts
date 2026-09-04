import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL, API_V1 } from '@app/core/api/api.config';
import type { ProviderCatalog, ReindexResult, SearchRequest, SearchResult } from '@app/core/api/api.models';

/** `/search` and the admin endpoints: the retrieval-debugging surface of the API. */
@Injectable({ providedIn: 'root' })
export class RetrievalApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  search(request: SearchRequest): Promise<SearchResult> {
    return firstValueFrom(this.http.post<SearchResult>(`${this.baseUrl}${API_V1}/search`, request));
  }

  providers(): Promise<ProviderCatalog> {
    return firstValueFrom(this.http.get<ProviderCatalog>(`${this.baseUrl}${API_V1}/providers`));
  }

  reindex(): Promise<ReindexResult> {
    return firstValueFrom(
      this.http.post<ReindexResult>(`${this.baseUrl}${API_V1}/admin/reindex`, {}),
    );
  }
}
