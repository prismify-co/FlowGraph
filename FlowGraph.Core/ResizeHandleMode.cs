namespace FlowGraph.Core;

/// <summary>
/// Specifies which resize handles should be displayed for a resizable element.
/// </summary>
/// <remarks>
/// Use this enum to customize which resize handles appear when an element is selected.
/// For example, a cylinder shape might only show edge handles (not corners) since
/// corner handles don't align well with curved geometry.
/// </remarks>
[Flags]
public enum ResizeHandleMode
{
  /// <summary>No resize handles.</summary>
  None = 0,

  /// <summary>Top-left corner handle.</summary>
  TopLeft = 1,

  /// <summary>Top-center edge handle.</summary>
  Top = 2,

  /// <summary>Top-right corner handle.</summary>
  TopRight = 4,

  /// <summary>Middle-left edge handle.</summary>
  Left = 8,

  /// <summary>Middle-right edge handle.</summary>
  Right = 16,

  /// <summary>Bottom-left corner handle.</summary>
  BottomLeft = 32,

  /// <summary>Bottom-center edge handle.</summary>
  Bottom = 64,

  /// <summary>Bottom-right corner handle.</summary>
  BottomRight = 128,

  /// <summary>All corner handles (TopLeft, TopRight, BottomLeft, BottomRight).</summary>
  Corners = TopLeft | TopRight | BottomLeft | BottomRight,

  /// <summary>All edge handles (Top, Bottom, Left, Right).</summary>
  Edges = Top | Bottom | Left | Right,

  /// <summary>Horizontal edge handles only (Left, Right).</summary>
  Horizontal = Left | Right,

  /// <summary>Vertical edge handles only (Top, Bottom).</summary>
  Vertical = Top | Bottom,

  /// <summary>All 8 resize handles.</summary>
  All = Corners | Edges
}
