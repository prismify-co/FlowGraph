namespace FlowGraph.Core.Models;

/// <summary>
/// Declarative styling for edge appearance including colors, dash patterns, and effects.
/// </summary>
/// <remarks>
/// <para>
/// EdgeStyle allows per-edge customization of visual appearance without requiring
/// custom renderers. Styles can be applied directly to edges or used as presets.
/// </para>
/// <para>
/// Common use cases:
/// - Color-coding edges by data type (red for errors, green for success)
/// - Dashed lines for optional/conditional connections
/// - Animated flow to show data direction
/// - Glow effects to highlight active connections
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a styled edge
/// var edge = new Edge(...)
/// {
///     Definition = new EdgeDefinition
///     {
///         // ... connection info ...
///         Style = new EdgeStyle
///         {
///             StrokeColor = "#E53935",
///             StrokeWidth = 3,
///             DashPattern = EdgeDashPattern.Dashed,
///             AnimatedFlow = true,
///             FlowSpeed = 2.0
///         }
///     }
/// };
/// 
/// // Use a preset
/// edge.Definition = edge.Definition with { Style = EdgeStyle.Error };
/// </code>
/// </example>
public sealed record EdgeStyle
{
  /// <summary>
  /// The stroke color as a hex string (#RRGGBB or #AARRGGBB).
  /// Null means use theme default.
  /// </summary>
  public string? StrokeColor { get; init; }

  /// <summary>
  /// The stroke width in logical pixels.
  /// Null means use settings default.
  /// </summary>
  public double? StrokeWidth { get; init; }

  /// <summary>
  /// The dash pattern for the edge stroke.
  /// </summary>
  public EdgeDashPattern DashPattern { get; init; } = EdgeDashPattern.Solid;

  /// <summary>
  /// Custom dash array when <see cref="DashPattern"/> is <see cref="EdgeDashPattern.Custom"/>.
  /// Values are dash length, gap length, repeating.
  /// </summary>
  public double[]? CustomDashArray { get; init; }

  /// <summary>
  /// Whether to animate the edge with a flowing effect.
  /// </summary>
  public bool AnimatedFlow { get; init; }

  /// <summary>
  /// Speed of the animated flow effect (1.0 = normal, 2.0 = double speed).
  /// Only used when <see cref="AnimatedFlow"/> is true.
  /// </summary>
  public double FlowSpeed { get; init; } = 1.0;

  /// <summary>
  /// Direction of the animated flow.
  /// </summary>
  public EdgeFlowDirection FlowDirection { get; init; } = EdgeFlowDirection.Forward;

  /// <summary>
  /// Whether to show a glow effect around the edge.
  /// </summary>
  public bool Glow { get; init; }

  /// <summary>
  /// The glow color as a hex string. If null, uses <see cref="StrokeColor"/>.
  /// </summary>
  public string? GlowColor { get; init; }

  /// <summary>
  /// The glow intensity/radius (1.0 = subtle, 3.0 = strong).
  /// </summary>
  public double GlowIntensity { get; init; } = 1.5;

  /// <summary>
  /// Opacity of the edge (0.0 to 1.0).
  /// </summary>
  public double Opacity { get; init; } = 1.0;

  /// <summary>
  /// Whether to enable rainbow color cycling effect.
  /// </summary>
  public bool RainbowEffect { get; init; }

  /// <summary>
  /// Speed of rainbow color cycling (1.0 = one full cycle per second).
  /// </summary>
  public double RainbowSpeed { get; init; } = 0.5;

  /// <summary>
  /// Whether to enable pulsing opacity effect.
  /// </summary>
  public bool PulseEffect { get; init; }

  /// <summary>
  /// Frequency of pulse effect in Hz (cycles per second).
  /// </summary>
  public double PulseFrequency { get; init; } = 1.0;

  /// <summary>
  /// Minimum opacity during pulse (0.0 to 1.0).
  /// </summary>
  public double PulseMinOpacity { get; init; } = 0.3;

  #region Preset Styles

  /// <summary>
  /// Default edge style (uses theme defaults).
  /// </summary>
  public static EdgeStyle Default { get; } = new();

  /// <summary>
  /// Success/valid connection style (green).
  /// </summary>
  public static EdgeStyle Success { get; } = new()
  {
    StrokeColor = "#4CAF50"
  };

