# Performance Optimization

FlowGraph is designed to scale from simple diagrams to complex graphs with thousands of nodes. This guide shows you how to configure FlowGraph for optimal performance at any scale.

> **How does FlowGraph compare?** Most web-based graph editors (React Flow, JointJS) recommend limiting graphs to 500 nodes. FlowGraph's architecture supports 2000+ nodes with smooth interactions when properly configured. The techniques in this guide unlock that scale.

## Quick Start: Optimization Presets

For most use cases, start with these presets and adjust as needed:

```csharp
// Preset for general-purpose graphs (works well up to ~500 nodes)
public static FlowCanvasSettings Balanced() => new()
{
    EnableVirtualization = true,
    VirtualizationBuffer = 200,
    UseSimplifiedNodeRendering = false,
    AutoRouteEdges = true,
    RouteOnlyAffectedEdges = true
};

// Preset for maximum performance (2000+ nodes)
public static FlowCanvasSettings HighPerformance() => new()
{
    EnableVirtualization = true,
    VirtualizationBuffer = 100,
    UseSimplifiedNodeRendering = true,
    DirectRenderingNodeThreshold = 100,
    AutoRouteEdges = false
};
```

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

Virtualization is the single most impactful optimization. It ensures only visible elements consume rendering resources.

| Graph Size    | Recommendation | Why                              |
| ------------- | -------------- | -------------------------------- |
| < 100 nodes   | Optional       | Overhead may not be worth it     |
| 100-500 nodes | Recommended    | Noticeable improvement           |
| 500+ nodes    | Essential      | Critical for smooth interactions |

> **Pro tip:** Set `VirtualizationBuffer` based on your node sizes. Larger nodes need larger buffers to avoid pop-in during fast panning.

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

For very large graphs, FlowGraph can bypass the Avalonia visual tree and render directly to the GPU. This is how professional diagramming tools achieve smooth performance with thousands of elements.

```csharp
// Enable direct rendering
canvas.EnableDirectRendering();

// This mode:
// - Draws nodes/edges directly to DrawingContext
// - Bypasses visual tree overhead
// - Trades some interactivity for performance
// - Ideal for 500+ nodes

// Automatically enables at threshold
canvas.Settings.DirectRenderingNodeThreshold = 100;

// Disable direct rendering
canvas.DisableDirectRendering();
```

### What You Keep vs. What Changes

| Feature                  | Standard Mode | Direct Rendering |
| ------------------------ | ------------- | ---------------- |
| Pan/Zoom                 | ✅ Full       | ✅ Full          |
| Node dragging            | ✅ Full       | ✅ Full          |
| Edge connections         | ✅ Full       | ✅ Full          |
| Selection                | ✅ Full       | ✅ Full          |
| Hover effects            | ✅ Animated   | ⚡ Simplified    |
| Custom controls in nodes | ✅ Full       | ❌ Not supported |
| Animations               | ✅ Full       | ⚡ Basic         |

> **When to use:** Enable direct rendering when you need smooth interactions with 500+ nodes and don't require embedded controls (buttons, text inputs) inside nodes. Most workflow and diagram applications work perfectly in this mode.

## Spatial Indexing (Quadtree)

FlowGraph uses a Quadtree spatial index for O(log N) hit testing performance. This is automatically enabled and provides significant improvements for graphs with 100+ elements.

### How It Works

Instead of checking every node/shape when you click or hover, the Quadtree partitions the canvas into regions, allowing FlowGraph to quickly narrow down which elements are near the cursor.

| Graph Size     | Linear Search | Quadtree   |
| -------------- | ------------- | ---------- |
| 100 elements   | ~100 checks   | ~7 checks  |
| 1000 elements  | ~1000 checks  | ~10 checks |
| 10000 elements | ~10000 checks | ~13 checks |

### Benefits

- **Faster hit testing** - Click and hover detection scales logarithmically
- **Smoother interactions** - Less CPU work during mouse movement
- **Automatic** - No configuration needed, works out of the box

