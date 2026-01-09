# FlowGraph

A node-based graph editor for .NET and Avalonia, inspired by [React Flow](https://reactflow.dev/).

[![NuGet](https://img.shields.io/nuget/v/FlowGraph.Avalonia.svg)](https://www.nuget.org/packages/FlowGraph.Avalonia/)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.2-blue)](https://avaloniaui.net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

![FlowGraph Demo](demo.png)

## Features

### Core

- **Pan & Zoom** - Mouse wheel zoom, middle-click pan, configurable drag behavior
- **Node System** - Draggable, resizable, selectable nodes with custom renderers
- **Port Renderers** - Customizable port visuals via `IPortRenderer` extensibility API
- **Edge Types** - Bezier, Straight, Step, SmoothStep curves with arrow markers
- **Edge Labels** - Positioned labels with anchor points (Start, Center, End) and offsets
- **Connection Validation** - Custom rules for valid connections with built-in validators
- **Undo/Redo** - Full command history with keyboard shortcuts
- **Clipboard** - Copy, cut, paste, duplicate operations
- **Serialization** - JSON save/load support with batch loading
- **Definition + State Pattern** - Immutable definitions with mutable runtime state

### Components

- **FlowMinimap** - Overview with viewport navigation
- **FlowControls** - Zoom in/out/fit buttons panel
- **FlowBackground** - Dots, lines, or cross patterns
- **NodeToolbar** - Floating toolbar on node selection
- **FlowDiagnostics** - Debug panel for development
- **FlowPanel** - Positioned overlay panels (9 positions)

### Advanced

- **Grouping** - Collapsible node groups with nesting and proxy ports
- **Edge Routing** - Orthogonal, bezier, and smart routing algorithms with A\* pathfinding
- **Animations** - Smooth viewport, node, edge, and group animations
- **State Machine** - Clean input handling architecture
- **Edge Reconnection** - Drag edge endpoints to reconnect or disconnect
- **Label Editing** - Double-click to edit node, edge, and group labels
- **Virtualization** - Render only visible nodes for large graphs (500+ nodes)
- **Direct Rendering** - GPU-accelerated rendering bypassing visual tree
- **Context Menus** - Customizable right-click menus

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
using System.Collections.Immutable;

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

        var processNode = new Node(
            new NodeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Type = "process",
                Label = "Process",
                Inputs = [new PortDefinition { Id = "in", Type = "data" }],
                Outputs = [new PortDefinition { Id = "out", Type = "data" }]
            },
            new NodeState { X = 300, Y = 100 }
        );

        var outputNode = new Node(
            new NodeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Type = "output",
                Label = "Output",
                Inputs = [new PortDefinition { Id = "in", Type = "data" }]
            },
            new NodeState { X = 500, Y = 100 }
        );

        MyGraph.AddNode(inputNode);
        MyGraph.AddNode(processNode);
        MyGraph.AddNode(outputNode);

        // Create edges using EdgeDefinition
        MyGraph.AddEdge(new Edge(
            new EdgeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Source = inputNode.Id,
                Target = processNode.Id,
                SourcePort = "out",
                TargetPort = "in",
                Type = EdgeType.Bezier,
                MarkerEnd = EdgeMarker.Arrow
            }
        ));

        MyGraph.AddEdge(new Edge(
            new EdgeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Source = processNode.Id,
                Target = outputNode.Id,
                SourcePort = "out",
                TargetPort = "in",
                Type = EdgeType.Bezier,
                MarkerEnd = EdgeMarker.ArrowClosed
            }
        ));
    }
}

// Backward-compatible API (using parameterless constructor)
public class LegacyViewModel
{
    public Graph MyGraph { get; } = new Graph();

