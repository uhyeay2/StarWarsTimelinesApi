using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.OpenApi;

/// <summary>
/// Shared example values used to populate the Swagger request and response examples. The examples mirror the demo
/// data seeded into the development database and are serialized with the same JSON conventions the API uses at
/// runtime (enums are numbers, property names are camel-cased).
/// </summary>
public static class ExampleValues
{
    /// <summary>A plausible trace identifier used inside ProblemDetails examples.</summary>
    public const string TraceId = "00-3f2e1d4c5b6a7f8e9d0c1b2a3f4e5d6c-0a1b2c3d4e5f6a7b-00";

    /// <summary>A structurally valid JWT shaped like the tokens the API issues (not signed with the real key).</summary>
    public const string BearerToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIyMjIyMjIyMi0yMjIyLTIyMjItMjIyMi0yMjIyMjIyMjIyMjIiLCJuYW1lIjoicGFkbWUiLCJyb2xlIjoiU3RhbmRhcmQiLCJpc3MiOiJTdGFyV2Fyc1RpbWVsaW5lcyIsImF1ZCI6IlN0YXJXYXJzVGltZWxpbmVzIiwibmJmIjoxNzAwMDAwMDAwLCJleHAiOjE3MDAwMDM2MDB9.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    /// <summary>Builds the ProblemDetails body returned for failed validation.</summary>
    public static ProblemDetails BadRequest(string detail) => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = "Bad Request",
        Status = StatusCodes.Status400BadRequest,
        Detail = detail,
        Instance = TraceId
    };

    /// <summary>Builds a ProblemDetails body describing a missing or rejected authentication.</summary>
    public static ProblemDetails Unauthorized(string detail) => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = "Unauthorized",
        Status = StatusCodes.Status401Unauthorized,
        Detail = detail,
        Instance = TraceId
    };

    /// <summary>Builds a ProblemDetails body describing an authenticated caller without the required role.</summary>
    public static ProblemDetails Forbidden(string detail) => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = "Forbidden",
        Status = StatusCodes.Status403Forbidden,
        Detail = detail,
        Instance = TraceId
    };

    /// <summary>Builds a ProblemDetails body describing a request for a resource that does not exist.</summary>
    public static ProblemDetails NotFound(string detail) => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = "Not Found",
        Status = StatusCodes.Status404NotFound,
        Detail = detail,
        Instance = TraceId
    };

    // ---- Authentication ----

    /// <summary>A verified account logging in with the correct password.</summary>
    public static readonly LoginRequest ValidLogin = new("padme", "padme123");

    /// <summary>A username that does not exist, or a wrong password.</summary>
    public static readonly LoginRequest UnknownCredentials = new("nobody", "not-the-password");

    /// <summary>A well-formed registration that creates a new account.</summary>
    public static readonly RegisterRequest ValidRegistration = new(
        "ahsoka.tano",
        "Ahsoka Tano",
        "ahsoka.tano@example.com",
        "trusttheforce1");

    /// <summary>A registration rejected because the email address is already in use.</summary>
    public static readonly RegisterRequest DuplicateEmailRegistration = new(
        "ahsoka.tano.2",
        "Ahsoka Tano",
        "ahsoka.tano@example.com",
        "trusttheforce1");

    /// <summary>A registration rejected because the password is shorter than six characters.</summary>
    public static readonly RegisterRequest WeakPasswordRegistration = new("rey", "Rey", "rey@example.com", "123");

    /// <summary>A verification token shaped like the ones issued at registration.</summary>
    public static readonly VerifyEmailRequest ValidEmailToken = new(
        "3f4c9d7b2e8a6f10c3d5e7a9b1c4d6e8f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8");

    /// <summary>A verification token that is malformed, expired, or has already been used.</summary>
    public static readonly VerifyEmailRequest InvalidEmailToken = new(
        "0000000000000000000000000000000000000000000000000000000000000000");

    /// <summary>Resends the verification link using the account username.</summary>
    public static readonly ResendVerificationEmailRequest ResendByUsername = new("ahsoka.tano");

    /// <summary>Resends the verification link using the account email address.</summary>
    public static readonly ResendVerificationEmailRequest ResendByEmail = new("ahsoka.tano@example.com");

    /// <summary>The seeded demo user "padme".</summary>
    public static readonly UserResponse PadmeUser = new(
        new Guid("22222222-2222-2222-2222-222222222222"),
        "padme",
        "Padmé Amidala",
        UserRole.Standard);

    /// <summary>A successful authentication response with a signed bearer token.</summary>
    public static readonly AuthResponse ValidAuthResponse = new(BearerToken, PadmeUser);

    /// <summary>The response returned when a registration succeeds.</summary>
    public static readonly RegisterResponse ValidRegistrationResponse = new(
        new Guid("00000000-0000-0000-0000-500000000001"),
        "ahsoka.tano",
        "Ahsoka Tano",
        "AHSOKA.TANO@EXAMPLE.COM");
}
