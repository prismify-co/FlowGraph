# FlowGraph Canvas-First Refactor - Implementation TODO

> **Date:** January 9, 2026  
> **Target:** Complete in ~4 hours  
> **Breaking Change:** Yes (v0.4.0)

---

## Phase 1: Core Abstractions (FlowGraph.Core) - 1.5 hours

### 1.1 Create Element Hierarchy

- [x] Create `Elements/` folder
- [x] Create `Elements/ICanvasElement.cs` - base interface
- [x] Create `Elements/CanvasElement.cs` - base class with INPC
- [x] Create `Elements/ElementCollection.cs` - observable collection
- [x] Create `Elements/Rect.cs` - bounds structure

### 1.2 Update Node (PRAGMATIC: Implement interface, don't move)

- [x] Create `Elements/Nodes/` folder (placeholder for future)
- [x] Update `Node.cs` to implement `ICanvasElement`
- [x] Add `IsVisible`, `ZIndex`, `GetBounds()` to Node

### 1.3 Update Edge (PRAGMATIC: Implement interface, don't move)

- [x] Create `Elements/Edges/` folder (placeholder for future)
- [x] Update `Edge.cs` to implement `ICanvasElement`
- [x] Add explicit interface implementations for Position, Width, Height
- [x] Add `IsVisible`, `ZIndex`, `GetBounds()` to Edge

### 1.4 Create Shape Elements

- [x] Create `Elements/Shapes/` folder
- [x] Create `Elements/Shapes/ShapeElement.cs` - base for shapes
- [x] Create `Elements/Shapes/LineElement.cs` - standalone line
- [x] Create `Elements/Shapes/RectangleElement.cs` - rectangle/region
- [x] Create `Elements/Shapes/TextElement.cs` - text annotation
- [x] Create `Elements/Shapes/EllipseElement.cs` - ellipse/circle

### 1.5 Update Graph

- [x] Add `ElementCollection Elements` property
- [x] Keep `Nodes` and `Edges` as computed properties (backward compat with [Obsolete])
- [x] Add generic `AddElement()` / `RemoveElement()` methods
- [x] Add `AddElements()` for bulk operations
- [x] Update internal logic to sync ElementCollection with legacy collections

### 1.6 Update Dependent Code

- [ ] Update `Commands/` - remove obsolete warnings (optional, low priority)
- [ ] Update `DataFlow/` - GraphExecutor (optional, low priority)
- [ ] Update `Serialization/` - add Shape serialization support
- [ ] Add re-exports in root namespace if needed

---

## Phase 2: Rendering (FlowGraph.Avalonia) - 1.5 hours

### 2.1 Create Element Renderer Infrastructure

- [ ] Create `Rendering/Elements/` folder
- [ ] Create `Rendering/Elements/IElementRenderer.cs` - base interface
- [ ] Create `Rendering/Elements/ElementRenderContext.cs` - shared context
- [ ] Create `Rendering/Elements/ElementVisualManager.cs` - orchestrates rendering

### 2.2 Update Node Rendering (DEFERRED - works as-is)

- [ ] Rename `Rendering/NodeRenderers/` to `Rendering/Nodes/`
- [ ] Update `INodeRenderer` to extend `IElementRenderer`
- [ ] Update `NodeRendererRegistry`
- [ ] Update `NodeVisualManager` to implement element pattern
- [ ] Update all node renderer implementations

### 2.3 Update Edge Rendering (DEFERRED - works as-is)

- [ ] Rename `Rendering/EdgeRenderers/` to `Rendering/Edges/`
- [ ] Update `IEdgeRenderer` to extend `IElementRenderer`
- [ ] Update `EdgeRendererRegistry`
- [ ] Update `EdgeVisualManager` to implement element pattern
- [ ] Update all edge renderer implementations

### 2.4 Create Shape Rendering

