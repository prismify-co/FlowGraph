namespace FlowGraph.Core.Models;

/// <summary>
/// Immutable definition of an edge's identity and connection structure.
/// Contains all properties that define "what" an edge connects, not its runtime state.
/// </summary>
/// <remarks>
/// This is a sealed record for value equality and immutability.
/// Use <c>with</c> expressions to create modified copies:
/// <code>
/// var reconnected = definition with { Target = "newNodeId" };
/// </code>
/// </remarks>
public sealed record EdgeDefinition
{
    /// <summary>
    /// Unique identifier for the edge. Required and immutable.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The ID of the source node.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The ID of the target node.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// The ID of the source port on the source node.
    /// </summary>
    public required string SourcePort { get; init; }

    /// <summary>
    /// The ID of the target port on the target node.
    /// </summary>
    public required string TargetPort { get; init; }

    /// <summary>
    /// The visual type of the edge (bezier, straight, step, etc.).
    /// </summary>
    public EdgeType Type { get; init; } = EdgeType.Bezier;

    /// <summary>
    /// The marker to display at the start of the edge.
    /// </summary>
    public EdgeMarker MarkerStart { get; init; } = EdgeMarker.None;

    /// <summary>
    /// The marker to display at the end of the edge.
    /// </summary>
    public EdgeMarker MarkerEnd { get; init; } = EdgeMarker.Arrow;

    /// <summary>
    /// Optional label to display on the edge.
    /// </summary>
    /// <remarks>
    /// For simple labels, use this property directly. For advanced positioning control,
    /// use <see cref="LabelInfo"/> instead. If both are set, <see cref="LabelInfo"/> takes precedence.
    /// </remarks>
    public string? Label { get; init; }

    /// <summary>
    /// Enhanced label with positioning information.
    /// </summary>
    /// <remarks>
    /// When set, this takes precedence over <see cref="Label"/>. Use <see cref="EffectiveLabel"/>
    /// to get the resolved label text regardless of which property was set.
    /// </remarks>
    public LabelInfo? LabelInfo { get; init; }

    /// <summary>
    /// Gets the effective label text, preferring <see cref="LabelInfo"/> over <see cref="Label"/>.
    /// </summary>
    public string? EffectiveLabel => LabelInfo?.Text ?? Label;

    /// <summary>
    /// Whether this edge should use automatic routing to avoid obstacles.
    /// </summary>
    /// <remarks>
    /// This property is maintained for backward compatibility.
    /// For new code, prefer using <see cref="RoutingMode"/> instead.
    /// When <see cref="RoutingMode"/> is explicitly set, it takes precedence.
    /// </remarks>
    public bool AutoRoute { get; init; }

    /// <summary>
    /// Determines how this edge's path is calculated.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><see cref="EdgeRoutingMode.Auto"/> - Router fully controls path (default)</item>
    /// <item><see cref="EdgeRoutingMode.Manual"/> - Uses only user-defined waypoints</item>
    /// <item><see cref="EdgeRoutingMode.Guided"/> - Router path passes through user waypoints</item>
    /// </list>
    /// </remarks>
    public EdgeRoutingMode RoutingMode { get; init; } = EdgeRoutingMode.Auto;

    /// <summary>
    /// Visual style for the edge including colors, dash patterns, and effects.
    /// </summary>
    /// <remarks>
    /// When null, the edge uses theme defaults. Set to customize individual edges
    /// or use preset styles like <see cref="EdgeStyle.Success"/> or <see cref="EdgeStyle.Error"/>.
    /// </remarks>
    public EdgeStyle? Style { get; init; }

    /// <summary>
    /// Gets the effective routing mode, considering both <see cref="RoutingMode"/> 
    /// and legacy <see cref="AutoRoute"/> property.
    /// </summary>
    /// <remarks>
    /// If <see cref="AutoRoute"/> is true and <see cref="RoutingMode"/> is Auto,
    /// obstacle avoidance is enabled. Otherwise, <see cref="RoutingMode"/> determines behavior.
    /// </remarks>
    public EdgeRoutingMode EffectiveRoutingMode =>
        RoutingMode != EdgeRoutingMode.Auto ? RoutingMode :
        AutoRoute ? EdgeRoutingMode.Auto : EdgeRoutingMode.Auto;

    /// <summary>
    /// Creates a new definition with a different target node and port.
    /// </summary>
    public EdgeDefinition ReconnectTarget(string newTarget, string newTargetPort) =>
        this with { Target = newTarget, TargetPort = newTargetPort };

    /// <summary>
    /// Creates a new definition with a different source node and port.
    /// </summary>
    public EdgeDefinition ReconnectSource(string newSource, string newSourcePort) =>
        this with { Source = newSource, SourcePort = newSourcePort };

    /// <summary>
    /// Creates a new definition with a different label.
    /// </summary>
    public EdgeDefinition WithLabel(string? label) =>
        this with { Label = label };

    /// <summary>
    /// Creates a new definition with enhanced label positioning.
    /// </summary>
    /// <param name="labelInfo">The label info with text and positioning.</param>
    public EdgeDefinition WithLabelInfo(LabelInfo? labelInfo) =>
        this with { LabelInfo = labelInfo };

    /// <summary>
    /// Creates a new definition with enhanced label positioning.
    /// </summary>
    /// <param name="text">The label text.</param>
    /// <param name="anchor">Where along the edge to position the label.</param>
    /// <param name="offsetX">Horizontal offset from anchor point.</param>
    /// <param name="offsetY">Vertical offset from anchor point.</param>
    public EdgeDefinition WithLabelInfo(string text, LabelAnchor anchor = LabelAnchor.Center, double offsetX = 0, double offsetY = 0) =>
        this with { LabelInfo = new LabelInfo(text, anchor, offsetX, offsetY) };

    /// <summary>
    /// Creates a new definition with a different edge type.
    /// </summary>
    public EdgeDefinition WithType(EdgeType type) =>
        this with { Type = type };

    /// <summary>
    /// Creates a new definition with a different routing mode.
    /// </summary>
    public EdgeDefinition WithRoutingMode(EdgeRoutingMode mode) =>
        this with { RoutingMode = mode };

    /// <summary>
    /// Creates a new definition with a different visual style.
    /// </summary>
    public EdgeDefinition WithStyle(EdgeStyle? style) =>
        this with { Style = style };
}
