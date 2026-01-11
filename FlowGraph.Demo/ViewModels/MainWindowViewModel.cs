using FlowGraph.Core;
using FlowGraph.Core.Commands;
using FlowGraph.Core.Elements.Shapes;
using FlowGraph.Core.Models;
using FlowGraph.Core.Serialization;
using System.Collections.Immutable;
using System.IO;

namespace FlowGraph.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Graph MyGraph { get; }
    public CommandHistory CommandHistory { get; }

    // Default dimensions (should match FlowCanvasSettings)
    private const double DefaultNodeWidth = 150;
    private const double DefaultNodeHeight = 80;
    private const double GroupPadding = 20;
    private const double GroupHeaderHeight = 30;

    public MainWindowViewModel()
    {
        MyGraph = new Graph();
        CommandHistory = new CommandHistory();

        // ============================================
        // Element-First Architecture Demo
        // Showcases: Nodes, Edges, Shapes, Z-Order, Commands, Serialization
        // ============================================

        CreateDemoGraph();
    }

    private void CreateDemoGraph()
    {
        // Add decorative title shape (background layer, Z-index 50)
        var titleBox = new RectangleElement("title-box")
        {
            Position = new Core.Point(30, 10),
            Width = 560,
            Height = 50,
            Fill = "#E3F2FD",
            Stroke = "#1976D2",
            StrokeWidth = 2,
            CornerRadius = 8,
            ZIndex = 50,
            Label = "Canvas-First Architecture Demo"
        };
        MyGraph.AddElement(titleBox);

        // Add title text shape (above title box, Z-index 60)
        var titleText = new TextElement("title-text")
        {
            Position = new Core.Point(40, 25),
            Width = 540,
            Height = 30,
            Text = "🎨 Canvas-First Architecture Demo",
            FontSize = 18,
            FontWeight = FlowGraph.Core.Elements.Shapes.FontWeight.Bold,
            Fill = "#1565C0",
            ZIndex = 60
        };
        MyGraph.AddElement(titleText);

        MyGraph.AddElement(titleText);

        // Shape Color input node (left side, top)
        var shapeColorNode = CreateNode(
            id: "shape-color",
            type: "colorpicker",
            label: "shape color",
            x: 50, y: 90,
            width: 160, height: 100,
            outputs: [new PortDefinition { Id = "color", Type = "color", Label = "Color" }]
        );

        // Shape Type input node (left side, middle)
        var shapeTypeNode = CreateNode(
            id: "shape-type",
            type: "radiobutton",
            label: "shape type",
            x: 50, y: 220,
            width: 160, height: 120,
            data: new List<string> { "cube", "pyramid" },
            outputs: [new PortDefinition { Id = "type", Type = "string", Label = "Type" }]
        );

        // Zoom Level input node (left side, bottom)
        var zoomLevelNode = CreateNode(
            id: "zoom-level",
            type: "zoomslider",
            label: "zoom level",
            x: 50, y: 370,
            width: 160, height: 90,
            outputs: [new PortDefinition { Id = "zoom", Type = "number", Label = "Zoom" }]
        );

        // Output node (right side) - uses 3D renderer
        var outputNode = CreateNode(
            id: "output",
            type: "output3d",
            label: "3D output",
            x: 350, y: 140,
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

        // Add decorative annotation shapes to demonstrate Z-order and element types

        // Background panel for shape showcase (Z-index 80, behind nodes which are 300)
        var shapesPanel = new RectangleElement("shapes-panel")
        {
            Position = new Core.Point(30, 500),
            Width = 540,
            Height = 200,
            Fill = "#FFF3E0",
            Stroke = "#F57C00",
            StrokeWidth = 2,
            CornerRadius = 8,
            ZIndex = 80
        };
        MyGraph.AddElement(shapesPanel);

        // Section title
        var shapesTitleText = new TextElement("shapes-title")
        {
            Position = new Core.Point(40, 510),
            Width = 520,
            Height = 30,
            Text = "Shape Elements (Z-order demonstration)",
            FontSize = 14,
            FontWeight = FlowGraph.Core.Elements.Shapes.FontWeight.SemiBold,
            Fill = "#E65100",
            ZIndex = 90
        };
        MyGraph.AddElement(shapesTitleText);

        // Rectangle shape example (Z-index 100)
        var rectExample = new RectangleElement("rect-example")
        {
            Position = new Core.Point(50, 550),
            Width = 100,
            Height = 60,
            Fill = "#BBDEFB",
            Stroke = "#1976D2",
            StrokeWidth = 2,
            CornerRadius = 4,
            ZIndex = 100,
            Label = "Rectangle"
        };
        MyGraph.AddElement(rectExample);

        // Line shape example (Z-index 95, appears behind rectangle)
        var lineExample = new LineElement("line-example")
        {
            Position = new Core.Point(160, 565),
            EndX = 90,
            EndY = 0,
            Stroke = "#F57C00",
            StrokeWidth = 3,
            StrokeDashArray = "5,5",
            StartCap = LineCapStyle.Round,
            EndCap = LineCapStyle.Arrow,
            ZIndex = 95,
            Label = "Line"
        };
        MyGraph.AddElement(lineExample);

        // Ellipse shape example (Z-index 100)
        var ellipseExample = new EllipseElement("ellipse-example")
        {
            Position = new Core.Point(270, 550),
            Width = 80,
            Height = 60,
            Fill = "#C8E6C9",
            Stroke = "#388E3C",
            StrokeWidth = 2,
            ZIndex = 100,
            Label = "Ellipse"
        };
        MyGraph.AddElement(ellipseExample);

        // Text shape example (Z-index 110, in front of all)
        var textExample = new TextElement("text-example")
        {
            Position = new Core.Point(370, 555),
            Width = 180,
            Height = 50,
            Text = "Text Element\n(Multi-line support)",
            FontSize = 12,
            FontWeight = FlowGraph.Core.Elements.Shapes.FontWeight.Normal,
            Fill = "#D32F2F",
            TextAlignment = FlowGraph.Core.Elements.Shapes.TextAlignment.Center,
            ZIndex = 110
        };
        MyGraph.AddElement(textExample);

        // Add annotation arrow pointing to shapes
        var annotationArrow = new LineElement("annotation-arrow")
        {
            Position = new Core.Point(50, 640),
            EndX = 150,
            EndY = 0,
            Stroke = "#9E9E9E",
            StrokeWidth = 2,
            EndCap = LineCapStyle.Arrow,
            ZIndex = 85
        };
        MyGraph.AddElement(annotationArrow);

        var annotationText = new TextElement("annotation-text")
        {
            Position = new Core.Point(210, 630),
            Width = 340,
            Height = 30,
            Text = "← All shapes serialize/deserialize with V2 format",
            FontSize = 11,
            Fill = "#616161",
            ZIndex = 85
        };
        MyGraph.AddElement(annotationText);
    }

    // Element command methods for toolbar buttons
    public void SaveGraph()
    {
        try
        {
            var json = GraphSerializer.Serialize(MyGraph);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "flowgraph-demo.json");
            File.WriteAllText(path, json);
            System.Diagnostics.Debug.WriteLine($"Graph saved to: {path}");
            System.Diagnostics.Debug.WriteLine($"Format version: 2 (elements[] array)");
            System.Diagnostics.Debug.WriteLine($"Element count: {MyGraph.Elements.Count()}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
        }
    }

    public void LoadGraph()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "flowgraph-demo.json");
            if (!File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine("No saved graph found");
                return;
            }

            var json = File.ReadAllText(path);
            var loadedGraph = GraphSerializer.Deserialize(json);

            if (loadedGraph != null)
            {
                // Clear current graph
                var allElements = MyGraph.Elements.ToList();
                foreach (var element in allElements)
                {
                    MyGraph.RemoveElement(element);
                }

                // Add loaded elements
                foreach (var element in loadedGraph.Elements)
                {
                    MyGraph.AddElement(element);
                }

                System.Diagnostics.Debug.WriteLine($"Graph loaded from: {path}");
                System.Diagnostics.Debug.WriteLine($"Element count: {MyGraph.Elements.Count()}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load failed: {ex.Message}");
        }
    }

    public void DemoMoveElements()
    {
        // Demonstrate element-based move command on mixed selection
        var elements = MyGraph.Elements.Where(e => e is ShapeElement).Take(2).ToList();
        if (elements.Count == 0) return;

        var oldPositions = elements.ToDictionary(e => e.Id, e => e.Position);
        var newPositions = elements.ToDictionary(e => e.Id, e => new Core.Point(e.Position.X + 20, e.Position.Y + 20));

        var command = new MoveElementsCommand(MyGraph, oldPositions, newPositions);
        CommandHistory.Execute(command);

        System.Diagnostics.Debug.WriteLine($"Moved {elements.Count} elements (demo)");
    }

    public void DemoUndo()
    {
        if (CommandHistory.CanUndo)
        {
            CommandHistory.Undo();
            System.Diagnostics.Debug.WriteLine("Undo executed");
        }
    }

    public void DemoRedo()
    {
        if (CommandHistory.CanRedo)
        {
            CommandHistory.Redo();
            System.Diagnostics.Debug.WriteLine("Redo executed");
        }
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