- [x] Create `Rendering/ShapeRenderers/` folder
- [x] Create `Rendering/ShapeRenderers/IShapeRenderer.cs`
- [x] Create `Rendering/ShapeRenderers/ShapeRenderContext.cs`
- [x] Create `Rendering/ShapeRenderers/ShapeRendererRegistry.cs`
- [x] Create `Rendering/ShapeRenderers/ShapeVisualManager.cs`
- [x] Create `Rendering/ShapeRenderers/RectangleRenderer.cs`
- [x] Create `Rendering/ShapeRenderers/LineRenderer.cs`
- [x] Create `Rendering/ShapeRenderers/TextRenderer.cs`
- [x] Create `Rendering/ShapeRenderers/EllipseRenderer.cs`

### 2.5 Update GraphRenderer (COMPLETED)

- [x] Add RenderShapes() method to FlowCanvas.Rendering.cs
- [x] Integrate shape rendering into RenderAll() pipeline
- [x] Use ShapeVisualManager for shape visual lifecycle
- [x] Support shapes through Graph.Elements collection
- [x] Maintain backward compat for existing Node/Edge code paths

### 2.6 Add Public Coordinate API (COMPLETED)

- [x] Add `FlowCanvas.CanvasToScreen(x, y)` method
- [x] Add `FlowCanvas.CanvasToScreen(Point)` method
- [x] Add `FlowCanvas.ScreenToCanvas(x, y)` method
- [x] Add `FlowCanvas.ScreenToCanvas(Point)` method
- [x] Note: `FlowCanvas.CurrentZoom` already exists
- [x] Add `FlowCanvas.Offset` property
- [x] Add `FlowCanvas.VisibleBounds` property

### 2.7 Update FlowCanvas.Rendering.cs (COMPLETED)

- [x] Update `RenderAll()` to use element-based flow
- [x] Renamed `RenderGraph()` → `RenderElements()` - COMPLETED
- [x] Ensure backgrounds still work
- [x] Add FlowGraphLogger calls throughout

### 2.8 Update Using Statements (COMPLETED)

- [x] Update all files in FlowGraph.Avalonia to use Elements.Nodes/Elements.Edges
- [x] Update Input states (IdleState, DraggingState, ConnectingState, ReconnectingState, BoxSelectingState)
- [x] Update Animation code (FlowCanvas.Animation.cs)
- [x] Update Clipboard/Selection managers
- [x] Update DirectGraphRenderer, EdgeVisualManager, GraphRenderModel
- [x] Update GroupManager, GroupProxyManager
- [x] Update FlowMinimap, FlowCanvasContextMenu, FlowDiagnostics
- [x] Preserved CollectionChanged subscriptions (intentional - needs observable collection)

---

## Phase 3: Shape Elements & Renderers - 0.5 hours (COMPLETED)

### 3.1 Implement Shape Renderers

- [x] Implement `LineRenderer` - draws Line with start/end
- [x] Implement `RectangleRenderer` - draws Rectangle with fill/stroke
- [x] Implement `TextRenderer` - draws text at position
- [x] Implement `EllipseRenderer` - draws ellipse/circle

### 3.2 Register Shape Renderers

- [x] Auto-register in ShapeRendererRegistry constructor
- [x] Expose via FlowCanvas.ShapeRenderers property

---

## Phase 4: Pro & Demo Updates - 0.5 hours

### 4.1 Update FlowGraph.Pro

- [ ] Update package references
- [ ] Update all `using` statements
- [ ] Update layout algorithms for new namespaces
- [ ] Update NodeSearch, SelectionHelpers
- [ ] Update sequence diagram to use proper elements (not background hack)

### 4.2 Update FlowGraph.Pro.Avalonia

- [ ] Update custom renderers
- [ ] Update LifelineBackgroundRenderer → consider LifelineElement
- [ ] Update MessageBoxNodeRenderer

### 4.3 Update FlowGraph.Pro.Demo

