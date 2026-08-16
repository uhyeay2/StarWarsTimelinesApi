using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for a user's own account settings.
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    /// Registers the account settings endpoints under <c>api/users/{userId}</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users/{userId:guid}").WithTags("Users");

        // Gets a user's account details; the caller must be the user themselves or an administrator.
        group.MapGet("/", async (Guid userId, IAccountService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessAccount(principal, userId))
            {
                return Results.Forbid();
            }
            var account = await service.GetAsync(userId, ct);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
        .WithName("GetUserAccount")
        .RequireAuthorization()
        .Produces<UserAccountResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Example account", "The demo user's account details.", ExampleValues.PadmeAccount),
            (StatusCodes.Status403Forbidden, "Not your account", "Only the account owner or an administrator can view account details.", ExampleValues.Forbidden("The caller does not own this account.")),
            (StatusCodes.Status404NotFound, "Unknown user", "No account has the requested identifier.", ExampleValues.NotFound("No user with the requested identifier was found.")));

        // Updates a user's display name; the caller must be the user themselves or an administrator.
        group.MapPut("/display-name", async (Guid userId, UpdateDisplayNameRequest request, IAccountService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessAccount(principal, userId))
            {
                return Results.Forbid();
            }
            var account = await service.UpdateDisplayNameAsync(userId, request.DisplayName, ct);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
        .WithName("UpdateUserDisplayName")
        .RequireAuthorization()
        .Produces<UserAccountResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Valid request", "Changes the display name shown in the user interface.", ExampleValues.UpdateDisplayName),
            ("Blank display name", "Rejected when the display name is empty or whitespace.", ExampleValues.BlankDisplayName))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Account updated", "The account with the new display name.", ExampleValues.PadmeAccountUpdatedName),
            (StatusCodes.Status400BadRequest, "Blank display name", "The display name is required.", ExampleValues.BadRequest("A display name is required.")),
            (StatusCodes.Status403Forbidden, "Not your account", "Only the account owner or an administrator can update account details.", ExampleValues.Forbidden("The caller does not own this account.")),
            (StatusCodes.Status404NotFound, "Unknown user", "No account has the requested identifier.", ExampleValues.NotFound("No user with the requested identifier was found.")));

        // Changes a user's email address and emails a fresh verification link; the caller must be the user themselves
        // or an administrator.
        group.MapPut("/email", async (Guid userId, UpdateEmailRequest request, IAccountService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessAccount(principal, userId))
            {
                return Results.Forbid();
            }
            var account = await service.UpdateEmailAsync(userId, request.Email, ct);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
        .WithName("UpdateUserEmail")
        .RequireAuthorization()
        .Produces<UserAccountResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Valid request", "Changes the email address; the account must verify the new address before logging in again.", ExampleValues.UpdateEmail),
            ("Duplicate email", "Rejected when the email address already belongs to another account.", ExampleValues.DuplicateEmailUpdate))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Account updated", "The account with the new (unverified) email address.", ExampleValues.PadmeAccountUpdatedEmail),
            (StatusCodes.Status400BadRequest, "Duplicate email", "The email address is already in use.", ExampleValues.BadRequest("A user with this email address is already registered.")),
            (StatusCodes.Status403Forbidden, "Not your account", "Only the account owner or an administrator can update account details.", ExampleValues.Forbidden("The caller does not own this account.")),
            (StatusCodes.Status404NotFound, "Unknown user", "No account has the requested identifier.", ExampleValues.NotFound("No user with the requested identifier was found.")));

        // Changes a user's password after verifying the current password; the caller must be the user themselves or an
        // administrator.
        group.MapPut("/password", async (Guid userId, UpdatePasswordRequest request, IAccountService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessAccount(principal, userId))
            {
                return Results.Forbid();
            }
            var account = await service.UpdatePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
        .WithName("UpdateUserPassword")
        .RequireAuthorization()
        .Produces<UserAccountResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Valid request", "Changes the password when the current password is correct.", ExampleValues.UpdatePassword),
            ("Wrong current password", "Rejected when the current password does not match.", ExampleValues.WrongCurrentPassword))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Account updated", "The account after the password change.", ExampleValues.PadmeAccount),
            (StatusCodes.Status400BadRequest, "Wrong current password", "The current password is incorrect.", ExampleValues.BadRequest("The current password is incorrect.")),
            (StatusCodes.Status403Forbidden, "Not your account", "Only the account owner or an administrator can update account details.", ExampleValues.Forbidden("The caller does not own this account.")),
            (StatusCodes.Status404NotFound, "Unknown user", "No account has the requested identifier.", ExampleValues.NotFound("No user with the requested identifier was found.")));

        return group;
    }

    /// <summary>
    /// Determines whether the caller may manage the account of the given user.
    /// </summary>
    /// <param name="principal">The caller's claims principal.</param>
    /// <param name="userId">The identifier of the user whose account is being accessed.</param>
    /// <returns><c>true</c> when the caller is the user themselves or an administrator; otherwise <c>false</c>.</returns>
    private static bool CanAccessAccount(ClaimsPrincipal principal, Guid userId) =>
        principal.IsInRole(UserRole.Admin.ToString()) ||
        principal.FindFirstValue(ClaimTypes.NameIdentifier) == userId.ToString();
}
