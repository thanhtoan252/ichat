namespace IChat.Api.Endpoints.Auth.V1.Validators;

using IChat.Api.Endpoints.Auth.V1.DTOs;
using FluentValidation;

public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        // Cố tình không áp quy tắc độ dài/ký tự như lúc đăng ký: tài khoản seed là
        // "admin"/"admin", và ràng buộc ở đây chỉ tổ lộ định dạng mật khẩu hợp lệ.
        RuleFor(dto => dto.UserName).NotEmpty().MaximumLength(64).WithName("userName");
        RuleFor(dto => dto.Password).NotEmpty().MaximumLength(128).WithName("password");
    }
}
