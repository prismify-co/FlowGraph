# FlowGraph Architecture Refactor: Canvas-First Design

> **Date:** January 9, 2026  
> **Status:** Planning Complete - Ready for Implementation  
> **Breaking Change:** Yes - Major version bump required (v0.4.0)

---

## Executive Summary

FlowGraph is being refactored from a **graph-centric** architecture to a **canvas-first** architecture. The coordinate system (grid) becomes the foundation, with graph nodes/edges being just one type of content that can be rendered.

**Before:** `FlowCanvas` renders a `Graph` (nodes + edges only)  
**After:** `FlowCanvas` renders `CanvasElement` instances (nodes, edges, swimlanes, lifelines, annotations, etc.)

---

## Mental Model Change

### Current (Graph-Centric)

```
FlowCanvas
  └── Graph
        ├── Nodes (everything must be a Node)
        ├── Edges (everything must be an Edge)
        └── Grid (decoration)
```

### Target (Canvas-First)

```
FlowCanvas (Coordinate System)
  ├── Grid Pattern (dots/lines/crosses/none)
  ├── Layers (z-ordered collections)
  │   ├── BackgroundLayer (swimlanes, regions)
  │   ├── ConnectionLayer (edges, arrows, lifelines)
  │   └── ContentLayer (nodes, annotations, shapes)
  └── ViewportState (pan, zoom, transforms)
```

---

## File Structure Changes

### FlowGraph.Core - New Structure

```
FlowGraph.Core/
├── Elements/                         # NEW - Element hierarchy
│   ├── CanvasElement.cs             # Base class for all positioned content
│   ├── ICanvasElement.cs            # Interface for element contract
│   ├── ElementCollection.cs         # Observable collection of elements
│   ├── Nodes/                       # Node-specific classes
│   │   ├── Node.cs                  # Refactored - extends CanvasElement
│   │   ├── NodeDefinition.cs        # Moved from Models/
│   │   ├── NodeState.cs             # Moved from Models/
│   │   └── Port.cs                  # Moved from root
│   ├── Edges/                       # Edge-specific classes
│   │   ├── Edge.cs                  # Refactored - extends CanvasElement
│   │   ├── EdgeDefinition.cs        # Moved from Models/
│   │   └── EdgeState.cs             # Moved from Models/
│   └── Shapes/                      # Non-graph elements
│       ├── ShapeElement.cs          # Base for shapes
│       ├── LineElement.cs           # Standalone lines
│       ├── RectangleElement.cs      # Rectangle regions
│       └── TextElement.cs           # Standalone text/annotations
├── Graph.cs                         # Simplified - wraps ElementCollection
├── GraphDefaults.cs                 # Keep as-is
├── Point.cs                         # Keep as-is
├── Commands/                        # Keep as-is (update for elements)
├── DataFlow/                        # Keep as-is
├── Diagnostics/                     # Keep as-is (our new logger!)
├── Models/                          # Will be emptied, then removed
├── Routing/                         # Keep as-is
└── Serialization/                   # Update for elements
```

### FlowGraph.Avalonia - New Structure

```
FlowGraph.Avalonia/
├── Rendering/
│   ├── Elements/                    # NEW - Element rendering
│   │   ├── IElementRenderer.cs      # Base renderer interface
│   │   ├── ElementRendererRegistry.cs
│   │   ├── ElementVisualManager.cs  # Manages all element visuals
│   │   └── ElementRenderContext.cs  # Context for element rendering
│   ├── Nodes/                       # Renamed from NodeRenderers/
│   │   ├── INodeRenderer.cs         # Extends IElementRenderer
│   │   ├── NodeRendererRegistry.cs
│   │   └── ... (existing renderers)
│   ├── Edges/                       # Renamed from EdgeRenderers/
│   │   ├── IEdgeRenderer.cs         # Extends IElementRenderer
│   │   ├── EdgeRendererRegistry.cs
│   │   └── ... (existing renderers)
│   ├── Shapes/                      # NEW - Shape renderers
│   │   ├── IShapeRenderer.cs
│   │   ├── ShapeRendererRegistry.cs
│   │   ├── LineRenderer.cs
│   │   ├── RectangleRenderer.cs
│   │   └── TextRenderer.cs
│   ├── Backgrounds/                 # Renamed from BackgroundRenderers/
│   │   └── ... (keep existing)
│   ├── GraphRenderer.cs             # Refactored - renders elements
│   ├── RenderContext.cs             # Add public coordinate API
│   └── ...
├── FlowCanvas.axaml.cs              # Add public coordinate API
├── FlowCanvas.Rendering.cs          # Update for element rendering
└── ...
```

---

## Core Abstractions

### 1. ICanvasElement Interface

