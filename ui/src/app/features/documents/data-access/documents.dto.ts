/**
 * Wire types for `/documents`. ASP.NET Core serialises with the camelCase policy, so
 * these mirror the JSON exactly; `documents.mapper.ts` maps them onto the feature's
 * own domain model.
 */

export type DocumentStatusDto = 'Pending' | 'Processing' | 'Indexed' | 'Failed';

export interface DocumentSummaryDto {
  readonly id: string;
  readonly title: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeInBytes: number;
  readonly status: DocumentStatusDto;
  readonly errorMessage: string | null;
  readonly chunkCount: number;
  readonly createdAt: string;
  readonly indexedAt: string | null;
}

export interface DocumentChunkDto {
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

export interface UploadDocumentResultDto {
  readonly id: string;
  readonly title: string;
  readonly status: DocumentStatusDto;
}
