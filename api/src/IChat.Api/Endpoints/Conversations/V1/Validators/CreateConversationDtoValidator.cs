namespace IChat.Api.Endpoints.Conversations.V1.Validators;

using IChat.Api.Endpoints.Conversations.V1.DTOs;
using FluentValidation;

public sealed class CreateConversationDtoValidator : AbstractValidator<CreateConversationDto>
{
    public CreateConversationDtoValidator()
    {
        RuleFor(dto => dto.Title).MaximumLength(300).WithName("title");
        RuleFor(dto => dto.UserId).MaximumLength(200).WithName("userId");
    }
}
