using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for the source material unit catalog.
/// </summary>
public static class SourceMaterialUnitEndpoints
{
    /// <summary>
    /// Registers the unit endpoints under <c>api/source-materials/{sourceMaterialId}/units</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapSourceMaterialUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/source-materials/{sourceMaterialId:guid}/units").WithTags("SourceMaterialUnits");

        // Gets all units of a source material ordered by number; anonymous access is allowed.
        group.MapGet("/", async (Guid sourceMaterialId, ISourceMaterialUnitService service, CancellationToken ct) =>
        {
            var items = await service.GetBySourceMaterialAsync(sourceMaterialId, ct);
            return items is null ? Results.NotFound() : Results.Ok(items);
        })
        .WithName("GetSourceMaterialUnits")
        .Produces<List<SourceMaterialUnitResponse>>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Example units", "The units of the requested source material.", new List<SourceMaterialUnitResponse>
            {
                new(new Guid("00000000-0000-0000-0000-500000000025"), new Guid("00000000-0000-0000-0000-000000000012"), UnitType.Episode, 1, 1, "Chapter 1: The Mandalorian"),
                new(new Guid("00000000-0000-0000-0000-500000000026"), new Guid("00000000-0000-0000-0000-000000000012"), UnitType.Episode, 1, 2, "Chapter 2: The Child"),
                new(new Guid("00000000-0000-0000-0000-500000000027"), new Guid("00000000-0000-0000-0000-000000000012"), UnitType.Episode, 1, 3, "Chapter 3: The Sin")
            }),
            (StatusCodes.Status404NotFound, "Source material not found", "No source material has the requested identifier.", ExampleValues.NotFound("No source material with the requested identifier was found.")));

        // Creates a unit for a source material; restricted to administrators.
        group.MapPost("/", async (Guid sourceMaterialId, CreateSourceMaterialUnitRequest request, ISourceMaterialUnitService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(sourceMaterialId, request, ct);
            if (created is not null)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("source-material-units", "created", created.Id));
            }
            return created is null
                ? Results.NotFound()
                : Results.Created($"/api/source-materials/{sourceMaterialId}/units/{created.Id}", created);
        })
        .WithName("CreateSourceMaterialUnit")
        .RequireAuthorization("AdminOnly")
        .Produces<SourceMaterialUnitResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body.", new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, 9, "Chapter 9: The Marshal")),
            ("Invalid number", "Rejected when the number is not a positive integer.", new CreateSourceMaterialUnitRequest(UnitType.Episode, null, 0, null)))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Unit created", "The unit as created.", new SourceMaterialUnitResponse(new Guid("00000000-0000-0000-0000-500000000025"), new Guid("00000000-0000-0000-0000-000000000012"), UnitType.Episode, 1, 1, "Chapter 1: The Mandalorian")),
            (StatusCodes.Status400BadRequest, "Invalid number", "The unit number must be positive.", ExampleValues.BadRequest("Number must be greater than zero.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Source material not found", "No source material has the requested identifier.", ExampleValues.NotFound("No source material with the requested identifier was found.")));

        // Partially updates a unit; restricted to administrators.
        group.MapPut("/{unitId:guid}", async (Guid unitId, UpdateSourceMaterialUnitRequest request, ISourceMaterialUnitService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(unitId, request, ct);
            if (updated is not null)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("source-material-units", "updated", unitId));
            }
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateSourceMaterialUnit")
        .RequireAuthorization("AdminOnly")
        .Produces<SourceMaterialUnitResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Add title", "Sets a title on a unit that had none.", new UpdateSourceMaterialUnitRequest(null, null, null, "Chapter 1: The Mandalorian")),
            ("Invalid number", "Rejected when the number is not a positive integer.", new UpdateSourceMaterialUnitRequest(null, null, 0, null)))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Unit updated", "The unit after the update.", new SourceMaterialUnitResponse(new Guid("00000000-0000-0000-0000-500000000025"), new Guid("00000000-0000-0000-0000-000000000012"), UnitType.Episode, 1, 1, "Chapter 1: The Mandalorian")),
            (StatusCodes.Status400BadRequest, "Invalid number", "The unit number must be positive.", ExampleValues.BadRequest("Number must be greater than zero.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Unit not found", "No unit has the requested identifier.", ExampleValues.NotFound("No unit with the requested identifier was found.")));

        // Deletes a unit; restricted to administrators.
        group.MapDelete("/{unitId:guid}", async (Guid unitId, ISourceMaterialUnitService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(unitId, ct);
            if (deleted)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("source-material-units", "deleted", unitId));
            }
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSourceMaterialUnit")
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Unit not found", "No unit has the requested identifier.", ExampleValues.NotFound("No unit with the requested identifier was found.")));

        return group;
    }
}