- [ ] Verify all use cases work
- [ ] Update SequenceDiagramUseCase
- [ ] Test pan/zoom with new architecture

### 4.4 Update FlowGraph.Demo

- [ ] Verify basic demo works
- [ ] Test all node types

---

## Phase 5: Testing & Documentation - ongoing

### 5.1 Tests

- [ ] Run all FlowGraph.Core.Tests
- [ ] Run all FlowGraph.Pro.Tests
- [ ] Fix any broken tests
- [ ] Add tests for new element types

### 5.2 Documentation

- [ ] Update XML docs on all public APIs
- [ ] Update ARCHITECTURE_REFACTOR.md with final state
- [ ] Update ROADMAP.md
- [ ] Update README.md if needed

---

## Progress Tracker

| Phase                             | Status         | Notes                                  |
| --------------------------------- | -------------- | -------------------------------------- |
| 1.1 Element Hierarchy             | ✅ Complete    | ICanvasElement, CanvasElement, Rect    |
| 1.2 Update Node                   | ✅ Complete    | Implements ICanvasElement              |
| 1.3 Update Edge                   | ✅ Complete    | Implements ICanvasElement              |
| 1.4 Shape Elements                | ✅ Complete    | Rectangle, Line, Text, Ellipse         |
| 1.5 Update Graph                  | ✅ Complete    | ElementCollection with backward compat |
| 1.6 Update Dependent Code         | ⏸️ Deferred    | Low priority, warnings are acceptable  |
| 2.1 Element Renderer Infra        | ⏸️ Deferred    | Not needed for MVP                     |
| 2.2 Update Node Rendering         | ⏸️ Deferred    | Working as-is                          |
| 2.3 Update Edge Rendering         | ⏸️ Deferred    | Working as-is                          |
| 2.4 Create Shape Rendering        | ✅ Complete    | All renderers implemented              |
| 2.5 Update GraphRenderer          | ✅ Complete    | RenderShapes() integrated              |
| 2.6 Public Coordinate API         | ✅ Complete    | CanvasToScreen, ScreenToCanvas added   |
| 2.7 Update FlowCanvas.Rendering   | ✅ Complete    | RenderGraph → RenderElements           |
| 2.8 Update Using Statements       | ✅ Complete    | All files use Elements.Nodes/Edges     |
| 3.1 Implement Shape Renderers     | ✅ Complete    | Rectangle, Line, Text, Ellipse         |
| 3.2 Register Shape Renderers      | ✅ Complete    | Auto-registered, exposed via property  |
| 4.1 Update FlowGraph.Pro          | ⬜ Not Started |                                        |
| 4.2 Update FlowGraph.Pro.Avalonia | ⬜ Not Started |                                        |
| 4.3 Update FlowGraph.Pro.Demo     | ⬜ Not Started |                                        |
| 4.4 Update FlowGraph.Demo         | ⬜ Not Started |                                        |
| 5.1 Tests                         | ✅ Pass        | 593 tests (360 + 233)                  |
| 5.2 Documentation                 | 🔄 In Progress |                                        |

---

## Quick Reference: Namespace Changes

```csharp
// Old → New
FlowGraph.Core.Node → FlowGraph.Core.Elements.Nodes.Node
FlowGraph.Core.Edge → FlowGraph.Core.Elements.Edges.Edge
FlowGraph.Core.Port → FlowGraph.Core.Elements.Nodes.Port
FlowGraph.Core.Models.NodeDefinition → FlowGraph.Core.Elements.Nodes.NodeDefinition
FlowGraph.Core.Models.EdgeDefinition → FlowGraph.Core.Elements.Edges.EdgeDefinition

// Re-exports for backward compat (in FlowGraph.Core namespace)
using Node = FlowGraph.Core.Elements.Nodes.Node;
using Edge = FlowGraph.Core.Elements.Edges.Edge;
```

---

_Last updated: January 9, 2026_
