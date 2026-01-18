using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;
using FlowGraph.Core.Elements.Shapes;
using FlowGraph.Core.Models;
using FlowGraph.Demo.Helpers;
using FlowGraph.Demo.Renderers;
using FlowGraph.Demo.Theme;
using System.Collections.Immutable;
using static FlowGraph.Demo.Theme.DesignTokens;

namespace FlowGraph.Demo.Pages;

/// <summary>
/// Interactive 3D demo showcasing data flow between custom nodes.
/// </summary>
public class InteractiveDemoPage : IDemoPage
{
    public string Title => "Interactive 3D Demo";
    public string Description => "Data flow with custom node renderers";

    private FlowCanvas? _canvas;
    private FlowBackground? _background;
    private Graph? _graph;
    private TextBlock? _statusText;

    public Control CreateContent()
    {
        _graph = CreateDemoGraph();

        var mainPanel = new DockPanel();

        // Header
        var header = CreateSectionHeader(
            "Interactive 3D Demo",
            "Demonstrates data flow between custom node renderers. " +
            "Change the color picker, shape selector, or zoom slider to see the 3D output update in real-time."
        );
        DockPanel.SetDock(header, Dock.Top);
        mainPanel.Children.Add(header);

        // Toolbar
        var toolbar = CreateToolbar();
        DockPanel.SetDock(toolbar, Dock.Top);
        mainPanel.Children.Add(toolbar);

        // Status bar
        var statusBar = CreateStatusBar(out _statusText);
        DockPanel.SetDock(statusBar, Dock.Bottom);
        mainPanel.Children.Add(statusBar);

        // Canvas with background and controls
        var canvasPanel = new Panel();

        _background = new FlowBackground
        {
            Variant = BackgroundVariant.Dots,
            Gap = 20,
            Size = 2,
            Color = Color.Parse("#40808080")
        };
        canvasPanel.Children.Add(_background);

        _canvas = new FlowCanvas
        {
            Graph = _graph,
            Settings = new FlowCanvasSettings
            {
                ShowGrid = false,
                ShowBackground = false,
                SnapToGrid = true,
                PanOnDrag = true
            }
        };

        // Register custom renderers
        RegisterCustomNodeRenderers();

        _background.TargetCanvas = _canvas;
        canvasPanel.Children.Add(_canvas);

        // Add FlowControls panel (positioned bottom-left)
        var flowControls = new FlowControls
        {
            TargetCanvas = _canvas,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16)
        };
        canvasPanel.Children.Add(flowControls);

