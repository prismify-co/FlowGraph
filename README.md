# FlowGraph

A node-based graph editor for .NET and Avalonia, inspired by [React Flow](https://reactflow.dev/).

[![NuGet](https://img.shields.io/nuget/v/FlowGraph.Avalonia.svg)](https://www.nuget.org/packages/FlowGraph.Avalonia/)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.3-blue)](https://avaloniaui.net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

![FlowGraph Demo](demo.png)

## Features

### Core

- **Pan & Zoom** - Mouse wheel zoom, middle-click pan, configurable drag behavior
- **Node System** - Draggable, selectable nodes with custom renderers
- **Port System** - Input/output ports with position, type, and connection limits
- **Port Tooltips** - Hover over ports to see descriptive tooltips
- **Edge Types** - Bezier, Straight, Step, SmoothStep curves with arrow markers
- **Edge Labels** - Positioned labels with anchor points (Start, Center, End) and offsets
- **Edge Locking** - Lock edges to prevent modification
- **Connection Validation** - Extensible validation with built-in validators (type matching, no cycles, no duplicates)
- **Undo/Redo** - Full command history with keyboard shortcuts
- **Clipboard** - Copy, cut, paste, duplicate operations
- **Serialization** - JSON save/load with full round-trip support
- **Definition + State Pattern** - Immutable definitions with mutable runtime state

### Components

- **FlowMinimap** - Overview with viewport navigation
- **FlowControls** - Zoom in/out/fit buttons panel
- **FlowBackground** - Dots, lines, cross, or hierarchical grid patterns
- **NodeToolbar** - Floating toolbar on node selection
- **FlowDiagnostics** - Debug panel for development
- **FlowPanel** - Positioned overlay panels (9 positions)

### Advanced

- **Grouping** - Collapsible node groups with proxy ports
- **Edge Routing** - Orthogonal, bezier, and smart routing algorithms with A\* pathfinding
- **Animations** - Smooth viewport, node, edge, and group animations
- **State Machine** - Clean input handling architecture
- **Edge Reconnection** - Drag edge endpoints to reconnect or disconnect
- **Label Editing** - Double-click to edit node and edge labels
- **Virtualization** - Render only visible nodes for large graphs (500+ nodes)
- **Direct Rendering** - GPU-accelerated rendering bypassing visual tree
- **Custom Renderers** - Extensible node, port, edge, and background renderers
- **Data Flow** - Built-in reactive data propagation system
- **Shape System** - Support for shapes with selection and serialization

## Installation

```bash
dotnet add package FlowGraph.Avalonia
```

Or via NuGet Package Manager:

```
Install-Package FlowGraph.Avalonia
```

## Quick Start

### 1. Add namespaces to your AXAML

```xml
<Window xmlns:fg="using:FlowGraph.Avalonia"
        xmlns:fgc="using:FlowGraph.Avalonia.Controls">
```

### 2. Add FlowCanvas and components

```xml
<Panel>
    <!-- Background pattern -->
    <fgc:FlowBackground x:Name="Background" Variant="Dots" Gap="20" />

    <!-- Main canvas -->
    <fg:FlowCanvas x:Name="Canvas" Graph="{Binding MyGraph}" />

    <!-- Controls panel -->
    <fgc:FlowPanel Position="BottomLeft" Margin="16">
        <fgc:FlowControls TargetCanvas="{Binding #Canvas}" />
    </fgc:FlowPanel>

    <!-- Minimap -->
    <fgc:FlowPanel Position="BottomRight" Margin="16">
        <fgc:FlowMinimap TargetCanvas="{Binding #Canvas}" />
    </fgc:FlowPanel>

    <!-- Node toolbar (appears on selection) -->
    <fgc:NodeToolbar TargetCanvas="{Binding #Canvas}" Position="Top" Offset="12">
        <StackPanel Orientation="Horizontal" Spacing="4">
            <Button Content="Delete" Click="OnDelete" />
            <Button Content="Duplicate" Click="OnDuplicate" />
        </StackPanel>
    </fgc:NodeToolbar>
</Panel>
```

### 3. Create a graph in your ViewModel

```csharp
using FlowGraph.Core;
using FlowGraph.Core.Models;

public class MainViewModel
{
    public Graph MyGraph { get; }

    public MainViewModel()
    {
        MyGraph = new Graph();

        // Create nodes using the Definition + State pattern
        var inputNode = new Node(
            new NodeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Type = "input",
                Label = "Input",
                Outputs = [new PortDefinition { Id = "out", Type = "data" }]
            },
            new NodeState { X = 100, Y = 100 }
        );

        var outputNode = new Node(
            new NodeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Type = "output",
                Label = "Output",
                Inputs = [new PortDefinition { Id = "in", Type = "data" }]
            },
            new NodeState { X = 300, Y = 100 }
        );

        MyGraph.AddNode(inputNode);
        MyGraph.AddNode(outputNode);

        // Create edge
        MyGraph.AddEdge(new Edge(
            new EdgeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Source = inputNode.Id,
                Target = outputNode.Id,
                SourcePort = "out",
                TargetPort = "in",
                Type = EdgeType.Bezier,
                MarkerEnd = EdgeMarker.Arrow
            }
        ));
    }
}
```

### 4. Connect the background (code-behind)

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Background.TargetCanvas = Canvas;
    }
}
```

## Configuration

```csharp
var settings = new FlowCanvasSettings
{
    // Grid & Snapping
    GridSpacing = 20,
    GridDotSize = 2,
    SnapToGrid = true,
    ShowGrid = true,
    ShowBackground = true,

    // Zoom
    MinZoom = 0.1,
    MaxZoom = 3.0,
    ZoomStep = 0.1,

    // Nodes
    NodeWidth = 150,
    NodeHeight = 80,
    PortSize = 12,
    ShowPorts = true,

    // Edges
    EdgeStrokeThickness = 2,
    EdgeSelectedStrokeThickness = 3,
    EdgeHitAreaWidth = 15,
    DefaultEdgeType = EdgeType.Bezier,

    // Connections
    StrictConnectionDirection = true,
    ConnectionSnapDistance = 30,
    SnapConnectionToNode = true,
    ShowEdgeEndpointHandles = true,
    EdgeEndpointHandleSize = 10,

    // Selection
    SelectionMode = SelectionMode.Partial,
    PanOnDrag = true,

    // Groups
    GroupPadding = 20,
    GroupHeaderHeight = 28,
    GroupBorderRadius = 8,
    GroupUseDashedBorder = true,
    GroupBackgroundOpacity = 0.1,
    UseProxyPortsOnCollapse = true,

    // Edge Routing
    AutoRouteEdges = false,
    RouteEdgesOnDrag = true,
    RouteNewEdges = true,
    RouteOnlyAffectedEdges = true,
    RoutingNodePadding = 10,
    DefaultRouterAlgorithm = RouterAlgorithm.Auto,

    // Editing
    EnableNodeLabelEditing = true,
    EnableEdgeLabelEditing = true,
    EnableGroupLabelEditing = false,

    // Performance
    EnableVirtualization = true,
    VirtualizationBuffer = 200,
    RenderBatchSize = 50,
    UseSimplifiedNodeRendering = false,
    DirectRenderingNodeThreshold = 100,

    // Viewport
    ViewportBounds = null, // Unconstrained
    ViewportBoundsPadding = 100,
    PanOnScroll = false,
    PanOnScrollSpeed = 1.0,

    // Diagnostics
    EnableDiagnostics = false,
    DebugCoordinateTransforms = false
};

canvas.Settings = settings;
```

## Custom Node Renderers

Create custom node appearances by implementing `INodeRenderer`:

```csharp
public class CustomNodeRenderer : DefaultNodeRenderer
{
    private static readonly IBrush CustomBackground = new SolidColorBrush(Color.Parse("#E3F2FD"));
    private static readonly IBrush CustomBorder = new SolidColorBrush(Color.Parse("#2196F3"));

    public override Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var control = base.CreateNodeVisual(node, context);

        if (control is Border border)
        {
            border.Background = CustomBackground;
            border.BorderBrush = CustomBorder;
            border.CornerRadius = new CornerRadius(12);
        }

        return control;
    }

    protected override string GetDisplayText(Node node)
    {
        return node.Data as string ?? node.Type;
    }
}

