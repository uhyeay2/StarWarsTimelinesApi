using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StarWarsTimelines.Api.OpenApi;

/// <summary>
/// Adds the request and response examples attached to endpoints via <see cref="EndpointExampleExtensions"/> to the
/// generated OpenAPI document. Example values are serialized with the same JSON conventions the API uses at runtime
/// (<see cref="JsonSerializerDefaults.Web"/>), so the examples match the real wire format.
/// </summary>
public sealed class ExampleOperationFilter : IOperationFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor?.EndpointMetadata;
        if (metadata is null)
        {
            return;
        }

        foreach (var example in metadata.OfType<RequestExampleMetadata>())
        {
            var requestContent = operation.RequestBody?.Content;
            if (requestContent is null || !requestContent.TryGetValue("application/json", out var mediaType))
            {
                continue;
            }

            mediaType.Examples ??= new Dictionary<string, IOpenApiExample>();
            mediaType.Examples[example.Name] = CreateExample(example.Name, example.Description, example.Value);
        }

        foreach (var example in metadata.OfType<ResponseExampleMetadata>())
        {
            var statusKey = example.StatusCode.ToString(CultureInfo.InvariantCulture);
            var responses = operation.Responses;
            if (responses is null || !responses.TryGetValue(statusKey, out var response) || response is not OpenApiResponse concrete)
            {
                continue;
            }

            var responseContent = concrete.Content ??= new Dictionary<string, OpenApiMediaType>();
            if (!responseContent.TryGetValue("application/json", out var mediaType))
            {
                mediaType = new OpenApiMediaType();
                responseContent["application/json"] = mediaType;
            }

            mediaType.Examples ??= new Dictionary<string, IOpenApiExample>();
            mediaType.Examples[example.Name] = CreateExample(example.Name, example.Description, example.Value);
        }
    }

    private static OpenApiExample CreateExample(string name, string description, object value) =>
        new()
        {
            Summary = name,
            Description = description,
            Value = JsonSerializer.SerializeToNode(value, JsonOptions)
        };
}
