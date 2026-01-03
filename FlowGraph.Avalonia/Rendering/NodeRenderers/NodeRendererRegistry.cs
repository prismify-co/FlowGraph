namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Registry for custom node type renderers.
/// Register custom renderers to handle specific node types.
/// </summary>
public class NodeRendererRegistry
{
    private readonly Dictionary<string, INodeRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);
    private readonly INodeRenderer _defaultRenderer;

    /// <summary>
    /// Creates a new registry with built-in renderers registered.
    /// </summary>
    public NodeRendererRegistry()
    {
        _defaultRenderer = new DefaultNodeRenderer();
        
        // Register built-in renderers
        RegisterBuiltInRenderers();
    }

    /// <summary>
    /// Creates a new registry with a custom default renderer.
    /// </summary>
    public NodeRendererRegistry(INodeRenderer defaultRenderer)
    {
        _defaultRenderer = defaultRenderer ?? throw new ArgumentNullException(nameof(defaultRenderer));
        RegisterBuiltInRenderers();
    }

    private void RegisterBuiltInRenderers()
    {
        // Register built-in node types
        Register("input", new InputNodeRenderer());
        Register("output", new OutputNodeRenderer());
    }

    /// <summary>
    /// Registers a custom renderer for a node type.
    /// </summary>
    /// <param name="nodeType">The node type name (case-insensitive).</param>
    /// <param name="renderer">The renderer to use for this node type.</param>
    public void Register(string nodeType, INodeRenderer renderer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeType);
        ArgumentNullException.ThrowIfNull(renderer);
        
        _renderers[nodeType] = renderer;
    }

    /// <summary>
    /// Removes a registered renderer for a node type.
    /// </summary>
    /// <param name="nodeType">The node type name to unregister.</param>
    /// <returns>True if a renderer was removed, false if none was registered.</returns>
    public bool Unregister(string nodeType)
    {
        return _renderers.Remove(nodeType);
    }

    /// <summary>
    /// Gets the renderer for a specific node type.
    /// Returns the default renderer if no specific renderer is registered.
    /// </summary>
    /// <param name="nodeType">The node type name.</param>
    /// <returns>The appropriate renderer for the node type.</returns>
    public INodeRenderer GetRenderer(string nodeType)
    {
        if (string.IsNullOrEmpty(nodeType))
            return _defaultRenderer;

        return _renderers.TryGetValue(nodeType, out var renderer) 
            ? renderer 
            : _defaultRenderer;
    }

    /// <summary>
    /// Gets the default renderer used when no specific renderer is registered.
    /// </summary>
    public INodeRenderer DefaultRenderer => _defaultRenderer;

    /// <summary>
    /// Gets all registered node type names.
    /// </summary>
    public IEnumerable<string> RegisteredTypes => _renderers.Keys;

    /// <summary>
    /// Checks if a renderer is registered for a specific node type.
    /// </summary>
    public bool IsRegistered(string nodeType) => _renderers.ContainsKey(nodeType);

    /// <summary>
    /// Clears all registered renderers except built-in ones.
    /// </summary>
    public void ClearCustomRenderers()
    {
        _renderers.Clear();
        RegisterBuiltInRenderers();
    }
}
