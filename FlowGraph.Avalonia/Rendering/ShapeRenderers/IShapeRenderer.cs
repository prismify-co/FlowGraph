using Avalonia.Controls;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Interface for custom shape renderers.
/// Implement this interface to create visual representations for shape element types.
/// </summary>
/// <remarks>
/// <para>
/// Shape renderers follow the same pattern as node and edge renderers,
/// allowing consistent extensibility for all canvas elements.
/// </para>
/// <para>
/// Register implementations with <see cref="ShapeRendererRegistry"/> to handle
/// specific shape types (rectangle, line, text, ellipse, etc.).
/// </para>
/// </remarks>
public interface IShapeRenderer
{
  /// <summary>
  /// Creates the visual representation for a shape element.
  /// </summary>
  /// <param name="shape">The shape element to render.</param>
  /// <param name="context">The rendering context with theme, scale, and settings.</param>
  /// <returns>A Control representing the shape visual.</returns>
  Control CreateShapeVisual(ShapeElement shape, ShapeRenderContext context);

  /// <summary>
  /// Updates an existing visual when the shape's properties change.
  /// </summary>
  /// <param name="visual">The shape's visual control.</param>
  /// <param name="shape">The shape element with updated data.</param>
  /// <param name="context">The rendering context.</param>
  void UpdateVisual(Control visual, ShapeElement shape, ShapeRenderContext context);

  /// <summary>
  /// Updates the visual state when selection changes.
  /// </summary>
  /// <param name="visual">The shape's visual control.</param>
  /// <param name="shape">The shape element.</param>
  /// <param name="context">The rendering context.</param>
  void UpdateSelection(Control visual, ShapeElement shape, ShapeRenderContext context);
}
