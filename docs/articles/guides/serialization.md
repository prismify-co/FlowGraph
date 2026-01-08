# Serialization

FlowGraph supports JSON serialization for saving and loading graphs.

## Basic Usage

```csharp
using FlowGraph.Core.Serialization;

// Serialize to JSON string
string json = GraphSerializer.Serialize(graph);

// Deserialize from JSON string
Graph graph = GraphSerializer.Deserialize(json);
```

## File Operations

Extension methods for file I/O:

```csharp
// Synchronous operations
graph.SaveToFile("my-graph.json");
var graph = GraphExtensions.LoadFromFile("my-graph.json");

// Async operations (recommended for large graphs)
await graph.SaveToFileAsync("my-graph.json");
var graph = await GraphExtensions.LoadFromFileAsync("my-graph.json");
```

## Batch Loading

For better performance when loading large graphs, use batch loading to suppress change notifications:

```csharp
// Using batch load mode
graph.BeginBatchLoad();
try
{
    // Add many nodes and edges
    foreach (var nodeData in largeDataSet)
    {
        graph.AddNode(CreateNode(nodeData));
    }

    foreach (var edgeData in edgeDataSet)
    {
        graph.AddEdge(CreateEdge(edgeData));
    }
}
finally
{
    graph.EndBatchLoad(); // Raises single notification
}

// Or use bulk operations
graph.AddNodes(nodeCollection); // Single notification for all nodes
graph.AddEdges(edgeCollection); // Single notification for all edges

// Subscribe to completion event
graph.BatchLoadCompleted += (s, e) =>
{
    Console.WriteLine("Graph loaded, UI will refresh");
};
```

## Cloning

Create a deep copy of a graph:

```csharp
var copy = graph.Clone();
```

## Definition + State Pattern

FlowGraph uses an immutable Definition + mutable State pattern:

```csharp
using FlowGraph.Core.Models;

// Node Definition (immutable) - structural properties
var definition = new NodeDefinition
{
    Id = Guid.NewGuid().ToString(),
    Type = "process",
    Label = "My Node",
    Inputs = [new PortDefinition { Id = "in", Type = "data" }],
    Outputs = [new PortDefinition { Id = "out", Type = "data" }],
    IsSelectable = true,
    IsDraggable = true,
    Data = customData
};

// Node State (mutable) - runtime properties
var state = new NodeState
{
    X = 100,
    Y = 100,
    Width = 150,
    Height = 80,
    IsSelected = false
};

var node = new Node(definition, state);

// Modify definition immutably
node.Definition = node.Definition with { Label = "Updated" };

// Modify state directly
node.State.X = 200;
node.State.IsSelected = true;

// Backward-compatible pass-through properties still work
node.Label = "Another Update"; // Updates Definition internally
node.Position = new Point(300, 300); // Updates State
```

## JSON Format

The serialized format includes all graph data:

```json
{
  "nodes": [
    {
      "id": "node-1",
      "type": "default",
      "label": "My Node",
      "position": { "x": 100, "y": 100 },
      "width": 150,
      "height": 80,
      "inputs": [{ "id": "in", "type": "data", "label": "Input" }],
      "outputs": [{ "id": "out", "type": "data", "label": "Output" }],
      "isSelectable": true,
      "isDraggable": true,
      "isDeletable": true,
      "isConnectable": true,
      "isResizable": true,
      "data": { "customProperty": "value" }
    }
  ],
  "edges": [
    {
      "id": "edge-1",
      "source": "node-1",
      "target": "node-2",
      "sourcePort": "out",
      "targetPort": "in",
      "type": "bezier",
      "markerEnd": "arrow",
      "label": "Connection",
      "autoRoute": false
    }
  ]
}
```

## Custom Data

Nodes support arbitrary data through the `Data` property:

```csharp
var node = new Node(
    new NodeDefinition
    {
        Id = "node1",
        Type = "custom",
        Data = new Dictionary<string, object>
        {
            { "value", 42 },
            { "name", "Example" }
        }
    },
    new NodeState { X = 100, Y = 100 }
);

// Backward-compatible approach
var legacyNode = new Node
{
    Type = "custom",
    Position = new Point(100, 100),
    Data = new { value = 42, name = "Example" }
};
```

Custom data is preserved during serialization.

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
```

## Performance Tips

- Use `BeginBatchLoad()` / `EndBatchLoad()` for large graphs
- Use `AddNodes()` / `AddEdges()` instead of individual adds
- Use async methods for file operations
- Consider virtualization for 100+ nodes (see [Performance Guide](performance.md))
