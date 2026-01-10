namespace FlowGraph.Core.Elements.Shapes;

/// <summary>
/// A line shape element that connects two points on the canvas.
/// </summary>
/// <remarks>
/// <para>
/// Lines are useful for:
/// - Sequence diagram lifelines
/// - Swimlane separators
/// - Custom connectors that aren't edges
/// - Diagram annotations
/// </para>
/// <para>
/// Unlike edges, lines don't connect nodes and don't have routing logic.
/// They simply draw from (X, Y) to (EndX, EndY).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a vertical lifeline for sequence diagram
/// var lifeline = new LineElement("lifeline-1")
/// {
///     Position = new Point(100, 50),  // Start point
///     EndX = 100,
///     EndY = 500,
///     Stroke = "#34495e",
///     StrokeWidth = 2,
///     StrokeDashArray = "5,5"  // Dashed line
/// };
/// </code>
/// </example>
public class LineElement : ShapeElement
{
  private double _endX;
  private double _endY;
  private string? _strokeDashArray;
  private LineCapStyle _startCap = LineCapStyle.None;
  private LineCapStyle _endCap = LineCapStyle.None;

  /// <summary>
  /// Creates a new line element with a generated ID.
  /// </summary>
  public LineElement() : this(Guid.NewGuid().ToString())
  {
  }

  /// <summary>
  /// Creates a new line element with the specified ID.
  /// </summary>
  /// <param name="id">The unique identifier for this line.</param>
  public LineElement(string id) : base(id)
  {
  }

  /// <inheritdoc />
  public override string Type => "line";

  /// <summary>
  /// Gets or sets the X coordinate of the end point.
  /// The start point is defined by <see cref="CanvasElement.Position"/>.
  /// </summary>
  public double EndX
  {
    get => _endX;
    set
    {
      if (_endX != value)
      {
        _endX = value;
        OnPropertyChanged(nameof(EndX));
        OnBoundsChanged();
      }
    }
  }

  /// <summary>
  /// Gets or sets the Y coordinate of the end point.
  /// The start point is defined by <see cref="CanvasElement.Position"/>.
  /// </summary>
  public double EndY
  {
    get => _endY;
    set
    {
      if (_endY != value)
      {
        _endY = value;
        OnPropertyChanged(nameof(EndY));
        OnBoundsChanged();
      }
    }
  }

  /// <summary>
  /// Gets or sets the dash pattern for the stroke.
  /// Format: "dash,gap,dash,gap" e.g., "5,3" or "10,5,2,5".
  /// Null for solid line.
  /// </summary>
  public string? StrokeDashArray
  {
    get => _strokeDashArray;
    set
    {
      if (_strokeDashArray != value)
      {
        _strokeDashArray = value;
        OnPropertyChanged(nameof(StrokeDashArray));
      }
    }
  }

  /// <summary>
  /// Gets or sets the cap style for the start of the line.
  /// </summary>
  public LineCapStyle StartCap
  {
    get => _startCap;
    set
    {
      if (_startCap != value)
      {
        _startCap = value;
        OnPropertyChanged(nameof(StartCap));
      }
    }
  }

  /// <summary>
  /// Gets or sets the cap style for the end of the line.
  /// </summary>
  public LineCapStyle EndCap
  {
    get => _endCap;
    set
    {
      if (_endCap != value)
      {
        _endCap = value;
        OnPropertyChanged(nameof(EndCap));
      }
    }
  }

  /// <inheritdoc />
  public override Rect GetBounds()
  {
    var minX = Math.Min(Position.X, EndX);
    var minY = Math.Min(Position.Y, EndY);
    var maxX = Math.Max(Position.X, EndX);
    var maxY = Math.Max(Position.Y, EndY);

    return new Rect(minX, minY, maxX - minX, maxY - minY);
  }
}

/// <summary>
/// Defines the cap styles for line endpoints.
/// </summary>
public enum LineCapStyle
{
  /// <summary>No cap - line ends at the point.</summary>
  None,

  /// <summary>Flat cap extends half the line width beyond the endpoint.</summary>
  Flat,

  /// <summary>Round cap creates a semicircle at the endpoint.</summary>
  Round,

  /// <summary>Arrow cap creates an arrowhead at the endpoint.</summary>
  Arrow,

  /// <summary>Diamond cap creates a diamond shape at the endpoint.</summary>
  Diamond,

  /// <summary>Circle cap creates a filled circle at the endpoint.</summary>
  Circle
}
