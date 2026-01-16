namespace FlowGraph.Core.Elements.Shapes;

/// <summary>
/// A sticky note style comment element for annotations on the canvas.
/// </summary>
/// <remarks>
/// <para>
/// CommentElement provides a styled annotation with:
/// - Background fill with optional transparency
/// - Corner radius for rounded appearance
/// - Padding for text inset
/// - Optional shadow effect
/// - Resizable bounds
/// </para>
/// <para>
/// By default, comments render in a foreground layer (above nodes) using
/// <see cref="ZIndexComments"/> (400), making them ideal for overlay annotations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a sticky note comment
/// var comment = new CommentElement("comment-1")
/// {
///     Position = new Point(200, 150),
///     Width = 200,
///     Height = 100,
///     Text = "TODO: Refactor this section",
///     BackgroundColor = "#FFF9C4", // Light yellow
///     TextColor = "#5D4037",
///     CornerRadius = 4
/// };
/// graph.AddElement(comment);
/// 
/// // Create a warning annotation
/// var warning = new CommentElement("warning-1")
/// {
///     Position = new Point(400, 200),
///     Text = "⚠️ Deprecated API",
///     BackgroundColor = "#FFCDD2", // Light red
///     TextColor = "#C62828",
///     ShowShadow = true
/// };
/// </code>
/// </example>
public class CommentElement : ShapeElement
{
  /// <summary>
  /// Default Z-index for comment elements (above nodes).
  /// </summary>
  public const int ZIndexComments = 400;

  private string _text = string.Empty;
  private string _backgroundColor = "#FFF9C4"; // Default: light yellow (sticky note)
  private string _textColor = "#5D4037"; // Default: brown text
  private string _borderColor = "#FBC02D"; // Default: darker yellow border
  private double _fontSize = 14;
  private string _fontFamily = "Segoe UI";
  private FontWeight _fontWeight = FontWeight.Normal;
  private double _padding = 12;
  private double _cornerRadius = 4;
  private bool _showShadow = true;
  private double _shadowBlur = 4;
  private double _shadowOffsetX = 2;
  private double _shadowOffsetY = 2;
  private string _shadowColor = "#40000000"; // Semi-transparent black
  private bool _isEditing;
  private CommentStyle _style = CommentStyle.StickyNote;

  /// <summary>
  /// Creates a new comment element with a generated ID.
  /// </summary>
  public CommentElement() : this(Guid.NewGuid().ToString())
  {
  }

  /// <summary>
  /// Creates a new comment element with the specified ID.
  /// </summary>
  /// <param name="id">The unique identifier for this comment.</param>
  public CommentElement(string id) : base(id)
  {
    ZIndex = ZIndexComments;
    Width = 200;
    Height = 100;
  }

  /// <inheritdoc />
  public override string Type => "comment";

