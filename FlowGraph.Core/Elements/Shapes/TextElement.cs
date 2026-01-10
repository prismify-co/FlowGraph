namespace FlowGraph.Core.Elements.Shapes;

/// <summary>
/// A text shape element for displaying standalone text on the canvas.
/// </summary>
/// <remarks>
/// <para>
/// TextElement is useful for:
/// - Annotations and comments
/// - Diagram titles and labels
/// - Swimlane headers
/// - Free-form notes
/// </para>
/// <para>
/// Unlike node labels, TextElement exists independently and can be positioned
/// anywhere on the canvas without being attached to a node.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a diagram title
/// var title = new TextElement("title-1")
/// {
///     Position = new Point(50, 20),
///     Text = "System Architecture",
///     FontSize = 24,
///     FontWeight = FontWeight.Bold,
///     Fill = "#2c3e50"
/// };
/// 
/// // Create an annotation
/// var note = new TextElement("note-1")
/// {
///     Position = new Point(200, 300),
///     Text = "TODO: Add error handling",
///     FontSize = 12,
///     FontStyle = FontStyle.Italic,
///     Fill = "#e74c3c"
/// };
/// </code>
/// </example>
public class TextElement : ShapeElement
{
  private string _text = string.Empty;
  private double _fontSize = 14;
  private string _fontFamily = "Segoe UI";
  private FontWeight _fontWeight = FontWeight.Normal;
  private FontStyle _fontStyle = FontStyle.Normal;
  private TextAlignment _textAlignment = TextAlignment.Left;
  private double? _maxWidth;

  /// <summary>
  /// Creates a new text element with a generated ID.
  /// </summary>
  public TextElement() : this(Guid.NewGuid().ToString())
  {
  }

  /// <summary>
  /// Creates a new text element with the specified ID.
  /// </summary>
  /// <param name="id">The unique identifier for this text element.</param>
  public TextElement(string id) : base(id)
  {
  }

  /// <inheritdoc />
  public override string Type => "text";

  /// <summary>
  /// Gets or sets the text content to display.
  /// </summary>
  public string Text
  {
    get => _text;
    set
    {
      if (_text != value)
      {
        _text = value ?? string.Empty;
        OnPropertyChanged(nameof(Text));
        OnBoundsChanged();
      }
    }
  }

  /// <summary>
  /// Gets or sets the font size in points.
  /// </summary>
  public double FontSize
  {
    get => _fontSize;
    set
    {
      if (_fontSize != value)
      {
        _fontSize = Math.Max(1, value);
        OnPropertyChanged(nameof(FontSize));
        OnBoundsChanged();
      }
    }
  }

  /// <summary>
  /// Gets or sets the font family name.
  /// </summary>
  public string FontFamily
  {
    get => _fontFamily;
    set
    {
      if (_fontFamily != value)
      {
        _fontFamily = value ?? "Segoe UI";
        OnPropertyChanged(nameof(FontFamily));
        OnBoundsChanged();
      }
    }
  }

  /// <summary>
  /// Gets or sets the font weight.
  /// </summary>
  public FontWeight FontWeight
  {
    get => _fontWeight;
    set
    {
      if (_fontWeight != value)
      {
        _fontWeight = value;
        OnPropertyChanged(nameof(FontWeight));
        OnBoundsChanged();
      }
    }
  }

  /// <summary>
  /// Gets or sets the font style (normal, italic, etc.).
  /// </summary>
  public FontStyle FontStyle
  {
    get => _fontStyle;
    set
    {
      if (_fontStyle != value)
      {
        _fontStyle = value;
        OnPropertyChanged(nameof(FontStyle));
      }
    }
  }

  /// <summary>
  /// Gets or sets the text alignment within the element bounds.
  /// </summary>
  public TextAlignment TextAlignment
  {
    get => _textAlignment;
    set
    {
      if (_textAlignment != value)
      {
        _textAlignment = value;
        OnPropertyChanged(nameof(TextAlignment));
      }
    }
  }

  /// <summary>
  /// Gets or sets the maximum width for text wrapping.
  /// Null means no wrapping - text extends as needed.
  /// </summary>
  public double? MaxWidth
  {
    get => _maxWidth;
    set
    {
      if (_maxWidth != value)
      {
        _maxWidth = value;
        OnPropertyChanged(nameof(MaxWidth));
        OnBoundsChanged();
      }
    }
  }
}

/// <summary>
/// Defines font weight values.
/// </summary>
public enum FontWeight
{
  /// <summary>Thin weight (100).</summary>
  Thin,

  /// <summary>Light weight (300).</summary>
  Light,

  /// <summary>Normal/regular weight (400).</summary>
  Normal,

  /// <summary>Medium weight (500).</summary>
  Medium,

  /// <summary>Semi-bold weight (600).</summary>
  SemiBold,

  /// <summary>Bold weight (700).</summary>
  Bold,

  /// <summary>Extra bold weight (800).</summary>
  ExtraBold,

  /// <summary>Black/heavy weight (900).</summary>
  Black
}

/// <summary>
/// Defines font style values.
/// </summary>
public enum FontStyle
{
  /// <summary>Normal (upright) style.</summary>
  Normal,

  /// <summary>Italic style.</summary>
  Italic,

  /// <summary>Oblique style.</summary>
  Oblique
}

/// <summary>
/// Defines text alignment values.
/// </summary>
public enum TextAlignment
{
  /// <summary>Left-aligned text.</summary>
  Left,

  /// <summary>Center-aligned text.</summary>
  Center,

  /// <summary>Right-aligned text.</summary>
  Right,

  /// <summary>Justified text (aligned to both left and right margins).</summary>
  Justify
}
