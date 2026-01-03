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
