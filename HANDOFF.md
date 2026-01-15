# FlowGraph Canvas-First Architecture - Handoff Document

> **Date:** January 9, 2026  
> **Session Goal:** Complete canvas-first architecture refactor  
> **Estimated Time:** 4 hours
> **Last Updated:** January 14, 2026 - Graph API Refactor Complete

---

## ⚠️ BREAKING CHANGE: Graph API Refactor (Jan 14, 2026)

### Summary

`Graph.Elements` is now the **single source of truth**. `Graph.Nodes` and `Graph.Edges` are read-only views.

### Migration Guide

| Old API                         | New API                       |
| ------------------------------- | ----------------------------- |
| `graph.Nodes.Add(node)`         | `graph.AddNode(node)`         |
| `graph.Edges.Add(edge)`         | `graph.AddEdge(edge)`         |
| `graph.Nodes.Remove(node)`      | `graph.Elements.Remove(node)` |
| `graph.Edges.Remove(edge)`      | `graph.Elements.Remove(edge)` |
| `graph.Nodes.CollectionChanged` | `graph.NodesChanged`          |
| `graph.Edges.CollectionChanged` | `graph.EdgesChanged`          |
| `graph.Nodes.Clear()`           | `graph.Elements.Clear()`      |

### Transform-Based Rendering

Renderers now use **logical (unscaled) dimensions**. The `MatrixTransform` on `MainCanvas` handles all zoom/pan.

| Property                     | Value            | Usage                          |
| ---------------------------- | ---------------- | ------------------------------ |
| `RenderContext.Scale`        | Always `1.0`     | Use for visual sizing          |
| `RenderContext.ViewportZoom` | Actual zoom      | Use for calculations           |
| `RenderContext.InverseScale` | `1/ViewportZoom` | Use for constant-size elements |

---

## Context

### What We're Building

FlowGraph is being refactored from a **graph-centric** to a **canvas-first** architecture. The coordinate system (grid) becomes the foundation, and graph concepts (nodes/edges) become just one type of content that can be rendered.

### Why This Matters

- **Current limitation:** Everything must be a Node or Edge, forcing workarounds like using `IBackgroundRenderer` for sequence diagram lifelines
- **New capability:** Render any positioned element (swimlanes, lifelines, annotations, shapes) as first-class citizens
- **Mental model:** The grid is the product; graph is just one thing you can render on it

### Key Insight from ReactFlow

ReactFlow provides `<ViewportPortal />` for arbitrary positioned content. FlowGraph needs an equivalent - achieved through the `CanvasElement` abstraction.

---

## Current State (UPDATED)

### What's Working

- ✅ FlowGraph.Core and FlowGraph.Avalonia build successfully
- ✅ FlowGraph.Pro and FlowGraph.Pro.Demo build successfully
- ✅ All 593 tests pass (360 community + 233 Pro)
- ✅ Diagnostics logger (`FlowGraphLogger`) implemented and integrated
- ✅ Panning bug fixed (`RenderAll()` called instead of `RenderGrid()`)
- ✅ **PHASE 1 COMPLETE** - Core abstractions created
- ✅ **PHASE 2.4 COMPLETE** - Shape renderers implemented
- ✅ **PHASE 2.5 COMPLETE** - Shape integration into FlowCanvas

### Phase 1 Completed Work

1. Created `FlowGraph.Core/Elements/` hierarchy:

   - `ICanvasElement.cs` - Base interface for all canvas elements
   - `CanvasElement.cs` - Abstract base class with INPC
   - `Rect.cs` - Bounds structure with Contains/Intersects
   - `ElementCollection.cs` - Observable collection with typed accessors

2. Updated Node and Edge (PRAGMATIC approach - interface, not inheritance):

   - `Node.cs` - Now implements `ICanvasElement`
   - `Edge.cs` - Now implements `ICanvasElement` with explicit interface members

3. Created Shape Elements:

   - `ShapeElement.cs` - Base class for shapes
   - `RectangleElement.cs` - Rounded rectangles, swimlanes
   - `LineElement.cs` - Lines with dash patterns, caps
   - `TextElement.cs` - Standalone text with formatting
   - `EllipseElement.cs` - Circles and ellipses

