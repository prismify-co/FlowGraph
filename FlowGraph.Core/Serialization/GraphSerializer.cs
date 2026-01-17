using System.Text.Json;
using System.Text.Json.Serialization;
using FlowGraph.Core.Serialization.Dtos;

namespace FlowGraph.Core.Serialization;

/// <summary>
/// Serializes and deserializes graphs to/from JSON.
/// </summary>
/// <remarks>
/// <para>
/// Supports two format versions:
/// </para>
/// <list type="bullet">
/// <item><b>Version 2 (default)</b>: Uses polymorphic <c>elements[]</c> array with "kind" discriminator</item>
/// <item><b>Version 1 (legacy)</b>: Uses separate <c>nodes[]</c>, <c>edges[]</c>, <c>shapes[]</c> arrays</item>
/// </list>
/// <para>
/// Serialization always outputs Version 2 format. Deserialization supports both formats
/// for backward compatibility with older saved files.
/// </para>
/// </remarks>
public static class GraphSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateDefaultOptions();

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        return options;
    }

    /// <summary>
    /// Serializes a graph to a JSON string.
    /// </summary>
    /// <param name="graph">The graph to serialize.</param>
    /// <param name="options">Optional JSON serializer options. Uses default if not specified.</param>
    /// <returns>JSON string representation of the graph.</returns>
    public static string Serialize(Graph graph, JsonSerializerOptions? options = null)
    {
        var dto = GraphDto.FromGraph(graph);
        return JsonSerializer.Serialize(dto, options ?? DefaultOptions);
    }

    /// <summary>
    /// Serializes a graph to a JSON file.
    /// </summary>
    /// <param name="graph">The graph to serialize.</param>
    /// <param name="filePath">Path to the output file.</param>
    /// <param name="options">Optional JSON serializer options. Uses default if not specified.</param>
    public static async Task SerializeToFileAsync(Graph graph, string filePath, JsonSerializerOptions? options = null)
    {
        var dto = GraphDto.FromGraph(graph);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, dto, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes a graph from a JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <param name="options">Optional JSON serializer options. Uses default if not specified.</param>
    /// <returns>Deserialized graph, or null if deserialization fails.</returns>
    public static Graph? Deserialize(string json, JsonSerializerOptions? options = null)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<GraphDto>(json, options ?? DefaultOptions);
            return dto?.ToGraph();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes a graph from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="options">Optional JSON serializer options. Uses default if not specified.</param>
    /// <returns>Deserialized graph, or null if deserialization fails.</returns>
    public static async Task<Graph?> DeserializeFromFileAsync(string filePath, JsonSerializerOptions? options = null)
    {
        await using var stream = File.OpenRead(filePath);
        var dto = await JsonSerializer.DeserializeAsync<GraphDto>(stream, options ?? DefaultOptions);
        return dto?.ToGraph();
    }

    /// <summary>
    /// Validates JSON without fully deserializing.
    /// </summary>
    /// <param name="json">JSON string to validate.</param>
    /// <param name="errorMessage">Error message if validation fails, null otherwise.</param>
    /// <returns>True if JSON is valid, false otherwise.</returns>
    public static bool TryValidate(string json, out string? errorMessage)
    {
        try
        {
            JsonSerializer.Deserialize<GraphDto>(json, DefaultOptions);
            errorMessage = null;
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
