# Edge Routing

FlowGraph supports multiple edge routing algorithms to control how edges are drawn between nodes.

## Edge Types

Set the edge type when creating an edge:

```csharp
var edge = new Edge
{
    Source = node1.Id,
    Target = node2.Id,
    SourcePort = "out",
    TargetPort = "in",
    Type = EdgeType.Bezier  // Default
};
```

### Available Types

| Type                  | Description                  |
| --------------------- | ---------------------------- |
| `EdgeType.Bezier`     | Smooth curved path (default) |
| `EdgeType.Straight`   | Direct line                  |
| `EdgeType.Step`       | Right-angle path             |
| `EdgeType.SmoothStep` | Rounded right-angle path     |

## Automatic Routing

Enable automatic edge routing to avoid node overlaps:

```csharp
canvas.Settings.EnableEdgeRouting = true;
canvas.Settings.DefaultEdgeRouter = EdgeRouterType.Orthogonal;
```

### Router Types

| Router                       | Description                            |
| ---------------------------- | -------------------------------------- |
| `EdgeRouterType.Direct`      | Straight lines (no routing)            |
| `EdgeRouterType.Orthogonal`  | Right-angle paths with A\* pathfinding |
| `EdgeRouterType.SmartBezier` | Bezier curves that avoid obstacles     |

## Manual Routing

Trigger routing manually:

```csharp
// Route all edges
canvas.Routing.RouteAllEdges();

// Route a specific edge
canvas.Routing.RouteEdge(edgeId);
```

## Edge Markers

Add arrow markers to edges:

```csharp
var edge = new Edge
{
    Source = node1.Id,
    Target = node2.Id,
    SourcePort = "out",
    TargetPort = "in",
    MarkerEnd = EdgeMarker.Arrow,      // Open arrow
    // or
    MarkerEnd = EdgeMarker.ArrowClosed // Filled arrow
};
```

## Edge Labels

Add labels to edges:

```csharp
// Simple label (centered on edge)
var edge = new Edge
{
    Source = node1.Id,
    Target = node2.Id,
    SourcePort = "out",
    TargetPort = "in",
    Label = "Connection"
};
```

### Advanced Label Positioning

Use `LabelInfo` for precise control over label placement:

```csharp
using FlowGraph.Core.Models;

// Label at the start of the edge (near source)
var edgeWithStartLabel = new Edge(new EdgeDefinition
{
    Id = "e1",
    Source = node1.Id,
    Target = node2.Id,
    SourcePort = "out",
    TargetPort = "in",
    LabelInfo = new LabelInfo("Yes", LabelAnchor.Start, offsetY: -10)
});

// Label at the end of the edge (near target)
var edgeWithEndLabel = new Edge(new EdgeDefinition
{
    Id = "e2",
    Source = node1.Id,
    Target = node2.Id,
    SourcePort = "out",
    TargetPort = "in",
    LabelInfo = new LabelInfo("No", LabelAnchor.End, offsetX: 5)
});

// Using the fluent builder method
var edge = existingEdge.Definition.WithLabelInfo("Label", LabelAnchor.Center, offsetX: 0, offsetY: -15);
```

### Label Anchor Positions

| Anchor               | Position | Description               |
| -------------------- | -------- | ------------------------- |
| `LabelAnchor.Start`  | ~25%     | Near the source node      |
| `LabelAnchor.Center` | ~50%     | At the midpoint (default) |
| `LabelAnchor.End`    | ~75%     | Near the target node      |

### LabelInfo Properties

| Property  | Type          | Description                                    |
| --------- | ------------- | ---------------------------------------------- |
| `Text`    | `string`      | The label text content                         |
| `Anchor`  | `LabelAnchor` | Position along the edge path                   |
| `OffsetX` | `double`      | Horizontal offset in pixels (positive = right) |
| `OffsetY` | `double`      | Vertical offset in pixels (positive = down)    |

> **Note:** When both `Label` and `LabelInfo` are set, `LabelInfo` takes precedence. Use `edge.Definition.EffectiveLabel` to get the resolved label text.

## Edge Reconnection

Users can drag edge endpoints to reconnect them to different ports. This is enabled by default.

```csharp
// Disable reconnection
canvas.Settings.EnableEdgeReconnection = false;
```
