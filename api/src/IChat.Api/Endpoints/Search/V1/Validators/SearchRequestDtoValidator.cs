namespace IChat.Api.Endpoints.Search.V1.Validators;

using IChat.Api.Endpoints.Search.V1.DTOs;
using FluentValidation;

public sealed class SearchRequestDtoValidator : AbstractValidator<SearchRequestDto>
{
    public SearchRequestDtoValidator()
    {
        RuleFor(dto => dto.Query)
            .NotEmpty().WithMessage("Query is required.")
            .MaximumLength(4000)
            .WithName("query");

        RuleFor(dto => dto.TopK)
            .InclusiveBetween(1, 100)
            .WithName("topK")
            .When(dto => dto.TopK is not null);

        RuleForEach(dto => dto.History).ChildRules(turn =>
        {
            turn.RuleFor(item => item.Role).NotEmpty();
            turn.RuleFor(item => item.Content).NotEmpty();
        });
    }
}
