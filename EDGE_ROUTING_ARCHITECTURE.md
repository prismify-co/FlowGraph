# Edge Routing Architecture

## Overview

This document outlines the edge routing architecture for FlowGraph, designed with clean separation of concerns between Community and Pro editions.

## Industry Research Summary

| Feature | React Flow | GoJS | FlowGraph (Target) |
|---------|-----------|------|-------------------|
| Edge Types | Bezier, Straight, Step, SmoothStep | Bezier, JumpOver, JumpGap | Bezier, Straight, Step, SmoothStep |
| Routing Modes | None (manual) | Normal, Orthogonal, AvoidsNodes | Direct, Orthogonal, SmartBezier (Pro), AvoidsNodes (Pro) |
| Port Spreading | No | fromSpot/toSpot | Port.Position + EdgeSpacing |
| Corner Radius | No | Yes | Yes (SmoothStep) |
| End Segments | No | fromEndSegmentLength/toEndSegmentLength | EndSegmentLength |
| Crossings | No | JumpOver, JumpGap | JumpOver (Pro) |

## Architecture

### Core Layer (FlowGraph.Core)

The Core layer provides:
1. **Data models** - `Edge`, `Port`, `PortPosition`, `EdgeType`
2. **Interfaces** - `IEdgeRouter`, `EdgeRoutingContext`, `EdgeRoutingOptions`
3. **Basic routers** - `DirectRouter`, `OrthogonalRouter`

```
FlowGraph.Core/
├── Edge.cs                    # Edge data model
├── EdgeType.cs                # Enum: Straight, Bezier, Step, SmoothStep
├── Port.cs                    # Port with Position property
├── PortPosition.cs            # Enum: Left, Right, Top, Bottom
└── Routing/
    ├── IEdgeRouter.cs         # Router interface
    ├── EdgeRoutingContext.cs  # Context passed to routers
    ├── EdgeRoutingOptions.cs  # Configuration options
    ├── EdgeRoutingService.cs  # Service coordinating routing
    ├── DirectRouter.cs        # Simple point-to-point
    └── OrthogonalRouter.cs    # Right-angle paths
```

### Avalonia Layer (FlowGraph.Avalonia)

The Avalonia layer provides:
1. **Path rendering** - `EdgePathHelper` for geometry creation
2. **Visual management** - `EdgeVisualManager` for rendering
3. **Routing integration** - `EdgeRoutingManager` bridging Core and UI

```
FlowGraph.Avalonia/
├── Rendering/
│   ├── BezierHelper.cs        # Path geometry creation (all edge types)
│   ├── EdgeVisualManager.cs   # Renders edges, respects Port.Position
│   └── EdgeRenderers/         # Pluggable edge renderers
└── Routing/
    └── EdgeRoutingManager.cs  # Integrates routing with canvas
```

### Pro Layer (FlowGraph.Pro)

The Pro layer provides advanced routing:
1. **Smart routing** - `SmartBezierRouter` with obstacle avoidance
2. **A* pathfinding** - `AStarRouter` for complex graphs
3. **Jump crossings** - `JumpOverRenderer` for crossing edges

```
FlowGraph.Pro/
├── Routing/
│   ├── SmartBezierRouter.cs   # Obstacle-aware bezier routing
│   ├── AStarRouter.cs         # Grid-based pathfinding
│   └── EdgeCrossingDetector.cs
└── Avalonia/
    └── EdgeRenderers/
        └── JumpOverEdgeRenderer.cs  # Renders crossing jumps
```

## Design Patterns

### 1. Strategy Pattern - Edge Routers

```csharp
public interface IEdgeRouter
{
    IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge);
    bool SupportsEdgeType(EdgeType type);
}

// Registration
routingService.RegisterRouter("smartbezier", new SmartBezierRouter());
```

### 2. Options Pattern - Configuration

