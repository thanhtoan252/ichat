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

    public static IServiceCollection AddIChatCore(this IServiceCollection services)
    {

        services.AddValidatorsFromAssemblyContaining<SendMessageRequestValidator>(ServiceLifetime.Scoped);

        services.AddScoped<RetrievalPipeline>();

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
