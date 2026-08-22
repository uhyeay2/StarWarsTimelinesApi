using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for the timeline event catalog.
/// </summary>
public static class SourceMaterialEventEndpoints
{
    /// <summary>
    /// Registers the event endpoints under <c>api/source-material-events</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapSourceMaterialEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/source-material-events").WithTags("SourceMaterialEvents");

        // Gets all timeline events; anonymous access is allowed.
        group.MapGet("/", async (ISourceMaterialEventService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetAllSourceMaterialEvents")
            .Produces<List<SourceMaterialEventResponse>>(StatusCodes.Status200OK, "application/json")
            .WithResponseExamples(
                (StatusCodes.Status200OK, "Example timeline", "A sample of the timeline events.", new List<SourceMaterialEventResponse>
                {
                    EventExample,
                    new(
                        new Guid("00000000-0000-0000-0000-400000000009"),
                        "The Battle of Yavin",
                        "Rebel pilots, led by Luke Skywalker, launch a desperate trench run against the Death Star.",
                        CanonType.CanonAndLegends,
                        0,
                        "0 BBY",
                        null,
                        new SourceMaterialResponse(new Guid("00000000-0000-0000-0000-000000000004"), "Star Wars: Episode IV - A New Hope", Medium.Movie, CanonType.CanonAndLegends),
                        null,
                        new List<CharacterResponse>
                        {
                            new(new Guid("00000000-0000-0000-0000-100000000022"), "Luke Skywalker", new Guid("00000000-0000-0000-0000-200000000044"), "Polis Massa", -19, -19, 34, 34, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
                            new(new Guid("00000000-0000-0000-0000-100000000023"), "Han Solo", new Guid("00000000-0000-0000-0000-200000000046"), "Corellia", -29, -29, 35, 35, new Guid("00000000-0000-0000-0000-600000000001"), "Human")
                        },
                        new List<LocationResponse> { new(new Guid("00000000-0000-0000-0000-200000000019"), "Yavin 4") },
                        new List<VehicleResponse>
                        {
                            new(new Guid("00000000-0000-0000-0000-300000000014"), "Millennium Falcon"),
                            new(new Guid("00000000-0000-0000-0000-300000000015"), "T-65 X-wing starfighter")
                        })
                }));

        // Gets a single timeline event by id; anonymous access is allowed.
        group.MapGet("/{id:guid}", async (Guid id, ISourceMaterialEventService service, CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetSourceMaterialEventById")
        .Produces<SourceMaterialEventResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Event found", "A single timeline event.", EventExample),
            (StatusCodes.Status404NotFound, "Event not found", "No event has the requested identifier.", ExampleValues.NotFound("No event with the requested identifier was found.")));

        // Creates a timeline event; restricted to administrators.
        group.MapPost("/", async (CreateSourceMaterialEventRequest request, ISourceMaterialEventService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            return Results.Created($"/api/source-material-events/{created.Id}", created);
        })
        .WithName("CreateSourceMaterialEvent")
        .RequireAuthorization("AdminOnly")
        .Produces<SourceMaterialEventResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body referencing existing catalog entries.", new CreateSourceMaterialEventRequest(
                "The Invasion of Naboo",
                "The Trade Federation blockades and invades Naboo.",
                CanonType.CanonAndLegends,
                -32,
                "32 BBY",
                null,
                new Guid("00000000-0000-0000-0000-000000000001"),
                null,
                new Guid[] { new("00000000-0000-0000-0000-100000000008"), new("00000000-0000-0000-0000-100000000009") },
                new Guid[] { new("00000000-0000-0000-0000-200000000005") },
                new Guid[] { new("00000000-0000-0000-0000-300000000004"), new("00000000-0000-0000-0000-300000000005") })),
            ("Blank title", "Rejected when the title is null or white space.", new CreateSourceMaterialEventRequest(
                "",
                "A description.",
                CanonType.Canon,
                0,
                "0 BBY",
                null,
                new Guid("00000000-0000-0000-0000-000000000001"),
                null,
                [],
                [],
                [])))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Event created", "The event as created.", EventExample),
            (StatusCodes.Status400BadRequest, "Blank title", "The title is required.", ExampleValues.BadRequest("Title must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")));

