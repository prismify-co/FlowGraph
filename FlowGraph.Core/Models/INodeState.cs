using System.ComponentModel;

namespace FlowGraph.Core.Models;

/// <summary>
/// Interface for the mutable runtime state of a node.
/// Separates position, size, and UI state from the immutable definition.
/// </summary>
/// <remarks>
/// Implementations must raise <see cref="INotifyPropertyChanged.PropertyChanged"/> 
/// when any property changes to support data binding.
/// 
/// Default implementation: <see cref="NodeState"/>
/// Avalonia-optimized: FlowGraph.Avalonia.Binding.ObservableNodeState
/// </remarks>
public interface INodeState : INotifyPropertyChanged
{
  #region Position

  /// <summary>
  /// The X coordinate of the node in canvas space.
  /// </summary>
  double X { get; set; }

  /// <summary>
  /// The Y coordinate of the node in canvas space.
  /// </summary>
  double Y { get; set; }

  #endregion

  #region Size

  /// <summary>
  /// The width of the node. Null means auto-sized based on content.
  /// </summary>
  double? Width { get; set; }

  /// <summary>
  /// The height of the node. Null means auto-sized based on content.
  /// </summary>
  double? Height { get; set; }

  #endregion

  #region UI State

  /// <summary>
  /// Whether this node is currently selected.
  /// </summary>
  bool IsSelected { get; set; }

  /// <summary>
  /// Whether this node is currently being dragged.
  /// </summary>
  bool IsDragging { get; set; }

  /// <summary>
  /// Whether a group node is collapsed (only meaningful when IsGroup is true).
  /// </summary>
  bool IsCollapsed { get; set; }

  /// <summary>
  /// Whether this node is visible in the canvas.
  /// Invisible nodes are not rendered but remain in the graph.
  /// </summary>
  bool IsVisible { get; set; }

  /// <summary>
  /// Z-index for rendering order. Higher values render on top.
  /// Default is CanvasElement.ZIndexNodes (300).
  /// </summary>
  int ZIndex { get; set; }

  #endregion
}
