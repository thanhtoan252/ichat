namespace IChat.Api.Endpoints.Search.V1;

using IChat.Api.Endpoints.Search.V1.DTOs;
using IChat.Api.Endpoints.Search.V1.Mappings;
using IChat.Api.Extensions;
using IChat.Core.Abstractions;
using FluentValidation;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchV1Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/search").WithTags("Search");

        group.MapPost("/", SearchChunksAsync)
            .WithName("SearchChunks")
            .WithSummary("Hybrid search over the chunk store.")
            .WithDescription("Returns the result of each stage plus the tsquery that was built, for debugging retrieval: rewrite, vector, fulltext, fusion, mmr, neighbors, rerank, final.")
            .Produces<SearchResponse>()
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> SearchChunksAsync(
        SearchRequestDto dto,
        IValidator<SearchRequestDto> validator,
        ISearchService search,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await search.SearchAsync(dto.ToServiceRequest(), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToResponse());
    }
}