```csharp
public class EdgeRoutingOptions
{
    public double CornerRadius { get; set; } = 10;
    public double EndSegmentLength { get; set; } = 50;
    public double NodePadding { get; set; } = 10;
    public double EdgeSpacing { get; set; } = 8;
    public bool SpreadEdgesOnPort { get; set; } = true;
}
```

### 3. Builder Pattern - Edge Creation

```csharp
var edge = Edge.Create("source", "target")
    .WithType(EdgeType.SmoothStep)
    .WithCornerRadius(15)
    .WithSourcePort("out-1")
    .WithTargetPort("in-1")
    .Build();
```

## Component Responsibilities

### EdgeRoutingOptions (Core)

Configuration object for routing behavior:

```csharp
public class EdgeRoutingOptions
{
    // Corner rounding for Step/SmoothStep edges
    public double CornerRadius { get; set; } = 10;
    
    // Minimum distance from port before first turn
    public double EndSegmentLength { get; set; } = 50;
    
    // Padding around nodes for AvoidsNodes routing
    public double NodePadding { get; set; } = 10;
    
    // Spacing between multiple edges from same port side
    public double EdgeSpacing { get; set; } = 8;
    
    // Whether to automatically spread edges from same port side
    public bool SpreadEdgesOnPort { get; set; } = true;
    
    // Pro: Enable obstacle avoidance
    public bool AvoidsNodes { get; set; } = false;
    
    // Pro: Jump style for edge crossings
    public EdgeCrossingStyle CrossingStyle { get; set; } = EdgeCrossingStyle.None;
}

public enum EdgeCrossingStyle
{
    None,
    JumpOver,    // Arc over crossing edge
    JumpGap      // Gap at crossing point
}
```

### EdgeType Enum (Core)

```csharp
public enum EdgeType
{
    Straight,    // Direct line between points
    Bezier,      // Smooth cubic bezier curve
    Step,        // Right-angle orthogonal path
    SmoothStep   // Right-angle path with rounded corners
}
```

### Port Spreading (Community)

When multiple edges connect to the same port side, spread them automatically:

```csharp
// EdgeVisualManager calculates spread positions
private (double X, double Y) GetPortCanvasPosition(
    Node node, Port? port, double nodeWidth, double nodeHeight, 
    bool isOutput, int edgeIndex, int totalEdges)
{
    var basePosition = GetBasePortPosition(node, port, nodeWidth, nodeHeight, isOutput);
    
    if (totalEdges <= 1 || !_settings.SpreadEdgesOnPort)
        return basePosition;
    
    // Spread edges along the port side
    var spacing = _settings.EdgeSpacing;
    var totalSpread = spacing * (totalEdges - 1);
    var offset = -totalSpread / 2 + edgeIndex * spacing;
    
    return position switch
    {
        PortPosition.Left or PortPosition.Right => (basePosition.X, basePosition.Y + offset),
        PortPosition.Top or PortPosition.Bottom => (basePosition.X + offset, basePosition.Y),
        _ => basePosition
    };
}
```

### Router Interface (Core)

```csharp
public interface IEdgeRouter
{
    /// <summary>
    /// Routes an edge, returning waypoints including start and end.
    /// </summary>
    IReadOnlyList<Point> Route(EdgeRoutingContext context, Edge edge);
    
    /// <summary>
    /// Whether this router supports the given edge type.
    /// </summary>
    bool SupportsEdgeType(EdgeType type) => true;
    
    /// <summary>
    /// Priority for automatic router selection (higher = preferred).
    /// </summary>
    int Priority => 0;
}
```

## Migration Path

### Phase 1: Core Cleanup
1. ✅ Add `Port.Position` property
2. ✅ Add `ShowPorts` setting  
3. Add `EdgeRoutingOptions` class
4. Refactor `EdgeRoutingService` to use options

### Phase 2: Community Edge Types
1. Ensure `EdgeType.Step` works correctly in `EdgePathHelper`
2. Add `SmoothStep` support with configurable corner radius
3. Implement port spreading for multiple edges

