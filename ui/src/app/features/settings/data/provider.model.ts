export interface ProviderInfo {
  readonly kind: string;
  readonly provider: string;
  readonly model: string;
  readonly available: boolean;
  readonly reason: string | null;
}

export interface ProviderCatalog {
  readonly chat: ProviderInfo;
  readonly utilityChat: ProviderInfo;
  readonly embedding: ProviderInfo;
  readonly allowedChatModels: readonly string[];
}

/** One provider card as the view renders it. */
export interface ProviderRow {
  readonly title: string;
  readonly explanation: string;
  readonly info: ProviderInfo;
}
