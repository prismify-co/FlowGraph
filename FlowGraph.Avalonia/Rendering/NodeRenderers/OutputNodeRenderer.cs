using Avalonia.Media;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renderer for output nodes with a distinctive red/coral color scheme.
/// Displays a target/circle icon and supports inline label editing.
/// </summary>
public class OutputNodeRenderer : StyledNodeRendererBase
{
    // Simple target/bullseye icon path
    private static readonly Geometry TargetIcon = Geometry.Parse("M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm0 4a6 6 0 1 1 0 12 6 6 0 0 1 0-12zm0 4a2 2 0 1 0 0 4 2 2 0 0 0 0-4z");

    /// <inheritdoc />
    protected override Geometry? IconGeometry => TargetIcon;

    /// <inheritdoc />
    protected override string DefaultLabel => "Output";

    /// <inheritdoc />
    protected override IBrush GetNodeBackground(ThemeResources theme) => theme.OutputNodeBackground;

    /// <inheritdoc />
    protected override IBrush GetNodeBorder(ThemeResources theme) => theme.OutputNodeBorder;

    /// <inheritdoc />
    protected override IBrush GetIconForeground(ThemeResources theme) => theme.OutputNodeIcon;

    /// <inheritdoc />
    protected override IBrush GetTextForeground(ThemeResources theme) => theme.OutputNodeText;
}
