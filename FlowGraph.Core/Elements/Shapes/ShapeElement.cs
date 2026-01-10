using System.ComponentModel;

namespace FlowGraph.Core.Elements.Shapes;

/// <summary>
/// Base class for shape elements that can be rendered on the canvas.
/// Shapes are arbitrary visual elements that are not nodes or edges.
/// </summary>
/// <remarks>
/// <para>
/// Shapes are the core building block for the canvas-first architecture.
/// They allow rendering arbitrary content like annotations, swimlanes,
/// diagrams, and other non-graph elements on the infinite canvas.
/// </para>
/// <para>
/// Shapes have a default Z-index of <see cref="CanvasElement.ZIndexShapes"/> (100),
/// placing them above backgrounds but below edges and nodes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a rectangle shape
/// var rect = new RectangleElement("rect-1")
/// {
///     Position = new Point(100, 100),
///     Width = 200,
///     Height = 100,
///     Fill = "#3498db",
///     Stroke = "#2980b9"
/// };
/// graph.AddElement(rect);
/// </code>
/// </example>
public abstract class ShapeElement : CanvasElement
{
  private string? _fill;
  private string? _stroke;
  private double _strokeWidth = 1.0;
  private double _opacity = 1.0;
  private double _rotation;
  private string? _label;

  /// <summary>
  /// Creates a new shape element with the specified ID.
  /// </summary>
  /// <param name="id">The unique identifier for this shape.</param>
  protected ShapeElement(string id) : base(id)
  {
    ZIndex = ZIndexShapes;
  }

  /// <summary>
  /// Gets or sets the fill color of the shape.
  /// </summary>
  /// <remarks>
  /// Color can be specified as hex (#RRGGBB or #AARRGGBB), named color, or null for no fill.
  /// </remarks>
  public string? Fill
  {
    get => _fill;
    set
    {
      if (_fill != value)
      {
        _fill = value;
        OnPropertyChanged(nameof(Fill));
      }
    }
  }

  /// <summary>
  /// Gets or sets the stroke (border) color of the shape.
  /// </summary>
  /// <remarks>
  /// Color can be specified as hex (#RRGGBB or #AARRGGBB), named color, or null for no stroke.
  /// </remarks>
  public string? Stroke
  {
    get => _stroke;
    set
    {
      if (_stroke != value)
      {
        _stroke = value;
        OnPropertyChanged(nameof(Stroke));
      }
    }
  }

  /// <summary>
  /// Gets or sets the stroke width in pixels.
  /// </summary>
  public double StrokeWidth
  {
    get => _strokeWidth;
    set
    {
      if (_strokeWidth != value)
      {
        _strokeWidth = value;
        OnPropertyChanged(nameof(StrokeWidth));
      }
    }
  }

  /// <summary>
  /// Gets or sets the opacity of the shape (0.0 to 1.0).
  /// </summary>
  public double Opacity
  {
    get => _opacity;
    set
    {
      if (_opacity != value)
      {
        _opacity = Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(Opacity));
      }
    }
  }

  /// <summary>
  /// Gets or sets the rotation angle in degrees.
  /// </summary>
  public double Rotation
  {
    get => _rotation;
    set
    {
      if (_rotation != value)
      {
        _rotation = value;
        OnPropertyChanged(nameof(Rotation));
      }
    }
  }

  /// <summary>
  /// Gets or sets an optional label to display on or near the shape.
  /// </summary>
  public string? Label
  {
    get => _label;
    set
    {
      if (_label != value)
      {
        _label = value;
        OnPropertyChanged(nameof(Label));
      }
    }
  }
}