4. Updated Graph:
   - Added `ElementCollection Elements` property
   - `Nodes` and `Edges` marked `[Obsolete]` for deprecation path
   - Added `AddElement()`, `RemoveElement()`, `AddElements()`
   - All methods sync to both collections for backward compat

### Phase 2 Completed Work

1. Created `FlowGraph.Avalonia/Rendering/ShapeRenderers/`:

   - ✅ `IShapeRenderer.cs` - Interface for shape renderers
   - ✅ `ShapeRenderContext.cs` - Context with settings, scale, color parsing
   - ✅ `ShapeRendererRegistry.cs` - Instance-based registry with built-in renderers
   - ✅ `ShapeVisualManager.cs` - Manages shape visuals on canvas
   - ✅ `RectangleRenderer.cs` - Rounded rectangles with labels
   - ✅ `LineRenderer.cs` - Lines with dash patterns and caps
   - ✅ `TextRenderer.cs` - Standalone text with formatting
   - ✅ `EllipseRenderer.cs` - Circles and ellipses

2. Integrated into FlowCanvas:
   - ✅ Added `_shapeVisualManager` field to `FlowCanvas.axaml.cs`
   - ✅ Initialized in `OnAttachedToVisualTree` after `_mainCanvas` is available
   - ✅ Added `RenderShapes()` method to `FlowCanvas.Rendering.cs`
   - ✅ Integrated into `RenderAll()` pipeline (Grid → Backgrounds → Shapes → Graph)
   - ✅ Exposed `ShapeRenderers` property for custom renderer registration
   - ✅ Added `GetShapeVisual(shapeId)` method for visual access

### Key Files Created/Modified This Session

```
FlowGraph.Core/
├── Elements/
│   ├── ICanvasElement.cs          ✅ NEW
│   ├── CanvasElement.cs           ✅ NEW
│   ├── Rect.cs                    ✅ NEW
│   ├── ElementCollection.cs       ✅ NEW
│   ├── Nodes/                     ✅ NEW (placeholder)
│   ├── Edges/                     ✅ NEW (placeholder)
│   └── Shapes/
│       ├── ShapeElement.cs        ✅ NEW
│       ├── RectangleElement.cs    ✅ NEW
│       ├── LineElement.cs         ✅ NEW
│       ├── TextElement.cs         ✅ NEW
│       └── EllipseElement.cs      ✅ NEW
├── Node.cs                        ✅ MODIFIED - implements ICanvasElement
├── Edge.cs                        ✅ MODIFIED - implements ICanvasElement
└── Graph.cs                       ✅ MODIFIED - has ElementCollection

FlowGraph.Avalonia/
├── Rendering/
│   └── ShapeRenderers/
│       ├── IShapeRenderer.cs      ✅ NEW
│       ├── ShapeRenderContext.cs  ✅ NEW
│       ├── ShapeRendererRegistry.cs ✅ NEW
│       ├── ShapeVisualManager.cs  ✅ NEW
│       ├── RectangleRenderer.cs   ✅ NEW
│       ├── LineRenderer.cs        ✅ NEW
│       ├── TextRenderer.cs        ✅ NEW
│       └── EllipseRenderer.cs     ✅ NEW
├── FlowCanvas.axaml.cs            ✅ MODIFIED - ShapeVisualManager, ShapeRenderers property
└── FlowCanvas.Rendering.cs        ✅ MODIFIED - RenderShapes() method
```

---

## How to Use Shapes (For Developers)

### Add a Shape to the Graph

```csharp
var graph = new Graph("My Graph");

// Add a rectangle shape
var rect = new RectangleElement
{
    Id = "swimlane-1",
    Label = "Customer",
    Position = new Point(50, 50),
    Width = 200,
    Height = 400,
    Fill = "#E3F2FD",
    Stroke = "#1976D2",
    StrokeWidth = 2,
    CornerRadius = 8
};
graph.AddElement(rect);

// Add a text annotation
var text = new TextElement
{
    Id = "note-1",
    Text = "Important process step",
    Position = new Point(100, 100),
    FontSize = 14,
    Fill = "#333333"
};
graph.AddElement(text);

// Add a line separator
var line = new LineElement
{
    Id = "divider-1",
    Position = new Point(0, 200),
    EndX = 600,
    EndY = 200,
    Stroke = "#CCCCCC",
    StrokeDashArray = "5,5"
};
graph.AddElement(line);
```

