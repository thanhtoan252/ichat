import type { ReindexResultDto } from '@app/core/api/retrieval.dto';
import type {
  DocumentChunkDto,
  DocumentStatusDto,
  DocumentSummaryDto,
  UploadDocumentResultDto,
} from './documents.dto';
import type {
  DocumentChunk,
  DocumentStatus,
  DocumentSummary,
  ReindexResult,
  UploadDocumentResult,
} from '../model/document.model';

function toDocumentStatus(dto: DocumentStatusDto): DocumentStatus {
  return dto;
}

export function toDocumentSummary(dto: DocumentSummaryDto): DocumentSummary {
  return {
    id: dto.id,
    title: dto.title,
    fileName: dto.fileName,
    contentType: dto.contentType,
    sizeInBytes: dto.sizeInBytes,
    status: toDocumentStatus(dto.status),
    errorMessage: dto.errorMessage,
    chunkCount: dto.chunkCount,
    createdAt: dto.createdAt,
    indexedAt: dto.indexedAt,
  };
}

export function toDocumentChunk(dto: DocumentChunkDto): DocumentChunk {
  return {
    id: dto.id,
    chunkIndex: dto.chunkIndex,
    content: dto.content,
    headingPath: dto.headingPath,
    embeddedText: dto.embeddedText,
    tokenCount: dto.tokenCount,
    embeddingModel: dto.embeddingModel,
    embeddingDimensions: dto.embeddingDimensions,
    metadata: dto.metadata,
  };
}

export function toUploadDocumentResult(dto: UploadDocumentResultDto): UploadDocumentResult {
  return {
    id: dto.id,
    title: dto.title,
    status: toDocumentStatus(dto.status),
  };
}

export function toReindexResult(dto: ReindexResultDto): ReindexResult {
  return { documentCount: dto.documentCount };
}
