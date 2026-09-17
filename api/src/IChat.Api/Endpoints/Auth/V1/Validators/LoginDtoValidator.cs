namespace IChat.Api.Endpoints.Auth.V1.Validators;

using IChat.Api.Endpoints.Auth.V1.DTOs;
using FluentValidation;

public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        // Deliberately no length/character rules like the ones used at registration: the seeded
        // account is "admin"/"admin", and rules here would only advertise what a valid password looks like.
        RuleFor(dto => dto.UserName).NotEmpty().MaximumLength(64).WithName("userName");
        RuleFor(dto => dto.Password).NotEmpty().MaximumLength(128).WithName("password");
    }
}