### Register a Custom Shape Renderer

```csharp
// Via FlowCanvas property
canvas.ShapeRenderers.Register("custom-shape", new MyCustomShapeRenderer());

// Or via singleton
ShapeRendererRegistry.Instance.Register("custom-shape", new MyCustomShapeRenderer());
```

### Access a Shape's Visual

```csharp
var visual = canvas.GetShapeVisual("swimlane-1");
if (visual != null)
{
    // Customize the visual
    visual.Opacity = 0.5;
}
```

---

## Phase 2.6 Complete: Public Coordinate API

The following coordinate API is now available on `FlowCanvas`:

```csharp
// Get current zoom level (already existed as CurrentZoom)
double zoom = canvas.CurrentZoom;

// Get current pan offset
Point offset = canvas.Offset;

// Convert canvas coordinates to screen coordinates
Point screenPos = canvas.CanvasToScreen(100, 200);
Point screenPos2 = canvas.CanvasToScreen(canvasPoint);

// Convert screen coordinates to canvas coordinates
Point canvasPos = canvas.ScreenToCanvas(150, 250);
Point canvasPos2 = canvas.ScreenToCanvas(screenPoint);

// Get the visible area in canvas coordinates
Rect visibleArea = canvas.VisibleBounds;
```

### Usage Example: Custom Overlay

```csharp
// Position an overlay at a specific canvas location
var overlayPanel = new Canvas();
var canvasX = 100.0;
var canvasY = 200.0;

// Convert to screen coordinates for positioning
var screenPos = canvas.CanvasToScreen(canvasX, canvasY);
Canvas.SetLeft(overlayPanel, screenPos.X);
Canvas.SetTop(overlayPanel, screenPos.Y);
```

---

## Next Steps

### Remaining Work

1. **Phase 4: Pro & Demo Updates**

   - Update FlowGraph.Pro.Demo to test shapes
   - Consider converting LifelineBackgroundRenderer to use shapes

2. **Optional Improvements**
   - Add shape serialization to GraphSerializer
   - Add shape selection support
   - Add shape hit-testing for click events

### Testing the Integration

To verify shapes work end-to-end:

```csharp
// In a demo app:
var graph = new Graph("Shape Test");

// Add a shape
var rect = new RectangleElement
{
    Id = "test-rect",
    Position = new Point(100, 100),
    Width = 200,
    Height = 100,
    Fill = "#4CAF50",
    Stroke = "#2E7D32",
    StrokeWidth = 2
};
graph.AddElement(rect);

// Set the graph on the canvas
canvas.Graph = graph;

// The shape should render automatically!
```

---

## File Organization Rules

### Prevent God-Files

- No file > 500 lines (split into partials or separate classes)
- One concept per file
- Group by domain, not by technical role

### Folder Structure

```
Elements/           ← Domain: what things ARE
  Nodes/           ← Subdomain: node-specific
  Edges/           ← Subdomain: edge-specific
  Shapes/          ← Subdomain: shape-specific

Rendering/         ← Domain: how things LOOK
  Nodes/           ← Subdomain: node rendering
  Edges/           ← Subdomain: edge rendering
  Shapes/          ← Subdomain: shape rendering
```

### Namespace Conventions

```csharp
FlowGraph.Core.Elements           // Base element types
FlowGraph.Core.Elements.Nodes     // Node-specific
FlowGraph.Core.Elements.Edges     // Edge-specific
FlowGraph.Core.Elements.Shapes    // Shape-specific

FlowGraph.Avalonia.Rendering.Nodes   // Node rendering
FlowGraph.Avalonia.Rendering.Edges   // Edge rendering
FlowGraph.Avalonia.Rendering.Shapes  // Shape rendering
```

---