// Register the renderer
canvas.NodeRenderers.Register("custom", new CustomNodeRenderer());
```

## Custom Port Renderers

Customize port visuals with `IPortRenderer`:

```csharp
public class CustomPortRenderer : DefaultPortRenderer
{
    public override Control CreatePortVisual(Port port, PortRenderContext context)
    {
        // Create custom port visual
        return new Ellipse
        {
            Width = context.Settings.PortSize,
            Height = context.Settings.PortSize,
            Fill = port.Type == "data" ? Brushes.Blue : Brushes.Green,
            Stroke = Brushes.White,
            StrokeThickness = 2
        };
    }
}

// Register the renderer
canvas.PortRenderers.Register("custom-port", new CustomPortRenderer());
```

## Custom Edge Renderers

Create custom edge visuals with `IEdgeRenderer`:

```csharp
public class SequenceMessageRenderer : IEdgeRenderer
{
    public EdgeRenderResult Render(EdgeRenderContext context)
    {
        // Custom rendering logic for sequence diagram messages
        var path = CreateMessagePath(context);
        var label = CreateMessageLabel(context);

        return new EdgeRenderResult
        {
            Path = path,
            HitArea = CreateHitArea(path),
            Labels = [label]
        };
    }
}

// Register with pattern matching
canvas.EdgeRenderers.Register("sequence-*", new SequenceMessageRenderer());
```

## Custom Background Renderers

Add custom background layers with `IBackgroundRenderer`:

```csharp
public class LifelineRenderer : IBackgroundRenderer
{
    public void Render(DrawingContext context, BackgroundRenderContext bgContext)
    {
        // Draw lifelines behind nodes
        foreach (var node in bgContext.Nodes.Where(n => n.Type == "lifeline"))
        {
            var screenPos = bgContext.CanvasToScreen(node.Position);
            // Draw vertical lifeline
            context.DrawLine(pen, screenPos, new Point(screenPos.X, bgContext.Bounds.Bottom));
        }
    }

