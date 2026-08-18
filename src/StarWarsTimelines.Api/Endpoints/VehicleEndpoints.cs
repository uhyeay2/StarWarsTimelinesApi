using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for the vehicle catalog.
/// </summary>
public static class VehicleEndpoints
{
    /// <summary>
    /// Registers the vehicle endpoints under <c>api/vehicles</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/vehicles").WithTags("Vehicles");

        // Gets all vehicles; anonymous access is allowed.
        group.MapGet("/", async (IVehicleService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetAllVehicles")
            .Produces<List<VehicleResponse>>(StatusCodes.Status200OK, "application/json")
            .WithResponseExamples(
                (StatusCodes.Status200OK, "Example catalog", "A sample of the vehicle catalog.", new List<VehicleResponse>
                {
                    new(new Guid("00000000-0000-0000-0000-300000000014"), "Millennium Falcon"),
                    new(new Guid("00000000-0000-0000-0000-300000000001"), "Ebon Hawk"),
                    new(new Guid("00000000-0000-0000-0000-300000000012"), "Death Star")
                }));

        // Gets a single vehicle by id; anonymous access is allowed.
        group.MapGet("/{id:guid}", async (Guid id, IVehicleService service, CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetVehicleById")
        .Produces<VehicleResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Vehicle found", "A single vehicle.", new VehicleResponse(new Guid("00000000-0000-0000-0000-300000000014"), "Millennium Falcon")),
            (StatusCodes.Status404NotFound, "Vehicle not found", "No vehicle has the requested identifier.", ExampleValues.NotFound("No vehicle with the requested identifier was found.")));

        // Creates a catalog entry; restricted to administrators.
        group.MapPost("/", async (CreateVehicleRequest request, IVehicleService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            await broadcaster.BroadcastAsync(new CatalogEvent("vehicles", "created", created.Id));
            return Results.Created($"/api/vehicles/{created.Id}", created);
        })
        .WithName("CreateVehicle")
        .RequireAuthorization("AdminOnly")
        .Produces<VehicleResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body.", new CreateVehicleRequest("Ghost")),
            ("Blank name", "Rejected when the name is null or white space.", new CreateVehicleRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Vehicle created", "The vehicle as created.", new VehicleResponse(new Guid("00000000-0000-0000-0000-300000000014"), "Millennium Falcon")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")));

        // Partially updates a catalog entry; restricted to administrators.
        group.MapPut("/{id:guid}", async (Guid id, UpdateVehicleRequest request, IVehicleService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            if (updated is not null)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("vehicles", "updated", id));
            }
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateVehicle")
        .RequireAuthorization("AdminOnly")
        .Produces<VehicleResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Rename", "Updates the vehicle's name.", new UpdateVehicleRequest("Millennium Falcon (YT-1300)")),
            ("Blank name", "Rejected when the name is set to null or white space.", new UpdateVehicleRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Vehicle updated", "The vehicle after the update.", new VehicleResponse(new Guid("00000000-0000-0000-0000-300000000014"), "Millennium Falcon (YT-1300)")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Vehicle not found", "No vehicle has the requested identifier.", ExampleValues.NotFound("No vehicle with the requested identifier was found.")));

        // Deletes a catalog entry; restricted to administrators.
        group.MapDelete("/{id:guid}", async (Guid id, IVehicleService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            if (deleted)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("vehicles", "deleted", id));
            }
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteVehicle")
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Vehicle not found", "No vehicle has the requested identifier.", ExampleValues.NotFound("No vehicle with the requested identifier was found.")));

        return group;
    }
}
