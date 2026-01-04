using System.Text.Json;

namespace FlowGraph.Core.Serialization;

/// <summary>
/// Extension methods for graph serialization.
/// </summary>
public static class GraphSerializationExtensions
{
    /// <summary>
    /// Serializes the graph to JSON.
    /// </summary>
    public static string ToJson(this Graph graph, bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        return GraphSerializer.Serialize(graph, options);
    }

    /// <summary>
    /// Saves the graph to a JSON file.
    /// </summary>
    public static async Task SaveAsync(this Graph graph, string filePath)
    {
        await GraphSerializer.SerializeToFileAsync(graph, filePath);
    }

    /// <summary>
    /// Saves the graph to a JSON file synchronously.
    /// </summary>
    public static void Save(this Graph graph, string filePath)
    {
        var json = graph.ToJson();
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a graph from JSON string.
    /// </summary>
    public static Graph? LoadFromJson(string json)
    {
        return GraphSerializer.Deserialize(json);
    }

    /// <summary>
    /// Loads a graph from a JSON file.
    /// </summary>
    public static async Task<Graph?> LoadFromFileAsync(string filePath)
    {
        return await GraphSerializer.DeserializeFromFileAsync(filePath);
    }

    /// <summary>
    /// Loads a graph from a JSON file synchronously.
    /// </summary>
    public static Graph? LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return GraphSerializer.Deserialize(json);
    }

    /// <summary>
    /// Creates a deep copy of the graph via serialization.
    /// </summary>
    public static Graph Clone(this Graph graph)
    {
        var json = graph.ToJson(false);
        return GraphSerializer.Deserialize(json) ?? new Graph();
    }
}
