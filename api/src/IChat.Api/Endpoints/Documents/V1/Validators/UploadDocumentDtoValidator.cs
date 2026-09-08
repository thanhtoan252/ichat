namespace IChat.Api.Endpoints.Documents.V1.Validators;

using IChat.Api.Endpoints.Documents.V1.DTOs;
using FluentValidation;

public sealed class UploadDocumentDtoValidator : AbstractValidator<UploadDocumentDto>
{
    public const long MaxSizeInBytes = 20 * 1024 * 1024;

    public UploadDocumentDtoValidator()
    {
        RuleFor(dto => dto.File.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .WithName("file").OverridePropertyName("file");

        RuleFor(dto => dto.File.Length)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(MaxSizeInBytes).WithMessage("The file exceeds the 20MB limit.")
            .WithName("file").OverridePropertyName("file");

        RuleFor(dto => dto.Title)
            .MaximumLength(300)
            .WithName("title");
    }
}
