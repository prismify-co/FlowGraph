using Avalonia.Controls;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.PortRenderers;

/// <summary>
/// Interface for custom port renderers.
/// Implement this interface to create custom visual representations for port types.
/// </summary>
public interface IPortRenderer
{
  /// <summary>
  /// Creates the visual representation for a port.
  /// </summary>
  /// <param name="port">The port data to render.</param>
  /// <param name="node">The parent node.</param>
  /// <param name="context">The rendering context with theme, scale, and settings.</param>
  /// <returns>A Control representing the port visual.</returns>
  Control CreatePortVisual(Port port, Node node, PortRenderContext context);

  /// <summary>
  /// Updates the visual state when the port state changes (hover, connected, etc.).
  /// </summary>
  /// <param name="visual">The port's visual control.</param>
  /// <param name="port">The port data.</param>
  /// <param name="node">The parent node.</param>
  /// <param name="context">The rendering context.</param>
  /// <param name="state">The current port state.</param>
  void UpdateState(Control visual, Port port, Node node, PortRenderContext context, PortVisualState state);

  /// <summary>
  /// Gets the size of the port visual in pixels (before scaling).
  /// Return null to use the default port size from settings.
  /// </summary>
  double? GetSize(Port port, Node node, FlowCanvasSettings settings);
}

/// <summary>
/// Context information passed to port renderers.
/// </summary>
public class PortRenderContext
{
  /// <summary>
  /// Theme resources for styling.
  /// </summary>
  public required ThemeResources Theme { get; init; }

  /// <summary>
  /// Canvas settings.
  /// </summary>
  public required FlowCanvasSettings Settings { get; init; }

  /// <summary>
  /// Current zoom scale.
  /// </summary>
  public required double Scale { get; init; }

  /// <summary>
  /// Whether this is an output port (true) or input port (false).
  /// </summary>
  public required bool IsOutput { get; init; }

  /// <summary>
  /// Index of this port among its siblings.
  /// </summary>
  public required int Index { get; init; }

  /// <summary>
  /// Total number of sibling ports.
  /// </summary>
  public required int TotalPorts { get; init; }
}

/// <summary>
/// Represents the visual state of a port.
/// </summary>
public record PortVisualState
{
  /// <summary>
  /// Whether the port is currently being hovered.
  /// </summary>
  public bool IsHovered { get; init; }

  /// <summary>
  /// Whether the port has any connections.
  /// </summary>
  public bool IsConnected { get; init; }

  /// <summary>
  /// Whether the port is a valid drop target for a dragged connection.
  /// </summary>
  public bool IsValidDropTarget { get; init; }

  /// <summary>
  /// Whether an edge is currently being dragged from this port.
  /// </summary>
  public bool IsDragging { get; init; }

  /// <summary>
  /// Default state instance.
  /// </summary>
  public static PortVisualState Default { get; } = new();
}
