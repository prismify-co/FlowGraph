# FlowGraph Coordinate System Architecture

## Executive Summary

This document describes the coordinate system architecture for FlowGraph, including completed fixes for Direct Rendering mode and the new type-safe coordinate infrastructure.

**Key principle:** All interaction logic should work in a single coordinate space (Canvas), with transforms applied only at rendering boundaries.

## Implementation Status

### Phase 1: Direct Rendering Fixes ✅ COMPLETE

All visual elements that need dual-mode support have been fixed:

| Component           | Visual Element             | Status                    |
| ------------------- | -------------------------- | ------------------------- |
| ConnectingState     | Temp connection line       | ✅ Fixed (commit 2ea4ec9) |
| BoxSelectingState   | Selection rectangle        | ✅ Fixed (commit 8b0a63a) |
| ReconnectingState   | Temp reconnection line     | ✅ Fixed (commit f5b1b1a) |
| ResizeHandleManager | Resize handles             | ✅ Fixed (commit 06e0387) |
| DraggingState       | None (modifies positions)  | ✅ OK                     |
| ResizingState       | None (modifies dimensions) | ✅ OK                     |
| PanningState        | None (modifies viewport)   | ✅ OK                     |

### Phase 2: Type-Safe Coordinate Infrastructure ✅ COMPLETE

New type-safe coordinate types have been added to prevent coordinate space confusion at compile time (commit cacb718):

**FlowGraph.Core/Coordinates:**

- `CanvasPoint`, `CanvasVector`, `CanvasRect` - Canvas/graph space coordinates
- `ViewportPoint`, `ViewportVector`, `ViewportRect` - Screen/viewport space coordinates
- `ITypedCoordinateTransformer` - Type-safe coordinate conversion interface
- `TypedCoordinateTransformer` - Default implementation

**FlowGraph.Avalonia/Coordinates:**

- `AvaloniaCoordinateExtensions` - Convert between typed coords and Avalonia types

**FlowGraph.Avalonia/Input:**

- `IInputCoordinates` - Rendering-mode agnostic input coordinate interface

**FlowGraph.Avalonia/Rendering:**

- `IRenderTarget` - Mode-agnostic temporary visual rendering interface

### Phase 3: Migration to Type-Safe Coordinates 🔲 NOT STARTED

Gradually migrate existing code to use the new type-safe coordinates. This is optional but recommended for long-term maintainability.

---

## Visual Element Audit (Direct Rendering Mode)

This section documents all visual elements that need dual-mode support and their current status.

### Status Summary

| Component           | Visual Element             | Current Container      | Current Coords | Status                              |
| ------------------- | -------------------------- | ---------------------- | -------------- | ----------------------------------- |
| ConnectingState     | Temp connection line       | Mode-aware             | Mode-aware     | ✅ Fixed (commit 2ea4ec9)           |
| BoxSelectingState   | Selection rectangle        | Mode-aware             | Mode-aware     | ✅ Fixed (commit 8b0a63a)           |
| ReconnectingState   | Temp reconnection line     | Mode-aware             | Mode-aware     | ✅ Fixed (commit f5b1b1a)           |
| ResizeHandleManager | Resize handles             | Skipped in Direct mode | N/A            | ✅ Fixed (commit 06e0387)           |
| ShapeVisualManager  | Shape overlays             | MainCanvas             | Canvas         | 🟡 Low Priority (shapes are static) |
| DraggingState       | None (modifies positions)  | N/A                    | N/A            | ✅ OK                               |
| ResizingState       | None (modifies dimensions) | N/A                    | N/A            | ✅ OK                               |
| PanningState        | None (modifies viewport)   | N/A                    | N/A            | ✅ OK                               |

### Completed Fixes

#### ✅ ConnectingState (commit 2ea4ec9)

**Solution:** Temp line added to RootPanel (viewport coords) in Direct mode, MainCanvas (canvas coords) in Visual Tree mode.

#### ✅ BoxSelectingState (commit 8b0a63a)

**Solution:** Selection box added to RootPanel (viewport coords) in Direct mode, MainCanvas (canvas coords) in Visual Tree mode.

#### ✅ ReconnectingState (commit f5b1b1a)

**Solution:** Uses temp line overlay (like ConnectingState) instead of modifying edge visual. Edge visual hidden during reconnection.

#### ✅ ResizeHandleManager (commit 06e0387)

**Solution:** Visual tree resize handles skipped in Direct Rendering mode. DirectGraphRenderer draws its own handles via DrawResizeHandles and provides hit testing via HitTestResizeHandle.

### Remaining Issue

#### 🟡 Low Priority: ShapeVisualManager

**Location:** [ShapeVisualManager.cs](../FlowGraph.Avalonia/Rendering/ShapeVisualManager.cs)

Shape overlays are added to MainCanvas. In Direct Rendering mode, these may conflict with directly-rendered shapes. However, shapes are typically static background elements and the visual conflict is minimal.
if (visiblePath == null) return; // This returns null in Direct Rendering!

---

## Current State Analysis

