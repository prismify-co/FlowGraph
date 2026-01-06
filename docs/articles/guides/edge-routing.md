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

| Type | Description |
|------|-------------|
| `EdgeType.Bezier` | Smooth curved path (default) |
| `EdgeType.Straight` | Direct line |
| `EdgeType.Step` | Right-angle path |
| `EdgeType.SmoothStep` | Rounded right-angle path |

## Automatic Routing

Enable automatic edge routing to avoid node overlaps:

```csharp
canvas.Settings.EnableEdgeRouting = true;
canvas.Settings.DefaultEdgeRouter = EdgeRouterType.Orthogonal;
```

### Router Types

| Router | Description |
|--------|-------------|
| `EdgeRouterType.Direct` | Straight lines (no routing) |
| `EdgeRouterType.Orthogonal` | Right-angle paths with A* pathfinding |
| `EdgeRouterType.SmartBezier` | Bezier curves that avoid obstacles |

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
var edge = new Edge
{
    Source = node1.Id,
    Target = node2.Id,
    SourcePort = "out",
    TargetPort = "in",
    Label = "Connection"
};
```

## Edge Reconnection

Users can drag edge endpoints to reconnect them to different ports. This is enabled by default.

```csharp
// Disable reconnection
canvas.Settings.EnableEdgeReconnection = false;
```
