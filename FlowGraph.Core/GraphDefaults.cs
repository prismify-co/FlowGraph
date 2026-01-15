namespace FlowGraph.Core;

/// <summary>
/// Default values used throughout the FlowGraph library.
/// Centralizes constants to avoid duplication and ensure consistency.
/// </summary>
public static class GraphDefaults
{
  #region Node Dimensions

  /// <summary>
  /// Default width for nodes when not explicitly specified.
  /// </summary>
  public const double NodeWidth = 150;

  /// <summary>
  /// Default height for nodes when not explicitly specified.
  /// </summary>
  public const double NodeHeight = 80;

  /// <summary>
  /// Default size for connection ports.
  /// </summary>
  public const double PortSize = 12;

  #endregion

  #region Group Settings

  /// <summary>
  /// Padding around children when calculating group bounds.
  /// </summary>
  public const double GroupPadding = 20;

  /// <summary>
  /// Height of the group header area.
  /// </summary>
  public const double GroupHeaderHeight = 30;

  /// <summary>
  /// Minimum width for empty groups.
  /// </summary>
  public const double GroupMinWidth = 200;

  /// <summary>
  /// Minimum height for empty groups.
  /// </summary>
  public const double GroupMinHeight = 100;

  /// <summary>
  /// Border radius for group corners.
  /// </summary>
  public const double GroupBorderRadius = 8;

  /// <summary>
  /// Default opacity for group backgrounds.
  /// </summary>
  public const double GroupBackgroundOpacity = 0.1;

  #endregion

  #region Grid & Snapping

  /// <summary>
  /// Default grid spacing for visual grid and snapping.
  /// </summary>
  public const double GridSpacing = 20;

  /// <summary>
  /// Default size of grid dots.
  /// </summary>
  public const double GridDotSize = 2;

  #endregion

  #region Zoom

  /// <summary>
  /// Minimum zoom level.
  /// </summary>
  public const double MinZoom = 0.1;

  /// <summary>
  /// Maximum zoom level.
  /// </summary>
  public const double MaxZoom = 3.0;

  /// <summary>
  /// Zoom increment per scroll step.
  /// </summary>
  public const double ZoomStep = 0.1;

  #endregion

  #region Edge Routing

  /// <summary>
  /// Padding around nodes for edge routing calculations.
  /// </summary>
  public const double RoutingNodePadding = 10;

  /// <summary>
  /// Width of the invisible hit area for edge click detection.
  /// </summary>
  public const double EdgeHitAreaWidth = 15;

  /// <summary>
  /// Distance threshold for snapping connections to ports.
  /// </summary>
  public const double ConnectionSnapDistance = 30;

  /// <summary>
  /// Minimum offset for bezier control points.
  /// Ensures curves have visible curvature even for close nodes.
  /// </summary>
  public const double MinBezierControlOffset = 50;

  /// <summary>
  /// Maximum offset for bezier control points.
  /// Prevents curves from extending too far for distant nodes.
  /// </summary>
  public const double MaxBezierControlOffset = 150;

  /// <summary>
  /// Default arrow size for edge markers.
  /// </summary>
  public const double EdgeArrowSize = 10;

  /// <summary>
  /// Arrow angle in radians (30 degrees / π/6).
  /// Controls the spread of arrow heads.
  /// </summary>
  public const double EdgeArrowAngle = Math.PI / 6;

  #endregion

  #region Command History

  /// <summary>
  /// Default maximum number of commands in undo history.
  /// </summary>
  public const int MaxHistorySize = 100;

  #endregion
}
