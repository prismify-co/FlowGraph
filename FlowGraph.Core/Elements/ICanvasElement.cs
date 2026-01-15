using System.ComponentModel;

namespace FlowGraph.Core.Elements;

/// <summary>
/// Base interface for all elements that can be placed on the canvas.
/// Represents any positioned, sized, and selectable content in the coordinate system.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the contract for all visual elements in FlowGraph,
/// including nodes, edges, shapes, annotations, and any custom element types.
/// </para>
/// <para>
/// The coordinate system uses canvas coordinates (not screen coordinates).
/// Use <c>FlowCanvas.CanvasToScreen</c> and <c>FlowCanvas.ScreenToCanvas</c> for conversions.
/// </para>
/// </remarks>
public interface ICanvasElement : INotifyPropertyChanged
{
  /// <summary>
  /// Gets the unique identifier for this element.
  /// </summary>
  /// <remarks>
  /// IDs are immutable after creation and used for lookups, serialization,
  /// and change tracking.
  /// </remarks>
  string Id { get; }

  /// <summary>
  /// Gets the type identifier for this element.
  /// </summary>
  /// <remarks>
  /// Used by renderer registries to select the appropriate renderer.
  /// For nodes, this typically comes from the node definition's Type property.
  /// For shapes, this identifies the shape type (line, rectangle, text, etc.).
  /// </remarks>
  string Type { get; }

  /// <summary>
  /// Gets or sets the position of this element in canvas coordinates.
  /// </summary>
  /// <remarks>
  /// The position typically represents the top-left corner of the element's
  /// bounding box, but this may vary by element type.
  /// </remarks>
  Point Position { get; set; }

  /// <summary>
  /// Gets or sets the width of this element, if applicable.
  /// </summary>
  /// <remarks>
  /// Some elements (like edges) may not have a meaningful width.
  /// Returns null if the element has no explicit width.
  /// </remarks>
  double? Width { get; set; }

  /// <summary>
  /// Gets or sets the height of this element, if applicable.
  /// </summary>
  /// <remarks>
  /// Some elements (like edges) may not have a meaningful height.
  /// Returns null if the element has no explicit height.
  /// </remarks>
  double? Height { get; set; }

  /// <summary>
  /// Gets or sets whether this element is currently selected.
  /// </summary>
  bool IsSelected { get; set; }

  /// <summary>
  /// Gets or sets whether this element can be selected by user interaction.
  /// </summary>
  /// <remarks>
  /// When false, clicking on the element will not select it.
  /// </remarks>
  bool IsSelectable { get; }

  /// <summary>
  /// Gets or sets whether this element is visible.
  /// </summary>
  /// <remarks>
  /// Invisible elements are not rendered but remain in the element collection.
  /// </remarks>
  bool IsVisible { get; }

  /// <summary>
  /// Gets the Z-index for rendering order.
  /// </summary>
  /// <remarks>
  /// Higher values render on top. Default values by type:
  /// <list type="bullet">
  /// <item>Backgrounds: 0</item>
  /// <item>Shapes: 100</item>
  /// <item>Edges: 200</item>
  /// <item>Nodes: 300</item>
  /// </list>
  /// </remarks>
  int ZIndex { get; }

  /// <summary>
  /// Gets the bounding rectangle of this element in canvas coordinates.
  /// </summary>
  /// <returns>The bounds as a <see cref="Rect"/>.</returns>
  Rect GetBounds();
}