### 1. ViewportState.cs - Transform Management

**Location:** [ViewportState.cs](../FlowGraph.Avalonia/ViewportState.cs)

The `ViewportState` class is the central authority for coordinate transforms:

```csharp
// Current implementation
public Point ViewportToCanvas(Point viewportPoint)
{
    return new Point(
        (viewportPoint.X - OffsetX) / Zoom,
        (viewportPoint.Y - OffsetY) / Zoom
    );
}

public Point CanvasToViewport(Point canvasPoint)
{
    return new Point(
        canvasPoint.X * Zoom + OffsetX,
        canvasPoint.Y * Zoom + OffsetY
    );
}
```

**Issues:**

- Transforms are duplicated in multiple places (ViewportState, DirectGraphRenderer.CoordinateTransforms.cs)
- No type safety - easy to pass viewport coords where canvas coords expected

### 2. InputStateContext.cs - Coordinate Methods Exposed

**Location:** [InputStateContext.cs](../FlowGraph.Avalonia/Input/InputStateContext.cs)

The context exposes coordinate helpers that delegate to ViewportState:

```csharp
public AvaloniaPoint ViewportToCanvas(AvaloniaPoint viewportPoint) => _viewport.ViewportToCanvas(viewportPoint);
public AvaloniaPoint CanvasToViewport(AvaloniaPoint canvasPoint) => _viewport.CanvasToViewport(canvasPoint);
```

**Issues:**

- Input states must decide which coordinate space to work in
- Different states use different approaches (some use `GetCanvasPosition`, some use `GetScreenPosition + ViewportToCanvas`)
- ConnectingState has complex mode-aware logic for temp line positioning

### 3. GraphRenderModel.cs - Geometry Calculations

**Location:** [GraphRenderModel.cs](../FlowGraph.Avalonia/Rendering/GraphRenderModel.cs)

All geometry calculations work in **Canvas coordinates**:

```csharp
public Rect GetNodeBounds(Node node)
{
    var width = GetNodeWidth(node);
    var height = GetNodeHeight(node);
    return new Rect(node.Position.X, node.Position.Y, width, height);
}

public AvaloniaPoint GetPortPosition(Node node, Port port, bool isOutput)
{
    var bounds = GetNodeBounds(node);
    // ... returns canvas coordinates
}
```

**This is correct!** The render model is the single source of truth for canvas-space geometry.

### 4. DirectGraphRenderer.cs - Transform Handling

**Location:** [DirectGraphRenderer.cs](../FlowGraph.Avalonia/Rendering/DirectGraphRenderer.cs)

The direct renderer performs its own transforms:

```csharp
// DirectGraphRenderer.CoordinateTransforms.cs
private AvaloniaPoint ScreenToCanvas(double screenX, double screenY)
{
    if (_viewport == null) return new AvaloniaPoint(screenX, screenY);
    return new AvaloniaPoint(
        (screenX - _viewport.OffsetX) / _viewport.Zoom,
        (screenY - _viewport.OffsetY) / _viewport.Zoom);
}

private AvaloniaPoint CanvasToScreen(AvaloniaPoint canvasPoint, double zoom, double offsetX, double offsetY)
{
    return new AvaloniaPoint(
        canvasPoint.X * zoom + offsetX,
        canvasPoint.Y * zoom + offsetY);
}
```

**Critical Issue:** The renderer has `LayerTransformMode.SelfTransformed` and must be placed in `RootPanel` (untransformed), while visual tree elements are in `MainCanvas` (transformed). This duality causes coordinate confusion in interaction code.

### 5. FlowCanvas.axaml - Visual Tree Transform

**Location:** [FlowCanvas.axaml](../FlowGraph.Avalonia/FlowCanvas.axaml)

```xml
<Panel Name="RootPanel" Background="{DynamicResource FlowCanvasBackground}">
  <Canvas Name="GridCanvas" ClipToBounds="False"/>
  <Canvas Name="MainCanvas" ClipToBounds="False">
    <Canvas.RenderTransform>
      <MatrixTransform/>  <!-- Applied by ViewportState.ApplyToTransforms -->
    </Canvas.RenderTransform>
  </Canvas>
</Panel>
```

**Hierarchy:**

- `RootPanel` - Untransformed container (viewport coordinates)
- `GridCanvas` - Grid rendering (needs own transform handling)
- `MainCanvas` - Visual tree nodes/edges (transformed via MatrixTransform)
- `DirectGraphRenderer` - Added to RootPanel when enabled (self-transformed)

---

## Root Cause Analysis

The fundamental problem is **coordinate space ambiguity**:

1. **Visual Tree Mode**: Elements in `MainCanvas` use canvas coordinates, the `MatrixTransform` handles viewport transform automatically
2. **Direct Rendering Mode**: Renderer is in `RootPanel`, must manually transform canvas→viewport for drawing
3. **Interaction Code**: Must work differently depending on mode, leading to bugs like the temp line positioning issue

### The ConnectingState Example

