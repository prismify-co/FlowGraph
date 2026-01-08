using FlowGraph.Core;
using FlowGraph.Core.Models;
using System.Collections.Immutable;

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
        var shapeColorNode = CreateNode(
            id: "shape-color",
            type: "colorpicker",
            label: "shape color",
            x: 50, y: 50,
            width: 160, height: 100,
            outputs: [new PortDefinition { Id = "color", Type = "color", Label = "Color" }]
        );

        // Shape Type input node (left side, middle)
        var shapeTypeNode = CreateNode(
            id: "shape-type",
            type: "radiobutton",
            label: "shape type",
            x: 50, y: 180,
            width: 160, height: 120,
            data: new List<string> { "cube", "pyramid" },
            outputs: [new PortDefinition { Id = "type", Type = "string", Label = "Type" }]
        );

        // Zoom Level input node (left side, bottom)
        var zoomLevelNode = CreateNode(
            id: "zoom-level",
            type: "zoomslider",
            label: "zoom level",
            x: 50, y: 330,
            width: 160, height: 90,
            outputs: [new PortDefinition { Id = "zoom", Type = "number", Label = "Zoom" }]
        );

        // Output node (right side) - uses 3D renderer
        var outputNode = CreateNode(
            id: "output",
            type: "output3d",
            label: "3D output",
            x: 350, y: 100,
            width: 220, height: 260,
            inputs: [
                new PortDefinition { Id = "color", Type = "color", Label = "Color" },
                new PortDefinition { Id = "shape", Type = "string", Label = "Shape" },
                new PortDefinition { Id = "zoom", Type = "number", Label = "Zoom" }
            ]
        );

        // Add nodes
        MyGraph.AddNode(shapeColorNode);
        MyGraph.AddNode(shapeTypeNode);
        MyGraph.AddNode(zoomLevelNode);
        MyGraph.AddNode(outputNode);

        // Add edges with dashed/curved style
        MyGraph.AddEdge(CreateEdge(
            source: shapeColorNode.Id,
            target: outputNode.Id,
            sourcePort: "color",
            targetPort: "color",
            markerEnd: EdgeMarker.None
        ));

        MyGraph.AddEdge(CreateEdge(
            source: shapeTypeNode.Id,
            target: outputNode.Id,
            sourcePort: "type",
            targetPort: "shape",
            markerEnd: EdgeMarker.None
        ));

        MyGraph.AddEdge(CreateEdge(
            source: zoomLevelNode.Id,
            target: outputNode.Id,
            sourcePort: "zoom",
            targetPort: "zoom",
            markerEnd: EdgeMarker.None
        ));
    }

    private static Node CreateNode(
        string id,
        string type,
        string? label = null,
        double x = 0,
        double y = 0,
        double? width = null,
        double? height = null,
        object? data = null,
        IEnumerable<PortDefinition>? inputs = null,
        IEnumerable<PortDefinition>? outputs = null)
    {
        var definition = new NodeDefinition
        {
            Id = id,
            Type = type,
            Label = label,
            Data = data,
            Inputs = inputs?.ToImmutableList() ?? [],
            Outputs = outputs?.ToImmutableList() ?? []
        };

        var state = new NodeState
        {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };

        return new Node(definition, state);
    }

    private static Edge CreateEdge(
        string source,
        string target,
        string sourcePort,
        string targetPort,
        EdgeType type = EdgeType.Bezier,
        EdgeMarker markerEnd = EdgeMarker.Arrow)
    {
        var definition = new EdgeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Source = source,
            Target = target,
            SourcePort = sourcePort,
            TargetPort = targetPort,
            Type = type,
            MarkerEnd = markerEnd
        };

        return new Edge(definition, new EdgeState());
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

