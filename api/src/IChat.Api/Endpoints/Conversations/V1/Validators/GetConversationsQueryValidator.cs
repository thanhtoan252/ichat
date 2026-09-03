namespace IChat.Api.Endpoints.Conversations.V1.Validators;

using IChat.Api.Endpoints.Common;
using IChat.Api.Endpoints.Conversations.V1.DTOs;

public sealed class GetConversationsQueryValidator : PagedQueryValidator<GetConversationsQuery>
{
    public GetConversationsQueryValidator()
        : base(maxPageSize: 100)
    {
    }
}
