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
            .WithDescription("Paged, most recently updated first, optionally filtered by userId.")
            .Produces<PagedResponse<ConversationResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}/messages", GetConversationMessagesAsync)
            .WithName("GetConversationMessages")
            .WithSummary("Message history of a conversation.")
            .WithDescription("Oldest first, including the marker-verified citations of each answer.")
            .Produces<PagedResponse<MessageResponse>>()
            .ProducesValidationProblem()
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
        IConversationService conversations,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await conversations.CreateAsync(dto.ToServiceRequest(), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = result.Value.ToResponse();

        return Results.Created($"/api/v1/conversations/{response.Id}", response);
    }

    private static async Task<IResult> GetConversationsAsync(
        [AsParameters] GetConversationsQuery query,
        IValidator<GetConversationsQuery> validator,
        IConversationService conversations,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await conversations.GetListAsync(
            query.EffectivePage,
            query.EffectivePageSize,
            query.UserId,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToPagedResponse(ConversationMapper.ToResponse));
    }

    private static async Task<IResult> GetConversationMessagesAsync(
        Guid id,
        [AsParameters] GetMessagesQuery query,
        IValidator<GetMessagesQuery> validator,
        IConversationService conversations,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await conversations.GetMessagesAsync(
            id,
            query.EffectivePage,
            query.EffectivePageSize,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToPagedResponse(ConversationMapper.ToResponse));
    }

    /// <summary>
    /// Không validate DTO ở đây: mọi lỗi của lượt chat đi qua kênh SSE dưới dạng
    /// event "error" với HTTP 200, nên ChatService là nơi duy nhất quyết định lỗi.
    /// </summary>
    private static IResult SendMessage(
        Guid id,
        SendMessageDto dto,
        IChatService chat,
        CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(Stream(chat, dto.ToServiceRequest(id), cancellationToken));
    }

    /// <summary>
    /// CancellationToken được truyền xuống tận nơi: client đóng tab thì service dừng
    /// stream và vẫn lưu phần đã sinh kèm ghi chú [interrupted].
    /// </summary>
    private static async IAsyncEnumerable<SseItem<object>> Stream(
        IChatService chat,
        SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var sseEvent in chat.StreamAnswerAsync(request, cancellationToken))
        {
            yield return new SseItem<object>(sseEvent.Payload, sseEvent.EventType);
        }
    }
}
