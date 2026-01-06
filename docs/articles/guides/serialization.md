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
// Save to file
graph.SaveToFile("my-graph.json");

// Load from file
var graph = GraphExtensions.LoadFromFile("my-graph.json");
```

## Cloning

Create a deep copy of a graph:

```csharp
var copy = graph.Clone();
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
      "inputs": [
        { "id": "in", "type": "data", "label": "Input" }
      ],
      "outputs": [
        { "id": "out", "type": "data", "label": "Output" }
      ],
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
      "label": "Connection"
    }
  ]
}
```

## Custom Data

Nodes support arbitrary data through the `Data` property:

```csharp
var node = new Node
{
    Type = "custom",
    Position = new Point(100, 100),
    Data = new Dictionary<string, object>
    {
        { "value", 42 },
        { "name", "Example" }
    }
};
```

Custom data is preserved during serialization.

## Async Operations

For large graphs, use async methods:

```csharp
// Async save
await graph.SaveToFileAsync("large-graph.json");

// Async load
var graph = await GraphExtensions.LoadFromFileAsync("large-graph.json");
```
