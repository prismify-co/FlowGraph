using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Core;
using FlowGraph.Core.Elements.Shapes;
using FlowGraph.Demo.Theme;
using static FlowGraph.Demo.Theme.DesignTokens;

namespace FlowGraph.Demo.Pages;

/// <summary>
/// Demonstrates shape elements and Z-index layering.
/// </summary>
public class ShapesDemoPage : IDemoPage
{
    public string Title => "Shapes & Z-Index";
    public string Description => "Shape elements with Z-order layering";

    private FlowCanvas? _canvas;
    private FlowBackground? _background;
    private Graph? _graph;
    private TextBlock? _statusText;

    public Control CreateContent()
    {
        _graph = CreateShapesGraph();

        var mainPanel = new DockPanel();

        // Header
        var header = CreateSectionHeader(
            "Shapes & Z-Index Demo",
            "Canvas-first architecture supports shape elements (Rectangle, Ellipse, Line, Text) " +
            "with proper Z-index ordering. Shapes can be layered behind or in front of nodes."
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

        SetStatus($"Shapes: {_graph.Elements.OfElementType<ShapeElement>().Count()} | Nodes: {_graph.Elements.Nodes.Count()}");

        return mainPanel;
    }

    private WrapPanel CreateToolbar()
    {
        var panel = new WrapPanel
        {
            Margin = new Thickness(0, SpaceSm)
        };

        panel.Children.Add(CreateButton("Add Rectangle", AddRectangle));
        panel.Children.Add(CreateButton("Add Ellipse", AddEllipse));
        panel.Children.Add(CreateButton("Add Line", AddLine));
        panel.Children.Add(CreateButton("Add Text", AddText));
        panel.Children.Add(CreateButton("Clear Shapes", ClearShapes));

        return panel;
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.Text = message;
    }

    private void AddRectangle()
    {
        if (_graph == null) return;

        var id = $"rect-{Guid.NewGuid():N}".Substring(0, 12);
        var rect = new RectangleElement(id)
        {
            Position = new Core.Point(Random.Shared.Next(100, 400), Random.Shared.Next(100, 300)),
            Width = Random.Shared.Next(80, 150),
            Height = Random.Shared.Next(50, 100),
            Fill = GetRandomColor(),
            Stroke = "#333333",
            StrokeWidth = 2,
            CornerRadius = 4,
            ZIndex = 100 + _graph.Elements.OfElementType<ShapeElement>().Count()
        };
        _graph.AddElement(rect);
        _canvas?.Refresh();
        SetStatus($"Added rectangle: {id}");
    }

    private void AddEllipse()
    {
        if (_graph == null) return;

        var id = $"ellipse-{Guid.NewGuid():N}".Substring(0, 12);
        var ellipse = new EllipseElement(id)
        {
            Position = new Core.Point(Random.Shared.Next(100, 400), Random.Shared.Next(100, 300)),
            Width = Random.Shared.Next(60, 120),
            Height = Random.Shared.Next(40, 80),
            Fill = GetRandomColor(),
            Stroke = "#333333",
            StrokeWidth = 2,
            ZIndex = 100 + _graph.Elements.OfElementType<ShapeElement>().Count()
        };
        _graph.AddElement(ellipse);
        _canvas?.Refresh();
        SetStatus($"Added ellipse: {id}");
    }

    private void AddLine()
    {
        if (_graph == null) return;

        var id = $"line-{Guid.NewGuid():N}".Substring(0, 12);
        var line = new LineElement(id)
        {
            Position = new Core.Point(Random.Shared.Next(100, 300), Random.Shared.Next(100, 200)),
            EndX = Random.Shared.Next(50, 150),
            EndY = Random.Shared.Next(-50, 100),
            Stroke = GetRandomColor(),
            StrokeWidth = 3,
            EndCap = LineCapStyle.Arrow,
            ZIndex = 100 + _graph.Elements.OfElementType<ShapeElement>().Count()
        };
        _graph.AddElement(line);
        _canvas?.Refresh();
        SetStatus($"Added line: {id}");
    }

    private void AddText()
    {
        if (_graph == null) return;

        var id = $"text-{Guid.NewGuid():N}".Substring(0, 12);
        var text = new TextElement(id)
        {
            Position = new Core.Point(Random.Shared.Next(100, 400), Random.Shared.Next(100, 300)),
            Width = 150,
            Height = 40,
            Text = $"Text #{_graph.Elements.OfElementType<TextElement>().Count() + 1}",
            FontSize = 14,
            Fill = GetRandomColor(),
            ZIndex = 110 + _graph.Elements.OfElementType<ShapeElement>().Count()
        };
        _graph.AddElement(text);
        _canvas?.Refresh();
        SetStatus($"Added text: {id}");
    }

    private void ClearShapes()
    {
        if (_graph == null) return;

        var shapes = _graph.Elements.OfElementType<ShapeElement>().ToList();
        _graph.RemoveElements(shapes);
        _canvas?.Refresh();
        SetStatus($"Cleared {shapes.Count} shapes");
    }

    private static string GetRandomColor()
    {
        var colors = new[] { "#BBDEFB", "#C8E6C9", "#FFE0B2", "#F8BBD9", "#D1C4E9", "#B2EBF2" };
        return colors[Random.Shared.Next(colors.Length)];
    }

    private Graph CreateShapesGraph()
    {
        var graph = new Graph();

        // Background panel (Z-index 50, behind everything)
        var bgPanel = new RectangleElement("bg-panel")
        {
            Position = new Core.Point(30, 30),
            Width = 540,
            Height = 400,
            Fill = "#FFF8E1",
            Stroke = "#FFB74D",
            StrokeWidth = 2,
            CornerRadius = 8,
            ZIndex = 50
        };
        graph.AddElement(bgPanel);

        // Title
        var title = new TextElement("title")
        {
            Position = new Core.Point(40, 40),
            Width = 520,
            Height = 30,
            Text = "Shape Elements - Z-Index Demonstration",
            FontSize = 16,
            FontWeight = FlowGraph.Core.Elements.Shapes.FontWeight.Bold,
            Fill = "#E65100",
            ZIndex = 60
        };
        graph.AddElement(title);

        // Rectangle (Z-index 100)
        var rect = new RectangleElement("demo-rect")
        {
            Position = new Core.Point(60, 100),
            Width = 120,
            Height = 80,
            Fill = "#BBDEFB",
            Stroke = "#1976D2",
            StrokeWidth = 2,
            CornerRadius = 4,
            ZIndex = 100,
            Label = "Rectangle"
        };
        graph.AddElement(rect);

        // Ellipse (Z-index 100)
        var ellipse = new EllipseElement("demo-ellipse")
        {
            Position = new Core.Point(220, 100),
            Width = 100,
            Height = 70,
            Fill = "#C8E6C9",
            Stroke = "#388E3C",
            StrokeWidth = 2,
            ZIndex = 100,
            Label = "Ellipse"
        };
        graph.AddElement(ellipse);

        // Line (Z-index 95, behind shapes)
        var line = new LineElement("demo-line")
        {
            Position = new Core.Point(360, 110),
            EndX = 120,
            EndY = 0,
            Stroke = "#F57C00",
            StrokeWidth = 3,
            StrokeDashArray = "5,5",
            EndCap = LineCapStyle.Arrow,
            ZIndex = 95,
            Label = "Line"
        };
        graph.AddElement(line);

        // Text (Z-index 110, in front)
        var text = new TextElement("demo-text")
        {
            Position = new Core.Point(60, 220),
            Width = 200,
            Height = 50,
            Text = "Text Element\n(Multi-line support)",
            FontSize = 14,
            Fill = "#D32F2F",
            TextAlignment = FlowGraph.Core.Elements.Shapes.TextAlignment.Left,
            ZIndex = 110
        };
        graph.AddElement(text);

        // Overlapping demonstration
        var overlapRect1 = new RectangleElement("overlap-1")
        {
            Position = new Core.Point(300, 200),
            Width = 100,
            Height = 80,
            Fill = "#E1BEE7",
            Stroke = "#7B1FA2",
            StrokeWidth = 2,
            ZIndex = 100
        };
        graph.AddElement(overlapRect1);

        var overlapRect2 = new RectangleElement("overlap-2")
        {
            Position = new Core.Point(340, 230),
            Width = 100,
            Height = 80,
            Fill = "#B3E5FC",
            Stroke = "#0288D1",
            StrokeWidth = 2,
            ZIndex = 105 // Higher Z-index, appears on top
        };
        graph.AddElement(overlapRect2);

        var overlapLabel = new TextElement("overlap-label")
        {
            Position = new Core.Point(300, 320),
            Width = 160,
            Height = 30,
            Text = "↑ Z-index ordering",
            FontSize = 12,
            Fill = "#666666",
            ZIndex = 110
        };
        graph.AddElement(overlapLabel);

        // Info text
        var info = new TextElement("info")
        {
            Position = new Core.Point(40, 370),
            Width = 520,
            Height = 40,
            Text = "All shapes serialize with V2 format → Use toolbar to add more shapes",
            FontSize = 11,
            Fill = "#757575",
            ZIndex = 60
        };
        graph.AddElement(info);

        return graph;
    }
}
