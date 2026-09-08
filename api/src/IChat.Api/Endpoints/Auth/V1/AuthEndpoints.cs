namespace IChat.Api.Endpoints.Auth.V1;

using IChat.Api.Endpoints.Auth.V1.DTOs;
using IChat.Api.Endpoints.Auth.V1.Mappings;
using IChat.Api.Extensions;
using IChat.Api.Security;
using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Core.Contracts.Auth;
using FluentValidation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthV1Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Sign in.")
            .WithDescription("Answers 401 with the same message whether the account is unknown or the password is wrong.")
            .Produces<AuthResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .WithName("RefreshSession")
            .WithSummary("Exchange the refresh cookie for a new access token.")
            .WithDescription("Rotates the refresh token: the one just used is revoked, so a stolen token works at most once.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Sign out.")
            .WithDescription("Revokes the refresh token and clears its cookie.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", GetProfileAsync)
            .WithName("GetProfile")
            .WithSummary("The signed-in account.")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginDto dto,
        IValidator<LoginDto> validator,
        IAuthService auth,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            return validation.ToValidationProblem();
        }

        var result = await auth.LoginAsync(dto.ToServiceRequest(), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Ok(httpContext, result.Value);
    }

    private static async Task<IResult> RefreshAsync(
        IAuthService auth,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(httpContext);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Error.Unauthorized("Không có phiên đăng nhập nào.").ToProblemResult();
        }

        var result = await auth.RefreshAsync(refreshToken, cancellationToken);

        if (result.IsFailure)
        {
            // Cookie đã hỏng thì xoá luôn, để client không lặp lại vòng refresh vô ích.
            RefreshTokenCookie.Delete(httpContext);

            return result.Error.ToProblemResult();
        }

        return Ok(httpContext, result.Value);
    }

    private static async Task<IResult> LogoutAsync(
        IAuthService auth,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(RefreshTokenCookie.Read(httpContext), cancellationToken);
        RefreshTokenCookie.Delete(httpContext);

        return Results.NoContent();
    }

    private static async Task<IResult> GetProfileAsync(
        IAuthService auth,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Error.Unauthorized("Không có phiên đăng nhập nào.").ToProblemResult();
        }

        var result = await auth.GetProfileAsync(userId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.Ok(result.Value.ToResponse());
    }

    private static IResult Ok(HttpContext httpContext, AuthResult result)
    {
        RefreshTokenCookie.Append(httpContext, result.RefreshToken, result.RefreshTokenExpiresAt);

        return Results.Ok(result.ToResponse());
    }
}
