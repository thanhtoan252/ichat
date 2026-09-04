/**
 * Wire types for the endpoints on the shared `RetrievalApi` client. ASP.NET Core
 * serialises with the camelCase policy, so these mirror the JSON exactly; each
 * consuming feature maps its slice onto its own domain model in its `data-access/`.
 */

// ---------------------------------------------------------------------- providers

export interface ProviderInfoDto {
  readonly kind: string;
  readonly provider: string;
  readonly model: string;
  readonly available: boolean;
  readonly reason: string | null;
}

export interface ProviderCatalogDto {
  readonly chat: ProviderInfoDto;
  readonly utilityChat: ProviderInfoDto;
  readonly embedding: ProviderInfoDto;
  readonly allowedChatModels: readonly string[];
}
