using Avalonia;
using Avalonia.Controls;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering.BackgroundRenderers;

/// <summary>
/// Specifies which canvas layer a background renderer should render to.
/// </summary>
public enum BackgroundRenderTarget
{
  /// <summary>
  /// Render to GridCanvas (no transform). 
  /// Use for fixed backgrounds that should not zoom/pan with nodes.
  /// Requires manual CanvasToScreen() coordinate conversion.
  /// </summary>
  GridCanvas,

  /// <summary>
  /// Render to MainCanvas (has MatrixTransform for zoom/pan).
  /// Use for diagram elements that should zoom/pan with nodes.
  /// Use canvas coordinates directly - transform handles zoom/pan automatically.
  /// </summary>
  MainCanvas
}

/// <summary>
/// Interface for custom background renderers.
/// Implement this to render custom backgrounds behind the main graph content.
/// </summary>
/// <remarks>
/// <para>
/// Background renderers are called during the render cycle, after the grid
/// but before edges and nodes. They can render static backgrounds (gradients,
/// images) or graph-dependent visuals (swimlane lanes, sequence diagram lifelines).
/// </para>
/// <para>
/// Multiple background renderers can be registered and are rendered in order.
/// Each renderer receives the full graph context and can access node positions.
/// </para>
/// <para>
/// <b>Important:</b> Set <see cref="RenderTarget"/> appropriately:
/// <list type="bullet">
/// <item><see cref="BackgroundRenderTarget.GridCanvas"/>: For fixed backgrounds (default). Use CanvasToScreen() for coordinates.</item>
/// <item><see cref="BackgroundRenderTarget.MainCanvas"/>: For diagram decorations that should zoom/pan with nodes. Use canvas coordinates directly.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a background renderer
/// canvas.BackgroundRenderers.Add(new LifelineBackgroundRenderer());
/// 
/// // Or set as the single background
/// canvas.BackgroundRenderers.SetSingle(new SwimlaneBackgroundRenderer());
/// </code>
/// </example>
public interface IBackgroundRenderer
{
  /// <summary>
  /// Gets the target canvas for this renderer.
  /// Default is GridCanvas (for backward compatibility).
  /// Override to return MainCanvas for diagram elements that should zoom/pan with nodes.
  /// </summary>
  BackgroundRenderTarget RenderTarget => BackgroundRenderTarget.GridCanvas;

  /// <summary>
  /// Renders background content onto the canvas.
  /// </summary>
  /// <param name="canvas">The canvas to render to.</param>
  /// <param name="context">The background rendering context.</param>
  void Render(Canvas canvas, BackgroundRenderContext context);

  /// <summary>
  /// Called when the graph changes to update cached calculations.
  /// </summary>
  /// <param name="graph">The updated graph.</param>
  void OnGraphChanged(Graph? graph) { }

  /// <summary>
  /// Called when the viewport changes (pan/zoom).
  /// </summary>
  /// <param name="context">The updated context.</param>
  void OnViewportChanged(BackgroundRenderContext context) { }

  /// <summary>
  /// Called when the renderer is being removed or replaced.
  /// Implementations should remove any visuals they added to the canvas.
  /// </summary>
  /// <param name="canvas">The canvas to clean up from.</param>
  void Cleanup(Canvas canvas) { }
}

/// <summary>
/// Context information passed to background renderers.
/// </summary>
public class BackgroundRenderContext
{
  /// <summary>
  /// Theme resources for colors and brushes.
  /// </summary>
  public required ThemeResources Theme { get; init; }

  /// <summary>
  /// Canvas settings.
  /// </summary>
  public required FlowCanvasSettings Settings { get; init; }

  /// <summary>
  /// Current zoom scale factor.
  /// </summary>
  public required double Scale { get; init; }

  /// <summary>
  /// The graph being rendered.
  /// </summary>
  public required Graph? Graph { get; init; }

  /// <summary>
  /// Canvas visible bounds in screen coordinates.
  /// </summary>
  public required Rect VisibleBounds { get; init; }

  /// <summary>
  /// Current viewport offset (pan position).
  /// </summary>
  public required AvaloniaPoint Offset { get; init; }

  /// <summary>
  /// Function to convert canvas coordinates to screen coordinates.
  /// </summary>
  public required Func<double, double, AvaloniaPoint> CanvasToScreen { get; init; }

  /// <summary>
  /// Function to convert screen coordinates to canvas coordinates.
  /// </summary>
  public required Func<double, double, AvaloniaPoint> ScreenToCanvas { get; init; }
}
