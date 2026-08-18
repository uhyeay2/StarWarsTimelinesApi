using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for authentication.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Registers the authentication endpoints under <c>api/auth</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/auth").WithTags("Auth");

        // Authenticates a user and returns a bearer token when the credentials are valid.
        group.MapPost("/login", async (LoginRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.AuthenticateAsync(request.Username, request.Password, ct);
            if (result.Auth is not null)
            {
                return Results.Ok(result.Auth);
            }

            return result.Failure == LoginFailure.EmailNotVerified
                ? Results.Json(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Email not verified",
                        Detail = "Please verify your email address before logging in."
                    },
                    statusCode: StatusCodes.Status401Unauthorized)
                : Results.Unauthorized();
        })
        .WithName("Login")
        .Produces<AuthResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/json")
        .WithRequestExamples(
            ("Valid credentials", "A verified account logging in with the correct password.", ExampleValues.ValidLogin),
            ("Unknown credentials", "A username that does not exist, or a wrong password.", ExampleValues.UnknownCredentials))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Authenticated", "A signed bearer token together with the authenticated user.", ExampleValues.ValidAuthResponse),
            (StatusCodes.Status401Unauthorized, "Email not verified", "Returned when the account exists but its email has not been verified yet.", ExampleValues.Unauthorized("Please verify your email address before logging in.")),
            (StatusCodes.Status400BadRequest, "Invalid request", "The request body is missing or malformed.", ExampleValues.BadRequest("The request body must contain a username and a password.")));

        // Creates a new user account and emails the user a verification link.
        group.MapPost("/register", async (RegisterRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.RegisterAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .WithRequestExamples(
            ("Valid registration", "Creates a new account and emails the verification link.", ExampleValues.ValidRegistration),
            ("Duplicate email", "Rejected when the email address is already registered.", ExampleValues.DuplicateEmailRegistration),
            ("Weak password", "Rejected when the password is shorter than six characters.", ExampleValues.WeakPasswordRegistration))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Account created", "The new account as stored.", ExampleValues.ValidRegistrationResponse),
            (StatusCodes.Status400BadRequest, "Duplicate email", "The email address is already in use.", ExampleValues.BadRequest("A user with the email 'ahsoka.tano@example.com' already exists.")),
            (StatusCodes.Status400BadRequest, "Weak password", "The password does not meet the minimum length.", ExampleValues.BadRequest("The password must be at least 6 characters long.")));

        // Marks a registered account's email address as verified.
        group.MapPost("/verify-email", async (VerifyEmailRequest request, IAuthService service, CancellationToken ct) =>
        {
            await service.VerifyEmailAsync(request.Token, ct);
            return Results.Ok();
        })
        .WithName("VerifyEmail")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .WithRequestExamples(
            ("Valid token", "The token received in the verification email.", ExampleValues.ValidEmailToken),
            ("Invalid token", "A token that is malformed, expired, or has already been used.", ExampleValues.InvalidEmailToken))
        .WithResponseExamples(
            (StatusCodes.Status400BadRequest, "Invalid token", "The token is malformed, expired, or does not match any account.", ExampleValues.BadRequest("The verification token is invalid or has expired.")));

        // Emails a fresh verification link to an unverified account.
        group.MapPost("/resend-verification-email", async (ResendVerificationEmailRequest request, IAuthService service, CancellationToken ct) =>
        {
            await service.ResendVerificationEmailAsync(request.UsernameOrEmail, ct);
            return Results.Ok();
        })
        .WithName("ResendVerificationEmail")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .WithRequestExamples(
            ("By username", "Resend the verification link using the account username.", ExampleValues.ResendByUsername),
            ("By email", "Resend the verification link using the account email address.", ExampleValues.ResendByEmail))
        .WithResponseExamples(
            (StatusCodes.Status400BadRequest, "Blank identifier", "The username or email address is required.", ExampleValues.BadRequest("A username or email address is required.")));

        // Exchanges a valid refresh token for a new access/refresh token pair.
        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService service, CancellationToken ct) =>
        {
            var result = await service.RefreshAsync(request.RefreshToken, ct);
            return Results.Ok(result);
        })
        .WithName("RefreshToken")
        .Produces<AuthResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .WithRequestExamples(
            ("Valid refresh token", "A non-expired, non-revoked refresh token.", ExampleValues.ValidRefreshToken),
            ("Expired refresh token", "A refresh token whose expiry has passed.", ExampleValues.ExpiredRefreshToken))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Rotated", "A new access token and refresh token pair.", ExampleValues.ValidAuthResponse),
            (StatusCodes.Status400BadRequest, "Invalid token", "The refresh token is invalid, revoked, or expired.", ExampleValues.BadRequest("The refresh token has expired.")));

        return group;
    }
}
