using FlowGraph.Core;

namespace FlowGraph.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Graph MyGraph { get; }

    // Default dimensions (should match FlowCanvasSettings)
    private const double DefaultNodeWidth = 150;
    private const double DefaultNodeHeight = 80;
    private const double GroupPadding = 20;
    private const double GroupHeaderHeight = 30;

    public MainWindowViewModel()
    {
        MyGraph = new Graph();

        // ============================================
        // React Flow Style Demo - Shape Configurator
        // ============================================

        // Shape Color input node (left side, top)
        var shapeColorNode = new Node
        {
            Id = "shape-color",
            Type = "colorpicker",
            Label = "shape color",
            Position = new Core.Point(50, 50),
            Width = 160,
            Height = 100,
            Outputs = [new Port { Id = "color", Type = "color", Label = "Color" }]
        };

        // Shape Type input node (left side, middle)
        var shapeTypeNode = new Node
        {
            Id = "shape-type",
            Type = "radiobutton",
            Label = "shape type",
            Position = new Core.Point(50, 180),
            Width = 160,
            Height = 120,
            Data = new List<string> { "cube", "pyramid" },
            Outputs = [new Port { Id = "type", Type = "string", Label = "Type" }]
        };

        // Zoom Level input node (left side, bottom)
        var zoomLevelNode = new Node
        {
            Id = "zoom-level",
            Type = "zoomslider",
            Label = "zoom level",
            Position = new Core.Point(50, 330),
            Width = 160,
            Height = 90,
            Outputs = [new Port { Id = "zoom", Type = "number", Label = "Zoom" }]
        };

        // Output node (right side)
        var outputNode = new Node
        {
            Id = "output",
            Type = "outputdisplay",
            Label = "output",
            Position = new Core.Point(350, 100),
            Width = 220,
            Height = 260,
            Inputs = [
                new Port { Id = "color", Type = "color", Label = "Color" },
                new Port { Id = "type", Type = "string", Label = "Type" },
                new Port { Id = "zoom", Type = "number", Label = "Zoom" }
            ]
        };

        // Add nodes
        MyGraph.AddNode(shapeColorNode);
        MyGraph.AddNode(shapeTypeNode);
        MyGraph.AddNode(zoomLevelNode);
        MyGraph.AddNode(outputNode);

        // Add edges with dashed/curved style
        MyGraph.AddEdge(new Edge
        {
            Source = shapeColorNode.Id,
            Target = outputNode.Id,
            SourcePort = "color",
            TargetPort = "color",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.None
        });

        MyGraph.AddEdge(new Edge
        {
            Source = shapeTypeNode.Id,
            Target = outputNode.Id,
            SourcePort = "type",
            TargetPort = "type",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.None
        });

        MyGraph.AddEdge(new Edge
        {
            Source = zoomLevelNode.Id,
            Target = outputNode.Id,
            SourcePort = "zoom",
            TargetPort = "zoom",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.None
        });
    }

    private static (Core.Point position, double width, double height) CalculateGroupBounds(Node[] nodes)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in nodes)
        {
            var nodeWidth = node.Width ?? DefaultNodeWidth;
            var nodeHeight = node.Height ?? DefaultNodeHeight;

            minX = Math.Min(minX, node.Position.X);
            minY = Math.Min(minY, node.Position.Y);
            maxX = Math.Max(maxX, node.Position.X + nodeWidth);
            maxY = Math.Max(maxY, node.Position.Y + nodeHeight);
        }

        var position = new Core.Point(minX - GroupPadding, minY - GroupPadding - GroupHeaderHeight);
        var width = maxX - minX + GroupPadding * 2;
        var height = maxY - minY + GroupPadding * 2 + GroupHeaderHeight;

        return (position, width, height);
    }
}