```csharp
// ConnectingState.cs - Mode-aware temp line positioning
if (context.DirectRenderer != null)
{
    // Direct mode: temp line in RootPanel (untransformed)
    // Must convert canvas coords to viewport coords
    startPoint = context.CanvasToViewport(startPointCanvas);
    endPoint = _endPointViewport; // Already in viewport coords
}
else
{
    // Visual tree mode: temp line in MainCanvas (transformed)
    // Use canvas coords directly
    startPoint = startPointCanvas;
    endPoint = _endPoint; // Canvas coords
}
```

**This per-state mode awareness is the anti-pattern we need to eliminate.**

---

## Proposed Architecture

### Design Principles

1. **Canvas-First**: All interaction logic works in canvas coordinates
2. **Transform at Boundary**: Viewport transforms applied only when rendering
3. **Mode Invisible**: Rendering mode is invisible to input states
4. **Type Safety**: Typed coordinate structs prevent accidental mixing

### 1. Typed Coordinate System

Create distinct types for each coordinate space to prevent mixing at compile time:

```csharp
// FlowGraph.Core/Coordinates/CanvasPoint.cs
namespace FlowGraph.Core.Coordinates;

/// <summary>
/// A point in canvas coordinate space (logical graph coordinates).
/// Node positions, port centers, edge endpoints all use canvas coordinates.
/// Values are stable regardless of zoom/pan.
/// </summary>
[DebuggerDisplay("Canvas({X}, {Y})")]
public readonly record struct CanvasPoint(double X, double Y)
{
    public static CanvasPoint Zero => new(0, 0);

    public static CanvasPoint FromNode(Node node) => new(node.Position.X, node.Position.Y);

    public static CanvasPoint operator +(CanvasPoint a, CanvasVector b) => new(a.X + b.DX, a.Y + b.DY);
    public static CanvasPoint operator -(CanvasPoint a, CanvasVector b) => new(a.X - b.DX, a.Y - b.DY);
    public static CanvasVector operator -(CanvasPoint a, CanvasPoint b) => new(a.X - b.X, a.Y - b.Y);

    // Explicit conversion to Avalonia Point (loses type safety, use sparingly)
    public Avalonia.Point ToAvalonia() => new(X, Y);
    public static CanvasPoint FromAvalonia(Avalonia.Point p) => new(p.X, p.Y);
}

/// <summary>
/// A vector/delta in canvas coordinate space.
/// Used for movements, offsets, and distances in canvas space.
/// </summary>
[DebuggerDisplay("CanvasΔ({DX}, {DY})")]
public readonly record struct CanvasVector(double DX, double DY)
{
    public static CanvasVector Zero => new(0, 0);
    public double Length => Math.Sqrt(DX * DX + DY * DY);
    public static CanvasVector operator *(CanvasVector v, double scalar) => new(v.DX * scalar, v.DY * scalar);
}

/// <summary>
/// A rectangle in canvas coordinate space.
/// </summary>
[DebuggerDisplay("Canvas[{X},{Y} {Width}x{Height}]")]
public readonly record struct CanvasRect(double X, double Y, double Width, double Height)
{
    public CanvasPoint TopLeft => new(X, Y);
    public CanvasPoint BottomRight => new(X + Width, Y + Height);
    public CanvasPoint Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(CanvasPoint point) =>
        point.X >= X && point.X <= X + Width &&
        point.Y >= Y && point.Y <= Y + Height;

    public Avalonia.Rect ToAvalonia() => new(X, Y, Width, Height);
    public static CanvasRect FromAvalonia(Avalonia.Rect r) => new(r.X, r.Y, r.Width, r.Height);
}
```

```csharp
// FlowGraph.Core/Coordinates/ViewportPoint.cs
namespace FlowGraph.Core.Coordinates;

/// <summary>
/// A point in viewport coordinate space (screen/control coordinates).
/// (0,0) is the top-left of the visible canvas area.
/// Values change as the user pans and zooms.
/// </summary>
[DebuggerDisplay("Viewport({X}, {Y})")]
public readonly record struct ViewportPoint(double X, double Y)
{
    public static ViewportPoint Zero => new(0, 0);

    public static ViewportPoint operator +(ViewportPoint a, ViewportVector b) => new(a.X + b.DX, a.Y + b.DY);
    public static ViewportPoint operator -(ViewportPoint a, ViewportVector b) => new(a.X - b.DX, a.Y - b.DY);
    public static ViewportVector operator -(ViewportPoint a, ViewportPoint b) => new(a.X - b.X, a.Y - b.Y);

    public Avalonia.Point ToAvalonia() => new(X, Y);
    public static ViewportPoint FromAvalonia(Avalonia.Point p) => new(p.X, p.Y);
}

/// <summary>
/// A vector/delta in viewport coordinate space.
/// </summary>
[DebuggerDisplay("ViewportΔ({DX}, {DY})")]
public readonly record struct ViewportVector(double DX, double DY)
{
    public static ViewportVector Zero => new(0, 0);
    public double Length => Math.Sqrt(DX * DX + DY * DY);
}

/// <summary>
/// A rectangle in viewport coordinate space.
/// </summary>
[DebuggerDisplay("Viewport[{X},{Y} {Width}x{Height}]")]
public readonly record struct ViewportRect(double X, double Y, double Width, double Height)
{
    public ViewportPoint TopLeft => new(X, Y);
    public ViewportPoint BottomRight => new(X + Width, Y + Height);

    public Avalonia.Rect ToAvalonia() => new(X, Y, Width, Height);
    public static ViewportRect FromAvalonia(Avalonia.Rect r) => new(r.X, r.Y, r.Width, r.Height);
}
```

