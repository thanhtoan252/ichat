namespace IChat.Api.Endpoints.Documents.V1;

using IChat.Api.Authorization;
using IChat.Api.Endpoints.Common;
using IChat.Api.Endpoints.Documents.V1.DTOs;
using IChat.Api.Endpoints.Documents.V1.Mappings;
using IChat.Api.Extensions;
using IChat.Core.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentV1Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents").WithTags("Documents");

        group.MapPost("/", UploadDocumentAsync)
            .RequireAuthorization(AuthPolicies.Admin)
            .DisableAntiforgery()
            .WithName("UploadDocument")
            .WithSummary("Upload a document.")
            .WithDescription("Accepts multipart/form-data, stores the file and pushes it onto the ingestion queue. Returns 202 because parsing and embedding run in the background.")
            .Produces<UploadDocumentResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        group.MapGet("/", GetDocumentsAsync)
            .WithName("GetDocuments")
            .WithSummary("List documents.")
            .WithDescription("Paged, newest first, optionally filtered by ingestion status.")
            .Produces<PagedResponse<DocumentResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetDocumentByIdAsync)
            .WithName("GetDocumentById")
            .WithSummary("Document details.")
            .WithDescription("Used to poll the ingestion status: Pending, Processing, Indexed, or Failed with an errorMessage.")
            .Produces<DocumentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/chunks", GetDocumentChunksAsync)
            .WithName("GetDocumentChunks")
            .WithSummary("Chunks of a document.")
            .WithDescription("An ingestion debugging tool: shows the headingPath and embeddedText that were actually produced. Does not return embedding vectors.")
            .Produces<PagedResponse<DocumentChunkResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteDocumentAsync)
            .RequireAuthorization(AuthPolicies.Admin)
            .WithName("DeleteDocument")
            .WithSummary("Delete a document.")
            .WithDescription("Deletes both the stored file and every chunk by cascade.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> UploadDocumentAsync(
        [FromForm] IFormFile file,
        [FromForm] string? title,
        IValidator<UploadDocumentDto> validator,
        IDocumentService documents,
        CancellationToken cancellationToken)
    {
        var dto = new UploadDocumentDto { File = file, Title = title };
        var validation = await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        await using var content = file.OpenReadStream();
        var result = await documents.UploadAsync(dto.ToServiceRequest(content), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = result.Value.ToResponse();

        return Results.Accepted($"/api/v1/documents/{response.Id}", response);
    }

    private static async Task<IResult> GetDocumentsAsync(
        [AsParameters] GetDocumentsQuery query,
        IValidator<GetDocumentsQuery> validator,
        IDocumentService documents,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await documents.GetListAsync(query.ToServiceRequest(), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToPagedResponse(DocumentMapper.ToResponse));
    }

    private static async Task<IResult> GetDocumentByIdAsync(
        Guid id,
        IDocumentService documents,
        CancellationToken cancellationToken)
    {
        var result = await documents.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetDocumentChunksAsync(
        Guid id,
        [AsParameters] GetDocumentChunksQuery query,
        IValidator<GetDocumentChunksQuery> validator,
        IDocumentService documents,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await documents.GetChunksAsync(query.ToServiceRequest(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToPagedResponse(DocumentMapper.ToResponse));
    }

    private static async Task<IResult> DeleteDocumentAsync(
        Guid id,
        IDocumentService documents,
        CancellationToken cancellationToken)
    {
        var result = await documents.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.NoContent();
    }
}
