using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Controls;

/// <summary>
/// High-performance minimap rendering using direct DrawingContext instead of UI elements.
/// This avoids the O(n) overhead of creating Rectangle/Line controls for each node/edge.
/// </summary>
public class MinimapRenderControl : Control
{
    // Cached brushes and pens (created once, reused)
    private static readonly IBrush EdgeBrush = new SolidColorBrush(Color.Parse("#808080")).ToImmutable();
    private static readonly IPen EdgePen = new Pen(EdgeBrush, 1);
    private static readonly IBrush GroupBrush = new SolidColorBrush(Color.FromArgb(40, 132, 94, 194)).ToImmutable();
    private static readonly IBrush CollapsedGroupBrush = new SolidColorBrush(Color.FromArgb(80, 132, 94, 194)).ToImmutable();
    private static readonly IBrush GroupBorderBrush = new SolidColorBrush(Color.Parse("#845EC2")).ToImmutable();
    private static readonly IPen GroupBorderPen = new Pen(GroupBorderBrush, 1);
    private static readonly IBrush NodeBrush = new SolidColorBrush(Color.Parse("#4682B4")).ToImmutable();
    private static readonly IBrush SelectedNodeBrush = new SolidColorBrush(Color.Parse("#FF6B00")).ToImmutable();
    private static readonly IBrush ViewportFillBrush = new SolidColorBrush(Color.FromArgb(25, 14, 165, 233)).ToImmutable();
    private static readonly IBrush ViewportStrokeBrush = new SolidColorBrush(Color.Parse("#0EA5E9")).ToImmutable();
    private static readonly IPen ViewportPen = new Pen(ViewportStrokeBrush, 2);

    private const double DefaultNodeWidth = 150;
    private const double DefaultNodeHeight = 80;

    // Render data - set by FlowMinimap, read during Render
    internal Graph? Graph { get; set; }
    internal List<Node>? VisibleNodes { get; set; }
    internal Dictionary<string, Node>? NodeById { get; set; }
    internal double Scale { get; set; }
    internal double ExtentX { get; set; }
    internal double ExtentY { get; set; }
    internal Rect ViewportBounds { get; set; }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Graph == null || VisibleNodes == null || VisibleNodes.Count == 0 || NodeById == null)
            return;

        var graph = Graph;
        var visibleNodes = VisibleNodes;
        var nodeById = NodeById;

        // Build set of visible node IDs for edge filtering
        var visibleNodeIds = new HashSet<string>(visibleNodes.Count);
        foreach (var node in visibleNodes)
        {
            visibleNodeIds.Add(node.Id);
        }

        // Draw edges (simple lines between node centers)
        foreach (var edge in graph.Elements.Edges)
        {
            if (!visibleNodeIds.Contains(edge.Source) || !visibleNodeIds.Contains(edge.Target))
                continue;

            if (!nodeById.TryGetValue(edge.Source, out var sourceNode) ||
                !nodeById.TryGetValue(edge.Target, out var targetNode))
                continue;

            var startPos = CanvasToMinimap(
                sourceNode.Position.X + GetNodeWidth(sourceNode) / 2,
                sourceNode.Position.Y + GetNodeHeight(sourceNode) / 2);
            var endPos = CanvasToMinimap(
                targetNode.Position.X + GetNodeWidth(targetNode) / 2,
                targetNode.Position.Y + GetNodeHeight(targetNode) / 2);

            context.DrawLine(EdgePen, startPos, endPos);
        }

        // Draw groups first (behind regular nodes)
        foreach (var node in visibleNodes)
        {
            if (!node.IsGroup) continue;

            var pos = CanvasToMinimap(node.Position.X, node.Position.Y);
            var width = Math.Max(GetNodeWidth(node) * Scale, 8);
            var height = Math.Max(GetNodeHeight(node) * Scale, 6);
            var rect = new Rect(pos.X, pos.Y, width, height);

            var fill = node.IsCollapsed ? CollapsedGroupBrush : GroupBrush;
            context.DrawRectangle(fill, GroupBorderPen, rect, 3, 3);
        }

        // Draw regular nodes (on top of groups)
        foreach (var node in visibleNodes)
        {
            if (node.IsGroup) continue;

            var pos = CanvasToMinimap(node.Position.X, node.Position.Y);
            var width = Math.Max(GetNodeWidth(node) * Scale, 4);
            var height = Math.Max(GetNodeHeight(node) * Scale, 3);
            var rect = new Rect(pos.X, pos.Y, width, height);

            var fill = node.IsSelected ? SelectedNodeBrush : NodeBrush;
            context.DrawRectangle(fill, null, rect, 2, 2);
        }

        // Draw viewport rectangle
        if (ViewportBounds.Width > 0 && ViewportBounds.Height > 0)
        {
            var vpTopLeft = CanvasToMinimap(ViewportBounds.X, ViewportBounds.Y);
            var vpWidth = Math.Max(ViewportBounds.Width * Scale, 10);
            var vpHeight = Math.Max(ViewportBounds.Height * Scale, 10);
            var vpRect = new Rect(vpTopLeft.X, vpTopLeft.Y, vpWidth, vpHeight);

            context.DrawRectangle(ViewportFillBrush, ViewportPen, vpRect);
        }
    }

    private AvaloniaPoint CanvasToMinimap(double canvasX, double canvasY)
    {
        return new AvaloniaPoint(
            (canvasX - ExtentX) * Scale,
            (canvasY - ExtentY) * Scale
        );
    }

    private static double GetNodeWidth(Node node)
    {
        return node.Width ?? DefaultNodeWidth;
    }

    private static double GetNodeHeight(Node node)
    {
        if (node.IsGroup && node.IsCollapsed)
        {
            return 28; // Header height only for collapsed groups
        }
        return node.Height ?? DefaultNodeHeight;
    }
}
