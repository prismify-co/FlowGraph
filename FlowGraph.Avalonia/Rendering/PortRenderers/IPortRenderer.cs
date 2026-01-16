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

  /// <summary>
  /// Called when the port visual is being removed from the canvas.
  /// Use this to stop animations and clean up resources.
  /// </summary>
  /// <param name="visual">The port's visual control being removed.</param>
  /// <param name="port">The port data.</param>
  /// <param name="node">The parent node.</param>
  void OnDetached(Control visual, Port port, Node node) { }

  /// <summary>
  /// Triggers a one-shot data pulse animation to indicate data flowing through the port.
  /// </summary>
  /// <param name="visual">The port's visual control.</param>
  /// <param name="port">The port data.</param>
  /// <param name="node">The parent node.</param>
  /// <param name="context">The rendering context.</param>
  void TriggerDataPulse(Control visual, Port port, Node node, PortRenderContext context) { }

  /// <summary>
  /// Triggers a one-shot error animation (e.g., shake effect) for validation failures.
  /// </summary>
  /// <param name="visual">The port's visual control.</param>
  /// <param name="port">The port data.</param>
  /// <param name="node">The parent node.</param>
  /// <param name="message">Optional error message.</param>
  void TriggerError(Control visual, Port port, Node node, string? message) { }

  /// <summary>
  /// Triggers a one-shot success animation (e.g., flash effect) for successful connections.
  /// </summary>
  /// <param name="visual">The port's visual control.</param>
  /// <param name="port">The port data.</param>
  /// <param name="node">The parent node.</param>
  void TriggerSuccess(Control visual, Port port, Node node) { }
}

/// <summary>
/// Context information passed to port renderers.
/// 
/// <para><b>TRANSFORM-BASED RENDERING:</b></para>
/// <para>
/// <see cref="Scale"/> is always 1.0 - create visuals at logical size.
/// The MatrixTransform on MainCanvas handles zoom. This enables O(1) zoom.
/// </para>
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
  /// Logical scale for visual sizing. Always 1.0 in transform-based rendering.
  /// </summary>
  public required double Scale { get; init; }

  /// <summary>
  /// Actual viewport zoom level. Use for calculations, not visual sizing.
  /// </summary>
  public double ViewportZoom { get; init; } = 1.0;

  /// <summary>
  /// Inverse scale for constant-size elements (1/ViewportZoom).
  /// Apply as ScaleTransform to elements that should stay same screen size.
  /// </summary>
  public double InverseScale => 1.0 / ViewportZoom;

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
  /// The type of state change that just occurred, if any.
  /// Use this to trigger one-shot animations (e.g., ripple on connect).
  /// </summary>
  public PortStateChange Change { get; init; } = PortStateChange.None;

  /// <summary>
  /// Number of edges connected to this port.
  /// Useful for showing connection count badges or varying visual intensity.
  /// </summary>
  public int ConnectionCount { get; init; }

  /// <summary>
  /// Whether data is currently being received on this port (for input ports).
  /// Use this for data flow indicators/animations.
  /// </summary>
  public bool IsReceivingData { get; init; }

  /// <summary>
  /// Whether data is currently being sent from this port (for output ports).
  /// Use this for data flow indicators/animations.
  /// </summary>
  public bool IsSendingData { get; init; }

  /// <summary>
  /// Whether the port has a validation error.
  /// </summary>
  public bool HasError { get; init; }

  /// <summary>
  /// Optional error message when <see cref="HasError"/> is true.
  /// </summary>
  public string? ErrorMessage { get; init; }

  /// <summary>
  /// Default state instance.
  /// </summary>
  public static PortVisualState Default { get; } = new();
}

/// <summary>
/// Indicates the type of state change that just occurred on a port.
/// Used to trigger one-shot animations.
/// </summary>
public enum PortStateChange
{
  /// <summary>
  /// No state change (steady state).
  /// </summary>
  None,

  /// <summary>
  /// An edge was just connected to this port.
  /// Use for connection ripple/pulse animation.
  /// </summary>
  JustConnected,

  /// <summary>
  /// An edge was just disconnected from this port.
  /// Use for disconnection fade/ripple animation.
  /// </summary>
  JustDisconnected,

  /// <summary>
  /// Data just passed through this port.
  /// Use for data flow pulse animation.
  /// </summary>
  DataPulse,

  /// <summary>
  /// A validation error just occurred.
  /// Use for error shake animation.
  /// </summary>
  ValidationError,

  /// <summary>
  /// A validation succeeded (e.g., valid drop target confirmed).
  /// Use for success flash animation.
  /// </summary>
  ValidationSuccess
}
