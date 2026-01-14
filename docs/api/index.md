# API Reference

This section contains the auto-generated API documentation for FlowGraph.

## Namespaces

### FlowGraph.Core

Core data models and commands (no UI dependencies).

- [Graph](xref:FlowGraph.Core.Graph) - Main graph container
- [Node](xref:FlowGraph.Core.Node) - Node data model
- [Edge](xref:FlowGraph.Core.Edge) - Edge/connection data model
- [Port](xref:FlowGraph.Core.Port) - Port data model
- [Commands](xref:FlowGraph.Core.Commands) - Undo/redo command system
- [Serialization](xref:FlowGraph.Core.Serialization) - JSON save/load

### FlowGraph.Avalonia

Avalonia UI controls and rendering.

- [FlowCanvas](xref:FlowGraph.Avalonia.FlowCanvas) - Main canvas control
- [FlowCanvasSettings](xref:FlowGraph.Avalonia.FlowCanvasSettings) - Configuration options

### FlowGraph.Avalonia.Controls

UI components.

- [FlowBackground](xref:FlowGraph.Avalonia.Controls.FlowBackground) - Background patterns
- [FlowControls](xref:FlowGraph.Avalonia.Controls.FlowControls) - Zoom controls
- [FlowMinimap](xref:FlowGraph.Avalonia.Controls.FlowMinimap) - Overview minimap
- [FlowPanel](xref:FlowGraph.Avalonia.Controls.FlowPanel) - Positioned panels
- [NodeToolbar](xref:FlowGraph.Avalonia.Controls.NodeToolbar) - Node action toolbar

### FlowGraph.Avalonia.Rendering

Rendering infrastructure.

- [INodeRenderer](xref:FlowGraph.Avalonia.Rendering.NodeRenderers.INodeRenderer) - Node renderer interface
- [DefaultNodeRenderer](xref:FlowGraph.Avalonia.Rendering.NodeRenderers.DefaultNodeRenderer) - Base renderer
- [ThemeResources](xref:FlowGraph.Avalonia.Rendering.ThemeResources) - Theme access

### FlowGraph.Avalonia.Animation

Animation system.

- [AnimationManager](xref:FlowGraph.Avalonia.Animation.AnimationManager) - Animation orchestration
- [IAnimation](xref:FlowGraph.Avalonia.Animation.IAnimation) - Animation interface

### FlowGraph.Avalonia.Validation

Connection validation.

- [IConnectionValidator](xref:FlowGraph.Avalonia.Validation.IConnectionValidator) - Validator interface
- [DefaultConnectionValidator](xref:FlowGraph.Avalonia.Validation.DefaultConnectionValidator) - Built-in validator

## Additional Documentation

For tutorials and guides, visit [flowgraph.prismify.co](https://flowgraph.prismify.co).
