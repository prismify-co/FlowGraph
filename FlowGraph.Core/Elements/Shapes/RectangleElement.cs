namespace FlowGraph.Core.Elements.Shapes;

/// <summary>
/// A rectangle shape element that can be rendered on the canvas.
/// </summary>
/// <remarks>
/// <para>
/// Rectangles are one of the most common shape elements, useful for:
/// - Swimlanes in process diagrams
/// - Containers/groups without the node behavior
/// - Annotations and labels
/// - Background regions
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a swimlane background
/// var swimlane = new RectangleElement("swimlane-1")
/// {
///     Position = new Point(0, 0),
///     Width = 300,
///     Height = 600,
///     Fill = "#ecf0f1",
///     Stroke = "#bdc3c7",
///     StrokeWidth = 2,
///     CornerRadius = 4,
///     Label = "Development"
/// };
/// </code>
/// </example>
public class RectangleElement : ShapeElement
{
  private double _cornerRadius;

  /// <summary>
  /// Creates a new rectangle element with a generated ID.
  /// </summary>
  public RectangleElement() : this(Guid.NewGuid().ToString())
  {
  }

  /// <summary>
  /// Creates a new rectangle element with the specified ID.
  /// </summary>
  /// <param name="id">The unique identifier for this rectangle.</param>
  public RectangleElement(string id) : base(id)
  {
  }

  /// <inheritdoc />
  public override string Type => "rectangle";

  /// <summary>
  /// Gets or sets the corner radius for rounded rectangles.
  /// A value of 0 creates sharp corners.
  /// </summary>
  public double CornerRadius
  {
    get => _cornerRadius;
    set
    {
      if (_cornerRadius != value)
      {
        _cornerRadius = Math.Max(0, value);
        OnPropertyChanged(nameof(CornerRadius));
      }
    }
  }
}