  /// <summary>
  /// Gets or sets the text content of the comment.
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
      }
    }
  }

  /// <summary>
  /// Gets or sets the background color of the comment.
  /// </summary>
  /// <remarks>
  /// Common sticky note colors:
  /// - Yellow: #FFF9C4
  /// - Blue: #BBDEFB
  /// - Green: #C8E6C9
  /// - Pink: #F8BBD9
  /// - Orange: #FFE0B2
  /// </remarks>
  public string BackgroundColor
  {
    get => _backgroundColor;
    set
    {
      if (_backgroundColor != value)
      {
        _backgroundColor = value ?? "#FFF9C4";
        OnPropertyChanged(nameof(BackgroundColor));
      }
    }
  }

  /// <summary>
  /// Gets or sets the text color.
  /// </summary>
  public string TextColor
  {
    get => _textColor;
    set
    {
      if (_textColor != value)
      {
        _textColor = value ?? "#5D4037";
        OnPropertyChanged(nameof(TextColor));
      }
    }
  }

  /// <summary>
  /// Gets or sets the border color.
  /// </summary>
  public string BorderColor
  {
    get => _borderColor;
    set
    {
      if (_borderColor != value)
      {
        _borderColor = value ?? "#FBC02D";
        OnPropertyChanged(nameof(BorderColor));
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
      }
    }
  }

  /// <summary>
  /// Gets or sets the padding between the border and text.
  /// </summary>
  public double Padding
  {
    get => _padding;
    set
    {
      if (_padding != value)
      {
        _padding = Math.Max(0, value);
        OnPropertyChanged(nameof(Padding));
      }
    }
  }

  /// <summary>
  /// Gets or sets the corner radius for rounded corners.
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

  /// <summary>
  /// Gets or sets whether to show a drop shadow.
  /// </summary>
  public bool ShowShadow
  {
    get => _showShadow;
    set
    {
      if (_showShadow != value)
      {
        _showShadow = value;
        OnPropertyChanged(nameof(ShowShadow));
      }
    }
  }

  /// <summary>
  /// Gets or sets the shadow blur radius.
  /// </summary>
  public double ShadowBlur
  {
    get => _shadowBlur;
    set
    {
      if (_shadowBlur != value)
      {
        _shadowBlur = Math.Max(0, value);
        OnPropertyChanged(nameof(ShadowBlur));
      }
    }
  }

  /// <summary>
  /// Gets or sets the shadow X offset.
  /// </summary>
  public double ShadowOffsetX
  {
    get => _shadowOffsetX;
    set
    {
      if (_shadowOffsetX != value)
      {
        _shadowOffsetX = value;
        OnPropertyChanged(nameof(ShadowOffsetX));
      }
    }
  }

  /// <summary>
  /// Gets or sets the shadow Y offset.
  /// </summary>
  public double ShadowOffsetY
  {
    get => _shadowOffsetY;
    set
    {
      if (_shadowOffsetY != value)
      {
        _shadowOffsetY = value;
        OnPropertyChanged(nameof(ShadowOffsetY));
      }
    }
  }

  /// <summary>
  /// Gets or sets the shadow color.
  /// </summary>
  public string ShadowColor
  {
    get => _shadowColor;
    set
    {
      if (_shadowColor != value)
      {
        _shadowColor = value ?? "#40000000";
        OnPropertyChanged(nameof(ShadowColor));
      }
    }
  }

  /// <summary>
  /// Gets or sets whether the comment is currently being edited.
  /// </summary>
  /// <remarks>
  /// When true, the renderer should show an editable text area.
  /// </remarks>
  public bool IsEditing
  {
    get => _isEditing;
    set
    {
      if (_isEditing != value)
      {
        _isEditing = value;
        OnPropertyChanged(nameof(IsEditing));
      }
    }
  }

  /// <summary>
  /// Gets or sets the visual style of the comment.
  /// </summary>
  public CommentStyle Style
  {
    get => _style;
    set
    {
      if (_style != value)
      {
        _style = value;
        ApplyStyleDefaults();
        OnPropertyChanged(nameof(Style));
      }
    }
  }

  /// <summary>
  /// Applies default colors based on the current style.
  /// </summary>
  private void ApplyStyleDefaults()
  {
    switch (_style)
    {
      case CommentStyle.StickyNote:
        _backgroundColor = "#FFF9C4";
        _textColor = "#5D4037";
        _borderColor = "#FBC02D";
        break;
      case CommentStyle.Info:
        _backgroundColor = "#E3F2FD";
        _textColor = "#1565C0";
        _borderColor = "#42A5F5";
        break;
      case CommentStyle.Warning:
        _backgroundColor = "#FFF3E0";
        _textColor = "#E65100";
        _borderColor = "#FF9800";
        break;
      case CommentStyle.Error:
        _backgroundColor = "#FFEBEE";
        _textColor = "#C62828";
        _borderColor = "#EF5350";
        break;
      case CommentStyle.Success:
        _backgroundColor = "#E8F5E9";
        _textColor = "#2E7D32";
        _borderColor = "#66BB6A";
        break;
      case CommentStyle.Plain:
        _backgroundColor = "#FFFFFF";
        _textColor = "#212121";
        _borderColor = "#BDBDBD";
        break;
    }
  }

  /// <summary>
  /// Creates a copy of this comment element with a new ID.
  /// </summary>
  /// <returns>A new CommentElement with copied properties.</returns>
  public CommentElement Clone()
  {
    return new CommentElement(Guid.NewGuid().ToString())
    {
      Position = Position,
      Width = Width,
      Height = Height,
      Text = Text,
      BackgroundColor = BackgroundColor,
      TextColor = TextColor,
      BorderColor = BorderColor,
      FontSize = FontSize,
      FontFamily = FontFamily,
      FontWeight = FontWeight,
      Padding = Padding,
      CornerRadius = CornerRadius,
      ShowShadow = ShowShadow,
      ShadowBlur = ShadowBlur,
      ShadowOffsetX = ShadowOffsetX,
      ShadowOffsetY = ShadowOffsetY,
      ShadowColor = ShadowColor,
      Style = Style,
      ZIndex = ZIndex,
      IsVisible = IsVisible,
      IsSelectable = IsSelectable
    };
  }
}

/// <summary>
/// Predefined visual styles for comment elements.
/// </summary>
public enum CommentStyle
{
  /// <summary>
  /// Classic yellow sticky note appearance.
  /// </summary>
  StickyNote,

  /// <summary>
  /// Blue informational style.
  /// </summary>
  Info,

  /// <summary>
  /// Orange warning style.
  /// </summary>
  Warning,

  /// <summary>
  /// Red error/alert style.
  /// </summary>
  Error,

  /// <summary>
  /// Green success style.
  /// </summary>
  Success,

  /// <summary>
  /// Plain white/gray minimal style.
  /// </summary>
  Plain
}
