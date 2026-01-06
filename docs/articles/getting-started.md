# Getting Started

This guide will help you set up FlowGraph in your Avalonia application.

## Prerequisites

- .NET 9.0 or later
- An Avalonia 11.2+ application

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

public class MainViewModel
{
    public Graph Graph { get; } = new Graph();

    public MainViewModel()
    {
        // Create nodes
        var node1 = new Node
        {
            Type = "default",
            Position = new Point(100, 100),
            Label = "Node 1",
            Outputs = [new Port { Id = "out", Type = "data" }]
        };

        var node2 = new Node
        {
            Type = "default",
            Position = new Point(350, 100),
            Label = "Node 2",
            Inputs = [new Port { Id = "in", Type = "data" }]
        };

        Graph.AddNode(node1);
        Graph.AddNode(node2);

        // Connect nodes
        Graph.AddEdge(new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out",
            TargetPort = "in"
        });
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
