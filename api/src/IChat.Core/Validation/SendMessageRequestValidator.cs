namespace IChat.Core.Validation;

using IChat.Core.Contracts.Conversations;
using FluentValidation;

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(request => request.Content).NotEmpty().WithMessage("Question content must not be empty.").MaximumLength(8000);
    }
}
