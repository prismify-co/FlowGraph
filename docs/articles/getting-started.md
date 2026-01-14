# Getting Started

This guide will help you set up FlowGraph in your Avalonia application.

## Prerequisites

- .NET 9.0 or later
- An Avalonia 11.3+ application

## Installation

Add the NuGet package to your project:

```bash
dotnet add package FlowGraph.Avalonia
```

## Basic Setup

### 1. Add Namespaces

In your AXAML file, add the FlowGraph namespaces:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:fg="using:FlowGraph.Avalonia"
        xmlns:fgc="using:FlowGraph.Avalonia.Controls">
```

### 2. Add the FlowCanvas

```xml
<Panel>
    <fgc:FlowBackground x:Name="Background" Variant="Dots" Gap="20" />
    <fg:FlowCanvas x:Name="Canvas" Graph="{Binding Graph}" />
</Panel>
```

### 3. Create a Graph

In your ViewModel or code-behind:

```csharp
using FlowGraph.Core;
using FlowGraph.Core.Models;
using System.Collections.Immutable;

public class MainViewModel
{
    public Graph Graph { get; } = new Graph();

    public MainViewModel()
    {
        // Create nodes using Definition + State pattern
        var node1Definition = new NodeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Type = "default",
            Label = "Node 1",
            Outputs = [new PortDefinition { Id = "out", Type = "data" }]
        };

        var node1State = new NodeState { X = 100, Y = 100 };
        var node1 = new Node(node1Definition, node1State);

        var node2Definition = new NodeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Type = "default",
            Label = "Node 2",
            Inputs = [new PortDefinition { Id = "in", Type = "data" }]
        };

        var node2State = new NodeState { X = 350, Y = 100 };
        var node2 = new Node(node2Definition, node2State);

        Graph.AddNode(node1);
        Graph.AddNode(node2);

        // Connect nodes
        var edgeDefinition = new EdgeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.Arrow
        };

        Graph.AddEdge(new Edge(edgeDefinition, new EdgeState()));
    }
}

// Backward-compatible shorthand using parameterless constructor
public class LegacyViewModel
{
    public Graph Graph { get; } = new Graph();

    public LegacyViewModel()
    {
        var node = new Node
        {
            Type = "default",
            Position = new Point(100, 100),
            Label = "My Node",
            Outputs = [new Port { Id = "out", Type = "data" }]
        };

        Graph.AddNode(node);
    }
}
```

### 4. Connect Background to Canvas

In your code-behind:

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

## Next Steps

- [Custom Node Renderers](guides/custom-nodes.md)
- [Theming](guides/theming.md)
- [Animations](guides/animations.md)
- [API Reference](../api/index.md)
