import { Injectable, computed, inject, signal } from '@angular/core';
import { toast } from 'ngx-sonner';
import { RetrievalApi } from '@app/core/api/retrieval.api';
import { describeHttpError } from '@app/core/api/http-error';
import { toProviderCatalog } from '../data/providers.mapper';
import type { ProviderCatalog, ProviderRow } from '../data/provider.model';

@Injectable()
export class SettingsStore {
  private readonly api = inject(RetrievalApi);

  private readonly catalogSignal = signal<ProviderCatalog | null>(null);
  private readonly loadingSignal = signal(true);

  public readonly catalog = this.catalogSignal.asReadonly();
  public readonly loading = this.loadingSignal.asReadonly();

  public readonly allowedChatModels = computed<readonly string[]>(
    () => this.catalogSignal()?.allowedChatModels ?? [],
  );

  public readonly providers = computed<readonly ProviderRow[]>(() => {
    const catalog = this.catalogSignal();

    if (!catalog) {
      return [];
    }

    return [
      {
        title: 'Chat',
        explanation: 'Composes the answer from the retrieved context.',
        info: catalog.chat,
      },
      {
        title: 'Utility chat',
        explanation: 'Cheaper model used for query rewriting and other side tasks.',
        info: catalog.utilityChat,
      },
      {
        title: 'Embedding',
        explanation: 'Turns chunks and questions into vectors for the pgvector index.',
        info: catalog.embedding,
      },
    ];
  });

  public async load(): Promise<void> {
    this.loadingSignal.set(true);

    try {
      this.catalogSignal.set(toProviderCatalog(await this.api.providers()));
    } catch (error) {
      toast.error(describeHttpError(error, 'Could not read the provider catalog.'));
    } finally {
      this.loadingSignal.set(false);
    }
  }
}