### 2. Updated ICoordinateTransformer

```csharp
// FlowGraph.Core/Rendering/ICoordinateTransformer.cs
namespace FlowGraph.Core.Rendering;

using FlowGraph.Core.Coordinates;

/// <summary>
/// Provides bidirectional coordinate transformation between canvas and viewport space.
/// </summary>
public interface ICoordinateTransformer
{
    double Zoom { get; }
    double OffsetX { get; }
    double OffsetY { get; }

    // Type-safe transforms
    CanvasPoint ToCanvas(ViewportPoint viewport);
    ViewportPoint ToViewport(CanvasPoint canvas);

    CanvasVector ToCanvasDelta(ViewportVector viewport);
    ViewportVector ToViewportDelta(CanvasVector canvas);

    CanvasRect ToCanvas(ViewportRect viewport);
    ViewportRect ToViewport(CanvasRect canvas);
}
```

### 3. Unified Input Coordinate Provider

Instead of exposing raw coordinate transforms to states, provide a unified interface:

```csharp
// FlowGraph.Avalonia/Input/IInputCoordinates.cs
namespace FlowGraph.Avalonia.Input;

using FlowGraph.Core.Coordinates;

/// <summary>
/// Provides coordinate information for input handling.
/// Abstracts away rendering mode differences - all coordinates are in canvas space.
/// </summary>
public interface IInputCoordinates
{
    /// <summary>
    /// Gets the current pointer position in canvas coordinates.
    /// Works correctly regardless of rendering mode.
    /// </summary>
    CanvasPoint GetPointerCanvasPosition(PointerEventArgs e);

    /// <summary>
    /// Gets the current pointer position in viewport coordinates.
    /// Use for auto-pan edge detection, fixed UI positioning.
    /// </summary>
    ViewportPoint GetPointerViewportPosition(PointerEventArgs e);

    /// <summary>
    /// Gets the visible area in canvas coordinates.
    /// </summary>
    CanvasRect GetVisibleCanvasRect();

    /// <summary>
    /// Gets the viewport size.
    /// </summary>
    ViewportRect GetViewportBounds();
}
```

Implementation handles the mode differences internally:

```csharp
// FlowGraph.Avalonia/Input/InputCoordinateProvider.cs
namespace FlowGraph.Avalonia.Input;

public class InputCoordinateProvider : IInputCoordinates
{
    private readonly ViewportState _viewport;
    private readonly Panel _rootPanel;
    private readonly Canvas _mainCanvas;

    public CanvasPoint GetPointerCanvasPosition(PointerEventArgs e)
    {
        // GetPosition(MainCanvas) returns canvas coords because the MatrixTransform
        // inverse is automatically applied. This works even if MainCanvas is empty
        // because the transform affects coordinate calculation, not hit testing.
        var point = e.GetPosition(_mainCanvas);
        return CanvasPoint.FromAvalonia(point);
    }

    public ViewportPoint GetPointerViewportPosition(PointerEventArgs e)
    {
        var point = e.GetPosition(_rootPanel);
        return ViewportPoint.FromAvalonia(point);
    }

    public CanvasRect GetVisibleCanvasRect()
    {
        return CanvasRect.FromAvalonia(_viewport.GetVisibleRect());
    }

    public ViewportRect GetViewportBounds()
    {
        return new ViewportRect(0, 0, _viewport.ViewSize.Width, _viewport.ViewSize.Height);
    }
}
```

### 4. Unified Render Target Interface

For temporary visuals (temp connection line, selection box), provide a unified interface:

```csharp
// FlowGraph.Avalonia/Rendering/IRenderTarget.cs
namespace FlowGraph.Avalonia.Rendering;

using FlowGraph.Core.Coordinates;

/// <summary>
/// Abstraction for adding temporary visual elements during interaction.
/// Handles coordinate transforms and container selection based on rendering mode.
/// </summary>
public interface IRenderTarget
{
    /// <summary>
    /// Creates a temporary bezier line for connection preview.
    /// Coordinates are in canvas space - transform is handled internally.
    /// </summary>
    IDisposable AddTempConnectionLine(
        CanvasPoint start,
        CanvasPoint end,
        IBrush stroke,
        double thickness,
        double[]? dashArray = null);

    /// <summary>
    /// Creates a temporary rectangle for box selection.
    /// Coordinates are in canvas space.
    /// </summary>
    IDisposable AddTempSelectionBox(
        CanvasRect bounds,
        IBrush? fill,
        IBrush stroke,
        double thickness);

    /// <summary>
    /// Updates the endpoint of an existing temp connection line.
    /// </summary>
    void UpdateTempConnectionLine(IDisposable handle, CanvasPoint newEnd);
}
```

