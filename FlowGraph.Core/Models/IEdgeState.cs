using System.ComponentModel;

namespace FlowGraph.Core.Models;

/// <summary>
/// Interface for the mutable runtime state of an edge.
/// Separates selection and routing state from the immutable definition.
/// </summary>
/// <remarks>
/// Implementations must raise <see cref="INotifyPropertyChanged.PropertyChanged"/> 
/// when any property changes to support data binding.
/// 
/// Default implementation: <see cref="EdgeState"/>
/// Avalonia-optimized: FlowGraph.Avalonia.Binding.ObservableEdgeState
/// </remarks>
public interface IEdgeState : INotifyPropertyChanged
{
  /// <summary>
  /// Whether this edge is currently selected.
  /// </summary>
  bool IsSelected { get; set; }

  /// <summary>
  /// Computed waypoints for edge rendering (set by router).
  /// When set, the edge will pass through these intermediate points.
  /// Does not include the start and end points (port positions).
  /// </summary>
  /// <remarks>
  /// This property is typically set by the routing system, not directly by users.
  /// For manual waypoint editing, use <see cref="UserWaypoints"/> instead.
  /// </remarks>
  IReadOnlyList<Point>? Waypoints { get; set; }

  /// <summary>
  /// User-defined waypoints for Manual/Guided routing modes.
  /// These are constraint points that the user explicitly placed.
  /// </summary>
  /// <remarks>
  /// <para>
  /// In <see cref="EdgeRoutingMode.Manual"/> mode, the edge passes directly
  /// through these points without auto-routing.
  /// </para>
  /// <para>
  /// In <see cref="EdgeRoutingMode.Guided"/> mode, the router will calculate
  /// a path that passes through these points while avoiding obstacles.
  /// </para>
  /// <para>
  /// In <see cref="EdgeRoutingMode.Auto"/> mode, this property is ignored.
  /// </para>
  /// </remarks>
  IReadOnlyList<Point>? UserWaypoints { get; set; }

  /// <summary>
  /// Whether this edge is visible in the canvas.
  /// Invisible edges are not rendered but remain in the graph.
  /// </summary>
  bool IsVisible { get; set; }

  /// <summary>
  /// Z-index for rendering order. Higher values render on top.
  /// Default is CanvasElement.ZIndexEdges (200).
  /// </summary>
  int ZIndex { get; set; }
}