  /// <summary>
  /// Warning/conditional connection style (orange, dashed).
  /// </summary>
  public static EdgeStyle Warning { get; } = new()
  {
    StrokeColor = "#FF9800",
    DashPattern = EdgeDashPattern.Dashed
  };

  /// <summary>
  /// Error/invalid connection style (red).
  /// </summary>
  public static EdgeStyle Error { get; } = new()
  {
    StrokeColor = "#F44336"
  };

  /// <summary>
  /// Disabled/inactive connection style (gray, dotted).
  /// </summary>
  public static EdgeStyle Disabled { get; } = new()
  {
    StrokeColor = "#9E9E9E",
    DashPattern = EdgeDashPattern.Dotted,
    Opacity = 0.5
  };

  /// <summary>
  /// Active/live data flow style (blue with animation).
  /// </summary>
  public static EdgeStyle ActiveFlow { get; } = new()
  {
    StrokeColor = "#2196F3",
    AnimatedFlow = true,
    FlowSpeed = 1.5,
    DashPattern = EdgeDashPattern.Dashed
  };

  /// <summary>
  /// Highlighted/selected style (cyan with glow).
  /// </summary>
  public static EdgeStyle Highlighted { get; } = new()
  {
    StrokeColor = "#00BCD4",
    Glow = true,
    GlowIntensity = 2.0
  };

  // Additional presets similar to vyuh_node_flow's connection effects

  /// <summary>
  /// Fast-paced data transfer style (green, fast animation).
  /// </summary>
  public static EdgeStyle DataStream { get; } = new()
  {
    StrokeColor = "#00E676",
    AnimatedFlow = true,
    FlowSpeed = 3.0,
    DashPattern = EdgeDashPattern.Dotted,
    StrokeWidth = 2
  };

  /// <summary>
  /// Bidirectional communication style (purple, bidirectional flow).
  /// </summary>
  public static EdgeStyle Bidirectional { get; } = new()
  {
    StrokeColor = "#9C27B0",
    AnimatedFlow = true,
    FlowSpeed = 1.5,
    FlowDirection = EdgeFlowDirection.Bidirectional,
    DashPattern = EdgeDashPattern.DashDot
  };

  /// <summary>
  /// Reverse flow style (orange, reverse direction).
  /// </summary>
  public static EdgeStyle ReverseFlow { get; } = new()
  {
    StrokeColor = "#FF5722",
    AnimatedFlow = true,
    FlowSpeed = 1.5,
    FlowDirection = EdgeFlowDirection.Reverse,
    DashPattern = EdgeDashPattern.Dashed
  };

  /// <summary>
  /// Pulsing electric style (yellow with glow and animation).
  /// </summary>
  public static EdgeStyle Electric { get; } = new()
  {
    StrokeColor = "#FFEB3B",
    AnimatedFlow = true,
    FlowSpeed = 2.5,
    Glow = true,
    GlowIntensity = 2.5,
    GlowColor = "#FFC107",
    DashPattern = EdgeDashPattern.Dotted
  };

  /// <summary>
  /// Slow, gentle flow style (light blue, slow animation).
  /// </summary>
  public static EdgeStyle Gentle { get; } = new()
  {
    StrokeColor = "#81D4FA",
    AnimatedFlow = true,
    FlowSpeed = 0.5,
    DashPattern = EdgeDashPattern.LongDash
  };

  /// <summary>
  /// Debug/trace style (pink, visible animation).
  /// </summary>
  public static EdgeStyle Debug { get; } = new()
  {
    StrokeColor = "#E91E63",
    AnimatedFlow = true,
    FlowSpeed = 2.0,
    DashPattern = EdgeDashPattern.DashDot,
    StrokeWidth = 3
  };

  /// <summary>
  /// Rainbow color cycling effect (cycles through all hues).
  /// </summary>
  public static EdgeStyle Rainbow { get; } = new()
  {
    RainbowEffect = true,
    RainbowSpeed = 0.5,
    AnimatedFlow = true,
    FlowSpeed = 1.5,
    DashPattern = EdgeDashPattern.Dashed,
    StrokeWidth = 2.5
  };

  /// <summary>
  /// Pulsing glow effect (breathing opacity).
  /// </summary>
  public static EdgeStyle Pulse { get; } = new()
  {
    StrokeColor = "#00BCD4",
    PulseEffect = true,
    PulseFrequency = 1.0,
    PulseMinOpacity = 0.4,
    Glow = true,
    GlowIntensity = 2.0
  };