Implementation:

```csharp
// FlowGraph.Avalonia/Rendering/UnifiedRenderTarget.cs
namespace FlowGraph.Avalonia.Rendering;

public class UnifiedRenderTarget : IRenderTarget
{
    private readonly Panel _rootPanel;
    private readonly Canvas _mainCanvas;
    private readonly ViewportState _viewport;
    private readonly DirectGraphRenderer? _directRenderer;

    private class TempLineHandle : IDisposable
    {
        public Path Line { get; set; }
        public Panel Container { get; set; }
        public bool IsDirectMode { get; set; }
        public ViewportState Viewport { get; set; }

        public void Dispose()
        {
            Container.Children.Remove(Line);
        }
    }

    public IDisposable AddTempConnectionLine(
        CanvasPoint start,
        CanvasPoint end,
        IBrush stroke,
        double thickness,
        double[]? dashArray = null)
    {
        var line = new Path
        {
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeDashArray = dashArray != null ? new AvaloniaList<double>(dashArray) : null,
            IsHitTestVisible = false
        };

        bool isDirectMode = _directRenderer != null;
        Panel container;

        if (isDirectMode)
        {
            // Direct mode: add to RootPanel, use viewport coordinates
            container = _rootPanel;
            var startViewport = _viewport.CanvasToViewport(start.ToAvalonia());
            var endViewport = _viewport.CanvasToViewport(end.ToAvalonia());
            line.Data = CreateBezierGeometry(
                ViewportPoint.FromAvalonia(startViewport),
                ViewportPoint.FromAvalonia(endViewport));
        }
        else
        {
            // Visual tree mode: add to MainCanvas, use canvas coordinates directly
            container = _mainCanvas;
            line.Data = CreateBezierGeometry(start, end);
        }

        container.Children.Add(line);

        return new TempLineHandle
        {
            Line = line,
            Container = container,
            IsDirectMode = isDirectMode,
            Viewport = _viewport
        };
    }

    public void UpdateTempConnectionLine(IDisposable handle, CanvasPoint newEnd)
    {
        if (handle is TempLineHandle h)
        {
            // Recreate geometry with new end point
            // (In a real implementation, you'd update the existing geometry)
            if (h.IsDirectMode)
            {
                var endViewport = h.Viewport.CanvasToViewport(newEnd.ToAvalonia());
                // Update geometry in viewport coords...
            }
            else
            {
                // Update geometry in canvas coords...
            }
        }
    }

    private static Geometry CreateBezierGeometry(CanvasPoint start, CanvasPoint end)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start.ToAvalonia(), false);
            var dx = end.X - start.X;
            var controlOffset = Math.Max(50, Math.Abs(dx) * 0.5);
            ctx.CubicBezierTo(
                new Point(start.X + controlOffset, start.Y),
                new Point(end.X - controlOffset, end.Y),
                end.ToAvalonia());
        }
        return geometry;
    }

    private static Geometry CreateBezierGeometry(ViewportPoint start, ViewportPoint end)
    {
        // Same logic but with ViewportPoint
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start.ToAvalonia(), false);
            // ... bezier calculation
        }
        return geometry;
    }
}
```

### 5. Simplified Input States

With the unified interfaces, input states become rendering-mode agnostic:

```csharp
// FlowGraph.Avalonia/Input/States/ConnectingState.cs (simplified)
public class ConnectingState : InputStateBase
{
    private readonly CanvasPoint _startPosition;
    private CanvasPoint _currentEndPosition;
    private IDisposable? _tempLineHandle;

    public override void Enter(InputStateContext context)
    {
        base.Enter(context);

        // Get port position in canvas coordinates
        _startPosition = context.RenderModel.GetPortPosition(_sourceNode, _sourcePort, _fromOutput);
        _currentEndPosition = _startPosition;

        // Create temp line - IRenderTarget handles mode differences
        _tempLineHandle = context.RenderTarget.AddTempConnectionLine(
            _startPosition,
            _currentEndPosition,
            context.Theme.EdgeStroke,
            2.0,
            dashArray: [5, 3]);

        context.RaiseConnectStart(_sourceNode, _sourcePort, _fromOutput);
    }

    public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
    {
        // Get canvas position - IInputCoordinates handles mode differences
        _currentEndPosition = context.Coordinates.GetPointerCanvasPosition(e);

        // Auto-pan uses viewport coordinates
        var viewportPos = context.Coordinates.GetPointerViewportPosition(e);
        HandleAutoPan(context, viewportPos);

        // Check for snap target
        var snapTarget = FindSnapTarget(context, _currentEndPosition);
        if (snapTarget.HasValue)
        {
            _currentEndPosition = context.RenderModel.GetPortPosition(
                snapTarget.Value.Node,
                snapTarget.Value.Port,
                snapTarget.Value.IsOutput);
        }

        // Update temp line - always in canvas coordinates
        context.RenderTarget.UpdateTempConnectionLine(_tempLineHandle!, _currentEndPosition);

        return StateTransitionResult.Stay();
    }

    public override void Exit(InputStateContext context)
    {
        _tempLineHandle?.Dispose();
        _tempLineHandle = null;
    }

    private void HandleAutoPan(InputStateContext context, ViewportPoint viewportPos)
    {
        var bounds = context.Coordinates.GetViewportBounds();
        var edgeDist = context.Settings.AutoPanEdgeDistance;

        double panX = 0, panY = 0;
        if (viewportPos.X < edgeDist) panX = context.Settings.AutoPanSpeed;
        else if (viewportPos.X > bounds.Width - edgeDist) panX = -context.Settings.AutoPanSpeed;
        // ... similar for Y

        if (panX != 0 || panY != 0)
        {
            context.Viewport.Pan(panX, panY);
        }
    }
}
```

