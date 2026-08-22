using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for the species catalog.
/// </summary>
public static class SpeciesEndpoints
{
    /// <summary>
    /// Registers the species endpoints under <c>api/species</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapSpeciesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/species").WithTags("Species");

        // Gets all species; anonymous access is allowed.
        group.MapGet("/", async (ISpeciesService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetAllSpecies")
            .Produces<List<SpeciesResponse>>(StatusCodes.Status200OK, "application/json")
            .WithResponseExamples(
                (StatusCodes.Status200OK, "Example catalog", "A sample of the species catalog.", new List<SpeciesResponse>
                {
                    new(new Guid("00000000-0000-0000-0000-600000000001"), "Human", new Guid("00000000-0000-0000-0000-200000000003"), "Coruscant"),
                    new(new Guid("00000000-0000-0000-0000-600000000005"), "Wookiee", new Guid("00000000-0000-0000-0000-200000000015"), "Kashyyyk"),
                    new(new Guid("00000000-0000-0000-0000-600000000007"), "Yoda's species", null, null)
                }));

        // Gets a single species by id; anonymous access is allowed.
        group.MapGet("/{id:guid}", async (Guid id, ISpeciesService service, CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetSpeciesById")
        .Produces<SpeciesResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Species found", "A single species.", new SpeciesResponse(new Guid("00000000-0000-0000-0000-600000000002"), "Twi'lek", new Guid("00000000-0000-0000-0000-200000000038"), "Ryloth")),
            (StatusCodes.Status404NotFound, "Species not found", "No species has the requested identifier.", ExampleValues.NotFound("No species with the requested identifier was found.")));

        // Creates a catalog entry; restricted to administrators.
        group.MapPost("/", async (CreateSpeciesRequest request, ISpeciesService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            await broadcaster.BroadcastAsync(new CatalogEvent("species", "created", created.Id));
            return Results.Created($"/api/species/{created.Id}", created);
        })
        .WithName("CreateSpecies")
        .RequireAuthorization("AdminOnly")
        .Produces<SpeciesResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body.", new CreateSpeciesRequest("Zabrak", new Guid("00000000-0000-0000-0000-200000000041"))),
            ("Unknown home planet", "The home planet may be omitted when it is not known.", new CreateSpeciesRequest("Yoda's species")),
            ("Blank name", "Rejected when the name is null or white space.", new CreateSpeciesRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Species created", "The species as created.", new SpeciesResponse(new Guid("00000000-0000-0000-0000-600000000004"), "Zabrak", new Guid("00000000-0000-0000-0000-200000000041"), "Iridonia")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")));

        // Partially updates a catalog entry; restricted to administrators.
        group.MapPut("/{id:guid}", async (Guid id, UpdateSpeciesRequest request, ISpeciesService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            if (updated is not null)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("species", "updated", id));
            }
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateSpecies")
        .RequireAuthorization("AdminOnly")
        .Produces<SpeciesResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Set home planet", "Replaces the species' data; the name is required on each call.", new UpdateSpeciesRequest("Iridonian Zabrak", new Guid("00000000-0000-0000-0000-200000000041"))),
            ("Clear home planet", "A null clears the home planet back to unknown.", new UpdateSpeciesRequest("Iridonian Zabrak", null)),
            ("Blank name", "Rejected when the name is missing or white space.", new UpdateSpeciesRequest("", null)))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Species updated", "The species after the update.", new SpeciesResponse(new Guid("00000000-0000-0000-0000-600000000004"), "Iridonian Zabrak", new Guid("00000000-0000-0000-0000-200000000041"), "Iridonia")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Species not found", "No species has the requested identifier.", ExampleValues.NotFound("No species with the requested identifier was found.")));

        // Deletes a catalog entry; restricted to administrators. Characters referencing the species keep their
        // identity but their species reference is cleared back to unknown.
        group.MapDelete("/{id:guid}", async (Guid id, ISpeciesService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            if (deleted)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("species", "deleted", id));
            }
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSpecies")
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Species not found", "No species has the requested identifier.", ExampleValues.NotFound("No species with the requested identifier was found.")));

        return group;
    }
}