```csharp
namespace FlowGraph.Core.Elements;

public interface ICanvasElement : INotifyPropertyChanged
{
    string Id { get; }
    string Type { get; }
    Point Position { get; set; }
    double? Width { get; set; }
    double? Height { get; set; }
    bool IsSelected { get; set; }
    bool IsVisible { get; set; }
    int ZIndex { get; set; }

    // For hit-testing and bounds calculation
    Rect GetBounds();
}
```

### 2. CanvasElement Base Class

```csharp
namespace FlowGraph.Core.Elements;

public abstract class CanvasElement : ICanvasElement
{
    public string Id { get; init; }
    public abstract string Type { get; }

    public Point Position { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool IsSelected { get; set; }
    public bool IsVisible { get; set; } = true;
    public int ZIndex { get; set; }

    public virtual Rect GetBounds() => new(Position.X, Position.Y, Width ?? 0, Height ?? 0);

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

### 3. Node Refactored

```csharp
namespace FlowGraph.Core.Elements.Nodes;

public class Node : CanvasElement
{
    public override string Type => Definition.Type;

    // Existing node-specific properties
    public NodeDefinition Definition { get; set; }
    public INodeState State { get; }
    public IReadOnlyList<Port> Inputs { get; }
    public IReadOnlyList<Port> Outputs { get; }

    // Graph-specific (optional)
    public string? ParentGroupId { get; set; }
    public bool IsCollapsed { get; set; }
    public List<string>? ChildNodeIds { get; set; }
}
```

### 4. Edge Refactored

```csharp
namespace FlowGraph.Core.Elements.Edges;

public class Edge : CanvasElement
{
    public override string Type => Definition.Type;

    // Connection points (can be standalone or node-connected)
    public Point? SourcePoint { get; set; }
    public Point? TargetPoint { get; set; }

    // Node connection (optional - null for standalone connections)
    public string? SourceNodeId { get; set; }
    public string? SourcePortId { get; set; }
    public string? TargetNodeId { get; set; }
    public string? TargetPortId { get; set; }

    // Existing edge-specific properties
    public EdgeDefinition Definition { get; set; }
    public IEdgeState State { get; }
}
```

### 5. ElementCollection

```csharp
namespace FlowGraph.Core.Elements;

public class ElementCollection : BulkObservableCollection<ICanvasElement>
{
    // Typed accessors for convenience
    public IEnumerable<Node> Nodes => this.OfType<Node>();
    public IEnumerable<Edge> Edges => this.OfType<Edge>();
    public IEnumerable<T> OfElementType<T>() where T : ICanvasElement => this.OfType<T>();

    // Find by ID
    public ICanvasElement? FindById(string id);
    public T? FindById<T>(string id) where T : ICanvasElement;
}
```

### 6. Graph (Simplified Wrapper)

```csharp
namespace FlowGraph.Core;

public class Graph
{
    public ElementCollection Elements { get; } = new();

    // Convenience accessors (backward compatible)
    public IEnumerable<Node> Nodes => Elements.Nodes;
    public IEnumerable<Edge> Edges => Elements.Edges;

    // Backward-compatible methods
    public void AddNode(Node node) => Elements.Add(node);
    public void AddEdge(Edge edge) => Elements.Add(edge);
    public void RemoveNode(Node node) => Elements.Remove(node);
    public void RemoveEdge(Edge edge) => Elements.Remove(edge);

    // New generic methods
    public void AddElement(ICanvasElement element) => Elements.Add(element);
    public void RemoveElement(ICanvasElement element) => Elements.Remove(element);
}
```

---

## Rendering Pipeline Changes

### Current Flow

```
RenderAll()
  ├── RenderGrid()           → GridCanvas
  ├── RenderCustomBackgrounds() → GridCanvas
  └── RenderGraph()          → MainCanvas
        ├── RenderGroupNodes()
        ├── RenderEdges()
        └── RenderRegularNodes()
```

### New Flow

```
RenderAll()
  ├── RenderGrid()           → GridCanvas
  ├── RenderBackgrounds()    → GridCanvas (IBackgroundRenderer[])
  └── RenderElements()       → MainCanvas
        ├── Elements sorted by ZIndex
        ├── For each element:
        │     └── ElementVisualManager.Render(element)
        │           └── Dispatches to appropriate renderer
```

---

## Public Coordinate API

Add to `FlowCanvas`:

```csharp
public partial class FlowCanvas
{
    // Public coordinate transforms
    public Point CanvasToScreen(double x, double y)
        => _viewport.CanvasToScreen(new Point(x, y));

    public Point CanvasToScreen(Point canvasPoint)
        => _viewport.CanvasToScreen(canvasPoint);

    public Point ScreenToCanvas(double x, double y)
        => _viewport.ScreenToCanvas(new Point(x, y));

    public Point ScreenToCanvas(Point screenPoint)
        => _viewport.ScreenToCanvas(screenPoint);