## Logging Integration

Use `FlowGraphLogger` throughout:

```csharp
using FlowGraph.Core.Diagnostics;

// In rendering code
FlowGraphLogger.Debug(LogCategory.Rendering, "RenderElements called", "FlowCanvas");

// In element operations
FlowGraphLogger.Debug(LogCategory.Nodes, $"Node {node.Id} added", "Graph.AddNode");

// For performance tracking
using (FlowGraphLogger.BeginScope("RenderAll"))
{
    // ... rendering code
}
```

---

## Backward Compatibility Strategy

### Preserved APIs

- `Graph.Nodes` / `Graph.Edges` (now read-only `IReadOnlyList<T>` views)
- `Graph.AddNode()` / `Graph.RemoveNode()` / `Graph.AddEdge()` / `Graph.RemoveEdge()`
- All Node/Edge properties
- All existing renderers (updated to use unscaled dimensions)

### Breaking Changes (Jan 14, 2026)

1. **Graph mutation API changed**:

   - `graph.Nodes.Add()` → `graph.AddNode()`
   - `graph.Edges.Add()` → `graph.AddEdge()`
   - `graph.Nodes.Remove()` → `graph.Elements.Remove()`
   - Collection events moved to `graph.NodesChanged` / `graph.EdgesChanged`

2. **Transform-based rendering**:

   - `RenderContext.Scale` now always returns `1.0`
   - Use `RenderContext.ViewportZoom` for actual zoom level
   - All visual dimensions are logical (unscaled)

3. **Removed**:
   - `BulkObservableCollection<T>` - no longer needed
   - Bidirectional sync between Nodes/Edges and Elements

### Migration for Consumers

```csharp
// BEFORE (obsolete)
graph.Nodes.Add(node);
graph.Edges.Add(edge);
graph.Nodes.CollectionChanged += OnNodesChanged;

// AFTER (new API)
graph.AddNode(node);
graph.AddEdge(edge);
graph.NodesChanged += OnNodesChanged;

// For removals:
graph.Elements.Remove(node);  // or graph.RemoveNode(node.Id)
graph.Elements.Remove(edge);  // or graph.RemoveEdge(edge.Id)

// For custom renderers - use unscaled dimensions:
var width = node.Width ?? settings.NodeWidth;  // NOT * scale
var height = node.Height ?? settings.NodeHeight;
```

---

## Testing Strategy

1. **After each phase:** Run `dotnet build` on all projects
2. **After Phase 3:** Run `FlowGraph.Core.Tests`
3. **After Phase 4:** Run `FlowGraph.Pro.Tests`
4. **After Phase 5:** Run demo apps manually

---

## Success Criteria

1. ✅ All projects build without errors
2. ✅ All existing tests pass
3. ✅ Demo apps work (including sequence diagram)
4. ✅ New shape elements can be added to canvas
5. ✅ Public coordinate API available
6. ✅ No god-files (all files < 500 lines, well-organized)

---

## Quick Commands

```powershell
# Build community
cd "c:\Users\Takeshi\Documents\Development\DotNet\FlowGraph"
dotnet build

# Build Pro
cd "c:\Users\Takeshi\Documents\Development\GitHub\FlowGraph.Pro"
dotnet build

# Run tests
dotnet test FlowGraph.Core.Tests
dotnet test FlowGraph.Pro.Tests

# Run demo
dotnet run --project FlowGraph.Pro.Demo
```

---

## Questions to Answer During Implementation

1. Should `CanvasElement.Position` be a `Point` or separate `X`/`Y` properties?

   - **Decision:** Use `Point` for cleaner API, matches existing patterns

2. Should shapes support selection/interaction?

   - **Decision:** Yes, via base `CanvasElement` properties

3. How to handle Node's existing Definition/State pattern?

   - **Decision:** Keep it, Position etc. delegate to State, CanvasElement provides interface

4. Z-Index defaults?
   - **Decision:** Backgrounds=0, Shapes=100, Edges=200, Nodes=300

---

_Ready to implement. Start with Phase 1: Create `FlowGraph.Core/Elements/ICanvasElement.cs`_
