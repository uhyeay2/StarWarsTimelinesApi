using Microsoft.AspNetCore.Routing;

namespace StarWarsTimelines.Api.OpenApi;

/// <summary>
/// Carries a single request-body example from an endpoint to the Swagger operation filter.
/// </summary>
/// <param name="Name">The name shown for the example in Swagger UI.</param>
/// <param name="Description">The description shown for the example in Swagger UI.</param>
/// <param name="Value">
/// The example value. It is serialized with the same JSON conventions the API uses at runtime so the example matches
/// the real wire format.
/// </param>
public sealed record RequestExampleMetadata(string Name, string Description, object Value);

/// <summary>
/// Carries a single response example for a status code from an endpoint to the Swagger operation filter.
/// </summary>
/// <param name="StatusCode">The HTTP status code the example applies to.</param>
/// <param name="Name">The name shown for the example in Swagger UI.</param>
/// <param name="Description">The description shown for the example in Swagger UI.</param>
/// <param name="Value">
/// The example value. It is serialized with the same JSON conventions the API uses at runtime so the example matches
/// the real wire format.
/// </param>
public sealed record ResponseExampleMetadata(int StatusCode, string Name, string Description, object Value);

/// <summary>
/// Fluent helpers that attach Swagger request and response examples to minimal API endpoints as endpoint metadata,
/// which the <see cref="ExampleOperationFilter"/> later turns into OpenAPI example objects.
/// </summary>
public static class EndpointExampleExtensions
{
    /// <summary>
    /// Attaches named examples for the endpoint's <c>application/json</c> request body.
    /// </summary>
    /// <param name="builder">The endpoint being configured.</param>
    /// <param name="examples">
    /// Tuples of <c>(Name, Description, Value)</c>. A single example is shown when one is supplied; when multiple are
    /// supplied Swagger UI offers them as a selectable list.
    /// </param>
    /// <returns>The builder so calls can be chained.</returns>
    public static RouteHandlerBuilder WithRequestExamples(
        this RouteHandlerBuilder builder,
        params (string Name, string Description, object Value)[] examples) =>
        builder.WithMetadata(examples.Select(e => new RequestExampleMetadata(e.Name, e.Description, e.Value)).ToArray());

    /// <summary>
    /// Attaches named response examples for the given status codes. The matching response (declared with
    /// <c>Produces</c>) must exist on the endpoint; when it declares <c>application/json</c> content the examples are
    /// added to that media type.
    /// </summary>
    /// <param name="builder">The endpoint being configured.</param>
    /// <param name="examples">
    /// Tuples of <c>(StatusCode, Name, Description, Value)</c>. A single example is shown when one is supplied; when
    /// multiple are supplied Swagger UI offers them as a selectable list.
    /// </param>
    /// <returns>The builder so calls can be chained.</returns>
    public static RouteHandlerBuilder WithResponseExamples(
        this RouteHandlerBuilder builder,
        params (int StatusCode, string Name, string Description, object Value)[] examples) =>
        builder.WithMetadata(examples.Select(e => new ResponseExampleMetadata(e.StatusCode, e.Name, e.Description, e.Value)).ToArray());
}
