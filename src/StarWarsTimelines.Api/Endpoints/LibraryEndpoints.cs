using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for per-user libraries of tracked source materials.
/// </summary>
public static class LibraryEndpoints
{
    /// <summary>
    /// Registers the library endpoints under <c>api/users/{userId}/source-materials</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users/{userId:guid}/source-materials").WithTags("Library");

        // Gets a user's library; the caller must be the user themselves or an administrator.
        group.MapGet("/", async (Guid userId, ILibraryService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessLibrary(principal, userId))
            {
                return Results.Forbid();
            }
            return Results.Ok(await service.GetLibraryAsync(userId, ct));
        })
        .WithName("GetUserLibrary")
        .RequireAuthorization()
        .Produces<List<LibraryItemResponse>>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Example library", "The demo user's tracked source materials with per-unit progress.", ExampleLibrary),
            (StatusCodes.Status403Forbidden, "Not your library", "Only the owner or an administrator can view a library.", ExampleValues.Forbidden("The caller does not own this library.")));

        // Adds a source material to a user's library; the caller must be the user themselves or an administrator.
        group.MapPost("/", async (Guid userId, AddLibraryItemRequest request, ILibraryService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessLibrary(principal, userId))
            {
                return Results.Forbid();
            }
            var created = await service.AddAsync(userId, request.SourceMaterialId, ct);
            return created is null
                ? Results.NotFound()
                : Results.Created($"/api/users/{userId}/source-materials/{created.SourceMaterialId}", created);
        })
        .WithName("AddLibraryItem")
        .RequireAuthorization()
        .Produces<LibraryItemResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Valid request", "Tracks a source material in the library.", new AddLibraryItemRequest(new Guid("00000000-0000-0000-0000-000000000018"))),
            ("Malformed body", "The sourceMaterialId is required.", new AddLibraryItemRequest(Guid.Empty)))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Item added", "The tracked item as stored.", new LibraryItemResponse(
                new Guid("00000000-0000-0000-0000-000000000018"),
                "The High Republic: Light of the Jedi",
                Medium.Book,
                CanonType.Canon,
                TrackingStatus.WishListed,
                false,
                [])),
            (StatusCodes.Status404NotFound, "Unknown source material", "No source material has the requested identifier.", ExampleValues.NotFound("No source material with the requested identifier was found.")),
            (StatusCodes.Status400BadRequest, "Malformed body", "The request body is missing or malformed.", ExampleValues.BadRequest("The request body must contain a sourceMaterialId.")),
            (StatusCodes.Status403Forbidden, "Not your library", "Only the owner or an administrator can modify a library.", ExampleValues.Forbidden("The caller does not own this library.")));

        // Reorders a user's library; the caller must be the user themselves or an administrator.
        group.MapPut("/reorder", async (Guid userId, ReorderLibraryItemsRequest request, ILibraryService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessLibrary(principal, userId))
            {
                return Results.Forbid();
            }
            return Results.Ok(await service.ReorderAsync(userId, request.OrderedSourceMaterialIds, ct));
        })
        .WithName("ReorderLibraryItems")
        .RequireAuthorization()
        .Produces<List<LibraryItemResponse>>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "The complete desired order; every library item appears exactly once.", new ReorderLibraryItemsRequest(new Guid[]
            {
                new("00000000-0000-0000-0000-000000000018"),
                new("00000000-0000-0000-0000-000000000010"),
                new("00000000-0000-0000-0000-000000000001")
            })),
            ("Incomplete order", "Rejected when the order does not contain exactly the user's items.", new ReorderLibraryItemsRequest(new Guid[]
            {
                new("00000000-0000-0000-0000-000000000018")
            })))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Library reordered", "The library in its new order.", ExampleLibrary),
            (StatusCodes.Status400BadRequest, "Incomplete order", "The order must contain every library item exactly once.", ExampleValues.BadRequest("The ordered source material identifiers must contain every library item exactly once.")),
            (StatusCodes.Status403Forbidden, "Not your library", "Only the owner or an administrator can modify a library.", ExampleValues.Forbidden("The caller does not own this library.")));

        // Updates a library item's status and favorite flag; the caller must be the user themselves or an administrator.
        group.MapPut("/{sourceMaterialId:guid}", async (Guid userId, Guid sourceMaterialId, UpdateLibraryItemRequest request, ILibraryService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessLibrary(principal, userId))
            {
                return Results.Forbid();
            }
            var updated = await service.UpdateAsync(userId, sourceMaterialId, request, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateLibraryItem")
        .RequireAuthorization()
        .Produces<LibraryItemResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Mark favorite", "Leaves the status unchanged and toggles the favorite flag.", new UpdateLibraryItemRequest(null, true)),
            ("Invalid status", "Rejected when the source material has sub-units and its status is derived from unit progress.", new UpdateLibraryItemRequest(TrackingStatus.Completed, null)))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Item updated", "The library item after the update.", new LibraryItemResponse(
                new Guid("00000000-0000-0000-0000-000000000010"),
                "Star Wars: The Clone Wars",
                Medium.AnimatedShow,
                CanonType.Canon,
                TrackingStatus.InProgress,
                true,
                new List<LibraryUnitResponse>
                {
                    new(new Guid("00000000-0000-0000-0000-500000000001"), UnitType.Episode, 1, 1, null, true),
                    new(new Guid("00000000-0000-0000-0000-500000000002"), UnitType.Episode, 1, 2, null, true),
                    new(new Guid("00000000-0000-0000-0000-500000000003"), UnitType.Episode, 1, 3, null, true)
                })),
            (StatusCodes.Status400BadRequest, "Invalid status", "The status cannot be set on a source material that has sub-units.", ExampleValues.BadRequest("The status is derived from unit progress and cannot be set directly.")),
            (StatusCodes.Status403Forbidden, "Not your library", "Only the owner or an administrator can modify a library.", ExampleValues.Forbidden("The caller does not own this library.")),
            (StatusCodes.Status404NotFound, "Item not found", "The source material is not tracked in this library.", ExampleValues.NotFound("The source material is not tracked in this library.")));

        // Sets the user's progress on a unit of a tracked source material; the caller must be the user themselves or an administrator.
        group.MapPut("/{sourceMaterialId:guid}/units/{unitId:guid}", async (Guid userId, Guid sourceMaterialId, Guid unitId, UpdateUnitProgressRequest request, ILibraryService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessLibrary(principal, userId))
            {
                return Results.Forbid();
            }
            var updated = await service.SetUnitProgressAsync(userId, sourceMaterialId, unitId, request.IsCompleted, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateUnitProgress")
        .RequireAuthorization()
        .Produces<LibraryUnitResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Mark complete", "Sets the unit as completed.", new UpdateUnitProgressRequest(true)),
            ("Mark incomplete", "Resets the unit progress.", new UpdateUnitProgressRequest(false)))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Progress updated", "The unit with the updated progress flag.", new LibraryUnitResponse(new Guid("00000000-0000-0000-0000-500000000004"), UnitType.Episode, 1, 4, null, true)),
            (StatusCodes.Status400BadRequest, "Malformed body", "The request body is missing or malformed.", ExampleValues.BadRequest("The request body must contain an isCompleted flag.")),
            (StatusCodes.Status404NotFound, "Not found", "The source material is not tracked, or the unit does not belong to it.", ExampleValues.NotFound("The unit is not part of a tracked source material.")),
            (StatusCodes.Status403Forbidden, "Not your library", "Only the owner or an administrator can modify a library.", ExampleValues.Forbidden("The caller does not own this library.")));

        // Removes a source material from a user's library; the caller must be the user themselves or an administrator.
        group.MapDelete("/{sourceMaterialId:guid}", async (Guid userId, Guid sourceMaterialId, ILibraryService service, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            if (!CanAccessLibrary(principal, userId))
            {
                return Results.Forbid();
            }
            var deleted = await service.RemoveAsync(userId, sourceMaterialId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteLibraryItem")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not your library", "Only the owner or an administrator can modify a library.", ExampleValues.Forbidden("The caller does not own this library.")),
            (StatusCodes.Status404NotFound, "Item not found", "The source material is not tracked in this library.", ExampleValues.NotFound("The source material is not tracked in this library.")));

        return group;
    }

    /// <summary>
    /// Determines whether the caller may access the library of the given user.
    /// </summary>
    /// <param name="principal">The caller's claims principal.</param>
    /// <param name="userId">The identifier of the user whose library is being accessed.</param>
    /// <returns><c>true</c> when the caller is the user themselves or an administrator; otherwise <c>false</c>.</returns>
    private static bool CanAccessLibrary(ClaimsPrincipal principal, Guid userId) =>
        principal.IsInRole(UserRole.Admin.ToString()) ||
        principal.FindFirstValue(ClaimTypes.NameIdentifier) == userId.ToString();

    /// <summary>An example library mirroring the demo user's seeded tracking progress.</summary>
    private static readonly List<LibraryItemResponse> ExampleLibrary =
    [
        new(
            new Guid("00000000-0000-0000-0000-000000000010"),
            "Star Wars: The Clone Wars",
            Medium.AnimatedShow,
            CanonType.Canon,
            TrackingStatus.InProgress,
            false,
            new List<LibraryUnitResponse>
            {
                new(new Guid("00000000-0000-0000-0000-500000000001"), UnitType.Episode, 1, 1, null, true),
                new(new Guid("00000000-0000-0000-0000-500000000002"), UnitType.Episode, 1, 2, null, true),
                new(new Guid("00000000-0000-0000-0000-500000000003"), UnitType.Episode, 1, 3, null, true),
                new(new Guid("00000000-0000-0000-0000-500000000004"), UnitType.Episode, 1, 4, null, false),
                new(new Guid("00000000-0000-0000-0000-500000000005"), UnitType.Episode, 1, 5, null, false)
            }),
        new(
            new Guid("00000000-0000-0000-0000-000000000001"),
            "Star Wars: Episode I - The Phantom Menace",
            Medium.Movie,
            CanonType.CanonAndLegends,
            TrackingStatus.Completed,
            true,
            []),
        new(
            new Guid("00000000-0000-0000-0000-000000000016"),
            "Darth Bane: Path of Destruction",
            Medium.Book,
            CanonType.Legends,
            TrackingStatus.Completed,
            true,
            [])
    ];
}
