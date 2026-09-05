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
    /// Không MediatR, không CQRS: mỗi nhóm nghiệp vụ là một service sau interface,
    /// đăng ký scoped, endpoint inject interface và gọi method trực tiếp.
    /// </summary>
    public static IServiceCollection AddIChatCore(this IServiceCollection services)
    {
        // Chỉ còn validator của luồng SSE: mọi lỗi của lượt chat phải đi qua kênh SSE nên
        // ChatService là nơi duy nhất được quyết định lỗi. Các luồng HTTP thường validate
        // DTO ngay ở tầng API để trả về ValidationProblemDetails có lỗi theo từng field.
        services.AddValidatorsFromAssemblyContaining<SendMessageRequestValidator>(ServiceLifetime.Scoped);

        services.AddScoped<RetrievalPipeline>();

        // Collaborator nội bộ của ChatService: đăng ký bằng kiểu cụ thể theo đúng tiền lệ
        // RetrievalPipeline / ContextAssembler, không đẻ thêm interface chỉ để tiêm.
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
