# FlowGraph

A powerful, extensible node-based graph editor for .NET/Avalonia, inspired by [React Flow](https://reactflow.dev/).

[![NuGet](https://img.shields.io/nuget/v/FlowGraph.Avalonia.svg)](https://www.nuget.org/packages/FlowGraph.Avalonia/)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.2-blue)](https://avaloniaui.net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

<!-- ![FlowGraph Demo](docs/images/demo-screenshot.png) -->

## ✨ Features

### Core
- 🖱️ **Pan & Zoom** - Mouse wheel zoom, middle-click pan, configurable drag behavior
- 📦 **Node System** - Draggable, resizable, selectable nodes with custom renderers
- 🔗 **Edge Types** - Bezier, Straight, Step, SmoothStep curves with arrow markers
- 🎯 **Connection Validation** - Custom rules for valid connections
- ↩️ **Undo/Redo** - Full command history with keyboard shortcuts
- 📋 **Clipboard** - Copy, cut, paste, duplicate operations
- 💾 **Serialization** - JSON save/load support

### Components
- 🗺️ **FlowMinimap** - Overview with viewport navigation
- 🎛️ **FlowControls** - Zoom in/out/fit buttons panel
- 🎨 **FlowBackground** - Dots, lines, or cross patterns
- 🔧 **NodeToolbar** - Floating toolbar on node selection
- 📊 **FlowDiagnostics** - Debug panel for development
- 📍 **FlowPanel** - Positioned overlay panels (9 positions)

### Advanced
- 📁 **Grouping** - Collapsible node groups with nesting
- 🛤️ **Edge Routing** - Orthogonal, bezier, and smart routing algorithms
- ✨ **Animations** - Smooth viewport, node, edge, and group animations
- 🎮 **State Machine** - Clean input handling architecture
- 🔄 **Edge Reconnection** - Drag edge endpoints to reconnect

## 📦 Installation

```bash
dotnet add package FlowGraph.Avalonia
```

Or via NuGet Package Manager:
```
Install-Package FlowGraph.Avalonia
```

## 🚀 Quick Start

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

public class MainViewModel
{
    public Graph MyGraph { get; }

    public MainViewModel()
    {
        MyGraph = new Graph();

        // Create nodes
        var inputNode = new Node
        {
            Type = "input",
            Position = new Point(100, 100),
            Outputs = [new Port { Id = "out", Type = "data" }]
        };

        var processNode = new Node
        {
            Type = "process",
            Position = new Point(300, 100),
            Inputs = [new Port { Id = "in", Type = "data" }],
            Outputs = [new Port { Id = "out", Type = "data" }]
        };

        var outputNode = new Node
        {
            Type = "output",
            Position = new Point(500, 100),
            Inputs = [new Port { Id = "in", Type = "data" }]
        };

        // Add nodes
        MyGraph.AddNode(inputNode);
        MyGraph.AddNode(processNode);
        MyGraph.AddNode(outputNode);

        // Create edges with arrows
        MyGraph.AddEdge(new Edge
        {
            Source = inputNode.Id,
            Target = processNode.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.Arrow
        });

        MyGraph.AddEdge(new Edge
        {
            Source = processNode.Id,
            Target = outputNode.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.ArrowClosed
        });
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
        
        // Connect background to canvas
        Background.TargetCanvas = Canvas;
    }
}
```

## ⚙️ Configuration

### FlowCanvasSettings

```csharp
var settings = new FlowCanvasSettings
{
    // Grid
    GridSize = 20,
    SnapToGrid = true,
    ShowGrid = true,
    
    // Zoom
    MinZoom = 0.1,
    MaxZoom = 4.0,
    ZoomStep = 0.1,
    
    // Nodes
    NodeWidth = 150,
    NodeHeight = 80,
    
    // Connections
    StrictConnectionDirection = true,  // Only allow connections from output ports
    ConnectionSnapDistance = 30,
    
    // Selection
    BoxSelectionMode = BoxSelectionMode.Intersect,
    
    // Viewport
    PanOnDrag = true,
    PanOnScroll = false,
    PanOnScrollSpeed = 1.0,
    
    // Viewport bounds (optional)
    ViewportBounds = new Rect(-1000, -1000, 3000, 3000),
    ViewportBoundsPadding = 100
};

canvas.Settings = settings;
```

## 🎨 Custom Node Renderers

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

## ✅ Connection Validation

Implement `IConnectionValidator` for custom connection rules:

```csharp
public class TypeMatchValidator : IConnectionValidator
{
    public ValidationResult Validate(ConnectionContext context)
    {
        // Prevent self-connections
        if (context.SourceNode.Id == context.TargetNode.Id)
            return ValidationResult.Invalid("Cannot connect to self");
            
        // Require matching port types
        if (context.SourcePort.Type != context.TargetPort.Type)
            return ValidationResult.Invalid("Port types must match");
            
        return ValidationResult.Valid();
    }
}

canvas.ConnectionValidator = new TypeMatchValidator();
```

## 📡 Events

```csharp
// Selection changed
canvas.SelectionChanged += (s, e) => 
{
    Console.WriteLine($"Selected: {e.SelectedNodes.Count} nodes, {e.SelectedEdges.Count} edges");
};

// Viewport changed
canvas.ViewportChanged += (s, e) => 
{
    Console.WriteLine($"Zoom: {e.Zoom:P0}, Offset: ({e.OffsetX:F0}, {e.OffsetY:F0})");
};

// Node drag lifecycle
canvas.NodeDragStart += (s, e) => Console.WriteLine($"Started dragging {e.Nodes.Count} nodes");
canvas.NodeDragStop += (s, e) => Console.WriteLine($"Stopped dragging");

// Connection lifecycle
canvas.ConnectStart += (s, e) => Console.WriteLine($"Connection started from {e.SourceNode.Type}");
canvas.ConnectEnd += (s, e) => 
{
    if (e.WasCompleted)
        Console.WriteLine("Connection created!");
    else
        Console.WriteLine("Connection cancelled");
};
```

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Delete` / `Backspace` | Delete selected |
| `Ctrl+A` | Select all |
| `Ctrl+C` | Copy |
| `Ctrl+X` | Cut |
| `Ctrl+V` | Paste |
| `Ctrl+D` | Duplicate |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo |
| `Ctrl+G` | Group selected |
| `Ctrl+Shift+G` | Ungroup |
| `Escape` | Deselect all / Cancel operation |

## ✨ Animations

```csharp
// Viewport animations
canvas.FitToViewAnimated(duration: 0.5);
canvas.CenterOnNodeAnimated(node, duration: 0.3);
canvas.ZoomToAnimated(targetZoom: 1.5, duration: 0.2);

// Node animations
canvas.AnimateNodesTo(positions, duration: 0.3);
canvas.AnimateNodesAppear(nodes, duration: 0.3, stagger: 0.05);
canvas.AnimateNodesDisappear(nodes, duration: 0.2);
canvas.AnimateSelectionPulse(node);

// Edge animations
canvas.AnimateEdgePulse(edge, pulseCount: 3);
canvas.AnimateEdgeFadeIn(edge, duration: 0.3);
canvas.AnimateEdgeFadeOut(edge, duration: 0.3);
canvas.AnimateEdgeColor(edge, Colors.Red, duration: 0.5);

// Flow animation (continuous)
var animation = canvas.StartEdgeFlowAnimation(edge, speed: 50);
canvas.StopEdgeFlowAnimation(animation);

// Group animations
canvas.AnimateGroupCollapse(groupId, duration: 0.5);
canvas.AnimateGroupExpand(groupId, duration: 0.5);
```

## 💾 Serialization

```csharp
using FlowGraph.Core.Serialization;

// Save to JSON string
var json = GraphSerializer.Serialize(graph);

// Load from JSON string
var graph = GraphSerializer.Deserialize(json);

// Save to file
graph.SaveToFile("graph.json");

// Load from file
var graph = GraphExtensions.LoadFromFile("graph.json");

// Clone a graph
var copy = graph.Clone();
```

## 📁 Node Grouping

```csharp
// Create a group from selected nodes
canvas.GroupSelected();  // or provide a label
canvas.GroupSelected("My Group");

// Collapse/expand
canvas.ToggleGroupCollapse(groupId);

// Animated collapse/expand
canvas.AnimateGroupCollapse(groupId, duration: 0.5);
canvas.AnimateGroupExpand(groupId, duration: 0.5);

// Add/remove nodes
canvas.GroupManager.AddNodeToGroup(groupId, nodeId);
canvas.GroupManager.RemoveNodeFromGroup(nodeId);

// Ungroup
canvas.UngroupSelected();
```

## 🛤️ Edge Routing

```csharp
// Enable automatic routing
canvas.Settings.EnableEdgeRouting = true;
canvas.Settings.DefaultEdgeRouter = EdgeRouterType.Orthogonal;

// Available routers
// - Direct: Straight lines
// - Orthogonal: Right-angle paths with A* pathfinding
// - SmartBezier: Bezier curves that avoid obstacles

// Manual routing
canvas.Routing.RouteAllEdges();
canvas.Routing.RouteEdge(edgeId);
```

## 🏗️ Project Structure

```
FlowGraph/
├── FlowGraph.Core           # Data models, commands, serialization (no UI)
├── FlowGraph.Avalonia       # Avalonia UI controls and rendering
├── FlowGraph.Demo           # Sample application
└── FlowGraph.Core.Tests     # Unit tests (279 tests)
```

## 📋 Requirements

- .NET 9.0+
- Avalonia 11.2+

## 🗺️ Roadmap

See [ROADMAP.md](ROADMAP.md) for the full feature roadmap.

**v0.1.0 (Current)**
- ✅ React Flow community feature parity
- ✅ Animations, grouping, edge routing
- ⬜ Documentation improvements
- ⬜ Architecture refinements

**v1.0.0 (Planned - PRO)**
- Auto Layout Package (Dagre, Tree, Force)
- Export Package (PNG, SVG)
- Subflows (nested graphs)
- Performance Package (5,000+ nodes)

## 🤝 Contributing

Contributions are welcome! Priority areas:
1. Documentation improvements
2. Unit tests
3. Bug fixes
4. Performance optimizations

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- Inspired by [React Flow](https://reactflow.dev/)
- Built with [Avalonia UI](https://avaloniaui.net/)