The spatial index automatically updates when nodes are added, removed, or moved.

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

Edge routing (calculating paths that avoid overlapping nodes) is computationally intensive. Here's how to optimize it without losing functionality:

```csharp
// Strategy 1: Defer routing during load
canvas.Settings.AutoRouteEdges = false;
LoadLargeGraph();
canvas.Settings.AutoRouteEdges = true;
canvas.Routing.RouteAllEdges(); // Single batch operation

// Strategy 2: Route only what changed
canvas.Settings.RouteOnlyAffectedEdges = true; // Only re-route edges connected to moved nodes

// Strategy 3: Disable re-routing during drag (route on drop)
canvas.Settings.RouteEdgesOnDrag = false;

// Strategy 4: Use simpler routing for large graphs
canvas.Settings.EdgeRoutingAlgorithm = RouterAlgorithm.Direct; // Straight lines
// or
canvas.Settings.EdgeRoutingAlgorithm = RouterAlgorithm.Bezier; // Simple curves
// vs.
canvas.Settings.EdgeRoutingAlgorithm = RouterAlgorithm.SmartBezier; // Obstacle avoidance (slower)
```

### Routing Algorithm Comparison

| Algorithm   | Speed          | Visual Quality | Node Avoidance |
| ----------- | -------------- | -------------- | -------------- |
| Direct      | ⚡⚡⚡ Fastest | Basic          | No             |
| Bezier      | ⚡⚡ Fast      | Good           | No             |
| Orthogonal  | ⚡ Medium      | Clean          | Yes            |
| SmartBezier | Slower         | Best           | Yes            |

> **Recommendation:** Use `SmartBezier` for graphs under 200 edges where visual clarity matters. Use `Bezier` or `Direct` for larger graphs.

## Recommended Settings by Graph Size

These configurations have been tested for optimal balance of features and performance.

### Small Graphs (< 100 nodes)

Full features, no compromises needed.

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = false, // Not needed
    UseSimplifiedNodeRendering = false,
    DirectRenderingNodeThreshold = 0, // Disabled
    AutoRouteEdges = true,
    RouteEdgesOnDrag = true,
    EdgeRoutingAlgorithm = RouterAlgorithm.SmartBezier
};
```

### Medium Graphs (100-500 nodes)

Enable virtualization, keep full interactivity.

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = true,
    VirtualizationBuffer = 200,
    UseSimplifiedNodeRendering = false,
    DirectRenderingNodeThreshold = 300, // Auto-enable if needed
    AutoRouteEdges = true,
    RouteEdgesOnDrag = true,
    RouteOnlyAffectedEdges = true,
    EdgeRoutingAlgorithm = RouterAlgorithm.Bezier
};
```

### Large Graphs (500-2000 nodes)

Optimized rendering, selective routing.

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = true,
    VirtualizationBuffer = 150,
    UseSimplifiedNodeRendering = true,
    RenderBatchSize = 50,
    DirectRenderingNodeThreshold = 100,
    AutoRouteEdges = true,
    RouteEdgesOnDrag = false, // Route on drop instead
    RouteOnlyAffectedEdges = true,
    EdgeRoutingAlgorithm = RouterAlgorithm.Bezier
};

canvas.Settings = settings;
canvas.EnableSimplifiedRendering();
```

### Very Large Graphs (2000+ nodes)

Maximum performance mode. Full pan/zoom/selection, simplified visuals.

```csharp
var settings = new FlowCanvasSettings
{
    EnableVirtualization = true,
    VirtualizationBuffer = 100,
    UseSimplifiedNodeRendering = true,
    RenderBatchSize = 100,
    DirectRenderingNodeThreshold = 50,
    AutoRouteEdges = false, // On-demand routing
    RouteEdgesOnDrag = false,
    EdgeRoutingAlgorithm = RouterAlgorithm.Direct
};

canvas.Settings = settings;
canvas.EnableDirectRendering();

