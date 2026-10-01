namespace IChat.Api.Endpoints.Documents.V1.Mappings;

using IChat.Api.Endpoints.Documents.V1.DTOs;
using IChat.Core.Contracts.Documents;
using IChat.Core.Domain.Documents;

public static class DocumentMapper
{
    public static UploadDocumentRequest ToServiceRequest(this UploadDocumentDto dto, Stream content) =>
        new()
        {
            Content = content,
            FileName = dto.File.FileName,
            DeclaredContentType = dto.File.ContentType,
            SizeInBytes = dto.File.Length,
            Title = dto.Title
        };

    public static GetDocumentsRequest ToServiceRequest(this GetDocumentsQuery query) =>
        new()
        {
            Status = (DocumentStatus?)query.Status,
            Offset = query.Offset,
            Limit = query.Limit
        };

    public static GetDocumentChunksRequest ToServiceRequest(this GetDocumentChunksQuery query, Guid documentId) =>
        new()
        {
            DocumentId = documentId,
            Offset = query.Offset,
            Limit = query.Limit
        };

    public static UploadDocumentResponse ToResponse(this UploadDocumentResult result) =>
        new()
        {
            Id = result.Id,
            Title = result.Title,
            Status = result.Status
        };

    public static DocumentResponse ToResponse(this DocumentSummary summary) =>
        new()
        {
            Id = summary.Id,
            Title = summary.Title,
            FileName = summary.FileName,
            ContentType = summary.ContentType,
            SizeInBytes = summary.SizeInBytes,
            Status = summary.Status,
            ErrorMessage = summary.ErrorMessage,
            ChunkCount = summary.ChunkCount,
            CreatedAt = summary.CreatedAt,
            IndexedAt = summary.IndexedAt
        };

    public static DocumentChunkResponse ToResponse(this ChunkView chunk) =>
        new()
        {
            Id = chunk.Id,
            ChunkIndex = chunk.ChunkIndex,
            Content = chunk.Content,
            HeadingPath = chunk.HeadingPath,
            EmbeddedText = chunk.EmbeddedText,
            TokenCount = chunk.TokenCount,
            EmbeddingModel = chunk.EmbeddingModel,
            EmbeddingDimensions = chunk.EmbeddingDimensions,
            Metadata = chunk.Metadata
        };
}
