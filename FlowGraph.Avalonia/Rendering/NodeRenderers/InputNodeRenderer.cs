using Avalonia.Media;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for input nodes with a distinctive green color scheme.
/// Displays a ">" icon and supports inline label editing.
/// </summary>
public class InputNodeRenderer : StyledNodeRendererBase
{
    // Simple arrow-right icon path (Lucide-compatible format)
    private static readonly Geometry ArrowRightIcon = Geometry.Parse("M5 12h14m-7-7 7 7-7 7");

    /// <inheritdoc />
    protected override Geometry? IconGeometry => ArrowRightIcon;

    /// <inheritdoc />
    protected override string DefaultLabel => "Input";

    /// <inheritdoc />
    protected override IBrush GetNodeBackground(ThemeResources theme) => theme.InputNodeBackground;

    /// <inheritdoc />
    protected override IBrush GetNodeBorder(ThemeResources theme) => theme.InputNodeBorder;

    /// <inheritdoc />
    protected override IBrush GetIconForeground(ThemeResources theme) => theme.InputNodeIcon;

    /// <inheritdoc />
    protected override IBrush GetTextForeground(ThemeResources theme) => theme.InputNodeText;
}
