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
    /// Default settings instance.
    /// </summary>
    public static FlowCanvasSettings Default { get; } = new();
}
