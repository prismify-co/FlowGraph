namespace FlowGraph.Core.Models;

/// <summary>
/// Specifies where along an edge the label should be anchored.
/// </summary>
/// <remarks>
/// The anchor point determines the base position of the label before any offsets are applied.
/// Use <see cref="LabelInfo.OffsetX"/> and <see cref="LabelInfo.OffsetY"/> for fine-tuning.
/// </remarks>
public enum LabelAnchor
{
  /// <summary>
  /// Position the label near the start of the edge (approximately 25% along the path).
  /// Useful for labels that describe the source or origin of the connection.
  /// </summary>
  Start,

  /// <summary>
  /// Position the label at the center/midpoint of the edge (50% along the path).
  /// This is the default and most common placement.
  /// </summary>
  Center,

  /// <summary>
  /// Position the label near the end of the edge (approximately 75% along the path).
  /// Useful for labels that describe the destination or target of the connection.
  /// </summary>
  End
}

/// <summary>
/// Provides enhanced label positioning for edges with anchor points and offsets.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="LabelInfo"/> extends the basic <see cref="EdgeDefinition.Label"/> string 
/// by adding precise positioning control. When both <see cref="EdgeDefinition.Label"/> 
/// and <see cref="EdgeDefinition.LabelInfo"/> are present, <see cref="EdgeDefinition.LabelInfo"/> 
/// takes precedence.
/// </para>
/// <para>
/// This design allows backward compatibility with existing code that uses the simple Label 
/// property while enabling advanced positioning for new scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple centered label
/// var label = new LabelInfo("Connection");
/// 
/// // Label near the start with offset
/// var startLabel = new LabelInfo("Out", LabelAnchor.Start, OffsetY: -15);
/// 
/// // Label near the end, shifted right
/// var endLabel = new LabelInfo("In", LabelAnchor.End, OffsetX: 10);
/// 
/// // Use with edge definition
/// var edge = new EdgeDefinition
/// {
///     Id = "e1",
///     Source = "node1",
///     Target = "node2",
///     SourcePort = "out",
///     TargetPort = "in",
///     LabelInfo = new LabelInfo("Yes", LabelAnchor.Start, OffsetY: -10)
/// };
/// </code>
/// </example>
public sealed record LabelInfo
{
  /// <summary>
  /// The text content of the label.
  /// </summary>
  public string Text { get; init; }

  /// <summary>
  /// Where along the edge to anchor the label.
  /// </summary>
  /// <remarks>
  /// <list type="bullet">
  /// <item><see cref="LabelAnchor.Start"/>: ~25% along the edge path</item>
  /// <item><see cref="LabelAnchor.Center"/>: ~50% along the edge path (default)</item>
  /// <item><see cref="LabelAnchor.End"/>: ~75% along the edge path</item>
  /// </list>
  /// </remarks>
  public LabelAnchor Anchor { get; init; } = LabelAnchor.Center;

  /// <summary>
  /// Horizontal offset in pixels from the anchor point. Positive values move right.
  /// </summary>
  public double OffsetX { get; init; }

  /// <summary>
  /// Vertical offset in pixels from the anchor point. Positive values move down.
  /// </summary>
  public double OffsetY { get; init; }

  /// <summary>
  /// Creates a new label info with the specified text and positioning.
  /// </summary>
  /// <param name="text">The label text.</param>
  /// <param name="anchor">Where along the edge to position the label. Defaults to center.</param>
  /// <param name="offsetX">Horizontal offset from anchor point. Defaults to 0.</param>
  /// <param name="offsetY">Vertical offset from anchor point. Defaults to 0.</param>
  public LabelInfo(string text, LabelAnchor anchor = LabelAnchor.Center, double offsetX = 0, double offsetY = 0)
  {
    Text = text ?? throw new ArgumentNullException(nameof(text));
    Anchor = anchor;
    OffsetX = offsetX;
    OffsetY = offsetY;
  }

  /// <summary>
  /// Creates a LabelInfo from a simple string label (centered, no offset).
  /// </summary>
  public static implicit operator LabelInfo(string text) => new(text);

  /// <summary>
  /// Creates a simple centered label from this info (extracts just the text).
  /// </summary>
  public override string ToString() => Text;
}
