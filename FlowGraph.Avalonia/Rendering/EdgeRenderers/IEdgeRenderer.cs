using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering.EdgeRenderers;

/// <summary>
/// Interface for custom edge renderers.
/// Implement this interface to create custom visual representations for edge types.
/// </summary>
/// <remarks>
/// <para>
/// Edge renderers control the visual appearance of connections between nodes.
/// Unlike the default edge rendering (bezier curves), custom renderers can create
/// any visual representation: orthogonal lines, curved paths, animated connections,
/// or complex visuals like sequence diagram lifelines.
/// </para>
/// <para>
/// Edge renderers are registered by edge type string (e.g., "sequence-message", 
/// "swimlane-flow") using <see cref="EdgeRendererRegistry"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a custom edge renderer
/// canvas.EdgeRenderers.Register("sequence-message", new SequenceMessageEdgeRenderer());
/// 
/// // Create an edge with custom type
/// var edge = new Edge(...) { Type = EdgeType.Custom };
/// edge.Definition = edge.Definition with { CustomType = "sequence-message" };
/// </code>
/// </example>
public interface IEdgeRenderer
{
  /// <summary>
  /// Creates the visual representation for an edge.
  /// </summary>
  /// <param name="edge">The edge to render.</param>
  /// <param name="context">The rendering context with coordinates, theme, and scale.</param>
  /// <returns>The rendered edge elements (path, labels, markers).</returns>
  EdgeRenderResult Render(Edge edge, EdgeRenderContext context);

  /// <summary>
  /// Updates the visual state when selection changes.
  /// </summary>
  /// <param name="result">The render result containing visual elements.</param>
  /// <param name="edge">The edge data.</param>
  /// <param name="context">The rendering context.</param>
  void UpdateSelection(EdgeRenderResult result, Edge edge, EdgeRenderContext context);

  /// <summary>
  /// Gets the hit test geometry for click detection.
  /// Return a wider path than the visible path for easier clicking.
  /// </summary>
  /// <param name="edge">The edge to get hit area for.</param>
  /// <param name="context">The rendering context.</param>
  /// <returns>A geometry for hit testing, or null to use default.</returns>
  Geometry? GetHitTestGeometry(Edge edge, EdgeRenderContext context) => null;
}

/// <summary>
/// Context information passed to edge renderers.
/// Contains all the information needed to render an edge.
/// </summary>
public class EdgeRenderContext
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
  /// The source node of the edge.
  /// </summary>
  public required Node SourceNode { get; init; }

  /// <summary>
  /// The target node of the edge.
  /// </summary>
  public required Node TargetNode { get; init; }

  /// <summary>
  /// Start point in screen coordinates.
  /// </summary>
  public required AvaloniaPoint StartPoint { get; init; }

  /// <summary>
  /// End point in screen coordinates.
  /// </summary>
  public required AvaloniaPoint EndPoint { get; init; }

  /// <summary>
  /// The graph containing the edge.
  /// </summary>
  public required Graph Graph { get; init; }
}

/// <summary>
/// Result of rendering an edge, containing all visual elements.
/// </summary>
public class EdgeRenderResult
{
  /// <summary>
  /// The main visible path (the actual edge line).
  /// </summary>
  public required AvaloniaPath VisiblePath { get; init; }

  /// <summary>
  /// The hit area path (invisible, wider for click detection).
  /// </summary>
  public required AvaloniaPath HitAreaPath { get; init; }

  /// <summary>
  /// Optional markers (arrows, circles) at edge endpoints.
  /// </summary>
  public IReadOnlyList<AvaloniaPath>? Markers { get; init; }

  /// <summary>
  /// Optional label displayed along the edge.
  /// </summary>
  public Control? Label { get; init; }

  /// <summary>
  /// Optional additional visuals (decorations, animations, etc.).
  /// </summary>
  public IReadOnlyList<Control>? AdditionalVisuals { get; init; }
}