    public void OnGraphChanged(Graph graph) { /* Update cached data */ }
    public void OnViewportChanged(ViewportState viewport) { /* Handle zoom/pan */ }
}

// Register background renderer
canvas.BackgroundRenderers.Add(new LifelineRenderer());
```

## Connection Validation

Implement `IConnectionValidator` for custom connection rules:

```csharp
using FlowGraph.Avalonia.Validation;

// Type matching validator
public class TypeMatchValidator : IConnectionValidator
{
    public ConnectionValidationResult Validate(ConnectionContext context)
    {
        if (context.SourceNode.Id == context.TargetNode.Id)
            return ConnectionValidationResult.Invalid("Cannot connect to self");

        if (context.SourcePort.Type != context.TargetPort.Type)
            return ConnectionValidationResult.Invalid("Port types must match");

        return ConnectionValidationResult.Valid();
    }
}

// Built-in validators
canvas.ConnectionValidator = new TypeMatchingConnectionValidator();
canvas.ConnectionValidator = new NoSelfConnectionValidator();
canvas.ConnectionValidator = new NoDuplicateConnectionValidator();
canvas.ConnectionValidator = new NoCycleConnectionValidator();

// Composite validator
var validator = new CompositeConnectionValidator()
    .Add(new NoSelfConnectionValidator())
    .Add(new NoDuplicateConnectionValidator())
    .Add(new TypeMatchingConnectionValidator())
    .Add(new NoCycleConnectionValidator());

canvas.ConnectionValidator = validator;

// Or use presets
canvas.ConnectionValidator = CompositeConnectionValidator.CreateStandard();
canvas.ConnectionValidator = CompositeConnectionValidator.CreateStrict();
```

## Events

```csharp
// Selection
canvas.SelectionChanged += (s, e) =>
{
    Console.WriteLine($"Selected: {e.SelectedNodes.Count} nodes, {e.SelectedEdges.Count} edges");
};

// Viewport
canvas.ViewportChanged += (s, e) =>
{
    Console.WriteLine($"Zoom: {e.Zoom:P0}, Pan: ({e.OffsetX:F0}, {e.OffsetY:F0})");
};

// Node dragging
canvas.NodeDragStart += (s, e) => Console.WriteLine($"Started dragging {e.Nodes.Count} nodes");
canvas.NodeDragStop += (s, e) => Console.WriteLine($"Stopped dragging");

// Connections
canvas.ConnectStart += (s, e) => Console.WriteLine($"Connection started from {e.Node.Type}");
canvas.ConnectEnd += (s, e) =>
{
    if (e.IsCompleted)
        Console.WriteLine("Connection created!");
    else
        Console.WriteLine("Connection cancelled");
};

canvas.ConnectionRejected += (s, e) =>
{
    Console.WriteLine($"Connection rejected: {e.Reason}");
};

