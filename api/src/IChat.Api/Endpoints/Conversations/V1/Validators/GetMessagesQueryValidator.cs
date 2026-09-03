namespace IChat.Api.Endpoints.Conversations.V1.Validators;

using IChat.Api.Endpoints.Common;
using IChat.Api.Endpoints.Conversations.V1.DTOs;

public sealed class GetMessagesQueryValidator : PagedQueryValidator<GetMessagesQuery>
{
    public GetMessagesQueryValidator()
        : base(maxPageSize: 200)
    {
    }
}
