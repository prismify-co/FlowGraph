# Performance Optimization

FlowGraph includes several performance features to handle large graphs efficiently.

## Virtualization

Virtualization renders only visible nodes and edges, significantly improving performance for large graphs.

```csharp
// Enable virtualization (enabled by default)
canvas.Settings.EnableVirtualization = true;

// Configure buffer zone (nodes within this distance from viewport are rendered)
canvas.Settings.VirtualizationBuffer = 200; // Canvas units

// Virtualization is recommended for graphs with 100+ nodes
```

### When to Use Virtualization

- **Small graphs (< 100 nodes)**: Optional, overhead may outweigh benefits
- **Medium graphs (100-500 nodes)**: Recommended
- **Large graphs (500+ nodes)**: Essential

## Simplified Node Rendering

For graphs with hundreds of nodes, use simplified rendering to reduce visual tree complexity:

```csharp
// Enable simplified rendering
canvas.EnableSimplifiedRendering();

// This replaces node renderers with minimal visual elements
// - Single Border + TextBlock per node
// - No shadows, gradients, or complex effects
// - Significantly faster rendering

// Disable when needed
canvas.DisableSimplifiedRendering();

// Or configure via settings
canvas.Settings.UseSimplifiedNodeRendering = true;
canvas.Settings.RenderBatchSize = 50; // Render in batches to keep UI responsive
```

## Direct GPU Rendering

For very large graphs, FlowGraph can bypass the Avalonia visual tree and render directly to the GPU:

```csharp
// Enable direct rendering
canvas.EnableDirectRendering();

// This mode:
// - Draws nodes/edges directly to DrawingContext
// - Bypasses visual tree overhead
// - Trades interactivity for performance
// - Best for 500+ nodes

// Automatically enables at threshold
canvas.Settings.DirectRenderingNodeThreshold = 100;

// Disable direct rendering
canvas.DisableDirectRendering();
```

### Direct Rendering Trade-offs

**Advantages:**

- 10-100x faster rendering for large graphs
- Lower memory usage
- Smooth panning and zooming

**Limitations:**

- Reduced interactivity (hover effects, animations)
- No custom node controls (buttons, inputs)
- Basic visual styling only

## Batch Loading

When initially loading a large graph, use batch operations to suppress individual change notifications:

```csharp
using FlowGraph.Core;

// Method 1: Batch load mode
graph.BeginBatchLoad();
try
{
    for (int i = 0; i < 10000; i++)
    {
        graph.AddNode(CreateNode(i));
    }

    for (int i = 0; i < 20000; i++)
    {
        graph.AddEdge(CreateEdge(i));
    }
}
finally
{
    graph.EndBatchLoad(); // Single notification at end
}

// Method 2: Bulk operations
var nodes = Enumerable.Range(0, 10000).Select(CreateNode);
var edges = Enumerable.Range(0, 20000).Select(CreateEdge);

graph.AddNodes(nodes); // Single notification
graph.AddEdges(edges); // Single notification
```

## Edge Routing Performance

Edge routing can be expensive for large graphs. Configure it wisely:

```csharp
// Disable routing during initial load
canvas.Settings.AutoRouteEdges = false;

// Load graph...
LoadLargeGraph();

// Enable routing after load
canvas.Settings.AutoRouteEdges = true;
canvas.Routing.RouteAllEdges();

// Performance options
canvas.Settings.RouteEdgesOnDrag = false; // Disable re-routing during drag
canvas.Settings.RouteOnlyAffectedEdges = true; // Only re-route connected edges
canvas.Settings.RouteNewEdges = true; // Auto-route new connections
```

## Recommended Settings by Graph Size

