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
  /// Optional waypoints for custom edge routing.
  /// When set, the edge will pass through these intermediate points.
  /// Does not include the start and end points (port positions).
  /// </summary>
  IReadOnlyList<Point>? Waypoints { get; set; }

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