### 6. Updated InputStateContext

```csharp
// FlowGraph.Avalonia/Input/InputStateContext.cs (updated)
public class InputStateContext
{
    // New unified interfaces
    public IInputCoordinates Coordinates { get; }
    public IRenderTarget RenderTarget { get; }
    public GraphRenderModel RenderModel { get; }

    // Still expose ViewportState for pan/zoom operations
    public ViewportState Viewport { get; }

    // Theme, Settings, Graph, etc. remain the same

    // REMOVED: Direct access to MainCanvas, RootPanel, DirectRenderer
    // States should not need to know about the visual tree structure
}
```

---

## Migration Strategy

### Phase 1: Add Type Infrastructure (Non-Breaking)

1. Add `CanvasPoint`, `ViewportPoint`, and related types to FlowGraph.Core
2. Add type-safe overloads to `ICoordinateTransformer` alongside existing methods
3. Create `IInputCoordinates` and `IRenderTarget` interfaces

### Phase 2: Create Implementations

1. Implement `InputCoordinateProvider`
2. Implement `UnifiedRenderTarget`
3. Wire up in `FlowCanvas` initialization

### Phase 3: Migrate Input States (Gradual)

1. Update `InputStateContext` to expose new interfaces
2. Migrate `ConnectingState` first (highest complexity)
3. Migrate remaining states one by one
4. Add deprecation warnings to old methods

### Phase 4: Cleanup

1. Remove deprecated methods from `InputStateContext`
2. Remove direct `MainCanvas`/`RootPanel` access from states
3. Mark old `Point`-based coordinate methods as obsolete

---

## API Comparison

### Before (Current)

```csharp
// ConnectingState - must know about rendering modes
public override void Enter(InputStateContext context)
{
    // Create temp line - must choose container based on mode
    if (context.DirectRenderer != null && context.RootPanel != null)
    {
        context.RootPanel.Children.Add(_tempLine);
        _tempLineContainer = context.RootPanel;
    }
    else if (context.MainCanvas != null)
    {
        context.MainCanvas.Children.Add(_tempLine);
        _tempLineContainer = context.MainCanvas;
    }
}

public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
{
    // Must handle coordinates differently per mode
    var screenPos = GetScreenPosition(context, e);
    _endPointViewport = screenPos;
    _endPoint = GetCanvasPosition(context, e);

    // Update temp line differently per mode
    if (context.DirectRenderer != null)
    {
        startPoint = context.CanvasToViewport(startPointCanvas);
        endPoint = _endPointViewport;
    }
    else
    {
        startPoint = startPointCanvas;
        endPoint = _endPoint;
    }
}
```

### After (Proposed)

```csharp
// ConnectingState - rendering mode agnostic
public override void Enter(InputStateContext context)
{
    var startCanvas = context.RenderModel.GetPortPosition(_sourceNode, _sourcePort, _fromOutput);

    // IRenderTarget handles container selection and coordinate transforms
    _tempLineHandle = context.RenderTarget.AddTempConnectionLine(
        startCanvas, startCanvas, _theme.EdgeStroke, 2.0);
}

public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
{
    // IInputCoordinates handles coordinate conversion
    var canvasPos = context.Coordinates.GetPointerCanvasPosition(e);

    // Always work in canvas coordinates
    context.RenderTarget.UpdateTempConnectionLine(_tempLineHandle, canvasPos);

    return StateTransitionResult.Stay();
}
```

---

## Benefits

1. **Type Safety**: Compile-time prevention of coordinate mixing
2. **Simplicity**: States don't need mode awareness
3. **Testability**: Can mock `IInputCoordinates` and `IRenderTarget`
4. **Extensibility**: New rendering modes only need to implement interfaces
5. **Maintainability**: Coordinate logic centralized in few places

## Risks and Mitigations

| Risk                                   | Mitigation                                    |
| -------------------------------------- | --------------------------------------------- |
| Performance overhead from typed coords | Structs are stack-allocated, minimal overhead |
| Breaking existing code                 | Gradual migration with deprecation warnings   |
| Complexity of IRenderTarget            | Start simple, expand as needed                |
| Edge cases in coordinate conversion    | Comprehensive unit tests for transforms       |