### Phase 3: Pro Migration
1. Move `SmartBezierRouter` to `FlowGraph.Pro`
2. Add `AStarRouter` for grid-based pathfinding
3. Implement `JumpOverEdgeRenderer`
4. Add `EdgeCrossingDetector`

### Phase 4: Testing & Documentation
1. Unit tests for all routers
2. Integration tests for edge rendering
3. Demo pages showcasing features
4. API documentation

## File Organization

### Before (Current)
```
FlowGraph.Core/Routing/
├── DirectRouter.cs
├── EdgeRoutingContext.cs
├── EdgeRoutingService.cs
├── IEdgeRouter.cs
├── OrthogonalRouter.cs
└── SmartBezierRouter.cs  ← Should be in Pro
```

### After (Target)
```
FlowGraph.Core/
├── Models/
│   ├── EdgeType.cs
│   └── PortPosition.cs
└── Routing/
    ├── IEdgeRouter.cs
    ├── EdgeRoutingContext.cs
    ├── EdgeRoutingOptions.cs
    ├── EdgeRoutingService.cs
    ├── DirectRouter.cs
    └── OrthogonalRouter.cs

FlowGraph.Pro/
└── Routing/
    ├── SmartBezierRouter.cs
    ├── AStarRouter.cs
    └── EdgeCrossingDetector.cs
```

## API Examples

### Basic Edge Creation (Community)

```csharp
// Simple edge with default routing
graph.AddEdge("node1", "node2");

// Edge with specific type
graph.AddEdge("node1", "node2", EdgeType.SmoothStep);

// Edge with port specification
graph.AddEdge(new EdgeDefinition
{
    Source = "node1",
    Target = "node2",
    SourcePort = "out-right",
    TargetPort = "in-left",
    Type = EdgeType.Bezier
});
```

### Advanced Routing (Pro)

```csharp
// Enable smart routing that avoids nodes
canvas.Settings.DefaultRouterAlgorithm = RouterAlgorithm.SmartBezier;
canvas.Settings.RoutingOptions = new EdgeRoutingOptions
{
    AvoidsNodes = true,
    NodePadding = 20,
    CornerRadius = 15
};

// Or use A* pathfinding for complex graphs
canvas.Settings.DefaultRouterAlgorithm = RouterAlgorithm.AStar;
```

### Custom Router Registration

```csharp
// Register a custom router
routingService.RegisterRouter("custom", new MyCustomRouter());

// Use for specific edges
var edge = graph.AddEdge("a", "b");
edge.RouterAlgorithm = "custom";
```

## Testing Strategy

### Unit Tests (Core)

```csharp
[TestClass]
public class DirectRouterTests
{
    [TestMethod]
    public void Route_ReturnsStartAndEnd_WhenNoObstacles()
    {
        var router = new DirectRouter();
        var context = CreateContext();
        var edge = CreateEdge("A", "B");
        
        var route = router.Route(context, edge);
        
        Assert.AreEqual(2, route.Count);
    }
}

[TestClass]
public class OrthogonalRouterTests
{
    [TestMethod]
    public void Route_CreatesRightAnglePath_ForHorizontalNodes()
    {
        // Test orthogonal routing creates proper L-shaped paths
    }
}
```

### Integration Tests (Avalonia)

```csharp
[TestClass]
public class EdgeVisualManagerTests
{
    [TestMethod]
    public void RenderEdge_RespectsPortPosition()
    {
        // Test that edges connect to correct port positions
    }
    
    [TestMethod]
    public void RenderEdge_SpreadsMultipleEdges()
    {
        // Test that multiple edges from same port are spread
    }
}
```

## Conclusion

This architecture provides:
1. **Clean separation** - Core vs Pro features are clearly delineated
2. **Extensibility** - New routers can be added via strategy pattern
3. **Configuration** - Options pattern allows flexible customization
4. **Testability** - Each component has clear responsibilities
5. **Industry alignment** - Features match React Flow and GoJS capabilities
