namespace FlowGraph.Core.Models;

/// <summary>
/// Default implementation of <see cref="INodeState"/> using <see cref="ObservableBase"/>.
/// Provides framework-agnostic property change notification with no external dependencies.
/// </summary>
/// <remarks>
/// For Avalonia applications, consider using ObservableNodeState from FlowGraph.Avalonia
/// which uses source generators for better performance.
/// </remarks>
public class NodeState : ObservableBase, INodeState
{
  private double _x;
  private double _y;
  private double? _width;
  private double? _height;
  private bool _isSelected;
  private bool _isDragging;
  private bool _isCollapsed;
  private bool _isVisible = true;
  private int _zIndex = Elements.CanvasElement.ZIndexNodes;

  /// <inheritdoc />
  public double X
  {
    get => _x;
    set => SetField(ref _x, value);
  }

  /// <inheritdoc />
  public double Y
  {
    get => _y;
    set => SetField(ref _y, value);
  }

  /// <inheritdoc />
  public double? Width
  {
    get => _width;
    set => SetField(ref _width, value);
  }

  /// <inheritdoc />
  public double? Height
  {
    get => _height;
    set => SetField(ref _height, value);
  }

  /// <inheritdoc />
  public bool IsSelected
  {
    get => _isSelected;
    set => SetField(ref _isSelected, value);
  }

  /// <inheritdoc />
  public bool IsDragging
  {
    get => _isDragging;
    set => SetField(ref _isDragging, value);
  }

  /// <inheritdoc />
  public bool IsCollapsed
  {
    get => _isCollapsed;
    set => SetField(ref _isCollapsed, value);
  }

  /// <inheritdoc />
  public bool IsVisible
  {
    get => _isVisible;
    set => SetField(ref _isVisible, value);
  }

  /// <inheritdoc />
  public int ZIndex
  {
    get => _zIndex;
    set => SetField(ref _zIndex, value);
  }

  /// <summary>
  /// Creates a copy of this state.
  /// </summary>
  public NodeState Clone() => new()
  {
    X = X,
    Y = Y,
    Width = Width,
    Height = Height,
    IsSelected = IsSelected,
    IsDragging = IsDragging,
    IsCollapsed = IsCollapsed,
    IsVisible = IsVisible,
    ZIndex = ZIndex
  };

  /// <summary>
  /// Copies values from another state instance.
  /// </summary>
  public void CopyFrom(INodeState other)
  {
    X = other.X;
    Y = other.Y;
    Width = other.Width;
    Height = other.Height;
    IsSelected = other.IsSelected;
    IsDragging = other.IsDragging;
    IsCollapsed = other.IsCollapsed;
    IsVisible = other.IsVisible;
    ZIndex = other.ZIndex;
  }
}