    // Viewport info
    public double Zoom => _viewport.Zoom;
    public Point Offset => new(_viewport.OffsetX, _viewport.OffsetY);
    public Rect VisibleBounds => _viewport.GetVisibleBounds(Bounds.Size);
}
```

---

## Migration Strategy

### Phase 1: Core Abstractions (FlowGraph.Core)

1. Create `Elements/` folder structure
2. Create `ICanvasElement` and `CanvasElement`
3. Refactor `Node` to extend `CanvasElement`
4. Refactor `Edge` to extend `CanvasElement`
5. Create `ElementCollection`
6. Update `Graph` to use `ElementCollection`
7. Move models to new locations
8. Update all `using` statements

### Phase 2: Rendering (FlowGraph.Avalonia)

1. Create `IElementRenderer` base interface
2. Refactor `INodeRenderer` to extend `IElementRenderer`
3. Refactor `IEdgeRenderer` to extend `IElementRenderer`
4. Create `ElementVisualManager`
5. Update `GraphRenderer` to use element-based rendering
6. Add public coordinate API to `FlowCanvas`
7. Update `FlowCanvas.Rendering.cs`

### Phase 3: Shape Elements

1. Create `ShapeElement`, `LineElement`, `RectangleElement`, `TextElement`
2. Create shape renderers
3. Add to registries

### Phase 4: Update Pro and Demo

1. Update FlowGraph.Pro to use new APIs
2. Refactor sequence diagram to use elements (not background hack)
3. Update all demos
4. Verify all tests pass

---

## Backward Compatibility

### Preserved

- `Graph.Nodes` and `Graph.Edges` accessors
- `AddNode()`, `RemoveNode()`, `AddEdge()`, `RemoveEdge()` methods
- Node/Edge property names and behavior
- All existing renderers (INodeRenderer, IEdgeRenderer)
- FlowCanvasSettings
- Commands (with updates)
- Logger (FlowGraphLogger)

### Breaking

- `Node` and `Edge` namespaces change
- `Node` and `Edge` now have base class (`CanvasElement`)
- Models/ folder reorganized
- Some internal APIs changed

### Required Updates for Consumers

```csharp
// Old
using FlowGraph.Core;
var node = new Node { ... };

// New (add using)
using FlowGraph.Core;
using FlowGraph.Core.Elements.Nodes;
var node = new Node { ... };
```

---

## Success Criteria

1. ✅ All existing demos work
2. ✅ All existing tests pass
3. ✅ Sequence diagram uses proper elements (not background hack)
4. ✅ New shape elements can be rendered
5. ✅ Public coordinate API available
6. ✅ FlowGraphLogger integrated throughout
7. ✅ No god-files (well-organized structure)
8. ✅ XML documentation on public APIs

---

## Estimated Effort

| Phase                       | Estimated Time |
| --------------------------- | -------------- |
| Phase 1: Core Abstractions  | 1.5 hours      |
| Phase 2: Rendering          | 1.5 hours      |
| Phase 3: Shape Elements     | 0.5 hours      |
| Phase 4: Pro & Demo Updates | 0.5 hours      |
| **Total**                   | **4 hours**    |

---

## Files to Create (New)

1. `FlowGraph.Core/Elements/ICanvasElement.cs`
2. `FlowGraph.Core/Elements/CanvasElement.cs`
3. `FlowGraph.Core/Elements/ElementCollection.cs`
4. `FlowGraph.Core/Elements/Nodes/` (folder)
5. `FlowGraph.Core/Elements/Edges/` (folder)
6. `FlowGraph.Core/Elements/Shapes/ShapeElement.cs`
7. `FlowGraph.Core/Elements/Shapes/LineElement.cs`
8. `FlowGraph.Core/Elements/Shapes/RectangleElement.cs`
9. `FlowGraph.Core/Elements/Shapes/TextElement.cs`
10. `FlowGraph.Avalonia/Rendering/Elements/IElementRenderer.cs`
11. `FlowGraph.Avalonia/Rendering/Elements/ElementRenderContext.cs`
12. `FlowGraph.Avalonia/Rendering/Shapes/` (folder + renderers)

## Files to Modify

1. `FlowGraph.Core/Node.cs` → move to `Elements/Nodes/`
2. `FlowGraph.Core/Edge.cs` → move to `Elements/Edges/`
3. `FlowGraph.Core/Port.cs` → move to `Elements/Nodes/`
4. `FlowGraph.Core/Graph.cs` → add ElementCollection
5. `FlowGraph.Core/Models/*` → move to Elements/
6. `FlowGraph.Avalonia/FlowCanvas.axaml.cs` → add public coords
7. `FlowGraph.Avalonia/FlowCanvas.Rendering.cs` → element-based
8. `FlowGraph.Avalonia/Rendering/GraphRenderer.cs` → element-based
9. All `using` statements throughout

---

_This document serves as the technical specification for the FlowGraph canvas-first architecture refactor._
