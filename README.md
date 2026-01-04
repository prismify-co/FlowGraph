# FlowGraph

A powerful, extensible node-based graph editor for .NET/Avalonia, inspired by [React Flow](https://reactflow.dev/).

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![Avalonia](https://img.shields.io/badge/Avalonia-11.x-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- 🎯 **Intuitive Editing** - Pan, zoom, drag nodes, create connections
- 🎨 **Custom Nodes** - Create your own node types with custom rendering
- 🔗 **Smart Edges** - Bezier, straight, step curves with routing algorithms
- 📦 **Node Grouping** - Group nodes with collapse/expand support
- ↩️ **Undo/Redo** - Full command history with Ctrl+Z/Y
- 💾 **Serialization** - Save/load graphs as JSON
- 🗺️ **Minimap** - Overview navigation
- ✅ **Validation** - Custom connection rules
- 📋 **Context Menus** - Right-click menus for nodes, edges, groups
- ✂️ **Clipboard** - Copy, cut, paste, duplicate operations

## Quick Start

### Installation

```bash
# Coming soon to NuGet
dotnet add package FlowGraph.Avalonia
```

### Basic Usage

```xml
<!-- In your AXAML -->
<Window xmlns:flow="using:FlowGraph.Avalonia">
    <flow:FlowCanvas Graph="{Binding Graph}" />
</Window>
```

```csharp
// In your ViewModel
public Graph Graph { get; } = new Graph
{
    Nodes =
    {
        new Node
        {
            Id = "1",
            Label = "Start",
            Position = new Point(100, 100),
            Outputs = { new Port { Id = "out1", Label = "Output" } }
        },
        new Node
        {
            Id = "2", 
            Label = "End",
            Position = new Point(400, 100),
            Inputs = { new Port { Id = "in1", Label = "Input" } }
        }
    },
    Edges =
    {
        new Edge { Source = "1", SourcePort = "out1", Target = "2", TargetPort = "in1" }
    }
};
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+A` | Select all |
| `Ctrl+C` | Copy |
| `Ctrl+X` | Cut |
| `Ctrl+V` | Paste |
| `Ctrl+D` | Duplicate |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo |
| `Ctrl+G` | Group selected |
| `Ctrl+Shift+G` | Ungroup |
| `Delete` | Delete selected |
| `Escape` | Deselect all / Cancel operation |

## Project Structure

```
FlowGraph.Core          - Data models, commands, serialization (no UI dependencies)
FlowGraph.Avalonia      - Avalonia UI controls and rendering
FlowGraph.Demo          - Sample application
FlowGraph.Core.Tests    - Unit tests (231 tests)
```

## Custom Node Types

```csharp
public class MyCustomNodeRenderer : INodeRenderer
{
    public bool CanRender(string nodeType) => nodeType == "custom";
    
    public Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        return new Border
        {
            Background = Brushes.Purple,
            Child = new TextBlock { Text = node.Label }
        };
    }
    
    // ... implement other interface members
}

// Register it
canvas.NodeRenderers.Register(new MyCustomNodeRenderer());
```

## Serialization

```csharp
// Save
var json = graph.ToJson();
graph.Save("graph.json");
await graph.SaveAsync("graph.json");

// Load
var graph = GraphSerializationExtensions.LoadFromFile("graph.json");
var graph = await GraphSerializationExtensions.LoadFromFileAsync("graph.json");

// Clone
var copy = graph.Clone();
```

## Connection Validation

```csharp
canvas.ConnectionValidator = new CompositeValidator(
    new PreventSelfConnectionValidator(),
    new PreventDuplicateConnectionValidator(),
    new TypeCompatibilityValidator()
);

canvas.ConnectionRejected += (s, e) => 
{
    Console.WriteLine($"Connection rejected: {e.Message}");
};
```

## Edge Routing

```csharp
// Built-in routers
var directRouter = new DirectRouter();           // Straight lines
var orthogonalRouter = new OrthogonalRouter();   // Right-angle paths
var bezierRouter = new SmartBezierRouter();      // Curved paths avoiding obstacles

// Use routing service
var routingService = new EdgeRoutingService();
routingService.SetRouter(orthogonalRouter);
routingService.RouteAllEdges(graph);
```

## Node Grouping

```csharp
// Create a group from selected nodes
var group = canvas.GroupSelected("My Group");

// Collapse/expand
canvas.SetGroupCollapsed(group.Id, true);
canvas.ToggleGroupCollapse(group.Id);

// Add nodes to existing group
canvas.AddNodesToGroup(groupId, nodeIds);

// Ungroup
canvas.UngroupSelected();
```

## Building

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run demo
dotnet run --project FlowGraph.Demo
```

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the full feature roadmap.

**Coming Soon:**
- 🎬 Animation system (smooth transitions)
- 📐 Auto-layout algorithms (Dagre, Elk, Force)
- 🎛️ Controls panel (zoom buttons)
- 📱 Touch/gesture support
- 🔄 State pattern refactor

## Technical Details

- **Target Framework:** .NET 9.0
- **UI Framework:** Avalonia 11.2.2
- **Language Version:** C# 14 (preview)
- **Nullable Reference Types:** Enabled

## Contributing

Contributions are welcome! Priority areas:
1. Animation system
2. Auto-layout algorithms
3. State pattern implementation
4. Documentation

## License

MIT License - feel free to use in commercial projects.

## Acknowledgments

- Inspired by [React Flow](https://reactflow.dev/)
- Built with [Avalonia UI](https://avaloniaui.net/)
