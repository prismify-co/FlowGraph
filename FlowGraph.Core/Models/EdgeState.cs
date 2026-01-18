namespace FlowGraph.Core.Models;

/// <summary>
/// Default implementation of <see cref="IEdgeState"/> using <see cref="ObservableBase"/>.
/// Provides framework-agnostic property change notification with no external dependencies.
/// </summary>
/// <remarks>
/// For Avalonia applications, consider using ObservableEdgeState from FlowGraph.Avalonia
/// which uses source generators for better performance.
/// </remarks>
public class EdgeState : ObservableBase, IEdgeState
{
  private bool _isSelected;
  private IReadOnlyList<Point>? _waypoints;
  private IReadOnlyList<Point>? _userWaypoints;
  private bool _isVisible = true;
  private int _zIndex = Elements.CanvasElement.ZIndexEdges;

  /// <inheritdoc />
  public bool IsSelected
  {
    get => _isSelected;
    set => SetField(ref _isSelected, value);
  }

  /// <inheritdoc />
  public IReadOnlyList<Point>? Waypoints
  {
    get => _waypoints;
    set
    {
      // Always update and notify for waypoints - list contents may differ even if reference equality passes
      _waypoints = value;
      OnPropertyChanged();
    }
  }

  /// <inheritdoc />
  public IReadOnlyList<Point>? UserWaypoints
  {
    get => _userWaypoints;
    set => SetField(ref _userWaypoints, value);
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
  public EdgeState Clone() => new()
  {
    IsSelected = IsSelected,
    Waypoints = Waypoints?.ToList(),
    UserWaypoints = UserWaypoints?.ToList(),
    IsVisible = IsVisible,
    ZIndex = ZIndex
  };

  /// <summary>
  /// Copies values from another state instance.
  /// </summary>
  public void CopyFrom(IEdgeState other)
  {
    IsSelected = other.IsSelected;
    Waypoints = other.Waypoints?.ToList();
    UserWaypoints = other.UserWaypoints?.ToList();
    IsVisible = other.IsVisible;
    ZIndex = other.ZIndex;
  }

  /// <summary>
  /// Sets waypoints from a list of points.
  /// </summary>
  public void SetWaypoints(IEnumerable<Point>? points)
  {
    Waypoints = points?.ToList();
  }

  /// <summary>
  /// Sets user waypoints from a list of points.
  /// </summary>
  public void SetUserWaypoints(IEnumerable<Point>? points)
  {
    UserWaypoints = points?.ToList();
  }

  /// <summary>
  /// Clears all waypoints (both computed and user-defined).
  /// </summary>
  public void ClearWaypoints()
  {
    Waypoints = null;
  }

  /// <summary>
  /// Clears user-defined waypoints.
  /// </summary>
  public void ClearUserWaypoints()
  {
    UserWaypoints = null;
  }
}