// Groups
canvas.GroupCollapsedChanged += (s, e) =>
{
    Console.WriteLine($"Group {e.GroupId} {(e.IsCollapsed ? "collapsed" : "expanded")}");
};

// Label editing
canvas.NodeLabelEditRequested += (s, e) =>
{
    var newLabel = ShowLabelEditDialog(e.Node.Label);
    if (newLabel != null)
        e.Node.Label = newLabel;
};

canvas.EdgeLabelEditRequested += (s, e) =>
{
    var newLabel = ShowLabelEditDialog(e.Edge.Label);
    if (newLabel != null)
        e.Edge.Label = newLabel;
};

// Input state changes
canvas.InputStateChanged += (s, e) =>
{
    Console.WriteLine($"Input state: {e.PreviousState} -> {e.NewState}");
};
```

## Keyboard Shortcuts

| Shortcut                  | Action                          |
| ------------------------- | ------------------------------- |
| `Delete` / `Backspace`    | Delete selected                 |
| `Ctrl+A`                  | Select all                      |
| `Ctrl+C`                  | Copy                            |
| `Ctrl+X`                  | Cut                             |
| `Ctrl+V`                  | Paste                           |
| `Ctrl+D`                  | Duplicate                       |
| `Ctrl+Z`                  | Undo                            |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo                            |
| `Ctrl+G`                  | Group selected                  |
| `Ctrl+Shift+G`            | Ungroup                         |
| `Escape`                  | Deselect all / Cancel operation |

## Animations

```csharp
// Viewport
canvas.FitToViewAnimated(duration: 0.5);
canvas.CenterOnNodeAnimated(node, duration: 0.3);
canvas.ZoomToAnimated(targetZoom: 1.5, duration: 0.2);

// Nodes
canvas.AnimateNodesTo(positions, duration: 0.3);
canvas.AnimateNodesAppear(nodes, duration: 0.3, stagger: 0.05);
canvas.AnimateNodesDisappear(nodes, duration: 0.2);
canvas.AnimateSelectionPulse(node);

// Edges
canvas.AnimateEdgePulse(edge, pulseCount: 3);
canvas.AnimateEdgeFadeIn(edge, duration: 0.3);
canvas.AnimateEdgeFadeOut(edge, duration: 0.3);
canvas.AnimateEdgeColor(edge, Colors.Red, duration: 0.5);

// Flow animation (continuous)
var animation = canvas.StartEdgeFlowAnimation(edge, speed: 50);
canvas.StopEdgeFlowAnimation(animation);

// Groups
canvas.AnimateGroupCollapse(groupId, duration: 0.5);
canvas.AnimateGroupExpand(groupId, duration: 0.5);
```

## Edge Styles & Effects

Customize edge appearance with `EdgeStyle`:

```csharp
var edge = new Edge
{
    Source = "node1",
    Target = "node2",
    Style = new EdgeStyle
    {
        // Basic styling
        Color = "#00BCD4",
        Thickness = 3,
        Dash = "5,5",           // Dashed line
        Opacity = 1.0,

        // Flow animation
        AnimatedFlow = true,
        FlowSpeed = 60,
        FlowDirection = FlowDirection.Forward,

        // Glow effect
        Glow = true,
        GlowColor = "#00BCD4",
        GlowIntensity = 2.0,

        // Rainbow color cycling
        Rainbow = true,
        RainbowSpeed = 0.5,

        // Pulse opacity effect
        Pulse = true,
        PulseFrequency = 1.5,
        PulseMinOpacity = 0.3
    }
};
```

### Predefined Styles

```csharp
// Built-in presets
edge.Style = EdgeStyle.Active;     // Cyan with flow animation
edge.Style = EdgeStyle.Stream;     // Blue continuous stream
edge.Style = EdgeStyle.Electric;   // Yellow electric effect
edge.Style = EdgeStyle.Neon;       // Bright glow
edge.Style = EdgeStyle.Pulse;      // Pulsing opacity
edge.Style = EdgeStyle.Rainbow;    // Color cycling
```

> **Note:** Glow effects use a background path technique rather than Avalonia's built-in `Effect` system due to a known Avalonia rendering issue where effects on transformed canvases can cause sibling elements to render at incorrect positions.

## Serialization

```csharp
using FlowGraph.Core.Serialization;

