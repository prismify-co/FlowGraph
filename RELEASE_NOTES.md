## FlowGraph v0.4.1

### Bug Fixes

- **Fixed Background Renderer Coordinate Transformation** - Custom background renderers now correctly align with nodes when the viewport is panned or zoomed
  - Fixed `CanvasToScreen` formula in `BackgroundRenderContext`: was `(x - offset) * zoom`, now correctly `x * zoom + offset`
  - Fixed `ScreenToCanvas` formula: was `x / zoom + offset`, now correctly `(x - offset) / zoom`
  - This resolves issues where sequence diagram lifelines and other background elements appeared misaligned with their associated nodes

---

## FlowGraph v0.4.0

### ⚠️ Breaking Changes

- **Canvas-First Architecture** - The Graph model now uses a unified `Elements` collection as the primary data source
  - **New API**: Use `graph.Elements.Nodes` and `graph.Elements.Edges` for accessing nodes and edges
  - **Deprecated**: `graph.Nodes` and `graph.Edges` properties are now marked `[Obsolete]` but remain functional for backward compatibility
  - **Migration**: Replace `graph.Nodes` with `graph.Elements.Nodes` and `graph.Edges` with `graph.Elements.Edges` in your code

### New Features

- **Unified Elements Collection** - Single source of truth for all canvas elements

  - `Graph.Elements` property provides access to all nodes, edges, and future element types
  - `Graph.Elements.Nodes` returns `IEnumerable<Node>` for type-safe node access
  - `Graph.Elements.Edges` returns `IEnumerable<Edge>` for type-safe edge access
  - Enables future extensibility for custom element types (annotations, shapes, etc.)

- **Improved Internal Architecture**
  - Cleaner separation between the unified elements model and legacy ObservableCollection-based APIs
  - Better support for custom renderers and element types
  - Simplified coordinate transformations with `RenderElements()` API

### Migration Guide

**Before (v0.3.x):**

```csharp
// Accessing nodes and edges
var nodes = graph.Nodes;
var edges = graph.Edges;
var node = graph.Nodes.FirstOrDefault(n => n.Id == "myNode");
```

**After (v0.4.0):**

```csharp
// Use Elements collection for new code
var nodes = graph.Elements.Nodes;
var edges = graph.Elements.Edges;
var node = graph.Elements.Nodes.FirstOrDefault(n => n.Id == "myNode");

// Legacy properties still work but show deprecation warnings
// graph.Nodes and graph.Edges are still available for backward compatibility
```

### Notes

- The deprecated `Nodes` and `Edges` properties will continue to work and fire `CollectionChanged` events
- Internal synchronization ensures both APIs stay in sync
- This change prepares the architecture for FlowGraph.Pro features and custom element types

---

## FlowGraph v0.3.6

### New Features

- **Custom Edge Renderers** - Full extensibility API for custom edge visuals

  - `IEdgeRenderer` interface for creating custom edge representations (sequence messages, swimlane flows, etc.)
  - `EdgeRendererRegistry` with wildcard pattern matching (e.g., `sequence-*` matches `sequence-message`)
  - `EdgeRenderContext` provides source/target nodes, coordinates, theme, and scale
  - `EdgeRenderResult` returns visible path, hit area, markers, labels, and additional visuals
  - Access via `canvas.EdgeRenderers.Register("my-edge-type", myRenderer)`

- **Custom Background Renderers** - Extensibility API for custom background layers
  - `IBackgroundRenderer` interface for rendering behind graph content (lifelines, swimlane lanes, etc.)
  - `BackgroundRendererRegistry` supports multiple concurrent renderers or single-renderer mode
  - `BackgroundRenderContext` provides viewport bounds, pan/zoom state, and coordinate transforms
  - Callbacks for `OnGraphChanged` and `OnViewportChanged` for efficient updates
  - Access via `canvas.BackgroundRenderers.Add(myRenderer)` or `canvas.BackgroundRenderers.SetSingle(myRenderer)`

### API Additions

- `FlowCanvas.EdgeRenderers` property for registering custom edge types
- `FlowCanvas.BackgroundRenderers` property for registering custom backgrounds
- Custom renderers are fully integrated into the FlowCanvas render pipeline

---

## FlowGraph v0.3.5

### Bug Fixes

- **Fixed Node Selection Flakiness** - Visual tree hit testing now correctly walks up parent hierarchy to find nodes when clicking on nested child elements (labels, inner grids, etc.)
- **Fixed Settings Propagation** - `FlowCanvasSettings` changes now properly propagate to all rendering components at runtime:
  - Added `UpdateSettings()` methods to `RenderContext`, `GraphRenderModel`, `NodeVisualManager`, `GraphRenderer`, `ViewportState`, `GridRenderer`, `InputStateContext`, and `DirectGraphRenderer`
  - Settings changes via property binding now trigger full component updates