    public LegacyViewModel()
    {
        var node = new Node
        {
            Type = "default",
            Position = new Point(100, 100),
            Label = "My Node",
            Inputs = [new Port { Id = "in", Type = "data" }]
        };

        MyGraph.AddNode(node);
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
    SnapGridSize = null, // Uses GridSpacing if null
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

    // Connections
    StrictConnectionDirection = true,
    ConnectionSnapDistance = 30,
    SnapConnectionToNode = true,
    ShowEdgeEndpointHandles = true,
    EdgeEndpointHandleSize = 10,
    EdgeHitAreaWidth = 15,

    // Selection
    SelectionMode = SelectionMode.Partial, // or SelectionMode.Full

    // Viewport
    PanOnDrag = true,
    PanOnScroll = false,
    PanOnScrollSpeed = 1.0,
    ViewportBounds = new Rect(-1000, -1000, 3000, 3000),
    ViewportBoundsPadding = 100,

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
    DefaultEdgeType = EdgeType.Bezier,
    DefaultRouterAlgorithm = RouterAlgorithm.Auto,

    // Editing
    EnableNodeLabelEditing = true,
    EnableGroupLabelEditing = false,
    EnableEdgeLabelEditing = true,

    // Performance
    EnableVirtualization = true,
    VirtualizationBuffer = 200,
    RenderBatchSize = 50,
    UseSimplifiedNodeRendering = false,
    DirectRenderingNodeThreshold = 100,
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

// Use built-in validators
canvas.ConnectionValidator = new TypeMatchingConnectionValidator();

// Or create a composite validator
var validator = new CompositeConnectionValidator()
    .Add(new NoSelfConnectionValidator())
    .Add(new NoDuplicateConnectionValidator())
    .Add(new TypeMatchingConnectionValidator())
    .Add(new NoCycleConnectionValidator());

canvas.ConnectionValidator = validator;

// Use standard or strict presets
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
    // Show label edit UI for node
    var newLabel = ShowLabelEditDialog(e.Node.Label);
    if (newLabel != null)
        e.Node.Label = newLabel;
};

canvas.EdgeLabelEditRequested += (s, e) =>
{
    // Show label edit UI for edge
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

For better performance when loading large graphs, use batch loading to suppress change notifications:

```csharp
// Batch loading mode
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
    graph.EndBatchLoad(); // Raises BatchLoadCompleted event
}

// Or use AddRange for bulk operations
graph.AddNodes(nodeCollection);
graph.AddEdges(edgeCollection);

// Subscribe to batch load completion
graph.BatchLoadCompleted += (s, e) =>
{
    Console.WriteLine("Batch load completed, UI will refresh");
};
```

## Definition + State Pattern

FlowGraph uses an immutable Definition + mutable State pattern for nodes and edges:

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
    IsResizable = true,
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
node.IsSelected = true; // Updates State

// Edge Definition + State works similarly
var edgeDef = new EdgeDefinition
{
    Id = Guid.NewGuid().ToString(),
    Source = sourceNodeId,
    Target = targetNodeId,
    SourcePort = "out",
    TargetPort = "in",
    Type = EdgeType.Bezier,
    MarkerEnd = EdgeMarker.Arrow,
    AutoRoute = false,
    Label = "Simple Label"  // Or use LabelInfo for positioning
};

var edge = new Edge(edgeDef, new EdgeState());
```

## Edge Label Positioning

Use `LabelInfo` for precise control over edge label placement:

```csharp
using FlowGraph.Core.Models;

// Label at different positions along the edge
var edgeWithLabel = new Edge(new EdgeDefinition
{
    Id = Guid.NewGuid().ToString(),
    Source = sourceNodeId,
    Target = targetNodeId,
    SourcePort = "out",
    TargetPort = "in",
    // Position label near the start with vertical offset
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

Ports now support additional properties:

```csharp
var port = new PortDefinition
{
    Id = "data_in",
    Type = "data",
    Label = "Data Input",
    Position = PortPosition.Left,
    MaxConnections = 1, // Limit connections
    IsRequired = true // Mark as required
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

// Create group from specific nodes
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

// Group events
canvas.GroupCollapsedChanged += (s, e) =>
{
    Console.WriteLine($"Group {e.GroupId} is {(e.IsCollapsed ? "collapsed" : "expanded")}");
};
```

## Performance Optimization

FlowGraph includes several performance features for large graphs:

```csharp
// Enable virtualization (default: true)
// Only renders nodes/edges visible in viewport
canvas.Settings.EnableVirtualization = true;
canvas.Settings.VirtualizationBuffer = 200; // Buffer in canvas units

// Use simplified node rendering for large graphs (500+ nodes)
canvas.EnableSimplifiedRendering();

// Or enable specific settings
canvas.Settings.UseSimplifiedNodeRendering = true;
canvas.Settings.RenderBatchSize = 50; // Batch render in chunks

// Enable direct GPU rendering (automatic for 100+ nodes)
canvas.Settings.DirectRenderingNodeThreshold = 100;
// Or manually enable/disable
canvas.EnableDirectRendering();
canvas.DisableDirectRendering();

// Disable simplified rendering when needed
canvas.DisableSimplifiedRendering();

// Batch loading for initial graph load
graph.BeginBatchLoad();
// Add 1000s of nodes/edges...
graph.EndBatchLoad();

// Edge routing optimization
canvas.Settings.RouteOnlyAffectedEdges = true; // Only re-route affected edges
canvas.Settings.RouteEdgesOnDrag = false; // Disable routing during drag

// Performance tips:
// - Use batch operations (AddNodes/AddEdges) instead of individual adds
// - Enable virtualization for graphs > 100 nodes
// - Use simplified rendering for graphs > 500 nodes
// - Direct rendering automatically kicks in at 100+ nodes
// - Disable edge routing during initial load, enable after
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

// Route only selected edges
canvas.Routing.RouteSelectedEdges();

// Clear routing for an edge
canvas.Routing.ClearEdgeRouting(edgeId);

// Configure routing behavior
canvas.Settings.RouteEdgesOnDrag = true; // Re-route while dragging
canvas.Settings.RouteNewEdges = true; // Auto-route new connections
canvas.Settings.RouteOnlyAffectedEdges = true; // Performance optimization
canvas.Settings.RoutingNodePadding = 10; // Space around nodes
```

## Project Structure

```
FlowGraph/
├── FlowGraph.Core           # Data models, commands, serialization
├── FlowGraph.Avalonia       # Avalonia UI controls and rendering
├── FlowGraph.Demo           # Sample application
└── FlowGraph.Core.Tests     # Unit tests
```

## Requirements

- .NET 9.0 or later
- Avalonia 11.2 or later

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