---

## Appendix: Complete Type Definitions

See proposed files:

- `FlowGraph.Core/Coordinates/CanvasPoint.cs`
- `FlowGraph.Core/Coordinates/ViewportPoint.cs`
- `FlowGraph.Core/Coordinates/CanvasRect.cs`
- `FlowGraph.Core/Coordinates/ViewportRect.cs`
- `FlowGraph.Avalonia/Input/IInputCoordinates.cs`
- `FlowGraph.Avalonia/Rendering/IRenderTarget.cs`
- `FlowGraph.Avalonia/Rendering/UnifiedRenderTarget.cs`

## Appendix: Coordinate Flow Diagrams

### Current Flow (Problem)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Pointer Event                                │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
           ┌───────────────────┴───────────────────┐
           ▼                                       ▼
┌─────────────────────┐                 ┌─────────────────────┐
│  GetScreenPosition  │                 │  GetCanvasPosition  │
│  (RootPanel coords) │                 │  (MainCanvas coords)│
└──────────┬──────────┘                 └──────────┬──────────┘
           │                                       │
           ▼                                       ▼
┌─────────────────────┐                 ┌─────────────────────┐
│ Viewport Coords     │                 │ Canvas Coords       │
│ (used for autopan)  │                 │ (used for hit test) │
└──────────┬──────────┘                 └──────────┬──────────┘
           │                                       │
           │     ┌─────────────────────────────────┘
           │     │
           ▼     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Input State Logic                               │
│  ┌────────────────────┐         ┌────────────────────┐              │
│  │  if (DirectMode)   │         │  else (VisualTree) │              │
│  │  - Add to RootPanel│         │  - Add to MainCanv │              │
│  │  - Convert to VP   │         │  - Use canvas as-is│              │
│  └────────────────────┘         └────────────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
                               │
           ┌───────────────────┴───────────────────┐
           ▼                                       ▼
┌─────────────────────────────┐       ┌─────────────────────────────┐
│   DirectGraphRenderer       │       │   Visual Tree (MainCanvas)  │
│   - Self-transforms         │       │   - MatrixTransform applied │
│   - Canvas → Viewport       │       │   - Canvas coords direct    │
└─────────────────────────────┘       └─────────────────────────────┘
```

### Proposed Flow (Solution)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Pointer Event                                │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    IInputCoordinates                                │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  GetPointerCanvasPosition(e) → CanvasPoint                  │    │
│  │  GetPointerViewportPosition(e) → ViewportPoint              │    │
│  │  (Handles mode internally)                                  │    │
│  └─────────────────────────────────────────────────────────────┘    │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│              Input State Logic (Mode Agnostic)                      │
│                                                                     │
│   CanvasPoint currentPos = context.Coordinates                     │
│       .GetPointerCanvasPosition(e);                                 │
│                                                                     │
│   context.RenderTarget.UpdateTempLine(_handle, currentPos);        │
│                                                                     │
│   // All logic in canvas coordinates!                              │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      IRenderTarget                                  │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  AddTempConnectionLine(CanvasPoint, CanvasPoint)           │    │
│  │  - Detects rendering mode                                   │    │
│  │  - Selects correct container (RootPanel vs MainCanvas)     │    │
│  │  - Converts coordinates if needed                           │    │
│  └─────────────────────────────────────────────────────────────┘    │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
           ┌───────────────────┴───────────────────┐
           ▼                                       ▼
┌─────────────────────────────┐       ┌─────────────────────────────┐
│   DirectGraphRenderer       │       │   Visual Tree (MainCanvas)  │
│   - Receives viewport coords│       │   - Receives canvas coords  │
│   - Renders directly        │       │   - Transform applied by    │
│                             │       │     MatrixTransform          │
└─────────────────────────────┘       └─────────────────────────────┘
```

---

## Related Work

- **react-diagrams**: Uses `engine.getRelativeMousePoint()` which always returns canvas coords
- **Konva.js**: `stage.getPointerPosition()` vs `node.getRelativePointerPosition()`
- **Three.js**: `Raycaster` works in normalized device coordinates, world coords abstracted
- **Unity**: `Camera.ScreenToWorldPoint()` / `WorldToScreenPoint()` with Vector3 types

## Conclusion

This architecture eliminates the root cause of coordinate confusion by:

1. Making rendering mode invisible to interaction code
2. Providing type-safe coordinate abstractions
3. Centralizing mode-specific logic in well-defined interfaces
4. Following patterns from established libraries

The migration can be done gradually without breaking existing code, and the end result is significantly simpler interaction code that "just works" regardless of rendering mode.

---

## Appendix: Typed Coordinate System Usage Guide

### A. Type-Safe Coordinate Types

The new coordinate types provide compile-time safety - you cannot accidentally pass a viewport point where a canvas point is expected:

