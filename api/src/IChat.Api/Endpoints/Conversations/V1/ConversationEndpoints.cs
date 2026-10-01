namespace IChat.Api.Endpoints.Conversations.V1;

using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using IChat.Api.Endpoints.Common;
using IChat.Api.Endpoints.Conversations.V1.DTOs;
using IChat.Api.Endpoints.Conversations.V1.Mappings;
using IChat.Api.Extensions;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Conversations;
using FluentValidation;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationV1Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations").WithTags("Conversations");

        group.MapPost("/", CreateConversationAsync)
            .WithName("CreateConversation")
            .WithSummary("Create a conversation.")
            .WithDescription("An empty title is replaced with the default name.")
            .Produces<ConversationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", GetConversationsAsync)
            .WithName("GetConversations")
            .WithSummary("List conversations.")
            .WithDescription("Paged, most recently updated first. Scoped to the caller; an administrator sees every conversation.")
            .Produces<PagedResponse<ConversationResponse>>();

        group.MapGet("/{id:guid}/messages", GetConversationMessagesAsync)
            .WithName("GetConversationMessages")
            .WithSummary("Message history of a conversation.")
            .WithDescription("Oldest first, including the marker-verified citations of each answer.")
            .Produces<PagedResponse<MessageResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/messages", SendMessage)
            .RequireRateLimiting("chat")
            .WithName("SendMessage")
            .WithSummary("RAG question answering, streamed over SSE.")
            .WithDescription("Four event types: status, sources, delta, done. Business errors come back as an error event with HTTP 200 rather than a different status code.")
            .Produces<SseItem<object>>(StatusCodes.Status200OK, "text/event-stream");

        return app;
    }

    private static async Task<IResult> CreateConversationAsync(
        CreateConversationDto dto,
        IValidator<CreateConversationDto> validator,
        IConversationService conversationService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await conversationService.CreateAsync(dto.ToServiceRequest(), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = result.Value.ToResponse();

        return Results.Created($"/api/v1/conversations/{response.Id}", response);
    }

    private static async Task<IResult> GetConversationsAsync(
        [AsParameters] GetConversationsQuery query,
        IConversationService conversationService,
        CancellationToken cancellationToken)
    {
        var result = await conversationService.GetListAsync(query.Offset, query.Limit, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToPagedResponse(ConversationMapper.ToResponse));
    }

    private static async Task<IResult> GetConversationMessagesAsync(
        Guid id,
        [AsParameters] GetMessagesQuery query,
        IConversationService conversationService,
        CancellationToken cancellationToken)
    {
        var result = await conversationService.GetMessagesAsync(id, query.Offset, query.Limit, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToPagedResponse(ConversationMapper.ToResponse));
    }

    /// <summary>
    /// The DTO is not validated here: every error of a chat turn travels over the SSE channel
    /// as an "error" event with HTTP 200, so ChatService is the only place that decides errors.
    /// </summary>
    private static IResult SendMessage(
        Guid id,
        SendMessageDto dto,
        IChatService chatService,
        CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(Stream(chatService, dto.ToServiceRequest(id), cancellationToken));
    }

    /// <summary>
    /// The CancellationToken is passed all the way down: when the client closes the tab the
    /// service stops streaming and still persists what was generated, marked [interrupted].
    /// </summary>
    private static async IAsyncEnumerable<SseItem<object>> Stream(
        IChatService chatService,
        SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var sseEvent in chatService.StreamAnswerAsync(request, cancellationToken))
        {
            yield return new SseItem<object>(sseEvent.Payload, sseEvent.EventType);
        }
    }
}
