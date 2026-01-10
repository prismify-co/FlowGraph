using System.ComponentModel;

namespace FlowGraph.Core.Elements;

/// <summary>
/// Base class for all elements that can be placed on the canvas.
/// Provides common properties for positioning, sizing, selection, and visibility.
/// </summary>
/// <remarks>
/// <para>
/// This is the foundation of the canvas-first architecture. All visual elements
/// (nodes, edges, shapes, annotations) extend this class.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a custom element
/// public class MyCustomElement : CanvasElement
/// {
///     public override string Type => "custom";
///     
///     public MyCustomElement() : base(Guid.NewGuid().ToString()) { }
/// }
/// </code>
/// </example>
public abstract class CanvasElement : ICanvasElement
{
  private Point _position;
  private double? _width;
  private double? _height;
  private bool _isSelected;
  private bool _isVisible = true;
  private int _zIndex;

  /// <summary>
  /// Default Z-index for background elements.
  /// </summary>
  public const int ZIndexBackground = 0;

  /// <summary>
  /// Default Z-index for shape elements.
  /// </summary>
  public const int ZIndexShapes = 100;

  /// <summary>
  /// Default Z-index for edge elements.
  /// </summary>
  public const int ZIndexEdges = 200;

  /// <summary>
  /// Default Z-index for node elements.
  /// </summary>
  public const int ZIndexNodes = 300;

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;

  /// <summary>
  /// Creates a new canvas element with the specified ID.
  /// </summary>
  /// <param name="id">The unique identifier for this element.</param>
  protected CanvasElement(string id)
  {
    Id = id ?? throw new ArgumentNullException(nameof(id));
  }

  /// <inheritdoc />
  public string Id { get; }

  /// <inheritdoc />
  public abstract string Type { get; }

  /// <inheritdoc />
  public Point Position
  {
    get => _position;
    set
    {
      if (_position != value)
      {
        _position = value;
        OnPropertyChanged(nameof(Position));
        OnBoundsChanged();
      }
    }
  }

  /// <inheritdoc />
  public double? Width
  {
    get => _width;
    set
    {
      if (_width != value)
      {
        _width = value;
        OnPropertyChanged(nameof(Width));
        OnBoundsChanged();
      }
    }
  }

  /// <inheritdoc />
  public double? Height
  {
    get => _height;
    set
    {
      if (_height != value)
      {
        _height = value;
        OnPropertyChanged(nameof(Height));
        OnBoundsChanged();
      }
    }
  }

  /// <inheritdoc />
  public bool IsSelected
  {
    get => _isSelected;
    set
    {
      if (_isSelected != value)
      {
        _isSelected = value;
        OnPropertyChanged(nameof(IsSelected));
      }
    }
  }

  /// <inheritdoc />
  public bool IsVisible
  {
    get => _isVisible;
    set
    {
      if (_isVisible != value)
      {
        _isVisible = value;
        OnPropertyChanged(nameof(IsVisible));
      }
    }
  }

  /// <inheritdoc />
  public int ZIndex
  {
    get => _zIndex;
    set
    {
      if (_zIndex != value)
      {
        _zIndex = value;
        OnPropertyChanged(nameof(ZIndex));
      }
    }
  }

  /// <inheritdoc />
  public virtual Rect GetBounds()
  {
    return new Rect(
        Position.X,
        Position.Y,
        Width ?? 0,
        Height ?? 0);
  }

  /// <summary>
  /// Called when any bounds-related property changes.
  /// Override to respond to bounds changes.
  /// </summary>
  protected virtual void OnBoundsChanged()
  {
    // Derived classes can override for custom behavior
  }

  /// <summary>
  /// Raises the PropertyChanged event.
  /// </summary>
  /// <param name="propertyName">Name of the property that changed.</param>
  protected virtual void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
