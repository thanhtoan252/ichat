export type DocumentStatus = 'Pending' | 'Processing' | 'Indexed' | 'Failed';

export interface DocumentSummary {
  readonly id: string;
  readonly title: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeInBytes: number;
  readonly status: DocumentStatus;
  readonly errorMessage: string | null;
  readonly chunkCount: number;
  readonly createdAt: string;
  readonly indexedAt: string | null;
}

export interface DocumentChunk {
  readonly id: string;
  readonly chunkIndex: number;
  readonly content: string;
  readonly headingPath: string | null;
  readonly embeddedText: string;
  readonly tokenCount: number;
  readonly embeddingModel: string;
  readonly embeddingDimensions: number;
  readonly metadata: string;
}

export interface UploadDocumentResult {
  readonly id: string;
  readonly title: string;
  readonly status: DocumentStatus;
}

/** Response of the admin reindex-all action, owned by this feature even though the
 * endpoint lives on the shared RetrievalApi client alongside /search and /providers. */
export interface ReindexResult {
  readonly documentCount: number;
}

export type StatusFilter = DocumentStatus | 'All';

export interface UploadProgress {
  readonly fileName: string;
  readonly percent: number;
}
