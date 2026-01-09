using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.PortRenderers;

/// <summary>
/// Registry for port renderers. Maps port types to their visual renderers.
/// Register custom port renderers to customize how ports are displayed.
/// </summary>
public class PortRendererRegistry
{
  private readonly Dictionary<string, IPortRenderer> _renderers = new();
  private IPortRenderer _defaultRenderer = DefaultPortRenderer.Instance;

  /// <summary>
  /// Creates a new port renderer registry with the default renderer.
  /// </summary>
  public PortRendererRegistry()
  {
  }

  /// <summary>
  /// Registers a renderer for a specific port type.
  /// </summary>
  /// <param name="portType">The port type to register the renderer for.</param>
  /// <param name="renderer">The renderer instance.</param>
  public void Register(string portType, IPortRenderer renderer)
  {
    _renderers[portType] = renderer;
  }

  /// <summary>
  /// Registers a renderer for multiple port types.
  /// </summary>
  /// <param name="portTypes">The port types to register the renderer for.</param>
  /// <param name="renderer">The renderer instance.</param>
  public void Register(IEnumerable<string> portTypes, IPortRenderer renderer)
  {
    foreach (var portType in portTypes)
    {
      _renderers[portType] = renderer;
    }
  }

  /// <summary>
  /// Unregisters the renderer for a specific port type.
  /// </summary>
  /// <param name="portType">The port type to unregister.</param>
  /// <returns>True if a renderer was removed, false otherwise.</returns>
  public bool Unregister(string portType)
  {
    return _renderers.Remove(portType);
  }

  /// <summary>
  /// Sets the default renderer used for unregistered port types.
  /// </summary>
  /// <param name="renderer">The default renderer.</param>
  public void SetDefaultRenderer(IPortRenderer renderer)
  {
    _defaultRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
  }

  /// <summary>
  /// Gets the renderer for a specific port type.
  /// Returns the default renderer if no specific renderer is registered.
  /// </summary>
  /// <param name="portType">The port type to get the renderer for.</param>
  /// <returns>The port renderer.</returns>
  public IPortRenderer GetRenderer(string portType)
  {
    return _renderers.TryGetValue(portType, out var renderer) ? renderer : _defaultRenderer;
  }

  /// <summary>
  /// Gets the renderer for a specific port.
  /// Uses the port's Type property to look up the renderer.
  /// </summary>
  /// <param name="port">The port to get the renderer for.</param>
  /// <returns>The port renderer.</returns>
  public IPortRenderer GetRenderer(Port port)
  {
    return GetRenderer(port.Type);
  }

  /// <summary>
  /// Checks if a specific renderer is registered for a port type.
  /// </summary>
  /// <param name="portType">The port type to check.</param>
  /// <returns>True if a specific renderer is registered.</returns>
  public bool HasRenderer(string portType)
  {
    return _renderers.ContainsKey(portType);
  }

  /// <summary>
  /// Gets all registered port types.
  /// </summary>
  public IEnumerable<string> RegisteredTypes => _renderers.Keys;

  /// <summary>
  /// Clears all registered renderers, keeping only the default.
  /// </summary>
  public void Clear()
  {
    _renderers.Clear();
  }
}