### Small Graphs (< 100 nodes)

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = false, // Not needed
    UseSimplifiedNodeRendering = false,
    DirectRenderingNodeThreshold = 0, // Disabled
    AutoRouteEdges = true,
    RouteEdgesOnDrag = true
};
```

### Medium Graphs (100-500 nodes)

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = true,
    VirtualizationBuffer = 200,
    UseSimplifiedNodeRendering = false,
    DirectRenderingNodeThreshold = 300,
    AutoRouteEdges = true,
    RouteEdgesOnDrag = true,
    RouteOnlyAffectedEdges = true
};
```

### Large Graphs (500-2000 nodes)

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = true,
    VirtualizationBuffer = 150,
    UseSimplifiedNodeRendering = true,
    RenderBatchSize = 50,
    DirectRenderingNodeThreshold = 100,
    AutoRouteEdges = false, // Manual routing
    RouteEdgesOnDrag = false,
    RouteOnlyAffectedEdges = true
};

canvas.Settings = settings;
canvas.EnableSimplifiedRendering();
```

### Very Large Graphs (2000+ nodes)

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = true,
    VirtualizationBuffer = 100,
    UseSimplifiedNodeRendering = true,
    RenderBatchSize = 100,
    DirectRenderingNodeThreshold = 50,
    AutoRouteEdges = false,
    RouteEdgesOnDrag = false
};

canvas.Settings = settings;
canvas.EnableDirectRendering();
```

## Performance Monitoring

Use the FlowDiagnostics control to monitor performance:

```xml
<fgc:FlowPanel Position="TopLeft" Margin="8">
    <fgc:FlowDiagnostics TargetCanvas="{Binding #Canvas}" />
</fgc:FlowPanel>
```

The diagnostics panel shows:

- Node count (visible/total)
- Edge count (visible/total)
- Render time
- Frame rate
- Viewport info
- Current input state

## Best Practices

1. **Use batch operations** - Always use `AddNodes()` / `AddEdges()` instead of loops with `AddNode()` / `AddEdge()`

2. **Load progressively** - For very large graphs, consider lazy loading or pagination

3. **Disable animations** - During initial load, disable animations for faster rendering

4. **Profile first** - Use FlowDiagnostics to identify bottlenecks before optimizing

5. **Consider data** - Store large custom data outside the graph, reference by ID

6. **Simplify renderers** - Custom node renderers should be as simple as possible

7. **Edge routing** - Disable during load, enable only if needed

8. **Viewport bounds** - Set reasonable bounds to prevent infinite panning

## Async Loading Example

```csharp
public async Task LoadLargeGraphAsync(string filePath)
{
    // Disable rendering updates
    canvas.Settings.AutoRouteEdges = false;

    // Load graph data
    var graph = await GraphExtensions.LoadFromFileAsync(filePath);

    // Show progress
    var progress = new Progress<int>(value =>
    {
        ProgressBar.Value = value;
    });

    // Batch load into canvas
    graph.BeginBatchLoad();

    // Could add nodes in chunks here if needed
    // for better responsiveness

    graph.EndBatchLoad();

    // Set graph
    canvas.Graph = graph;

    // Fit to view after load
    await Task.Delay(100); // Allow layout
    canvas.FitToView();

    // Re-enable routing if needed
    canvas.Settings.AutoRouteEdges = true;
}
```

## Memory Management

For long-running applications:

```csharp
// Clear graph when switching documents
canvas.Graph = null;

// Force garbage collection if needed
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

// Dispose of custom resources in node Data property
foreach (var node in graph.Nodes)
{
    if (node.Data is IDisposable disposable)
        disposable.Dispose();
}
```

## Summary

| Graph Size | Virtualization | Simplified Rendering | Direct Rendering | Edge Routing |
| ---------- | -------------- | -------------------- | ---------------- | ------------ |
| < 100      | Optional       | No                   | No               | Full         |
| 100-500    | Yes            | Optional             | Optional         | Full         |
| 500-2000   | Yes            | Yes                  | Yes              | Limited      |
| 2000+      | Yes            | Yes                  | Yes              | Manual       |
