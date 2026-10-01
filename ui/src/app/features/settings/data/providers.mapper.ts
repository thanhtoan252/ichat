import type { ProviderCatalogDto, ProviderInfoDto } from '@app/core/api/retrieval.dto';
import type { ProviderCatalog, ProviderInfo } from './provider.model';

export function toProviderInfo(dto: ProviderInfoDto): ProviderInfo {
  return {
    kind: dto.kind,
    provider: dto.provider,
    model: dto.model,
    available: dto.available,
    reason: dto.reason,
  };
}

export function toProviderCatalog(dto: ProviderCatalogDto): ProviderCatalog {
  return {
    chat: toProviderInfo(dto.chat),
    utilityChat: toProviderInfo(dto.utilityChat),
    embedding: toProviderInfo(dto.embedding),
    allowedChatModels: dto.allowedChatModels,
  };
}
