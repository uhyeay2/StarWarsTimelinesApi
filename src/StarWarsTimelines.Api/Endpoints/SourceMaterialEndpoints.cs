using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for the source material catalog.
/// </summary>
public static class SourceMaterialEndpoints
{
    /// <summary>
    /// Registers the catalog endpoints under <c>api/source-materials</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapSourceMaterialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/source-materials").WithTags("SourceMaterials");

        // Gets all source materials; anonymous access is allowed.
        group.MapGet("/", async (ISourceMaterialService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetAllSourceMaterials")
            .Produces<List<SourceMaterialResponse>>(StatusCodes.Status200OK, "application/json")
            .WithResponseExamples(
                (StatusCodes.Status200OK, "Example catalog", "A sample of the source material catalog.", new List<SourceMaterialResponse>
                {
                    new(new Guid("00000000-0000-0000-0000-000000000010"), "Star Wars: The Clone Wars", Medium.AnimatedShow, CanonType.Canon),
                    new(new Guid("00000000-0000-0000-0000-000000000022"), "Star Wars Jedi: Fallen Order", Medium.VideoGame, CanonType.Canon),
                    new(new Guid("00000000-0000-0000-0000-000000000018"), "The High Republic: Light of the Jedi", Medium.Book, CanonType.Canon)
                }));

        // Gets a single source material by id; anonymous access is allowed.
        group.MapGet("/{id:guid}", async (Guid id, ISourceMaterialService service, CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetSourceMaterialById")
        .Produces<SourceMaterialResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Source material found", "A single source material.", new SourceMaterialResponse(new Guid("00000000-0000-0000-0000-000000000010"), "Star Wars: The Clone Wars", Medium.AnimatedShow, CanonType.Canon)),
            (StatusCodes.Status404NotFound, "Source material not found", "No source material has the requested identifier.", ExampleValues.NotFound("No source material with the requested identifier was found.")));

        // Creates a catalog entry; restricted to administrators.
        group.MapPost("/", async (CreateSourceMaterialRequest request, ISourceMaterialService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            await broadcaster.BroadcastAsync(new CatalogEvent("source-materials", "created", created.Id));
            return Results.Created($"/api/source-materials/{created.Id}", created);
        })
        .WithName("CreateSourceMaterial")
        .RequireAuthorization("AdminOnly")
        .Produces<SourceMaterialResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body.", new CreateSourceMaterialRequest("The Mandalorian", Medium.LiveActionShow, CanonType.Canon)),
            ("Blank title", "Rejected when the title is null or white space.", new CreateSourceMaterialRequest("", null, null)))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Source material created", "The source material as created.", new SourceMaterialResponse(new Guid("00000000-0000-0000-0000-000000000010"), "Star Wars: The Clone Wars", Medium.AnimatedShow, CanonType.Canon)),
            (StatusCodes.Status400BadRequest, "Blank title", "The title is required.", ExampleValues.BadRequest("Title must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")));

        // Partially updates a catalog entry; restricted to administrators.
        group.MapPut("/{id:guid}", async (Guid id, UpdateSourceMaterialRequest request, ISourceMaterialService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            if (updated is not null)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("source-materials", "updated", id));
            }
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateSourceMaterial")
        .RequireAuthorization("AdminOnly")
        .Produces<SourceMaterialResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Retitle", "Updates only the title, leaving the medium and continuity unchanged.", new UpdateSourceMaterialRequest("The Mandalorian (Season One)", null, null)),
            ("Blank title", "Rejected when the title is set to null or white space.", new UpdateSourceMaterialRequest("", null, null)))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Source material updated", "The source material after the update.", new SourceMaterialResponse(new Guid("00000000-0000-0000-0000-000000000010"), "The Mandalorian (Season One)", Medium.AnimatedShow, CanonType.Canon)),
            (StatusCodes.Status400BadRequest, "Blank title", "The title is required.", ExampleValues.BadRequest("Title must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Source material not found", "No source material has the requested identifier.", ExampleValues.NotFound("No source material with the requested identifier was found.")));

        // Deletes a catalog entry; restricted to administrators.
        group.MapDelete("/{id:guid}", async (Guid id, ISourceMaterialService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            if (deleted)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("source-materials", "deleted", id));
            }
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSourceMaterial")
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Source material not found", "No source material has the requested identifier.", ExampleValues.NotFound("No source material with the requested identifier was found.")));

        return group;
    }
}
