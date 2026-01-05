using Avalonia;
using FlowGraph.Core;

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
    /// Whether to show the built-in grid background.
    /// Set to false when using a custom FlowBackground control.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Whether to show the built-in canvas background color.
    /// Set to false when using a custom FlowBackground control so it can show through.
    /// </summary>
    public bool ShowBackground { get; set; } = true;

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

    #region Edge Routing Settings

    /// <summary>
    /// Whether automatic edge routing is enabled.
    /// When enabled, edges will be routed around obstacles automatically.
    /// </summary>
    public bool AutoRouteEdges { get; set; } = false;

    /// <summary>
    /// Whether to re-route edges while dragging nodes.
    /// Only applies when AutoRouteEdges is enabled.
    /// Set to false for better performance with many edges.
    /// </summary>
    public bool RouteEdgesOnDrag { get; set; } = true;

    /// <summary>
    /// Padding around nodes for edge routing calculations.
    /// Higher values create more space between edges and nodes.
    /// </summary>
    public double RoutingNodePadding { get; set; } = 10;

    /// <summary>
    /// Whether to route only edges connected to dragged nodes during drag.
    /// When true, only affected edges are re-routed (better performance).
    /// When false, all edges are re-routed (more consistent but slower).
    /// </summary>
    public bool RouteOnlyAffectedEdges { get; set; } = true;

    /// <summary>
    /// Whether to automatically route newly created edges.
    /// When true, edges created by connecting ports will be routed immediately.
    /// </summary>
    public bool RouteNewEdges { get; set; } = true;

    /// <summary>
    /// The default edge type to use for newly created edges.
    /// </summary>
    public EdgeType DefaultEdgeType { get; set; } = EdgeType.Bezier;

    /// <summary>
    /// The default router algorithm to use for routing.
    /// </summary>
    public RouterAlgorithm DefaultRouterAlgorithm { get; set; } = RouterAlgorithm.Auto;

    #endregion

    #region Viewport Settings

    /// <summary>
    /// Optional bounds to constrain panning.
    /// If set, the viewport cannot pan outside these bounds.
    /// Use null for unconstrained panning.
    /// </summary>
    public Rect? ViewportBounds { get; set; } = null;

    /// <summary>
    /// Padding inside the viewport bounds.
    /// Only applies when ViewportBounds is set.
    /// </summary>
    public double ViewportBoundsPadding { get; set; } = 100;

    /// <summary>
    /// Whether to pan the viewport when scrolling (mouse wheel without Ctrl).
    /// When true: scroll = pan, Ctrl+scroll = zoom
    /// When false: scroll = zoom (default behavior)
    /// </summary>
    public bool PanOnScroll { get; set; } = false;

    /// <summary>
    /// Speed multiplier for pan-on-scroll behavior.
    /// Higher values result in faster panning.
    /// </summary>
    public double PanOnScrollSpeed { get; set; } = 1.0;

    #endregion

    #region Connection Settings

    /// <summary>
    /// Distance in screen pixels within which a connection will snap to the nearest compatible port.
    /// Set to 0 to disable snap-to-port behavior.
    /// </summary>
    public double ConnectionSnapDistance { get; set; } = 30;

    /// <summary>
    /// Whether to snap connections to nodes when dragging near them.
    /// When true, dragging near a node will find the nearest compatible port.
    /// </summary>
    public bool SnapConnectionToNode { get; set; } = true;

    /// <summary>
    /// Whether to show draggable handles at edge endpoints for reconnecting/disconnecting edges.
    /// </summary>
    public bool ShowEdgeEndpointHandles { get; set; } = true;

    /// <summary>
    /// Size of edge endpoint handles in pixels.
    /// </summary>
    public double EdgeEndpointHandleSize { get; set; } = 10;

    /// <summary>
    /// Whether connections can only be started from output ports.
    /// When true (default), dragging from an input port will not start a new connection.
    /// When false, connections can be started from either input or output ports (bidirectional).
    /// </summary>
    public bool StrictConnectionDirection { get; set; } = true;

    #endregion

    #region Editing Settings

    /// <summary>
    /// Whether double-clicking a node triggers a label edit request.
    /// When enabled, double-clicking raises the NodeLabelEditRequested event.
    /// </summary>
    public bool EnableNodeLabelEditing { get; set; } = true;

    /// <summary>
    /// Whether double-clicking a group triggers a label edit request.
    /// When false, double-click toggles collapse (default behavior).
    /// When true, double-click raises NodeLabelEditRequested for the group.
    /// </summary>
    public bool EnableGroupLabelEditing { get; set; } = false;

    /// <summary>
    /// Whether double-clicking an edge triggers a label edit request.
    /// When enabled, double-clicking raises the EdgeLabelEditRequested event.
    /// </summary>
    public bool EnableEdgeLabelEditing { get; set; } = true;

    #endregion

    #region Performance

    /// <summary>
    /// Gets or sets whether virtualization is enabled.
    /// When enabled, only nodes and edges visible in the viewport are rendered.
    /// This significantly improves performance for large graphs (500+ nodes).
    /// Default is true.
    /// </summary>
    public bool EnableVirtualization { get; set; } = true;

    /// <summary>
    /// Gets or sets the buffer (in canvas coordinates) around the viewport for virtualization.
    /// Nodes within this buffer are still rendered even if slightly outside the viewport.
    /// This prevents "popping" during pan/zoom. Default is 200.
    /// </summary>
    public double VirtualizationBuffer { get; set; } = 200;

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

/// <summary>
/// Available edge routing algorithms.
/// </summary>
public enum RouterAlgorithm
{
    /// <summary>
    /// Automatically select the best router based on edge type.
    /// </summary>
    Auto,

    /// <summary>
    /// Direct straight-line routing (no obstacle avoidance).
    /// </summary>
    Direct,

    /// <summary>
    /// Orthogonal routing with right-angle paths using A* pathfinding.
    /// </summary>
    Orthogonal,

    /// <summary>
    /// Smart Bezier curves that avoid obstacles.
    /// </summary>
    SmartBezier
}
