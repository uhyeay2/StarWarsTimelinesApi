using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for the location catalog.
/// </summary>
public static class LocationEndpoints
{
    /// <summary>
    /// Registers the location endpoints under <c>api/locations</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/locations").WithTags("Locations");

        // Gets all locations; anonymous access is allowed.
        group.MapGet("/", async (ILocationService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetAllLocations")
            .Produces<List<LocationResponse>>(StatusCodes.Status200OK, "application/json")
            .WithResponseExamples(
                (StatusCodes.Status200OK, "Example catalog", "A sample of the location catalog.", new List<LocationResponse>
                {
                    new(new Guid("00000000-0000-0000-0000-200000000005"), "Naboo"),
                    new(new Guid("00000000-0000-0000-0000-200000000003"), "Coruscant"),
                    new(new Guid("00000000-0000-0000-0000-200000000001"), "Tython")
                }));

        // Gets a single location by id; anonymous access is allowed.
        group.MapGet("/{id:guid}", async (Guid id, ILocationService service, CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetLocationById")
        .Produces<LocationResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Location found", "A single location.", new LocationResponse(new Guid("00000000-0000-0000-0000-200000000005"), "Naboo")),
            (StatusCodes.Status404NotFound, "Location not found", "No location has the requested identifier.", ExampleValues.NotFound("No location with the requested identifier was found.")));

        // Creates a catalog entry; restricted to administrators.
        group.MapPost("/", async (CreateLocationRequest request, ILocationService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            return Results.Created($"/api/locations/{created.Id}", created);
        })
        .WithName("CreateLocation")
        .RequireAuthorization("AdminOnly")
        .Produces<LocationResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body.", new CreateLocationRequest("Lothal")),
            ("Blank name", "Rejected when the name is null or white space.", new CreateLocationRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Location created", "The location as created.", new LocationResponse(new Guid("00000000-0000-0000-0000-200000000005"), "Naboo")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")));

        // Partially updates a catalog entry; restricted to administrators.
        group.MapPut("/{id:guid}", async (Guid id, UpdateLocationRequest request, ILocationService service, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateLocation")
        .RequireAuthorization("AdminOnly")
        .Produces<LocationResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Rename", "Updates the location's name.", new UpdateLocationRequest("Naboo (royal capital)")),
            ("Blank name", "Rejected when the name is set to null or white space.", new UpdateLocationRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Location updated", "The location after the update.", new LocationResponse(new Guid("00000000-0000-0000-0000-200000000005"), "Naboo (royal capital)")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Location not found", "No location has the requested identifier.", ExampleValues.NotFound("No location with the requested identifier was found.")));

        // Deletes a catalog entry; restricted to administrators.
        group.MapDelete("/{id:guid}", async (Guid id, ILocationService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteLocation")
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Location not found", "No location has the requested identifier.", ExampleValues.NotFound("No location with the requested identifier was found.")));

        return group;
    }
}
