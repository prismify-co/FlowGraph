using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.EdgeRenderers;

/// <summary>
/// Registry for custom edge renderers.
/// Maps edge type strings to renderer implementations.
/// </summary>
/// <remarks>
/// <para>
/// The registry uses a hierarchical lookup strategy:
/// 1. Exact match on edge's custom type (e.g., "sequence-message")
/// 2. Prefix match with wildcard (e.g., "sequence-*" matches "sequence-message")
/// 3. Fall back to default renderer
/// </para>
/// <para>
/// Edge type is determined from the edge's Type property when set to Custom,
/// combined with a custom type string stored in edge metadata.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var registry = new EdgeRendererRegistry();
/// 
/// // Register specific type
/// registry.Register("sequence-message", new SequenceMessageRenderer());
/// 
/// // Register wildcard pattern
/// registry.Register("swimlane-*", new SwimlaneEdgeRenderer());
/// 
/// // Set default renderer
/// registry.SetDefault(new OrthogonalEdgeRenderer());
/// </code>
/// </example>
public class EdgeRendererRegistry
{
  private readonly Dictionary<string, IEdgeRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);
  private IEdgeRenderer? _defaultRenderer;

  /// <summary>
  /// Registers a renderer for a specific edge type.
  /// </summary>
  /// <param name="edgeType">The edge type string to match.</param>
  /// <param name="renderer">The renderer to use for this type.</param>
  public void Register(string edgeType, IEdgeRenderer renderer)
  {
    ArgumentNullException.ThrowIfNull(edgeType);
    ArgumentNullException.ThrowIfNull(renderer);
    _renderers[edgeType] = renderer;
  }

  /// <summary>
  /// Unregisters a renderer for a specific edge type.
  /// </summary>
  /// <param name="edgeType">The edge type to unregister.</param>
  /// <returns>True if the renderer was removed, false if not found.</returns>
  public bool Unregister(string edgeType)
  {
    return _renderers.Remove(edgeType);
  }

  /// <summary>
  /// Sets the default renderer used when no specific match is found.
  /// </summary>
  /// <param name="renderer">The default renderer, or null to use built-in rendering.</param>
  public void SetDefault(IEdgeRenderer? renderer)
  {
    _defaultRenderer = renderer;
  }

  /// <summary>
  /// Gets the appropriate renderer for an edge.
  /// </summary>
  /// <param name="edge">The edge to get a renderer for.</param>
  /// <returns>The matching renderer, or null to use built-in rendering.</returns>
  public IEdgeRenderer? GetRenderer(Edge edge)
  {
    // First, try to match based on edge ID prefix (e.g., "hypothesis:workitem->metric")
    // This allows custom renderers to be registered by prefix without modifying Edge.Type
    if (!string.IsNullOrEmpty(edge.Id))
    {
      var colonIndex = edge.Id.IndexOf(':');
      if (colonIndex > 0)
      {
        var idPrefix = edge.Id[..colonIndex];
        if (_renderers.TryGetValue(idPrefix, out var prefixRenderer))
          return prefixRenderer;
      }
    }

    var edgeType = GetEdgeTypeString(edge);

    if (string.IsNullOrEmpty(edgeType))
      return _defaultRenderer;

    // Try exact match first
    if (_renderers.TryGetValue(edgeType, out var exactRenderer))
      return exactRenderer;

    // Try wildcard patterns (e.g., "sequence-*")
    foreach (var (pattern, renderer) in _renderers)
    {
      if (pattern.EndsWith('*'))
      {
        var prefix = pattern[..^1];
        if (edgeType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
          return renderer;
      }
    }

    return _defaultRenderer;
  }

  /// <summary>
  /// Checks if a custom renderer is registered for the given edge type.
  /// </summary>
  /// <param name="edgeType">The edge type to check.</param>
  /// <returns>True if a custom renderer exists.</returns>
  public bool HasRenderer(string edgeType)
  {
    return _renderers.ContainsKey(edgeType);
  }

  /// <summary>
  /// Clears all registered custom renderers.
  /// </summary>
  public void Clear()
  {
    _renderers.Clear();
    _defaultRenderer = null;
  }

  /// <summary>
  /// Gets the type string for an edge.
  /// </summary>
  /// <param name="edge">The edge to get the type for.</param>
  /// <returns>The edge type string based on EdgeType enum.</returns>
  private static string? GetEdgeTypeString(Edge edge)
  {
    // Map EdgeType enum to string for built-in types
    // Pro version can register renderers for custom string types
    // and use edge metadata/labels to determine rendering behavior
    return edge.Type switch
    {
      EdgeType.Bezier => "bezier",
      EdgeType.Straight => "straight",
      EdgeType.Step => "step",
      EdgeType.SmoothStep => "smooth-step",
      _ => null
    };
  }

  /// <summary>
  /// Gets a renderer for a specific string type key.
  /// This allows Pro features to use custom type strings without modifying core Edge.
  /// </summary>
  /// <param name="typeKey">The custom type key.</param>
  /// <returns>The registered renderer, or null if not found.</returns>
  public IEdgeRenderer? GetRendererByKey(string typeKey)
  {
    // Try exact match first
    if (_renderers.TryGetValue(typeKey, out var renderer))
      return renderer;

    // Try wildcard patterns (e.g., "sequence-*")
    foreach (var (pattern, wildcardRenderer) in _renderers)
    {
      if (pattern.EndsWith('*'))
      {
        var prefix = pattern[..^1];
        if (typeKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
          return wildcardRenderer;
      }
    }

    return _defaultRenderer;
  }
}
