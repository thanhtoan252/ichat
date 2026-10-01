namespace IChat.Core;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using IChat.Core.Services;
using IChat.Core.Services.Chat;
using IChat.Core.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>
    /// No MediatR, no CQRS: each group of business logic is one service behind an interface,
    /// registered scoped, and endpoints inject the interface and call its methods directly.
    /// </summary>
    public static IServiceCollection AddIChatCore(this IServiceCollection services)
    {
        // Only the SSE flow's validator is left: every error of a chat turn has to travel over the SSE
        // channel, so ChatService is the only place allowed to decide errors. The plain HTTP flows
        // validate their DTOs at the API layer to return ValidationProblemDetails with per-field errors.
        services.AddValidatorsFromAssemblyContaining<SendMessageRequestValidator>(ServiceLifetime.Scoped);

        services.AddScoped<RetrievalPipeline>();

        // ChatService's internal collaborators: registered by concrete type, following the precedent set
        // by RetrievalPipeline / ContextAssembler, rather than growing an interface just to inject them.
        services.AddScoped<ChatTurnContextBuilder>();
        services.AddScoped<AnswerGenerator>();
        services.AddScoped<ChatTurnRecorder>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
