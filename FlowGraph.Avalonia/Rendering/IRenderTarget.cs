using Avalonia.Media;
using FlowGraph.Core.Coordinates;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Abstraction for adding temporary visual elements during interaction.
/// 
/// <para>
/// This interface provides a rendering-mode agnostic way to create temporary visuals
/// such as connection preview lines, selection boxes, and drag previews. It handles
/// coordinate transforms and container selection internally based on the current
/// rendering mode (Visual Tree vs Direct Rendering).
/// </para>
/// 
/// <para>
/// <b>Key principle:</b> All coordinates passed to this interface are in canvas space.
/// The implementation handles conversion to viewport coordinates when necessary
/// (e.g., in Direct Rendering mode).
/// </para>
/// </summary>
/// <example>
/// <code>
/// // In ConnectingState
/// public override void Enter(InputStateContext context)
/// {
///     var startCanvas = context.RenderModel.GetPortPosition(sourceNode, sourcePort, fromOutput);
///     
///     // Create temp line - coordinates are in canvas space
///     _lineHandle = context.RenderTarget.CreateConnectionPreview(
///         startCanvas, startCanvas, 
///         context.Theme.EdgeStroke, 2.0);
/// }
/// 
/// public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
/// {
///     var endCanvas = context.Coordinates.GetPointerCanvasPosition(e);
///     
///     // Update with canvas coordinates - transform handled internally
///     context.RenderTarget.UpdateConnectionPreview(_lineHandle, startCanvas, endCanvas);
/// }
/// 
/// public override void Exit(InputStateContext context)
/// {
///     _lineHandle?.Dispose();
/// }
/// </code>
/// </example>
public interface IRenderTarget
{
  /// <summary>
  /// Creates a temporary bezier connection line for previewing connections.
  /// 
  /// <para>
  /// The line is styled with dashed strokes and reduced opacity to indicate
  /// it's a preview rather than a committed connection.
  /// </para>
  /// </summary>
  /// <param name="start">Start point in canvas coordinates (port position).</param>
  /// <param name="end">End point in canvas coordinates (cursor or snap target).</param>
  /// <param name="stroke">Stroke brush for the line.</param>
  /// <param name="strokeThickness">Stroke thickness (in screen pixels, not canvas units).</param>
  /// <param name="dashArray">Optional dash pattern. Defaults to [5, 3] for preview style.</param>
  /// <returns>A handle that can be used to update or dispose the preview line.</returns>
  IConnectionPreviewHandle CreateConnectionPreview(
      CanvasPoint start,
      CanvasPoint end,
      IBrush stroke,
      double strokeThickness,
      double[]? dashArray = null);

  /// <summary>
  /// Creates a temporary selection rectangle.
  /// 
  /// <para>
  /// Used during box selection to show the selection area.
  /// </para>
  /// </summary>
  /// <param name="bounds">Rectangle bounds in canvas coordinates.</param>
  /// <param name="fill">Optional fill brush (typically semi-transparent).</param>
  /// <param name="stroke">Stroke brush for the border.</param>
  /// <param name="strokeThickness">Border thickness (in screen pixels).</param>
  /// <returns>A handle that can be used to update or dispose the selection box.</returns>
  ISelectionBoxHandle CreateSelectionBox(
      CanvasRect bounds,
      IBrush? fill,
      IBrush stroke,
      double strokeThickness);

  /// <summary>
  /// Invalidates the render target, triggering a redraw.
  /// 
  /// <para>
  /// In Visual Tree mode, this may do nothing (elements update automatically).
  /// In Direct Rendering mode, this calls InvalidateVisual() on the renderer.
  /// </para>
  /// </summary>
  void Invalidate();
}

/// <summary>
/// Handle for managing a temporary connection preview line.
/// </summary>
public interface IConnectionPreviewHandle : IDisposable
{
  /// <summary>
  /// Updates the start point of the connection preview.
  /// </summary>
  /// <param name="start">New start point in canvas coordinates.</param>
  void UpdateStart(CanvasPoint start);

  /// <summary>
  /// Updates the end point of the connection preview.
  /// </summary>
  /// <param name="end">New end point in canvas coordinates.</param>
  void UpdateEnd(CanvasPoint end);

  /// <summary>
  /// Updates both endpoints of the connection preview.
  /// </summary>
  /// <param name="start">New start point in canvas coordinates.</param>
  /// <param name="end">New end point in canvas coordinates.</param>
  void Update(CanvasPoint start, CanvasPoint end);

  /// <summary>
  /// Gets or sets the stroke brush.
  /// </summary>
  IBrush Stroke { get; set; }

  /// <summary>
  /// Gets or sets the opacity (0.0 to 1.0).
  /// </summary>
  double Opacity { get; set; }

  /// <summary>
  /// Sets whether the line should use a "valid target" visual style.
  /// </summary>
  /// <param name="isValid">True for valid target style, false for normal preview style.</param>
  void SetValidTargetStyle(bool isValid);
}

/// <summary>
/// Handle for managing a temporary selection box.
/// </summary>
public interface ISelectionBoxHandle : IDisposable
{
  /// <summary>
  /// Updates the bounds of the selection box.
  /// </summary>
  /// <param name="bounds">New bounds in canvas coordinates.</param>
  void UpdateBounds(CanvasRect bounds);

  /// <summary>
  /// Gets or sets the fill brush.
  /// </summary>
  IBrush? Fill { get; set; }

  /// <summary>
  /// Gets or sets the stroke brush.
  /// </summary>
  IBrush Stroke { get; set; }
}
