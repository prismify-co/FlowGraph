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
