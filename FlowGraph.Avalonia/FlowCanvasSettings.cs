namespace FlowGraph.Avalonia;

/// <summary>
/// Configuration settings for the FlowCanvas control.
/// </summary>
public class FlowCanvasSettings
{
    /// <summary>
    /// Width of nodes in pixels.
    /// </summary>
    public double NodeWidth { get; set; } = 150;

    /// <summary>
    /// Height of nodes in pixels.
    /// </summary>
    public double NodeHeight { get; set; } = 80;

    /// <summary>
    /// Size of connection ports in pixels.
    /// </summary>
    public double PortSize { get; set; } = 12;

    /// <summary>
    /// Spacing between grid dots in pixels.
    /// </summary>
    public double GridSpacing { get; set; } = 20;

    /// <summary>
    /// Size of grid dots in pixels.
    /// </summary>
    public double GridDotSize { get; set; } = 2;

    /// <summary>
    /// Minimum zoom level.
    /// </summary>
    public double MinZoom { get; set; } = 0.1;

    /// <summary>
    /// Maximum zoom level.
    /// </summary>
    public double MaxZoom { get; set; } = 3.0;

    /// <summary>
    /// Zoom increment per scroll step.
    /// </summary>
    public double ZoomStep { get; set; } = 0.1;

    /// <summary>
    /// Whether to snap nodes to the grid when dragging.
    /// </summary>
    public bool SnapToGrid { get; set; } = true;

    /// <summary>
    /// Grid size for snapping (can differ from visual grid).
    /// If null, uses GridSpacing.
    /// </summary>
    public double? SnapGridSize { get; set; } = null;

    /// <summary>
    /// Gets the effective snap grid size.
    /// </summary>
    public double EffectiveSnapGridSize => SnapGridSize ?? GridSpacing;

    /// <summary>
    /// Selection mode for box selection.
    /// </summary>
    public SelectionMode SelectionMode { get; set; } = SelectionMode.Partial;

    /// <summary>
    /// Whether left-click drag on empty canvas pans (true) or starts box selection (false).
    /// When true, use Shift+drag for box selection.
    /// When false, use Shift+drag or middle-click for panning.
    /// </summary>
    public bool PanOnDrag { get; set; } = true;

    /// <summary>
    /// Whether to output debug information for coordinate transformations.
    /// </summary>
    public bool DebugCoordinateTransforms { get; set; } = false;

    /// <summary>
    /// Width of the invisible hit area for edge click detection (in pixels).
    /// Higher values make edges easier to click but may overlap with other elements.
    /// </summary>
    public double EdgeHitAreaWidth { get; set; } = 15;

    #region Group Settings

    /// <summary>
    /// Padding around children when auto-sizing groups.
    /// </summary>
    public double GroupPadding { get; set; } = 20;

    /// <summary>
    /// Height of the group header area.
    /// </summary>
    public double GroupHeaderHeight { get; set; } = 28;

    /// <summary>
    /// Border radius for group corners.
    /// </summary>
    public double GroupBorderRadius { get; set; } = 8;

    /// <summary>
    /// Whether to use dashed borders for groups (React Flow style).
    /// </summary>
    public bool GroupUseDashedBorder { get; set; } = true;

    /// <summary>
    /// Opacity of the group background (0.0 to 1.0).
    /// Lower values create more translucent groups.
    /// </summary>
    public double GroupBackgroundOpacity { get; set; } = 0.1;

    #endregion

    /// <summary>
    /// Default settings instance.
    /// </summary>
    public static FlowCanvasSettings Default { get; } = new();
}

/// <summary>
/// Mode for box selection behavior.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Node is selected if any part intersects the selection box.
    /// </summary>
    Partial,

    /// <summary>
    /// Node is selected only if fully contained in the selection box.
    /// </summary>
    Full
}
