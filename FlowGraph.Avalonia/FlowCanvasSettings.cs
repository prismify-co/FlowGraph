using Avalonia;
using Avalonia.Input;
using FlowGraph.Core;
using FlowGraph.Core.Diagnostics;
using FlowGraph.Core.Elements;
using FlowGraph.Core.Routing;

using AvaloniaRect = Avalonia.Rect;

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
    /// Whether to show port indicators on nodes.
    /// When false, ports still exist for edge routing but are not rendered visually.
    /// Useful for diagrams where connection points should be invisible.
    /// </summary>
    public bool ShowPorts { get; set; } = true;

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

    #region Edge Settings

    /// <summary>
    /// Stroke thickness for normal (unselected) edges.
    /// </summary>
    public double EdgeStrokeThickness { get; set; } = 2;

    /// <summary>
    /// Stroke thickness for selected edges.
    /// </summary>
    public double EdgeSelectedStrokeThickness { get; set; } = 3;

    #endregion

    #region Diagnostics Settings

    /// <summary>
    /// Gets or sets whether diagnostic logging is enabled.
    /// When enabled, FlowGraph will emit detailed diagnostic information.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum log level for diagnostics.
    /// Messages below this level are ignored.
    /// </summary>
    public LogLevel DiagnosticsMinimumLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the categories to include in diagnostics.
    /// Use bitwise OR to combine categories.
    /// </summary>
    public LogCategory DiagnosticsCategories { get; set; } = LogCategory.All;

    /// <summary>
    /// Gets or sets the path for file-based diagnostic logging.
    /// When set, diagnostic messages are also written to this file.
    /// </summary>
    public string? DiagnosticsLogFilePath { get; set; } = null;

    /// <summary>
    /// Applies the diagnostics settings to the global FlowGraphLogger.
    /// Call this after changing diagnostics settings.
    /// </summary>
    public void ApplyDiagnosticsSettings()
    {
        if (EnableDiagnostics)
        {
            FlowGraphLogger.Configure(config =>
            {
                config.Enable()
                      .WithMinimumLevel(DiagnosticsMinimumLevel)
                      .WithCategories(DiagnosticsCategories)
                      .WriteToDebug();

                if (!string.IsNullOrEmpty(DiagnosticsLogFilePath))
                {
                    config.WriteToFile(DiagnosticsLogFilePath);
                }
            });
        }
        else
        {
            FlowGraphLogger.DisableAll();
        }
    }

    #endregion

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

    /// <summary>
    /// Whether to show a distinct header background on groups.
    /// When true, the header area has a separate background color for visual separation.
    /// When false (default), the header blends with the group background.
    /// </summary>
    public bool ShowGroupHeaderBackground { get; set; } = false;

    /// <summary>
    /// Whether to use proxy ports when groups are collapsed.
    /// When true, edges that cross group boundaries are re-routed to proxy ports
    /// on the collapsed group, maintaining visual connectivity.
    /// When false (default), edges to/from hidden nodes are simply hidden.
    /// </summary>
    public bool UseProxyPortsOnCollapse { get; set; } = true;

    /// <summary>
    /// Default movement mode for groups when dragged.
    /// <see cref="GroupMovementMode.MoveWithChildren"/> moves all children with the group (default).
    /// <see cref="GroupMovementMode.MoveGroupOnly"/> moves only the group boundary.
    /// </summary>
    public GroupMovementMode GroupMovementMode { get; set; } = GroupMovementMode.MoveWithChildren;

    /// <summary>
    /// Modifier key that toggles the group movement mode while held.
    /// When pressed, temporarily switches between MoveWithChildren and MoveGroupOnly.
    /// Default is Shift (matching Nodify behavior).
    /// </summary>
    public KeyModifiers GroupMovementModeToggleKey { get; set; } = KeyModifiers.Shift;

    /// <summary>
    /// Whether to show a visible resize handle on group corners.
    /// When true, a small corner grip is displayed for easier resizing.
    /// When false (default), resize uses invisible edge zones for a cleaner look.
    /// </summary>
    public bool ShowGroupResizeHandle { get; set; } = false;

    /// <summary>
    /// Default ZIndex for group nodes.
    /// Groups typically render behind their children, so this defaults to ZIndexGroups (150).
    /// </summary>
    public int GroupDefaultZIndex { get; set; } = CanvasElement.ZIndexGroups;

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

    /// <summary>
    /// Advanced routing options controlling corner radius, edge spacing, etc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options provide fine-grained control over edge routing behavior:
    /// <list type="bullet">
    /// <item><see cref="EdgeRoutingOptions.CornerRadius"/> - Rounding for SmoothStep edges</item>
    /// <item><see cref="EdgeRoutingOptions.EndSegmentLength"/> - Stem length from ports</item>
    /// <item><see cref="EdgeRoutingOptions.EdgeSpacing"/> - Gap between edges from same port</item>
    /// </list>
    /// </para>
    /// </remarks>
    public EdgeRoutingOptions RoutingOptions { get; set; } = new();

    #endregion

    #region Viewport Settings

    /// <summary>
    /// Optional bounds to constrain panning.
    /// If set, the viewport cannot pan outside these bounds.
    /// Use null for unconstrained panning.
    /// </summary>
    public AvaloniaRect? ViewportBounds { get; set; } = null;

    /// <summary>
    /// Padding inside the viewport bounds.
    /// Only applies when ViewportBounds is set.
    /// </summary>
    public double ViewportBoundsPadding { get; set; } = 100;

    /// <summary>
    /// Minimum padding (in pixels) around content when using FitToView.
    /// The actual padding may be larger based on <see cref="FitToViewMinPaddingPercent"/>.
    /// </summary>
    public double FitToViewPadding { get; set; } = 50;

    /// <summary>
    /// Minimum padding as a percentage of viewport size (0.0 to 1.0) when using FitToView.
    /// The effective padding is the larger of <see cref="FitToViewPadding"/> or this percentage.
    /// For example, 0.15 means at least 15% of the viewport dimension will be padding.
    /// Set to 0 to use only fixed pixel padding.
    /// </summary>
    public double FitToViewMinPaddingPercent { get; set; } = 0.15;

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

    /// <summary>
    /// Whether double-clicking a shape (e.g., sticky note) triggers a text edit request.
    /// When enabled, double-clicking raises the ShapeTextEditRequested event.
    /// </summary>
    public bool EnableShapeTextEditing { get; set; } = true;

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

    /// <summary>
    /// Gets or sets the batch size for rendering nodes.
    /// When rendering large graphs, nodes are rendered in batches to keep the UI responsive.
    /// Set to 0 to disable batching (render all at once). Default is 50.
    /// </summary>
    public int RenderBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether to use simplified node rendering for better performance.
    /// When enabled, nodes use a minimal visual tree (single Border + TextBlock).
    /// Recommended for graphs with 500+ nodes. Default is false.
    /// </summary>
    public bool UseSimplifiedNodeRendering { get; set; } = false;

    /// <summary>
    /// Gets or sets the node count threshold for automatically enabling direct rendering.
    /// When the graph has more nodes than this threshold, DirectCanvasRenderer is automatically used.
    /// Set to 0 to disable auto-switching. Default is 100.
    /// </summary>
    /// <remarks>
    /// Direct rendering bypasses the Avalonia visual tree and draws directly to a DrawingContext,
    /// providing significantly better performance for large graphs (500+ nodes).
    /// </remarks>
    public int DirectRenderingNodeThreshold { get; set; } = 100;

    #endregion

    #region Level of Detail (LOD)

    /// <summary>
    /// Gets or sets the zoom threshold below which ports are hidden.
    /// When zoom level is below this value, ports are not rendered for better performance.
    /// Default is 0.4 (40% zoom).
    /// </summary>
    public double LodPortsZoomThreshold { get; set; } = 0.4;

    /// <summary>
    /// Gets or sets the zoom threshold below which node labels are hidden.
    /// When zoom level is below this value, labels are not rendered for better performance.
    /// Default is 0.3 (30% zoom).
    /// </summary>
    public double LodLabelsZoomThreshold { get; set; } = 0.3;

    /// <summary>
    /// Gets or sets the zoom threshold below which simplified node rendering is used.
    /// When zoom level is below this value, nodes are rendered as simple rectangles without
    /// rounded corners or custom renderers. Default is 0.5 (50% zoom).
    /// </summary>
    public double LodSimplifiedZoomThreshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets whether LOD (Level of Detail) rendering is enabled.
    /// When true, visual details are automatically hidden at low zoom levels for better performance.
    /// When false, all details are always rendered regardless of zoom level. Default is true.
    /// </summary>
    public bool EnableLod { get; set; } = true;

    #endregion

    #region Interaction Mode

    /// <summary>
    /// Gets or sets whether the canvas is in read-only mode.
    /// When true, all editing interactions are disabled (no dragging, connecting, selecting, etc.).
    /// Pan and zoom still work. Useful for displaying graphs without allowing modification.
    /// Default is false.
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets whether auto-pan is enabled when dragging near viewport edges.
    /// When true, the viewport automatically pans when dragging nodes or edges near the edge
    /// of the visible area. Default is true.
    /// </summary>
    public bool EnableAutoPan { get; set; } = true;

    /// <summary>
    /// Gets or sets the distance from viewport edge (in pixels) at which auto-pan activates.
    /// Default is 50 pixels.
    /// </summary>
    public double AutoPanEdgeDistance { get; set; } = 50;

    /// <summary>
    /// Gets or sets the speed of auto-pan in pixels per frame.
    /// Higher values result in faster panning. Default is 10.
    /// </summary>
    public double AutoPanSpeed { get; set; } = 10;

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