// Save/Load JSON
var json = GraphSerializer.Serialize(graph);
var graph = GraphSerializer.Deserialize(json);

// File operations
await graph.SaveToFileAsync("graph.json");
var graph = await GraphExtensions.LoadFromFileAsync("graph.json");

// Synchronous file operations
graph.SaveToFile("graph.json");
var graph = GraphExtensions.LoadFromFile("graph.json");

// Clone
var copy = graph.Clone();
```

## Batch Loading

For better performance when loading large graphs:

```csharp
graph.BeginBatchLoad();
try
{
    foreach (var nodeData in largeDataSet)
    {
        graph.AddNode(CreateNode(nodeData));
    }

    foreach (var edgeData in largeEdgeSet)
    {
        graph.AddEdge(CreateEdge(edgeData));
    }
}
finally
{
    graph.EndBatchLoad();
}

// Or use AddRange for bulk operations
graph.AddNodes(nodeCollection);
graph.AddEdges(edgeCollection);

// Subscribe to batch load completion
graph.BatchLoadCompleted += (s, e) =>
{
    Console.WriteLine("Batch load completed");
};
```

## Accessing Graph Elements

```csharp
// Primary API (v0.4.0+)
var nodes = graph.Elements.Nodes;
var edges = graph.Elements.Edges;

// Query nodes and edges
var selectedNodes = graph.Elements.Nodes.Where(n => n.IsSelected);
var node = graph.Elements.Nodes.FirstOrDefault(n => n.Id == "myNode");
var connectedEdges = graph.Elements.Edges.Where(e => e.Source == nodeId);

// Add and remove elements
graph.AddNode(newNode);
graph.AddEdge(newEdge);
graph.Elements.Remove(node);
graph.Elements.Remove(edge);

// Listen for changes
graph.NodesChanged += (s, e) => Console.WriteLine("Nodes changed");
graph.EdgesChanged += (s, e) => Console.WriteLine("Edges changed");
```

## Definition + State Pattern

FlowGraph uses an immutable Definition + mutable State pattern:

```csharp
// Node Definition (immutable) - defines "what" a node is
var definition = new NodeDefinition
{
    Id = Guid.NewGuid().ToString(),
    Type = "process",
    Label = "My Node",
    Inputs = [new PortDefinition { Id = "in", Type = "data" }],
    Outputs = [new PortDefinition { Id = "out", Type = "data" }],
    IsSelectable = true,
    IsDraggable = true,
    IsDeletable = true,
    IsConnectable = true,
    Data = customData
};

// Node State (mutable) - defines "where" a node is
var state = new NodeState
{
    X = 100,
    Y = 100,
    Width = 150,
    Height = 80,
    IsSelected = false,
    IsDragging = false,
    IsCollapsed = false
};

var node = new Node(definition, state);

// Modify definition immutably with 'with' expressions
node.Definition = node.Definition with { Label = "Updated Label" };

// Modify state directly
node.State.X = 200;
node.State.IsSelected = true;

// Backward-compatible pass-through properties
node.Label = "Another Update"; // Updates Definition
node.Position = new Point(300, 300); // Updates State
```

## Edge Label Positioning

```csharp
using FlowGraph.Core.Models;

// Label at different positions along the edge
var edge = new Edge(new EdgeDefinition
{
    Id = Guid.NewGuid().ToString(),
    Source = sourceNodeId,
    Target = targetNodeId,
    SourcePort = "out",
    TargetPort = "in",
    LabelInfo = new LabelInfo("Yes", LabelAnchor.Start, offsetY: -10)
});

// Fluent API for updating labels
edge.Definition = edge.Definition.WithLabelInfo("No", LabelAnchor.End, offsetX: 5);

// LabelAnchor options:
// - LabelAnchor.Start  (25% along edge, near source)
// - LabelAnchor.Center (50% along edge, default)
// - LabelAnchor.End    (75% along edge, near target)
```

## Port Definitions

```csharp
var port = new PortDefinition
{
    Id = "data_in",
    Type = "data",
    Label = "Data Input",
    Position = PortPosition.Left,
    MaxConnections = 1,
    IsRequired = true
};

// Add ports to node definition
var nodeDefinition = new NodeDefinition
{
    Id = "node1",
    Type = "process"
}.AddInput(port1)
 .AddOutput(port2);

