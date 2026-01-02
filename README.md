# FlowGraph - Cross-Platform Node-Based Graph Library

A reusable C# library for building node-based graph editors (similar to React Flow) that works cross-platform on Windows, macOS, Linux using Avalonia UI framework.

## Project Structure

The solution consists of three projects:

### 1. FlowGraph.Core
A pure C# class library containing core data models and business logic:
- **Point.cs** - 2D coordinate struct with arithmetic operators
- **Port.cs** - Input/output connection points on nodes
- **Node.cs** - Graph node with position, type, and data
- **Edge.cs** - Connections between nodes
- **Graph.cs** - Main graph container with ObservableCollections

**Target Framework:** .NET 9.0  
**Dependencies:** None (UI-agnostic)

### 2. FlowGraph.Avalonia
An Avalonia control library containing UI components:
- **FlowCanvas** - Custom UserControl for rendering graphs
- Automatic reactive updates via ObservableCollections
- Dark theme with grid background
- Styled nodes and edges

**Target Framework:** .NET 9.0  
**Dependencies:**
- Avalonia 11.2.2
- Avalonia.Themes.Fluent 11.2.2
- Avalonia.ReactiveUI 11.2.2
- FlowGraph.Core (project reference)

### 3. FlowGraph.Demo
An Avalonia MVVM demo application showcasing the library:
- Sample graph with 3 connected nodes
- MVVM architecture
- Data binding to FlowCanvas

**Target Framework:** .NET 9.0  
**Dependencies:**
- Avalonia packages 11.2.2
- FlowGraph.Avalonia (project reference)

## Features

- ✅ **Cross-Platform** - Runs on Windows, macOS, Linux
- ✅ **Reactive UI** - Automatic updates via ObservableCollections
- ✅ **Separation of Concerns** - Core logic is UI-agnostic
- ✅ **Modern C#** - Uses C# preview features, collection expressions, required properties
- ✅ **NuGet Ready** - FlowGraph.Avalonia can be packaged as a library
- ✅ **Dark Theme** - Professional dark UI with grid background

## Building

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run demo
dotnet run --project FlowGraph.Demo/FlowGraph.Demo.csproj
```

## Usage Example

```csharp
using FlowGraph.Core;

// Create a graph
var graph = new Graph();

// Add nodes
var inputNode = new Node
{
    Type = "Input",
    Position = new Point(100, 100)
};

var processNode = new Node
{
    Type = "Process",
    Position = new Point(400, 150)
};

graph.AddNode(inputNode);
graph.AddNode(processNode);

// Connect nodes
graph.AddEdge(new Edge
{
    Source = inputNode.Id,
    Target = processNode.Id,
    SourcePort = "out",
    TargetPort = "in"
});
```

In XAML:
```xml
<Window xmlns:avalonia="using:FlowGraph.Avalonia">
    <avalonia:FlowCanvas Graph="{Binding MyGraph}"/>
</Window>
```

## Visual Design

- **Nodes:** 150x80px dark slate gray rectangles with steel blue borders
- **Edges:** 2px gray lines connecting node centers
- **Background:** Dark (#1E1E1E) with subtle grid pattern
- **Text:** White text showing node type and ID

## Technical Details

- **Language Version:** C# preview
- **Nullable Reference Types:** Enabled
- **Implicit Usings:** Enabled
- **Target Framework:** .NET 9.0 (designed for .NET 10 compatibility)

## Architecture

The library follows clean architecture principles:

1. **FlowGraph.Core** - Domain layer (no dependencies)
2. **FlowGraph.Avalonia** - Presentation layer (depends on Core)
3. **FlowGraph.Demo** - Application layer (depends on Avalonia library)

This design allows the core library to be used in other UI frameworks if needed, and the Avalonia library to be consumed by multiple applications.

## License

This is a demo project created for learning purposes.