        // Add FlowMinimap (positioned bottom-right)
        var minimap = new FlowMinimap
        {
            TargetCanvas = _canvas,
            Width = 150,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16)
        };
        canvasPanel.Children.Add(minimap);

        var canvasContainer = CreateCanvasContainer(canvasPanel);
        mainPanel.Children.Add(canvasContainer);

        // Setup data flow after layout
        Dispatcher.UIThread.Post(SetupDataFlow, DispatcherPriority.Background);

        return mainPanel;
    }

    public void OnNavigatingFrom()
    {
        // Clean up data flow
        _canvas?.DisableDataFlow();
    }

    private WrapPanel CreateToolbar()
    {
        var panel = new WrapPanel
        {
            Margin = new Thickness(0, SpaceSm)
        };

        panel.Children.Add(CreateButton("Dots", () => SetBackground(BackgroundVariant.Dots)));
        panel.Children.Add(CreateButton("Lines", () => SetBackground(BackgroundVariant.Lines)));
        panel.Children.Add(CreateButton("Cross", () => SetBackground(BackgroundVariant.Cross)));

        return panel;
    }

    private void SetBackground(BackgroundVariant variant)
    {
        if (_background != null)
        {
            _background.Variant = variant;
            SetStatus($"Background: {variant}");
        }
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.Text = message;
    }

    private void RegisterCustomNodeRenderers()
    {
        if (_canvas == null) return;

        // Use ShadUI-styled color picker for better UX
        _canvas.NodeRenderers.Register("colorpicker", new ShadColorPickerNodeRenderer());
        _canvas.NodeRenderers.Register("radiobutton", new RadioButtonNodeRenderer());
        _canvas.NodeRenderers.Register("zoomslider", new ZoomSliderNodeRenderer());
        _canvas.NodeRenderers.Register("outputdisplay", new OutputDisplayNodeRenderer());
        _canvas.NodeRenderers.Register("output3d", new Output3DNodeRenderer());
    }

    private void SetupDataFlow()
    {
        if (_canvas?.Graph == null) return;

        var executor = _canvas.EnableDataFlow();
        var graph = _canvas.Graph;

        var colorNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == "shape-color");
        var shapeNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == "shape-type");
        var zoomNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == "zoom-level");
        var outputNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == "output");

        if (colorNode != null)
            _canvas.CreateInputProcessor<Color>(colorNode, "color", Color.FromRgb(255, 0, 113));

        if (shapeNode != null)
            _canvas.CreateInputProcessor<string>(shapeNode, "type", "cube");

        if (zoomNode != null)
            _canvas.CreateInputProcessor<double>(zoomNode, "zoom", 50.0);

        if (outputNode != null)
        {
            var processor = new MultiInputOutputProcessor(outputNode);
            _canvas.RegisterProcessor(processor);
        }

        _canvas.ExecuteGraph();
        SetStatus("Data flow enabled - interact with nodes to see updates");
    }

    private Graph CreateDemoGraph()
    {
        var graph = new Graph();

        // Title box
        var titleBox = new RectangleElement("title-box")
        {
            Position = new Core.Point(30, 10),
            Width = 560,
            Height = 50,
            Fill = "#E3F2FD",
            Stroke = "#1976D2",
            StrokeWidth = 2,
            CornerRadius = 8,
            ZIndex = 50
        };
        graph.AddElement(titleBox);

        var titleText = new TextElement("title-text")
        {
            Position = new Core.Point(40, 25),
            Width = 540,
            Height = 30,
            Text = "🎨 Interactive 3D Demo - Data Flow",
            FontSize = 18,
            FontWeight = FlowGraph.Core.Elements.Shapes.FontWeight.Bold,
            Fill = "#1565C0",
            ZIndex = 60
        };
        graph.AddElement(titleText);

        // Color picker node (ShadUI Card with ColorView)
        var colorNode = CreateNode("shape-color", "colorpicker", "shape color",
            50, 90, 160, 100,
            outputs: [new PortDefinition { Id = "color", Type = "color", Label = "Color" }]);
        graph.AddNode(colorNode);

        // Shape type node
        var shapeNode = CreateNode("shape-type", "radiobutton", "shape type",
            50, 220, 160, 120,
            data: new List<string> { "cube", "pyramid" },
            outputs: [new PortDefinition { Id = "type", Type = "string", Label = "Type" }]);
        graph.AddNode(shapeNode);

        // Zoom slider node
        var zoomNode = CreateNode("zoom-level", "zoomslider", "zoom level",
            50, 370, 160, 90,
            outputs: [new PortDefinition { Id = "zoom", Type = "number", Label = "Zoom" }]);
        graph.AddNode(zoomNode);

        // 3D output node
        var outputNode = CreateNode("output", "output3d", "3D output",
            350, 140, 220, 260,
            inputs: [
                new PortDefinition { Id = "color", Type = "color", Label = "Color" },
                new PortDefinition { Id = "shape", Type = "string", Label = "Shape" },
                new PortDefinition { Id = "zoom", Type = "number", Label = "Zoom" }
            ]);
        graph.AddNode(outputNode);

        // Edges
        graph.AddEdge(CreateEdge(colorNode.Id, outputNode.Id, "color", "color"));
        graph.AddEdge(CreateEdge(shapeNode.Id, outputNode.Id, "type", "shape"));
        graph.AddEdge(CreateEdge(zoomNode.Id, outputNode.Id, "zoom", "zoom"));

        return graph;
    }

    private static Node CreateNode(
        string id, string type, string label,
        double x, double y, double width, double height,
        ImmutableList<PortDefinition>? inputs = null,
        ImmutableList<PortDefinition>? outputs = null,
        object? data = null)
    {
        var definition = new NodeDefinition
        {
            Id = id,
            Type = type,
            Label = label,
            Inputs = inputs ?? ImmutableList<PortDefinition>.Empty,
            Outputs = outputs ?? ImmutableList<PortDefinition>.Empty,
            Data = data
        };

        var state = new NodeState { X = x, Y = y, Width = width, Height = height };
        return new Node(definition, state);
    }

    private static Edge CreateEdge(string source, string target, string sourcePort, string targetPort)
    {
        return new Edge
        {
            Source = source,
            Target = target,
            SourcePort = sourcePort,
            TargetPort = targetPort,
            Type = EdgeType.Bezier
        };
    }
}