// Or use WithPorts builder
var nodeDefinition = baseDefinition.WithPorts(
    inputs: [port1, port2],
    outputs: [port3, port4]
);
```

## Node Grouping

```csharp
// Create groups
canvas.Groups.GroupSelected();
canvas.Groups.GroupSelected("My Group");
canvas.Groups.CreateGroup(nodeIds, "Group Label");

// Collapse/expand
canvas.Groups.ToggleGroupCollapse(groupId);
canvas.Groups.CollapseGroup(groupId);
canvas.Groups.ExpandGroup(groupId);

// Animated collapse/expand
canvas.AnimateGroupCollapse(groupId, duration: 0.5);
canvas.AnimateGroupExpand(groupId, duration: 0.5);

// Manage nodes in groups
canvas.Groups.AddNodeToGroup(groupId, nodeId);
canvas.Groups.RemoveNodeFromGroup(nodeId);
canvas.Groups.Ungroup(groupId);
canvas.Groups.UngroupSelected();

// Check group state
bool isCollapsed = canvas.Groups.IsGroupCollapsed(groupId);
var childNodes = canvas.Groups.GetGroupChildren(groupId);
```

## Data Flow System

FlowGraph includes a reactive data propagation system:

```csharp
using FlowGraph.Core.DataFlow;

// Create executor
var executor = new GraphExecutor(graph);

// Register node processors
executor.RegisterProcessor(new MyNodeProcessor(node));

// Execute when inputs change (automatic via ReactivePort)
executor.Execute();

// Listen for events
executor.ExecutionStarted += (s, e) => Console.WriteLine("Execution started");
executor.ExecutionCompleted += (s, e) => Console.WriteLine("Execution completed");
executor.NodeProcessed += (s, e) => Console.WriteLine($"Processed: {e.NodeId}");

// Clean up
executor.Dispose();
```

## Edge Routing

```csharp
// Enable automatic edge routing
canvas.Settings.AutoRouteEdges = true;
canvas.Settings.DefaultRouterAlgorithm = RouterAlgorithm.Orthogonal;

// Available algorithms: Auto, Direct, Orthogonal, SmartBezier

// Route specific edges
canvas.Routing.RouteEdge(edgeId);

// Route all edges
canvas.Routing.RouteAllEdges();

// Configure routing behavior
canvas.Settings.RouteEdgesOnDrag = true;
canvas.Settings.RouteNewEdges = true;
canvas.Settings.RouteOnlyAffectedEdges = true;
canvas.Settings.RoutingNodePadding = 10;
```

## Performance Optimization

```csharp
// Enable virtualization (default: true)
canvas.Settings.EnableVirtualization = true;
canvas.Settings.VirtualizationBuffer = 200;

// Enable simplified rendering for large graphs
canvas.EnableSimplifiedRendering();
canvas.Settings.UseSimplifiedNodeRendering = true;
canvas.Settings.RenderBatchSize = 50;

// Enable direct GPU rendering (automatic for 100+ nodes)
canvas.Settings.DirectRenderingNodeThreshold = 100;
canvas.EnableDirectRendering();
canvas.DisableDirectRendering();

// Batch loading for initial graph load
graph.BeginBatchLoad();
// Add nodes/edges...
graph.EndBatchLoad();

// Edge routing optimization
canvas.Settings.RouteOnlyAffectedEdges = true;
canvas.Settings.RouteEdgesOnDrag = false; // Disable during large operations
```

## Project Structure

```
FlowGraph/
├── FlowGraph.Core           # Data models, commands, serialization, data flow
├── FlowGraph.Avalonia       # Avalonia UI controls and rendering
├── FlowGraph.3D             # 3D visualization integration (demo)
├── FlowGraph.Demo           # Sample application with interactive demos
└── FlowGraph.Core.Tests     # Unit tests
```

## Demo Application

The demo application showcases:

- **Interactive 3D Demo** - Data flow between custom nodes with 3D visualization
- **Shapes & Layers** - Shape system with selection and serialization
- **Performance Demo** - Stress testing with 500+ nodes

## Requirements

- .NET 9.0 or later
- Avalonia 11.3 or later

## Contributing

Contributions are welcome. Priority areas:

- Documentation
- Unit tests
- Bug fixes
- Performance improvements

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Inspired by [React Flow](https://reactflow.dev/)
- Built with [Avalonia UI](https://avaloniaui.net/)
- UI themed with [ShadUI](https://github.com/pureform-design/shad-ui)
