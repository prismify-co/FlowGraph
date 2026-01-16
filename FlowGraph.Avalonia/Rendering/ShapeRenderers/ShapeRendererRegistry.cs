using FlowGraph.Core.Diagnostics;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Registry for shape renderers, allowing custom renderers for different shape types.
/// </summary>
/// <remarks>
/// <para>
/// The registry follows the same pattern as <see cref="NodeRenderers.NodeRendererRegistry"/>
/// and <see cref="EdgeRenderers.EdgeRendererRegistry"/>, providing a consistent
/// extensibility model for all element types.
/// </para>
/// <para>
/// Shape types are identified by their <see cref="Core.Elements.CanvasElement.Type"/> property
/// (e.g., "rectangle", "line", "text", "ellipse").
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a custom renderer via singleton
/// ShapeRendererRegistry.Instance.Register("custom-shape", new CustomShapeRenderer());
/// 
/// // Or via FlowCanvas property
/// canvas.ShapeRenderers.Register("custom-shape", new CustomShapeRenderer());
/// 
/// // Get a renderer for a shape
/// var renderer = ShapeRendererRegistry.Instance.GetRenderer(shape.Type);
/// </code>
/// </example>
public class ShapeRendererRegistry
{
  private readonly Dictionary<string, IShapeRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);
  private IShapeRenderer? _defaultRenderer;

  /// <summary>
  /// Gets the shared singleton instance for global access.
  /// </summary>
  public static ShapeRendererRegistry Instance { get; } = new();

  /// <summary>
  /// Creates a new ShapeRendererRegistry with built-in renderers registered.
  /// </summary>
  public ShapeRendererRegistry()
  {
    RegisterBuiltInRenderers();
  }

  /// <summary>
  /// Creates a new ShapeRendererRegistry with a custom default renderer.
  /// </summary>
  /// <param name="defaultRenderer">The default renderer to use when no type-specific renderer is found.</param>
  public ShapeRendererRegistry(IShapeRenderer defaultRenderer)
  {
    _defaultRenderer = defaultRenderer ?? throw new ArgumentNullException(nameof(defaultRenderer));
    RegisterBuiltInRenderers();
  }

  /// <summary>
  /// Registers a renderer for a specific shape type.
  /// </summary>
  /// <param name="shapeType">The shape type identifier (e.g., "rectangle", "line").</param>
  /// <param name="renderer">The renderer implementation.</param>
  public void Register(string shapeType, IShapeRenderer renderer)
  {
    ArgumentNullException.ThrowIfNull(shapeType);
    ArgumentNullException.ThrowIfNull(renderer);

    _renderers[shapeType] = renderer;
    FlowGraphLogger.Debug(LogCategory.Rendering, $"Registered shape renderer for type '{shapeType}'", "ShapeRendererRegistry");
  }

  /// <summary>
  /// Sets the default renderer used when no type-specific renderer is found.
  /// </summary>
  /// <param name="renderer">The default renderer.</param>
  public void SetDefaultRenderer(IShapeRenderer renderer)
  {
    _defaultRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
  }

  /// <summary>
  /// Gets the renderer for the specified shape type.
  /// Falls back to the default renderer if no specific renderer is registered.
  /// </summary>
  /// <param name="shapeType">The shape type to get a renderer for.</param>
  /// <returns>The renderer for the shape type, or the default renderer.</returns>
  /// <exception cref="InvalidOperationException">Thrown if no renderer is found and no default is set.</exception>
  public IShapeRenderer GetRenderer(string shapeType)
  {
    if (_renderers.TryGetValue(shapeType, out var renderer))
      return renderer;

    if (_defaultRenderer != null)
    {
      FlowGraphLogger.Debug(LogCategory.Rendering, $"Using default renderer for shape type '{shapeType}'", "ShapeRendererRegistry");
      return _defaultRenderer;
    }

    throw new InvalidOperationException($"No renderer registered for shape type '{shapeType}' and no default renderer set.");
  }

  /// <summary>
  /// Gets the renderer for a specific shape element.
  /// </summary>
  /// <param name="shape">The shape element.</param>
  /// <returns>The appropriate renderer for the shape.</returns>
  public IShapeRenderer GetRenderer(ShapeElement shape)
  {
    ArgumentNullException.ThrowIfNull(shape);
    return GetRenderer(shape.Type);
  }

  /// <summary>
  /// Checks if a renderer is registered for the specified type.
  /// </summary>
  /// <param name="shapeType">The shape type to check.</param>
  /// <returns>True if a renderer is registered; otherwise, false.</returns>
  public bool HasRenderer(string shapeType)
  {
    return _renderers.ContainsKey(shapeType) || _defaultRenderer != null;
  }

  /// <summary>
  /// Gets all registered shape types.
  /// </summary>
  /// <returns>An enumerable of registered shape type identifiers.</returns>
  public IEnumerable<string> GetRegisteredTypes() => _renderers.Keys;

  /// <summary>
  /// Resets the registry to built-in renderers only. Useful for testing.
  /// </summary>
  public void Reset()
  {
    _renderers.Clear();
    _defaultRenderer = null;
    RegisterBuiltInRenderers();
  }

  /// <summary>
  /// Clears all registered renderers. Useful for testing.
  /// </summary>
  internal void ClearAll()
  {
    _renderers.Clear();
    _defaultRenderer = null;
  }

  /// <summary>
  /// Registers the built-in shape renderers.
  /// </summary>
  private void RegisterBuiltInRenderers()
  {
    // Register built-in renderers
    Register("rectangle", new RectangleRenderer());
    Register("line", new LineRenderer());
    Register("text", new TextRenderer());
    Register("ellipse", new EllipseRenderer());
    Register("comment", new CommentRenderer());

    // Set rectangle as the default fallback
    _defaultRenderer = new RectangleRenderer();

    FlowGraphLogger.Debug(LogCategory.Rendering, "Registered built-in shape renderers", "ShapeRendererRegistry");
  }
}