```csharp
// FlowGraph.Core.Coordinates namespace
using FlowGraph.Core.Coordinates;

// Canvas coordinates - stable graph positions
CanvasPoint nodePos = new(100, 200);
CanvasVector delta = new(10, -5);
CanvasRect bounds = new(0, 0, 200, 150);

// Viewport coordinates - screen positions (affected by zoom/pan)
ViewportPoint screenPos = new(500, 300);
ViewportVector screenDelta = new(20, 10);
ViewportRect viewBounds = ViewportRect.FromSize(800, 600);

// Type safety - this won't compile!
// CanvasPoint wrong = screenPos;  // Error: cannot convert ViewportPoint to CanvasPoint
```

### B. Coordinate Transformation

Use `ITypedCoordinateTransformer` for conversions:

```csharp
// Create transformer with current viewport state
var transformer = new TypedCoordinateTransformer(zoom: 1.5, offsetX: 100, offsetY: 50);

// Type-safe conversions
ViewportPoint viewport = new(500, 300);
CanvasPoint canvas = transformer.ToCanvas(viewport);  // Returns CanvasPoint
ViewportPoint back = transformer.ToViewport(canvas);  // Returns ViewportPoint

// Vectors (deltas) - only zoom applied, no offset
ViewportVector screenDelta = new(30, 20);
CanvasVector canvasDelta = transformer.ToCanvas(screenDelta);  // 30/1.5, 20/1.5 = (20, 13.3)

// Rectangles
CanvasRect nodeBounds = new(100, 100, 200, 150);
ViewportRect screenBounds = transformer.ToViewport(nodeBounds);
```

### C. Avalonia Integration

Use extension methods to convert between typed coords and Avalonia types:

```csharp
using FlowGraph.Avalonia.Coordinates;
using AvaloniaPoint = Avalonia.Point;

// From Avalonia to typed
AvaloniaPoint avaloniaPos = e.GetPosition(mainCanvas);
CanvasPoint canvasPos = avaloniaPos.ToCanvasPoint();
ViewportPoint viewportPos = avaloniaPos.ToViewportPoint();

// From typed to Avalonia (for rendering)
CanvasPoint nodeCenter = new(150, 200);
AvaloniaPoint renderPos = nodeCenter.ToAvalonia();
context.DrawEllipse(brush, pen, renderPos, 5, 5);

// Direct transform with Avalonia types
AvaloniaPoint transformed = avaloniaPos.ToCanvasSpace(transformer);
```

### D. Built-in Operations

The coordinate types include useful operations:

```csharp
// Point arithmetic
CanvasPoint start = new(100, 100);
CanvasVector offset = new(50, 30);
CanvasPoint end = start + offset;  // (150, 130)

// Vector between points
CanvasVector diff = end - start;  // (50, 30)

// Distance calculations
double dist = start.DistanceTo(end);
double distSq = start.DistanceSquaredTo(end);  // Faster for comparisons

// Grid snapping
CanvasPoint snapped = start.SnappedToGrid(20);  // (100, 100)

// Rectangle operations
CanvasRect rect = new(50, 50, 200, 150);
bool contains = rect.Contains(start);  // true
CanvasRect inflated = rect.Inflate(10);  // Expand by 10 on all sides
CanvasRect union = rect.Union(otherRect);

// Viewport edge detection (for auto-pan)
ViewportPoint cursor = new(10, 300);
ViewportRect bounds = ViewportRect.FromSize(800, 600);
bool nearEdge = cursor.IsNearEdge(bounds, edgeDistance: 20);  // true (near left)
ViewportVector panDir = cursor.GetEdgePanDirection(bounds, 20);  // (1, 0) = pan right
```

### E. Future: Mode-Agnostic Input Handling

The `IInputCoordinates` interface (not yet integrated) will simplify input states:

```csharp
// Future usage in input states
public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
{
    // Get position - works correctly in both rendering modes
    CanvasPoint canvasPos = context.Coordinates.GetPointerCanvasPosition(e);

    // Use for hit testing, snapping, etc.
    var snapTarget = FindSnapTarget(canvasPos);

    // Get viewport position for auto-pan
    ViewportPoint viewportPos = context.Coordinates.GetPointerViewportPosition(e);
    if (viewportPos.IsNearEdge(context.Coordinates.GetViewportBounds(), 30))
    {
        TriggerAutoPan(viewportPos.GetEdgePanDirection(viewportBounds, 30));
    }
}
```

### F. Future: Mode-Agnostic Rendering

The `IRenderTarget` interface (not yet integrated) will simplify temp visual creation:

```csharp
// Future usage in input states
public override void Enter(InputStateContext context)
{
    var startCanvas = context.RenderModel.GetPortPosition(sourceNode, sourcePort);

    // Create preview - IRenderTarget handles mode differences internally
    _lineHandle = context.RenderTarget.CreateConnectionPreview(
        startCanvas, startCanvas,
        context.Theme.EdgeStroke,
        strokeThickness: 2.0);
}

public override void Exit(InputStateContext context)
{
    _lineHandle?.Dispose();  // Cleanup handled automatically
}
```
