using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using System.Collections.Immutable;
using static FlowGraph.Demo.Theme.DesignTokens;

namespace FlowGraph.Demo.Pages;

/// <summary>
/// Demo page showcasing animated edge styles.
/// </summary>
public class EdgeStylesDemoPage : IDemoPage
{
  public string Title => "Edge Styles";
  public string Description => "Animated edge flow effects";

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
        "Edge Styles & Animations",
        "Demonstrates declarative edge styling with animated flow effects. " +
        "Edges automatically animate based on their EdgeStyle.AnimatedFlow setting."
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

    // Canvas with background
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

    var canvasContainer = CreateCanvasContainer(canvasPanel);
    mainPanel.Children.Add(canvasContainer);

    // Update status bar periodically
    var timer = new System.Timers.Timer(500);
    timer.Elapsed += (_, _) => Dispatcher.UIThread.InvokeAsync(UpdateStatus);
    timer.Start();

    return mainPanel;
  }

  public void OnNavigatingFrom()
  {
    // Clean up if needed
  }

  private Graph CreateDemoGraph()
  {
    var graph = new Graph();

    // Create source nodes on the left
    var sourceNode = CreateNode(
        id: "source",
        label: "Data Source",
        x: 50, y: 150,
        width: 140, height: 80,
        type: "source",
        outputs: [
            new PortDefinition { Id = "flow-out", Type = "flow", Label = "Flow" },
            new PortDefinition { Id = "data-out", Type = "data", Label = "Data" },
            new PortDefinition { Id = "status-out", Type = "status", Label = "Status" },
            new PortDefinition { Id = "extra-out", Type = "extra", Label = "Extra" }
        ]);

    var sourceNode2 = CreateNode(
        id: "source2",
        label: "Effects Source",
        x: 50, y: 350,
        width: 140, height: 80,
        type: "source",
        outputs: [
            new PortDefinition { Id = "rainbow-out", Type = "rainbow", Label = "Rainbow" },
            new PortDefinition { Id = "pulse-out", Type = "pulse", Label = "Pulse" },
            new PortDefinition { Id = "neon-out", Type = "neon", Label = "Neon" }
        ]);

    // Create processing nodes in the middle
    var processNode1 = CreateNode(
        id: "process1",
        label: "Processor A",
        x: 300, y: 30,
        width: 140, height: 70,
        type: "processor",
        inputs: [new PortDefinition { Id = "in", Type = "data", Label = "Input" }],
        outputs: [new PortDefinition { Id = "out", Type = "data", Label = "Output" }]);

    var processNode2 = CreateNode(
        id: "process2",
        label: "Processor B",
        x: 300, y: 130,
        width: 140, height: 70,
        type: "processor",
        inputs: [new PortDefinition { Id = "in", Type = "data", Label = "Input" }],
        outputs: [new PortDefinition { Id = "out", Type = "data", Label = "Output" }]);

    var processNode3 = CreateNode(
        id: "process3",
        label: "Processor C",
        x: 300, y: 230,
        width: 140, height: 70,
        type: "processor",
        inputs: [new PortDefinition { Id = "in", Type = "data", Label = "Input" }],
        outputs: [new PortDefinition { Id = "out", Type = "data", Label = "Output" }]);

    var processNode4 = CreateNode(
        id: "process4",
        label: "Rainbow FX",
        x: 300, y: 330,
        width: 140, height: 70,
        type: "processor",
        inputs: [new PortDefinition { Id = "in", Type = "data", Label = "Input" }],
        outputs: [new PortDefinition { Id = "out", Type = "data", Label = "Output" }]);

    var processNode5 = CreateNode(
        id: "process5",
        label: "Pulse FX",
        x: 300, y: 430,
        width: 140, height: 70,
        type: "processor",
        inputs: [new PortDefinition { Id = "in", Type = "data", Label = "Input" }],
        outputs: [new PortDefinition { Id = "out", Type = "data", Label = "Output" }]);

    var processNode6 = CreateNode(
        id: "process6",
        label: "Neon FX",
        x: 300, y: 530,
        width: 140, height: 70,
        type: "processor",
        inputs: [new PortDefinition { Id = "in", Type = "data", Label = "Input" }],
        outputs: [new PortDefinition { Id = "out", Type = "data", Label = "Output" }]);

    // Create sink nodes on the right
    var sinkNode = CreateNode(
        id: "sink",
        label: "Data Sink",
        x: 550, y: 150,
        width: 140, height: 80,
        type: "sink",
        inputs: [
            new PortDefinition { Id = "flow-in", Type = "flow", Label = "Flow" },
            new PortDefinition { Id = "data-in", Type = "data", Label = "Data" },
            new PortDefinition { Id = "status-in", Type = "status", Label = "Status" },
            new PortDefinition { Id = "extra-in", Type = "extra", Label = "Extra" }
        ]);

    var sinkNode2 = CreateNode(
        id: "sink2",
        label: "Effects Sink",
        x: 550, y: 400,
        width: 140, height: 80,
        type: "sink",
        inputs: [
            new PortDefinition { Id = "rainbow-in", Type = "rainbow", Label = "Rainbow" },
            new PortDefinition { Id = "pulse-in", Type = "pulse", Label = "Pulse" },
            new PortDefinition { Id = "neon-in", Type = "neon", Label = "Neon" }
        ]);

    // Add nodes to graph
    graph.Elements.AddRange([
        sourceNode, sourceNode2,
        processNode1, processNode2, processNode3, processNode4, processNode5, processNode6,
        sinkNode, sinkNode2
    ]);

    // Create edges with different styles - Flow animations
    var flowEdges = new[]
    {
        // Active Flow (animated blue)
        CreateStyledEdge("e1", "source", "flow-out", "process1", "in",
            EdgeStyle.ActiveFlow, "Active Flow"),

        // Data Stream (fast animated green)
        CreateStyledEdge("e2", "source", "data-out", "process2", "in",
            EdgeStyle.DataStream, "Data Stream"),

        // Bidirectional (purple, alternating)
        CreateStyledEdge("e3", "source", "status-out", "process3", "in",
            EdgeStyle.Bidirectional, "Bidirectional"),

        // Electric (yellow glow, animated)
        CreateStyledEdge("e4", "process1", "out", "sink", "flow-in",
            EdgeStyle.Electric, "Electric"),

        // Gentle (slow, light blue)
        CreateStyledEdge("e5", "process2", "out", "sink", "data-in",
            EdgeStyle.Gentle, "Gentle Flow"),

        // Signal (fast green dots)
        CreateStyledEdge("e6", "process3", "out", "sink", "status-in",
            EdgeStyle.Signal, "Signal")
    };

    // Create edges with new effects - Rainbow, Pulse, Neon
    var effectEdges = new[]
    {
        // Rainbow (color cycling)
        CreateStyledEdge("e7", "source2", "rainbow-out", "process4", "in",
            EdgeStyle.Rainbow, "Rainbow"),

        // Pulse (breathing opacity)
        CreateStyledEdge("e8", "source2", "pulse-out", "process5", "in",
            EdgeStyle.Pulse, "Pulse"),

        // Neon (cyan glow with flow)
        CreateStyledEdge("e9", "source2", "neon-out", "process6", "in",
            EdgeStyle.Neon, "Neon"),

        // Heartbeat (fast red pulse)
        CreateStyledEdge("e10", "process4", "out", "sink2", "rainbow-in",
            EdgeStyle.Heartbeat, "Heartbeat"),

        // Custom combination - Reverse Flow
        CreateStyledEdge("e11", "process5", "out", "sink2", "pulse-in",
            EdgeStyle.ReverseFlow, "Reverse"),

        // Debug style
        CreateStyledEdge("e12", "process6", "out", "sink2", "neon-in",
            EdgeStyle.Debug, "Debug")
    };

    graph.Elements.AddRange(flowEdges);
    graph.Elements.AddRange(effectEdges);

    return graph;
  }

  private static Node CreateNode(
      string id, string label,
      double x, double y,
      double width = 100, double height = 60,
      string type = "default",
      ImmutableList<PortDefinition>? inputs = null,
      ImmutableList<PortDefinition>? outputs = null)
  {
    var definition = new NodeDefinition
    {
      Id = id,
      Type = type,
      Label = label,
      Inputs = inputs ?? ImmutableList<PortDefinition>.Empty,
      Outputs = outputs ?? ImmutableList<PortDefinition>.Empty
    };

    var state = new NodeState { X = x, Y = y, Width = width, Height = height };
    return new Node(definition, state);
  }

  private static Edge CreateStyledEdge(string id, string sourceId, string sourcePort,
      string targetId, string targetPort, EdgeStyle style, string label, EdgeMarker marker = EdgeMarker.Arrow)
  {
    var definition = new EdgeDefinition
    {
      Id = id,
      Source = sourceId,
      Target = targetId,
      SourcePort = sourcePort,
      TargetPort = targetPort,
      Label = label,
      MarkerEnd = marker,
      Style = style
    };
    return new Edge(definition);
  }

  private void UpdateStatus()
  {
    if (_statusText != null && _canvas != null)
    {
      var flowCount = _canvas.ActiveFlowAnimationCount;
      var effectsCount = _canvas.ActiveEffectsCount;
      _statusText.Text = $"Flow animations: {flowCount} | Effects: {effectsCount}";
    }
  }

  #region UI Helpers

  private Border CreateSectionHeader(string title, string description)
  {
    return new Border
    {
      Padding = new Thickness(SpaceLg),
      Child = new StackPanel
      {
        Spacing = SpaceSm,
        Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = description,
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.7
                    }
                }
      }
    };
  }

  private Border CreateToolbar()
  {
    var mainPanel = new StackPanel
    {
      Spacing = SpaceSm
    };

    // Row 1: Flow animation styles
    var flowPanel = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      Spacing = SpaceSm
    };
    flowPanel.Children.Add(new TextBlock
    {
      Text = "Flow: ",
      VerticalAlignment = VerticalAlignment.Center,
      FontWeight = FontWeight.SemiBold,
      Width = 50
    });

    var flowStyles = new (string Name, string Color)[]
    {
        ("Active", "#2196F3"),
        ("Stream", "#00E676"),
        ("Bidir", "#9C27B0"),
        ("Electric", "#FFEB3B"),
        ("Gentle", "#81D4FA"),
        ("Signal", "#76FF03")
    };
    foreach (var (name, color) in flowStyles)
    {
      flowPanel.Children.Add(CreateStyleChip(name, color));
    }
    mainPanel.Children.Add(flowPanel);

    // Row 2: Effect styles
    var effectPanel = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      Spacing = SpaceSm
    };
    effectPanel.Children.Add(new TextBlock
    {
      Text = "Effects: ",
      VerticalAlignment = VerticalAlignment.Center,
      FontWeight = FontWeight.SemiBold,
      Width = 50
    });

    var effectStyles = new (string Name, string Color)[]
    {
        ("Rainbow", "#FF00FF"),
        ("Pulse", "#00BCD4"),
        ("Neon", "#00FFFF"),
        ("Heartbeat", "#E53935"),
        ("Reverse", "#FF5722"),
        ("Debug", "#E91E63")
    };
    foreach (var (name, color) in effectStyles)
    {
      effectPanel.Children.Add(CreateStyleChip(name, color));
    }
    mainPanel.Children.Add(effectPanel);

    return new Border
    {
      Padding = new Thickness(SpaceMd),
      Child = mainPanel
    };
  }

  private static Border CreateStyleChip(string name, string color)
  {
    var isDark = name is "Electric" or "Neon" or "Rainbow";
    return new Border
    {
      Background = new SolidColorBrush(Color.Parse(color)),
      CornerRadius = new CornerRadius(4),
      Padding = new Thickness(6, 3),
      Margin = new Thickness(1),
      Child = new TextBlock
      {
        Text = name,
        FontSize = 11,
        Foreground = isDark ? Brushes.Black : Brushes.White
      }
    };
  }

  private Border CreateStatusBar(out TextBlock statusText)
  {
    statusText = new TextBlock
    {
      Text = "Initializing...",
      FontSize = 12
    };

    return new Border
    {
      Padding = new Thickness(SpaceMd),
      Child = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        Spacing = SpaceMd,
        Children =
                {
                    new TextBlock { Text = "Status:", FontWeight = FontWeight.Bold },
                    statusText
                }
      }
    };
  }

  private Border CreateCanvasContainer(Control canvas)
  {
    return new Border
    {
      Margin = new Thickness(SpaceMd),
      BorderBrush = new SolidColorBrush(Color.Parse("#40808080")),
      BorderThickness = new Thickness(1),
      CornerRadius = new CornerRadius(RadiusMd),
      ClipToBounds = true,
      Child = canvas
    };
  }

  #endregion
}
