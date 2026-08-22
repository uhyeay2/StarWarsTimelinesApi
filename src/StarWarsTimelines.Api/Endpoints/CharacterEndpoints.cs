using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the minimal API endpoints for the character catalog.
/// </summary>
public static class CharacterEndpoints
{
    /// <summary>
    /// Registers the character endpoints under <c>api/characters</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/characters").WithTags("Characters");

        // Gets all characters; anonymous access is allowed.
        group.MapGet("/", async (ICharacterService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetAllCharacters")
            .Produces<List<CharacterResponse>>(StatusCodes.Status200OK, "application/json")
            .WithResponseExamples(
                (StatusCodes.Status200OK, "Example catalog", "A sample of the character catalog.", new List<CharacterResponse>
                {
                    new(new Guid("00000000-0000-0000-0000-100000000015"), "Ahsoka Tano", null, null, -36, -36, null, null, new Guid("00000000-0000-0000-0000-600000000003"), "Togruta"),
                    new(new Guid("00000000-0000-0000-0000-100000000010"), "Padme Amidala", new Guid("00000000-0000-0000-0000-200000000005"), "Naboo", -46, -46, -19, -19, new Guid("00000000-0000-0000-0000-600000000001"), "Human"),
                    new(new Guid("00000000-0000-0000-0000-100000000012"), "Yoda", null, null, -900, -890, 4, 4, null, null)
                }));

        // Gets a single character by id; anonymous access is allowed.
        group.MapGet("/{id:guid}", async (Guid id, ICharacterService service, CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetCharacterById")
        .Produces<CharacterResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Character found", "A single character with biographical attributes.", new CharacterResponse(new Guid("00000000-0000-0000-0000-100000000015"), "Ahsoka Tano", null, null, -36, -36, null, null, new Guid("00000000-0000-0000-0000-600000000003"), "Togruta")),
            (StatusCodes.Status404NotFound, "Character not found", "No character has the requested identifier.", ExampleValues.NotFound("No character with the requested identifier was found.")));

        // Creates a catalog entry; restricted to administrators.
        group.MapPost("/", async (CreateCharacterRequest request, ICharacterService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            await broadcaster.BroadcastAsync(new CatalogEvent("characters", "created", created.Id));
            return Results.Created($"/api/characters/{created.Id}", created);
        })
        .WithName("CreateCharacter")
        .RequireAuthorization("AdminOnly")
        .Produces<CharacterResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body with biographical attributes.", new CreateCharacterRequest("Padme Amidala", new Guid("00000000-0000-0000-0000-200000000005"), -46, -46, -19, -19, new Guid("00000000-0000-0000-0000-600000000001"))),
            ("Estimated birth year range", "Palpatine's birth year is only known as a range of 84-88 BBY and his death as 4-35 ABY; negative values are BBY.", new CreateCharacterRequest("Emperor Palpatine", new Guid("00000000-0000-0000-0000-200000000005"), -88, -84, 4, 35, new Guid("00000000-0000-0000-0000-600000000001"))),
            ("Names-only request", "All biographical attributes are optional and default to unknown.", new CreateCharacterRequest("Revan")),
            ("Blank name", "Rejected when the name is null or white space.", new CreateCharacterRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Character created", "The character as created.", new CharacterResponse(new Guid("00000000-0000-0000-0000-100000000010"), "Padme Amidala", new Guid("00000000-0000-0000-0000-200000000005"), "Naboo", -46, -46, -19, -19, new Guid("00000000-0000-0000-0000-600000000001"), "Human")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")));

        // Partially updates a catalog entry; restricted to administrators.
        group.MapPut("/{id:guid}", async (Guid id, UpdateCharacterRequest request, ICharacterService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            if (updated is not null)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("characters", "updated", id));
            }
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithName("UpdateCharacter")
        .RequireAuthorization("AdminOnly")
        .Produces<CharacterResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithRequestExamples(
            ("Replace all details", "Replaces every attribute of the character; the name is required on each call.", new UpdateCharacterRequest("Snips", new Guid("00000000-0000-0000-0000-200000000005"), -36, -36, null, null, new Guid("00000000-0000-0000-0000-600000000003"))),
            ("Clear optional attributes", "Null values clear biography fields back to unknown.", new UpdateCharacterRequest("Snips", null, null, null, null, null, null)),
            ("Incomplete year range", "Rejected because one side of the range is missing.", new UpdateCharacterRequest("Snips", null, -36, null, null, null, null)),
            ("Blank name", "Rejected when the name is missing or white space.", new UpdateCharacterRequest("", null, null, null, null, null, null)))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Character updated", "The character after the update.", new CharacterResponse(new Guid("00000000-0000-0000-0000-100000000015"), "Snips", null, null, -36, -36, null, null, new Guid("00000000-0000-0000-0000-600000000003"), "Togruta")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Character not found", "No character has the requested identifier.", ExampleValues.NotFound("No character with the requested identifier was found.")));

        // Deletes a catalog entry; restricted to administrators.
        group.MapDelete("/{id:guid}", async (Guid id, ICharacterService service, CatalogEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            if (deleted)
            {
                await broadcaster.BroadcastAsync(new CatalogEvent("characters", "deleted", id));
            }
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteCharacter")
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithResponseExamples(
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Character not found", "No character has the requested identifier.", ExampleValues.NotFound("No character with the requested identifier was found.")));

        return group;
    }
}
