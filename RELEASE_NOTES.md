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
