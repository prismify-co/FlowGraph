namespace FlowGraph.Core.Routing;

/// <summary>
/// Configuration options for edge routing behavior.
/// </summary>
/// <remarks>
/// <para>
/// EdgeRoutingOptions controls how edges are routed between nodes. Different routers
/// may use different subsets of these options based on their capabilities.
/// </para>
/// <para>
/// For Community edition:
/// <list type="bullet">
/// <item><see cref="CornerRadius"/> - Rounding for Step/SmoothStep edges</item>
/// <item><see cref="EndSegmentLength"/> - Minimum distance from port before turning</item>
/// <item><see cref="EdgeSpacing"/> - Gap between multiple edges from same port</item>
/// <item><see cref="SpreadEdgesOnPort"/> - Whether to spread edges from same side</item>
/// </list>
/// </para>
/// <para>
/// For Pro edition (additional options):
/// <list type="bullet">
/// <item><see cref="AvoidsNodes"/> - Enable smart obstacle avoidance</item>
/// <item><see cref="CrossingStyle"/> - How to handle edge crossings</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new EdgeRoutingOptions
/// {
///     CornerRadius = 15,
///     EndSegmentLength = 60,
///     EdgeSpacing = 10,
///     SpreadEdgesOnPort = true
/// };
/// 
/// // Apply to canvas settings
/// canvas.Settings.RoutingOptions = options;
/// </code>
/// </example>
public class EdgeRoutingOptions
{
    /// <summary>
    /// Default routing options instance.
    /// </summary>
    public static EdgeRoutingOptions Default { get; } = new();

    /// <summary>
    /// Corner radius for Step and SmoothStep edges.
    /// Controls how rounded the corners are at direction changes.
    /// Default is 10 pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <see cref="EdgeType.Step"/> edges, corners are always sharp (90°).
    /// For <see cref="EdgeType.SmoothStep"/> edges, this value determines the arc radius.
    /// </para>
    /// <para>
    /// The actual radius is clamped to not exceed half the segment length,
    /// ensuring corners don't overlap.
    /// </para>
    /// </remarks>
    public double CornerRadius { get; set; } = 10;

    /// <summary>
    /// Minimum distance from the port before the edge can make its first turn.
    /// Default is 50 pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This creates a "stem" coming out of the port before the edge can bend.
    /// Helps prevent awkward curves that turn immediately at the node boundary.
    /// </para>
    /// <para>
    /// Similar to GoJS's fromEndSegmentLength and toEndSegmentLength properties.
    /// </para>
    /// </remarks>
    public double EndSegmentLength { get; set; } = 50;

    /// <summary>
    /// Padding around nodes for obstacle avoidance routing.
    /// Default is 10 pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When routing edges around nodes, this padding ensures edges don't
    /// pass too close to node boundaries.
    /// </para>
    /// <para>
    /// Only used by routers that support obstacle avoidance (SmartBezier, AStar).
    /// </para>
    /// </remarks>
    public double NodePadding { get; set; } = 10;

    /// <summary>
    /// Spacing between multiple edges connecting to the same port side.
    /// Default is 8 pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When multiple edges connect to the same side of a node (e.g., all from the right),
    /// this value determines how far apart they are spread vertically or horizontally.
    /// </para>
    /// <para>
    /// Similar to GoJS's fromSpot/toSpot spreading behavior.
    /// </para>
    /// </remarks>
    public double EdgeSpacing { get; set; } = 8;

    /// <summary>
    /// Whether to automatically spread multiple edges from the same port side.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, if three edges all connect to the right side of a node,
    /// they will be spread vertically to avoid overlap.
    /// </para>
    /// <para>
    /// When false, all edges from the same side will connect at the same point.
    /// </para>
    /// </remarks>
    public bool SpreadEdgesOnPort { get; set; } = true;

    /// <summary>
    /// Whether edges should route around other nodes to avoid overlap.
    /// Default is false (Pro feature).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, the router will attempt to find paths that don't pass through
    /// other nodes in the graph. This is a Pro feature.
    /// </para>
    /// <para>
    /// Similar to GoJS's Routing.AvoidsNodes option.
    /// </para>
    /// </remarks>
    public bool AvoidsNodes { get; set; } = false;

    /// <summary>
    /// How to render edge crossings.
    /// Default is None (Pro feature).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When edges cross each other, this option determines the visual treatment:
    /// <list type="bullet">
    /// <item>None - Edges simply cross (default)</item>
    /// <item>JumpOver - Arc over the crossing edge</item>
    /// <item>JumpGap - Small gap at the crossing point</item>
    /// </list>
    /// </para>
    /// <para>
    /// Similar to GoJS's Curve.JumpOver and Curve.JumpGap options.
    /// </para>
    /// </remarks>
    public EdgeCrossingStyle CrossingStyle { get; set; } = EdgeCrossingStyle.None;

    /// <summary>
    /// Radius for JumpOver and JumpGap crossing styles.
    /// Default is 8 pixels.
    /// </summary>
    public double CrossingRadius { get; set; } = 8;

    /// <summary>
    /// Creates a copy of these options.
    /// </summary>
    public EdgeRoutingOptions Clone()
    {
        return new EdgeRoutingOptions
        {
            CornerRadius = CornerRadius,
            EndSegmentLength = EndSegmentLength,
            NodePadding = NodePadding,
            EdgeSpacing = EdgeSpacing,
            SpreadEdgesOnPort = SpreadEdgesOnPort,
            AvoidsNodes = AvoidsNodes,
            CrossingStyle = CrossingStyle,
            CrossingRadius = CrossingRadius
        };
    }
}

/// <summary>
/// Visual style for rendering edge crossings.
/// </summary>
public enum EdgeCrossingStyle
{
    /// <summary>
    /// No special treatment - edges cross normally.
    /// </summary>
    None,

    /// <summary>
    /// Arc over crossing edges (Pro feature).
    /// </summary>
    JumpOver,

    /// <summary>
    /// Small gap at crossing points (Pro feature).
    /// </summary>
    JumpGap
}