  /// <summary>
  /// Fast heartbeat-style pulse effect (red, quick pulses).
  /// </summary>
  public static EdgeStyle Heartbeat { get; } = new()
  {
    StrokeColor = "#E53935",
    PulseEffect = true,
    PulseFrequency = 1.5,
    PulseMinOpacity = 0.5,
    StrokeWidth = 2.5
  };

  /// <summary>
  /// Neon glow style (bright cyan with strong glow and flow).
  /// </summary>
  public static EdgeStyle Neon { get; } = new()
  {
    StrokeColor = "#00FFFF",
    AnimatedFlow = true,
    FlowSpeed = 2.0,
    DashPattern = EdgeDashPattern.Dotted,
    Glow = true,
    GlowIntensity = 3.0,
    GlowColor = "#00FFFF"
  };

  /// <summary>
  /// Signal transmission style (fast-moving pulses).
  /// </summary>
  public static EdgeStyle Signal { get; } = new()
  {
    StrokeColor = "#76FF03",
    AnimatedFlow = true,
    FlowSpeed = 4.0,
    DashPattern = EdgeDashPattern.Dotted,
    StrokeWidth = 1.5
  };

  #endregion

  /// <summary>
  /// Creates a copy with the specified stroke color.
  /// </summary>
  public EdgeStyle WithColor(string color) => this with { StrokeColor = color };

  /// <summary>
  /// Creates a copy with animated flow enabled.
  /// </summary>
  public EdgeStyle WithFlow(double speed = 1.0, EdgeFlowDirection direction = EdgeFlowDirection.Forward) =>
      this with { AnimatedFlow = true, FlowSpeed = speed, FlowDirection = direction };

  /// <summary>
  /// Creates a copy with glow effect enabled.
  /// </summary>
  public EdgeStyle WithGlow(double intensity = 1.5, string? color = null) =>
      this with { Glow = true, GlowIntensity = intensity, GlowColor = color };

  /// <summary>
  /// Creates a copy with the specified dash pattern.
  /// </summary>
  public EdgeStyle WithDash(EdgeDashPattern pattern) =>
      this with { DashPattern = pattern };

  /// <summary>
  /// Creates a copy with specified stroke width.
  /// </summary>
  public EdgeStyle WithWidth(double width) =>
      this with { StrokeWidth = width };

  /// <summary>
  /// Creates a copy with specified opacity.
  /// </summary>
  public EdgeStyle WithOpacity(double opacity) =>
      this with { Opacity = Math.Clamp(opacity, 0.0, 1.0) };

  /// <summary>
  /// Creates a copy with rainbow color cycling enabled.
  /// </summary>
  public EdgeStyle WithRainbow(double speed = 0.5) =>
      this with { RainbowEffect = true, RainbowSpeed = speed };

  /// <summary>
  /// Creates a copy with pulsing opacity effect enabled.
  /// </summary>
  public EdgeStyle WithPulse(double frequency = 1.0, double minOpacity = 0.3) =>
      this with { PulseEffect = true, PulseFrequency = frequency, PulseMinOpacity = minOpacity };
}

/// <summary>
/// Predefined dash patterns for edge strokes.
/// </summary>
public enum EdgeDashPattern
{
  /// <summary>
  /// Solid line (no dashes).
  /// </summary>
  Solid,

  /// <summary>
  /// Standard dashed line (long dashes).
  /// </summary>
  Dashed,

  /// <summary>
  /// Dotted line (small dots).
  /// </summary>
  Dotted,

  /// <summary>
  /// Dash-dot pattern (dash, dot, dash, dot).
  /// </summary>
  DashDot,

  /// <summary>
  /// Long dash pattern.
  /// </summary>
  LongDash,

  /// <summary>
  /// Custom dash array (use <see cref="EdgeStyle.CustomDashArray"/>).
  /// </summary>
  Custom
}

/// <summary>
/// Direction of animated flow effect on edges.
/// </summary>
public enum EdgeFlowDirection
{
  /// <summary>
  /// Flow from source to target (default).
  /// </summary>
  Forward,

  /// <summary>
  /// Flow from target to source.
  /// </summary>
  Reverse,

  /// <summary>
  /// Alternating flow direction.
  /// </summary>
  Bidirectional
}