// Route edges on-demand when user selects nodes
canvas.SelectionChanged += (s, e) =>
{
    if (e.AddedNodes.Any())
        canvas.Routing.RouteEdgesForNodes(e.AddedNodes);
};
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

### Loading Large Graphs

1. **Use batch operations** - Always use `AddNodes()` / `AddEdges()` instead of loops with individual `AddNode()` / `AddEdge()` calls

2. **Disable animations during load** - Prevents rendering overhead during initial population

3. **Defer routing** - Disable `AutoRouteEdges` during load, enable after

```csharp
// Optimized loading pattern
public async Task LoadGraphOptimized(IEnumerable<Node> nodes, IEnumerable<Edge> edges)
{
    // Prepare canvas
    canvas.Settings.AutoRouteEdges = false;
    canvas.BeginBatchUpdate();

    try
    {
        // Batch add - triggers single notification
        graph.AddNodes(nodes);
        graph.AddEdges(edges);
    }
    finally
    {
        canvas.EndBatchUpdate();
    }

    // Fit view and route
    canvas.FitToView();
    canvas.Settings.AutoRouteEdges = true;
}
```

### Custom Node Renderers

If you're creating custom node renderers, keep them lightweight:

```csharp
// ❌ Avoid: Complex visual tree
public override Control CreateVisual(Node node)
{
    return new Border
    {
        Child = new StackPanel
        {
            Children =
            {
                new Border { Child = new TextBlock { /* header */ } },
                new ItemsControl { /* ports */ },
                new Border { Child = new ContentPresenter { /* content */ } }
            }
        }
    };
}

// ✅ Better: Flat structure
public override Control CreateVisual(Node node)
{
    return new NodeControl(node); // Single custom control
}

// ✅ Best: Direct rendering
public override void Render(DrawingContext context, Node node)
{
    // Draw directly - no visual tree overhead
    context.DrawRectangle(brush, pen, bounds);
    context.DrawText(formattedText, position);
}
```

### Data Management

Store large payloads outside the graph:

```csharp
// ❌ Avoid: Large data in node
node.Data = new {
    Image = LoadBitmap(),      // Large!
    Document = LoadXml(),      // Large!
    Metadata = complexObject   // Serialization overhead
};

// ✅ Better: Reference by ID
node.Data = new NodeData {
    ImageId = "img-123",
    DocumentId = "doc-456"
};

// Load on demand
var image = await imageCache.GetAsync(nodeData.ImageId);
```

### Profiling Tips

1. **Profile first** - Use FlowDiagnostics to identify actual bottlenecks before optimizing
2. **Check visible counts** - If visible nodes << total nodes, virtualization is working
3. **Monitor render time** - Should be < 16ms for 60fps
4. **Watch memory** - Growing memory indicates caching issues

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

FlowGraph scales from small diagrams to enterprise-grade graphs with thousands of nodes. The key is matching your configuration to your graph size.

| Graph Size | Virtualization | Simplified Rendering | Direct Rendering | Edge Routing       |
| ---------- | -------------- | -------------------- | ---------------- | ------------------ |
| < 100      | Optional       | No                   | No               | Full (SmartBezier) |
| 100-500    | Yes            | Optional             | Optional         | Full (Bezier)      |
| 500-2000   | Yes            | Recommended          | Recommended      | Selective          |
| 2000+      | Yes            | Yes                  | Yes              | On-Demand          |

> **Remember:** These are recommendations, not limitations. FlowGraph's architecture supports all configurations—you choose the balance of features and performance that fits your application.

### Common Scenarios

| Use Case                          | Recommended Config | Why                                  |
| --------------------------------- | ------------------ | ------------------------------------ |
| Node-based editor (Blender-style) | Balanced           | Need interactive nodes with controls |
| Workflow designer                 | Balanced           | Moderate size, full features         |
| Network topology viewer           | High Performance   | Large graphs, read-mostly            |
| Data lineage visualization        | High Performance   | Thousands of nodes, minimal editing  |
| Mind mapping                      | Balanced           | Interactive, moderate size           |