- **Fixed DirectGraphRenderer Settings Sync** - Direct rendering mode now correctly updates its internal `GraphRenderModel` when settings change, fixing spatial index/hit testing mismatches

### Improvements

- Enhanced debug logging for input hit testing diagnostics

---

## FlowGraph v0.3.3

### New Features

- **Custom Port Renderers** - Full extensibility API for custom port visuals
  - `IPortRenderer` interface for creating custom port shapes and styles
  - `PortRendererRegistry` to map port types to custom renderers
  - `DefaultPortRenderer` maintains backward compatibility with standard circular ports
  - `PortRenderContext` provides port metadata, position, and theme resources
  - `PortVisualState` enum for hover, connected, and dragging states
  - Access via `canvas.GraphRenderer.PortRenderers.Register("MyType", myRenderer)`

### Documentation

- Added Custom Port Renderers section to README features
- New custom-ports.md guide in documentation

---

## FlowGraph v0.3.2

### New Features

- **LabelInfo for Edge Labels** - Enhanced edge label positioning with anchor points and offsets
  - `LabelAnchor.Start` - Position label at ~25% along edge (near source)
  - `LabelAnchor.Center` - Position label at midpoint (default)
  - `LabelAnchor.End` - Position label at ~75% along edge (near target)
  - Custom `OffsetX` and `OffsetY` for fine-tuning label position
  - Fluent API: `edge.Definition.WithLabelInfo("Label", LabelAnchor.Start, offsetY: -10)`
  - Full JSON serialization support via `LabelInfoDto`

### Documentation

- Added Edge Label Positioning section to README
- Updated edge-routing.md with LabelInfo documentation
- Added tables documenting LabelAnchor positions and LabelInfo properties

---

## FlowGraph v0.3.1

### Documentation Updates

- **Comprehensive API Documentation** - Updated README and docs to reflect all v0.3.0 API changes
- **Definition + State Pattern** - Documented new immutable Definition + mutable State architecture
- **Batch Loading Guide** - Added documentation for `BeginBatchLoad()`, `EndBatchLoad()`, and bulk operations
- **Performance Guide** - New comprehensive guide for optimizing large graphs (virtualization, simplified rendering, direct GPU rendering)
- **Enhanced Configuration** - Documented all new FlowCanvasSettings properties
- **Connection Validation** - Updated with new built-in validators and composite patterns
- **Events Documentation** - Updated event examples with correct property names and new events
- **Edge Routing Guide** - Updated with new routing API and configuration options

---

## FlowGraph v0.3.0

### API Improvements

- Definition + State pattern for cleaner separation of concerns
- `NodeDefinition` and `NodeState` for immutable/mutable properties
- `EdgeDefinition` and `EdgeState` for better edge management
- `PortDefinition` with new properties (`MaxConnections`, `IsRequired`)
- `BulkObservableCollection` with `AddRange()` for batch operations
- Improved connection validation with built-in validators
- Enhanced performance settings for large graphs (500+ nodes)

### Breaking Changes

- Node and Edge constructors now accept Definition and State parameters (backward-compatible pass-through properties maintained)
- `ValidationResult` renamed to `ConnectionValidationResult`
- `BoxSelectionMode` renamed to `SelectionMode`
- Event property names updated for consistency

---

## FlowGraph v0.2.2

### Improvements

- Fixed all compiler warnings (CS8601, CS8602, CS0108, CS0109)
- Improved null safety across codebase
- Renamed internal `FlowDirection` enum to `EdgeFlowDirection` to avoid conflict with `Visual.FlowDirection`

---

## FlowGraph v0.2.0

### New Features

- **FlowGraph.3D** - New package for 3D rendering using OpenGL via Silk.NET
- **Data Flow System** - Real-time data propagation between connected nodes
- **Interactive Node Renderers** - Color picker, radio buttons, sliders with state persistence
- **3D Output Node** - Demo showcasing data flow with 3D OpenGL visualization

### Improvements

- State persistence across visual tree rebuilds (pan/zoom operations)
- Static registry pattern for control reuse

---

## FlowGraph v0.1.0

The first public release of FlowGraph - a node-based graph editor for .NET and Avalonia.

### Features

- Pan, zoom, and viewport controls
- Draggable, resizable nodes with custom renderers
- Multiple edge types (Bezier, Straight, Step, SmoothStep)
- Connection validation
- Undo/redo with full command history
- Copy, cut, paste, duplicate
- Node grouping with collapse/expand
- Minimap and controls
- Animations
- Theming support (light/dark)
- JSON serialization

### Installation

```bash
dotnet add package FlowGraph.Avalonia
```

### Documentation

- [GitHub Repository](https://github.com/prismify-co/FlowGraph)
- [API Documentation](https://prismify-co.github.io/FlowGraph)

### Requirements

- .NET 9.0+
- Avalonia 11.2+

---

MIT License - Prismify LLC