        // Partially updates a timeline event; restricted to administrators.
        group.MapPut("/{id:guid}", async (Guid id, UpdateSourceMaterialEventRequest request, ISourceMaterialEventService service, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateSourceMaterialEvent")
        .RequireAuthorization("AdminOnly")
        .Produces<SourceMaterialEventResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Adjust year", "Updates only the year, leaving the other fields unchanged.", new UpdateSourceMaterialEventRequest(null, null, null, -30, null, null, null, null, null, null, null)),
            ("Blank title", "Rejected when the title is set to null or white space.", new UpdateSourceMaterialEventRequest("", null, null, null, null, null, null, null, null, null, null)))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Event updated", "The event after the update.", UpdatedEventExample),
            (StatusCodes.Status400BadRequest, "Blank title", "The title is required.", ExampleValues.BadRequest("Title must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Event not found", "No event has the requested identifier.", ExampleValues.NotFound("No event with the requested identifier was found.")));

        // Deletes a timeline event; restricted to administrators.
        group.MapDelete("/{id:guid}", async (Guid id, ISourceMaterialEventService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSourceMaterialEvent")
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Event not found", "No event has the requested identifier.", ExampleValues.NotFound("No event with the requested identifier was found.")));

        return group;
    }

    /// <summary>An example event drawn from the seeded "Invasion of Naboo" timeline entry.</summary>
    private static readonly SourceMaterialEventResponse EventExample = new(
        new Guid("00000000-0000-0000-0000-400000000004"),
        "The Invasion of Naboo",
        "The Trade Federation blockades and invades Naboo, setting the stage for the return of the Sith and the rise of Anakin Skywalker.",
        CanonType.CanonAndLegends,
        -32,
        "32 BBY",
        null,
        new SourceMaterialResponse(new Guid("00000000-0000-0000-0000-000000000001"), "Star Wars: Episode I - The Phantom Menace", Medium.Movie, CanonType.CanonAndLegends),
        null,
        new List<CharacterResponse>
        {
            new(new Guid("00000000-0000-0000-0000-100000000008"), "Qui-Gon Jinn", null, null, -80, -80, -32, -32, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
            new(new Guid("00000000-0000-0000-0000-100000000009"), "Obi-Wan Kenobi", new Guid("00000000-0000-0000-0000-200000000048"), "Stewjon", -57, -57, 0, 0, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
            new(new Guid("00000000-0000-0000-0000-100000000010"), "Padme Amidala", new Guid("00000000-0000-0000-0000-200000000005"), "Naboo", -46, -46, -19, -19, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
            new(new Guid("00000000-0000-0000-0000-100000000011"), "Darth Maul", new Guid("00000000-0000-0000-0000-200000000039"), "Dathomir", -54, -54, -2, -2, new Guid("00000000-0000-0000-0000-600000000004"), "Zabrak")
        },
        new List<LocationResponse>
        {
            new(new Guid("00000000-0000-0000-0000-200000000005"), "Naboo"),
            new(new Guid("00000000-0000-0000-0000-200000000007"), "Theed")
        },
        new List<VehicleResponse>
        {
            new(new Guid("00000000-0000-0000-0000-300000000004"), "Radiant VII"),
            new(new Guid("00000000-0000-0000-0000-300000000005"), "Sith Infiltrator"),
            new(new Guid("00000000-0000-0000-0000-300000000006"), "Naboo N-1 starfighter")
        });

    /// <summary>An example event used for the "year adjusted" update response.</summary>
    private static readonly SourceMaterialEventResponse UpdatedEventExample = new(
        new Guid("00000000-0000-0000-0000-400000000004"),
        "The Invasion of Naboo",
        "The Trade Federation blockades and invades Naboo, setting the stage for the return of the Sith and the rise of Anakin Skywalker.",
        CanonType.CanonAndLegends,
        -30,
        "30 BBY",
        null,
        new SourceMaterialResponse(new Guid("00000000-0000-0000-0000-000000000001"), "Star Wars: Episode I - The Phantom Menace", Medium.Movie, CanonType.CanonAndLegends),
        null,
        new List<CharacterResponse>
        {
            new(new Guid("00000000-0000-0000-0000-100000000008"), "Qui-Gon Jinn", null, null, -80, -80, -32, -32, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
            new(new Guid("00000000-0000-0000-0000-100000000009"), "Obi-Wan Kenobi", new Guid("00000000-0000-0000-0000-200000000048"), "Stewjon", -57, -57, 0, 0, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
            new(new Guid("00000000-0000-0000-0000-100000000010"), "Padme Amidala", new Guid("00000000-0000-0000-0000-200000000005"), "Naboo", -46, -46, -19, -19, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
            new(new Guid("00000000-0000-0000-0000-100000000011"), "Darth Maul", new Guid("00000000-0000-0000-0000-200000000039"), "Dathomir", -54, -54, -2, -2, new Guid("00000000-0000-0000-0000-600000000004"), "Zabrak")
        },
        new List<LocationResponse>
        {
            new(new Guid("00000000-0000-0000-0000-200000000005"), "Naboo"),
            new(new Guid("00000000-0000-0000-0000-200000000007"), "Theed")
        },
        new List<VehicleResponse>
        {
            new(new Guid("00000000-0000-0000-0000-300000000004"), "Radiant VII"),
            new(new Guid("00000000-0000-0000-0000-300000000005"), "Sith Infiltrator"),
            new(new Guid("00000000-0000-0000-0000-300000000006"), "Naboo N-1 starfighter")
        });
}
