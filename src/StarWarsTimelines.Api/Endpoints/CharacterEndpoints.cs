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
                    new(new Guid("00000000-0000-0000-0000-100000000015"), "Ahsoka Tano"),
                    new(new Guid("00000000-0000-0000-0000-100000000003"), "Revan"),
                    new(new Guid("00000000-0000-0000-0000-100000000010"), "Padme Amidala")
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
            (StatusCodes.Status200OK, "Character found", "A single character.", new CharacterResponse(new Guid("00000000-0000-0000-0000-100000000015"), "Ahsoka Tano")),
            (StatusCodes.Status404NotFound, "Character not found", "No character has the requested identifier.", ExampleValues.NotFound("No character with the requested identifier was found.")));

        // Creates a catalog entry; restricted to administrators.
        group.MapPost("/", async (CreateCharacterRequest request, ICharacterService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            return Results.Created($"/api/characters/{created.Id}", created);
        })
        .WithName("CreateCharacter")
        .RequireAuthorization("AdminOnly")
        .Produces<CharacterResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithRequestExamples(
            ("Valid request", "A well-formed request body.", new CreateCharacterRequest("Ahsoka Tano")),
            ("Blank name", "Rejected when the name is null or white space.", new CreateCharacterRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status201Created, "Character created", "The character as created.", new CharacterResponse(new Guid("00000000-0000-0000-0000-100000000015"), "Ahsoka Tano")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")));

        // Partially updates a catalog entry; restricted to administrators.
        group.MapPut("/{id:guid}", async (Guid id, UpdateCharacterRequest request, ICharacterService service, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
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
            ("Rename", "Updates the character's name.", new UpdateCharacterRequest("Snips")),
            ("Blank name", "Rejected when the name is set to null or white space.", new UpdateCharacterRequest("")))
        .WithResponseExamples(
            (StatusCodes.Status200OK, "Character updated", "The character after the update.", new CharacterResponse(new Guid("00000000-0000-0000-0000-100000000015"), "Snips")),
            (StatusCodes.Status400BadRequest, "Blank name", "The name is required.", ExampleValues.BadRequest("Name must not be blank.")),
            (StatusCodes.Status403Forbidden, "Not an administrator", "Only administrators can modify the catalog.", ExampleValues.Forbidden("The caller does not have the Admin role.")),
            (StatusCodes.Status404NotFound, "Character not found", "No character has the requested identifier.", ExampleValues.NotFound("No character with the requested identifier was found.")));

        // Deletes a catalog entry; restricted to administrators.
        group.MapDelete("/{id:guid}", async (Guid id, ICharacterService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
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
