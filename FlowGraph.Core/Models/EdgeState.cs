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
    set => SetField(ref _waypoints, value);
  }

  /// <summary>
  /// Creates a copy of this state.
  /// </summary>
  public EdgeState Clone() => new()
  {
    IsSelected = IsSelected,
    Waypoints = Waypoints?.ToList()
  };

  /// <summary>
  /// Copies values from another state instance.
  /// </summary>
  public void CopyFrom(IEdgeState other)
  {
    IsSelected = other.IsSelected;
    Waypoints = other.Waypoints?.ToList();
  }

  /// <summary>
  /// Sets waypoints from a list of points.
  /// </summary>
  public void SetWaypoints(IEnumerable<Point>? points)
  {
    Waypoints = points?.ToList();
  }

  /// <summary>
  /// Clears all waypoints.
  /// </summary>
  public void ClearWaypoints()
  {
    Waypoints = null;
  }
}
