import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL, API_V1 } from '@app/core/api/api.config';
import type {
  ProviderCatalogDto,
  ReindexResultDto,
  SearchRequestDto,
  SearchResultDto,
} from './retrieval.dto';

/** `/search` and the admin endpoints: the retrieval-debugging surface of the API. */
@Injectable({ providedIn: 'root' })
export class RetrievalApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  search(request: SearchRequestDto): Promise<SearchResultDto> {
    return firstValueFrom(
      this.http.post<SearchResultDto>(`${this.baseUrl}${API_V1}/search`, request),
    );
  }

  providers(): Promise<ProviderCatalogDto> {
    return firstValueFrom(
      this.http.get<ProviderCatalogDto>(`${this.baseUrl}${API_V1}/providers`),
    );
  }

  reindex(): Promise<ReindexResultDto> {
    return firstValueFrom(
      this.http.post<ReindexResultDto>(`${this.baseUrl}${API_V1}/admin/reindex`, {}),
    );
  }
}
