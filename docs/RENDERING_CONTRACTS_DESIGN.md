# FlowGraph Rendering Contracts Design

**Status**: ✅ Phase 1 Complete | Phases 3-4 Future Work  
**Last Updated**: January 14, 2026  
**Purpose**: Define clear, backend-agnostic contracts for coordinate systems, rendering, and hit testing

## Implementation Status

| Phase   | Description                | Status                                                                               |
| ------- | -------------------------- | ------------------------------------------------------------------------------------ |
| Phase 1 | Core Interfaces            | ✅ **Complete** - ICoordinateTransformer, IViewportState, IRenderLayer, IHitTestable |
| Phase 2 | Documentation & Attributes | ✅ **Complete** - CoordinateSpaceAttribute, XML docs                                 |
| Phase 3 | Element Renderer Interface | 🔮 Future - IElementRenderer<T,V> for backend-agnostic rendering                     |
| Phase 4 | Backend Abstraction        | 🔮 Future - ICanvasBackend factory pattern for multi-platform                        |

## Tests

- `CoordinateTransformerTests.cs` - 26 tests for coordinate transforms
- `ViewportStateInterfaceTests.cs` - 18 tests for viewport state contract
- `HitTestResultTests.cs` - 16 tests for hit test results

## Files Created

| File                                                                                                            | Purpose                                     |
| --------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| [FlowGraph.Core/Rendering/CoordinateSpaceAttribute.cs](../FlowGraph.Core/Rendering/CoordinateSpaceAttribute.cs) | Attribute for documenting coordinate spaces |
| [FlowGraph.Core/Rendering/ICoordinateTransformer.cs](../FlowGraph.Core/Rendering/ICoordinateTransformer.cs)     | Bidirectional coordinate transform contract |
| [FlowGraph.Core/Rendering/IRenderLayer.cs](../FlowGraph.Core/Rendering/IRenderLayer.cs)                         | Layer transform mode definitions            |
| [FlowGraph.Core/Rendering/IViewportState.cs](../FlowGraph.Core/Rendering/IViewportState.cs)                     | Viewport state management contract          |
| [FlowGraph.Core/Input/IHitTestable.cs](../FlowGraph.Core/Input/IHitTestable.cs)                                 | Generic hit testing contract                |
| [FlowGraph.Core/Input/IGraphHitTester.cs](../FlowGraph.Core/Input/IGraphHitTester.cs)                           | Graph-wide hit testing with priority        |
| [FlowGraph.Core/Elements/Size.cs](../FlowGraph.Core/Elements/Size.cs)                                           | Size record struct                          |

## Executive Summary

This document proposes a set of contracts (interfaces and conventions) to formalize coordinate system handling, rendering abstraction, and hit testing across FlowGraph. The design is inspired by established open-source projects (react-diagrams, Konva.js, mxGraph) and prioritizes multi-backend support.

---

## 1. Core Principles

### 1.1 Coordinate Space Discipline

Every method, property, and parameter that deals with positions MUST declare which coordinate space it operates in. No implicit assumptions.

```
┌────────────────────────────────────────────────────────────────────┐
│                         SCREEN SPACE                               │
│   • Raw pixel coordinates from pointer events                      │
│   • What you see on the monitor                                    │
│   • Origin: Top-left of the control/window                         │
├────────────────────────────────────────────────────────────────────┤
│                          ↕ Transform (Zoom + Offset)               │
├────────────────────────────────────────────────────────────────────┤
│                         CANVAS SPACE                               │
│   • Logical coordinates of graph elements                          │
│   • Node positions, edge endpoints                                 │
│   • Stable regardless of zoom/pan                                  │
│   • Origin: Infinite canvas (can be negative)                      │
└────────────────────────────────────────────────────────────────────┘
```

### 1.2 Transform Ownership

Each rendering component MUST explicitly declare whether it:

- **Expects transformed input** (already in screen coords)
- **Applies transforms itself** (receives canvas coords, applies viewport internally)
- **Is transform-agnostic** (operates purely in canvas space, parent handles transform)

### 1.3 Backend Agnosticism

Core contracts live in `FlowGraph.Core` (or a new `FlowGraph.Abstractions` project).
Backend-specific implementations live in `FlowGraph.Avalonia`, with clear extension points
for future backends (WPF, MAUI, SkiaSharp, Web/Blazor).

---

## 2. Coordinate System Contracts

### 2.1 Core Geometry Types (Platform-Agnostic)

```csharp
// FlowGraph.Core/Geometry/Point.cs (already exists, extend)
namespace FlowGraph.Core;

/// <summary>
/// Represents a point in a 2D coordinate space.
/// The coordinate space must be specified by the context in which the point is used.
/// </summary>
public readonly record struct Point(double X, double Y)
{
    public static readonly Point Zero = new(0, 0);

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator *(Point p, double scale) => new(p.X * scale, p.Y * scale);
    public static Point operator /(Point p, double scale) => new(p.X / scale, p.Y / scale);

    public double DistanceTo(Point other) =>
        Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
}

/// <summary>
/// Represents a rectangle in a 2D coordinate space.
/// </summary>
public readonly record struct Rect(double X, double Y, double Width, double Height)
{
    public static readonly Rect Empty = new(0, 0, 0, 0);

    public Point TopLeft => new(X, Y);
    public Point BottomRight => new(X + Width, Y + Height);
    public Point Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Point p) =>
        p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height;

    public bool Intersects(Rect other) =>
        X < other.X + other.Width && X + Width > other.X &&
        Y < other.Y + other.Height && Y + Height > other.Y;
}
```

### 2.2 Coordinate Transformer Interface

```csharp
// FlowGraph.Core/Rendering/ICoordinateTransformer.cs
namespace FlowGraph.Core.Rendering;

/// <summary>
/// Provides bidirectional coordinate transformation between canvas and screen space.
///
/// Inspired by:
/// - react-diagrams: CanvasEngine.getRelativeMousePoint(), getRelativePoint()
/// - Konva.js: Stage.getPointerPosition() vs Node.getRelativePointerPosition()
/// - AnyChart: scale.transform() / scale.inverseTransform()
/// </summary>
public interface ICoordinateTransformer
{
    /// <summary>
    /// The current zoom level (1.0 = 100%).
    /// </summary>
    double Zoom { get; }

    /// <summary>
    /// The current pan offset in screen coordinates.
    /// </summary>
    Point Offset { get; }

    /// <summary>
    /// Transforms a point from screen space to canvas space.
    ///
    /// Formula: canvasPoint = (screenPoint - offset) / zoom
    ///
    /// Use when:
    /// - Converting pointer event positions to canvas coordinates
    /// - Hit testing against canvas elements
    /// </summary>
    /// <param name="screenPoint">A point in screen coordinates.</param>
    /// <returns>The equivalent point in canvas coordinates.</returns>
    Point ScreenToCanvas(Point screenPoint);

    /// <summary>
    /// Transforms a point from canvas space to screen space.
    ///
    /// Formula: screenPoint = canvasPoint * zoom + offset
    ///
    /// Use when:
    /// - Drawing directly to a DrawingContext (bypassing visual tree)
    /// - Calculating visible bounds in screen space
    /// </summary>
    /// <param name="canvasPoint">A point in canvas coordinates.</param>
    /// <returns>The equivalent point in screen coordinates.</returns>
    Point CanvasToScreen(Point canvasPoint);

    /// <summary>
    /// Transforms a rectangle from canvas space to screen space.
    /// </summary>
    Rect CanvasToScreen(Rect canvasRect);

    /// <summary>
    /// Transforms a rectangle from screen space to canvas space.
    /// </summary>
    Rect ScreenToCanvas(Rect screenRect);

    /// <summary>
    /// Transforms a delta/vector from screen space to canvas space.
    /// Unlike point transforms, this only applies zoom (not offset).
    ///
    /// Formula: canvasDelta = screenDelta / zoom
    ///
    /// Use when:
    /// - Converting drag distances to canvas movement
    /// - Zoom-adjusted displacement calculations
    /// </summary>
    Point ScreenToCanvasDelta(Point screenDelta);

    /// <summary>
    /// Transforms a delta/vector from canvas space to screen space.
    /// Unlike point transforms, this only applies zoom (not offset).
    /// </summary>
    Point CanvasToScreenDelta(Point canvasDelta);
}
```

### 2.3 Viewport State Interface

```csharp
// FlowGraph.Core/Rendering/IViewportState.cs
namespace FlowGraph.Core.Rendering;

/// <summary>
/// Read-only viewport state for observing pan/zoom changes.
///
/// Inspired by:
/// - react-diagrams: CanvasModel with offsetX, offsetY, zoom
/// - Konva.js: Stage with scale, position properties
/// </summary>
public interface IReadOnlyViewportState : ICoordinateTransformer
{
    /// <summary>
    /// The visible size of the viewport in screen coordinates.
    /// </summary>
    Size ViewSize { get; }

    /// <summary>
    /// Gets the currently visible area in canvas coordinates.
    /// </summary>
    Rect GetVisibleCanvasRect();

    /// <summary>
    /// Event raised when any viewport property changes.
    /// </summary>
    event EventHandler? ViewportChanged;
}

/// <summary>
/// Mutable viewport state for controlling pan/zoom.
/// </summary>
public interface IViewportState : IReadOnlyViewportState
{
    /// <summary>
    /// Sets the zoom level, optionally zooming toward a specific screen point.
    /// </summary>
    /// <param name="zoom">The new zoom level.</param>
    /// <param name="zoomCenter">Optional screen coordinate to zoom toward.</param>
    void SetZoom(double zoom, Point? zoomCenter = null);

    /// <summary>
    /// Pans by the specified delta in screen coordinates.
    /// </summary>
    void Pan(double deltaX, double deltaY);

    /// <summary>
    /// Centers the viewport on a canvas point.
    /// </summary>
    void CenterOn(Point canvasPoint);

    /// <summary>
    /// Fits the viewport to show the specified canvas bounds.
    /// </summary>
    void FitToBounds(Rect canvasBounds, double padding = 50);
}
```

---

## 3. Rendering Contracts

### 3.1 Coordinate Space Attribute

```csharp
// FlowGraph.Core/Rendering/CoordinateSpaceAttribute.cs
namespace FlowGraph.Core.Rendering;

/// <summary>
/// Specifies which coordinate space a parameter, property, or return value uses.
/// </summary>
public enum CoordinateSpace
{
    /// <summary>
    /// Canvas coordinates - logical positions independent of zoom/pan.
    /// </summary>
    Canvas,

    /// <summary>
    /// Screen coordinates - pixel positions after zoom/pan transforms.
    /// </summary>
    Screen,

    /// <summary>
    /// Local coordinates - relative to a parent element's origin.
    /// </summary>
    Local
}

/// <summary>
/// Marks a parameter, property, or method with its expected coordinate space.
/// Use this for documentation and potential static analysis.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property |
                AttributeTargets.ReturnValue | AttributeTargets.Method)]
public class CoordinateSpaceAttribute : Attribute
{
    public CoordinateSpace Space { get; }

    public CoordinateSpaceAttribute(CoordinateSpace space)
    {
        Space = space;
    }
}
```

### 3.2 Render Layer Interface

```csharp
// FlowGraph.Core/Rendering/IRenderLayer.cs
namespace FlowGraph.Core.Rendering;

/// <summary>
/// Defines how a layer participates in viewport transformations.
///
/// Inspired by:
/// - react-diagrams: LayerModel.options.transformed flag
/// - Konva.js: Layer inheriting from Node with transform properties
/// </summary>
public enum LayerTransformMode
{
    /// <summary>
    /// Layer IS affected by viewport transforms.
    /// Elements are positioned in canvas coordinates.
    /// Parent container applies zoom/pan via transform.
    ///
    /// Use for: Nodes, edges, ports, resize handles
    /// </summary>
    Transformed,

    /// <summary>
    /// Layer is NOT affected by viewport transforms.
    /// Elements are positioned in screen coordinates.
    /// Layer handles transforms internally if needed.
    ///
    /// Use for: Grid overlays, fixed UI, direct renderers
    /// </summary>
    Untransformed,

    /// <summary>
    /// Layer manages its own transforms independently.
    /// Used for renderers that do their own viewport transforms.
    ///
    /// Use for: DirectGraphRenderer, custom DrawingContext renderers
    /// </summary>
    SelfTransformed
}

/// <summary>
/// Represents a rendering layer in the graph canvas.
/// </summary>
public interface IRenderLayer
{
    /// <summary>
    /// Unique identifier for this layer.
    /// </summary>
    string LayerId { get; }

    /// <summary>
    /// The Z-order of this layer (lower = behind, higher = in front).
    /// </summary>
    int ZIndex { get; }

    /// <summary>
    /// How this layer participates in viewport transformations.
    /// </summary>
    LayerTransformMode TransformMode { get; }

    /// <summary>
    /// Whether the layer is currently visible.
    /// </summary>
    bool IsVisible { get; set; }
}
```

### 3.3 Element Renderer Interface (Backend-Agnostic)

```csharp
// FlowGraph.Core/Rendering/IElementRenderer.cs
namespace FlowGraph.Core.Rendering;

/// <summary>
/// Core interface for element renderers that works across backends.
/// Backend-specific interfaces extend this with platform types.
/// </summary>
/// <typeparam name="TElement">The type of graph element to render.</typeparam>
/// <typeparam name="TVisual">The platform-specific visual type (Control, UIElement, etc.).</typeparam>
public interface IElementRenderer<TElement, TVisual>
{
    /// <summary>
    /// Gets the coordinate space that position inputs to this renderer expect.
    /// </summary>
    CoordinateSpace InputCoordinateSpace { get; }

    /// <summary>
    /// Creates a visual representation of the element.
    /// </summary>
    /// <param name="element">The element to render.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>A platform-specific visual.</returns>
    TVisual CreateVisual(TElement element, IRenderContext context);

    /// <summary>
    /// Updates an existing visual when element data changes.
    /// </summary>
    void UpdateVisual(TVisual visual, TElement element, IRenderContext context);

    /// <summary>
    /// Disposes resources associated with a visual.
    /// </summary>
    void DisposeVisual(TVisual visual);
}
```

### 3.4 Render Context Interface

```csharp
// FlowGraph.Core/Rendering/IRenderContext.cs
namespace FlowGraph.Core.Rendering;

/// <summary>
/// Provides context for rendering operations.
/// Platform-specific implementations add platform types.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// The coordinate transformer for this render context.
    /// </summary>
    ICoordinateTransformer CoordinateTransformer { get; }

    /// <summary>
    /// The current zoom/scale level.
    /// </summary>
    double Scale { get; }

    /// <summary>
    /// Checks if a canvas-space rectangle is within the visible viewport.
    /// </summary>
    /// <param name="canvasRect">Rectangle in canvas coordinates.</param>
    /// <returns>True if the rectangle intersects the visible area.</returns>
    bool IsVisible([CoordinateSpace(CoordinateSpace.Canvas)] Rect canvasRect);
}
```

---

## 4. Hit Testing Contracts

### 4.1 Hit Test Interface

```csharp
// FlowGraph.Core/Input/IHitTestable.cs
namespace FlowGraph.Core.Input;

/// <summary>
/// Result of a hit test operation.
/// </summary>
/// <typeparam name="TElement">The type of element that was hit.</typeparam>
public class HitTestResult<TElement>
{
    /// <summary>
    /// The element that was hit, or null if nothing was hit.
    /// </summary>
    public TElement? Element { get; init; }

    /// <summary>
    /// The position where the hit occurred, in canvas coordinates.
    /// </summary>
    [CoordinateSpace(CoordinateSpace.Canvas)]
    public Point CanvasPosition { get; init; }

    /// <summary>
    /// The position where the hit occurred, in local coordinates relative to the element.
    /// Only meaningful when Element is not null.
    /// </summary>
    [CoordinateSpace(CoordinateSpace.Local)]
    public Point LocalPosition { get; init; }

    /// <summary>
    /// Distance from the exact hit point to the element's bounds or center.
    /// Useful for edge hit testing where some tolerance is needed.
    /// </summary>
    public double Distance { get; init; }

    /// <summary>
    /// Whether an element was hit.
    /// </summary>
    public bool IsHit => Element is not null;
}

/// <summary>
/// Interface for components that support hit testing.
///
/// Inspired by:
/// - Konva.js: Stage.getIntersection(pos) returning the top Shape
/// - react-diagrams: getModelAtPosition with link/node distinction
/// </summary>
/// <typeparam name="TElement">The type of element this tests against.</typeparam>
public interface IHitTestable<TElement>
{
    /// <summary>
    /// The coordinate space expected for the hit test point.
    /// Implementations MUST document and enforce this.
    /// </summary>
    CoordinateSpace HitTestCoordinateSpace { get; }

    /// <summary>
    /// Performs a hit test at the specified position.
    /// </summary>
    /// <param name="position">
    /// Position to test. MUST be in the coordinate space specified by
    /// <see cref="HitTestCoordinateSpace"/>.
    /// </param>
    /// <param name="tolerance">
    /// Optional tolerance/margin for the hit test in the same coordinate space.
    /// </param>
    /// <returns>The hit test result.</returns>
    HitTestResult<TElement> HitTest(Point position, double tolerance = 0);
}
```

### 4.2 Composite Hit Tester

```csharp
// FlowGraph.Core/Input/IGraphHitTester.cs
namespace FlowGraph.Core.Input;

/// <summary>
/// The type of element hit during a graph hit test.
/// </summary>
public enum HitTargetType
{
    None,
    Canvas,
    Node,
    Edge,
    Port,
    ResizeHandle,
    Group,
    Custom
}

/// <summary>
/// Result of a graph-wide hit test.
/// </summary>
public class GraphHitTestResult
{
    public HitTargetType TargetType { get; init; }
    public object? Target { get; init; }

    [CoordinateSpace(CoordinateSpace.Canvas)]
    public Point CanvasPosition { get; init; }

    public double Distance { get; init; }

    // Convenience typed accessors
    public Node? Node => Target as Node;
    public Edge? Edge => Target as Edge;
    public Port? Port => (Target as (Node, Port, bool))?.Item2;
    public Node? PortOwner => (Target as (Node, Port, bool))?.Item1;
    public bool IsInputPort => Target is (_, _, bool isInput) && isInput;
}

/// <summary>
/// Hit tester for the entire graph surface.
/// Aggregates hit tests across all element types with proper priority.
/// </summary>
public interface IGraphHitTester
{
    /// <summary>
    /// The coordinate space expected for hit test inputs.
    /// </summary>
    CoordinateSpace InputCoordinateSpace { get; }

    /// <summary>
    /// Performs a comprehensive hit test against all graph elements.
    ///
    /// Priority order (first match wins):
    /// 1. Resize handles
    /// 2. Ports
    /// 3. Nodes (front to back by Z-order)
    /// 4. Edges
    /// 5. Canvas (empty space)
    /// </summary>
    /// <param name="position">
    /// Position to test in the coordinate space specified by
    /// <see cref="InputCoordinateSpace"/>.
    /// </param>
    /// <returns>The hit test result.</returns>
    GraphHitTestResult HitTest(Point position);
}
```

---

## 5. Backend Abstraction

### 5.1 Canvas Backend Interface

```csharp
// FlowGraph.Core/Backends/ICanvasBackend.cs
namespace FlowGraph.Core.Backends;

/// <summary>
/// Abstraction for different UI framework backends.
/// Each backend (Avalonia, WPF, MAUI, etc.) implements this.
///
/// Inspired by:
/// - react-diagrams: AbstractFactory, AbstractModelFactory, AbstractReactFactory
/// - mxGraph: mxAbstractCanvas2D with draw primitives
/// </summary>
public interface ICanvasBackend
{
    /// <summary>
    /// Name of this backend for debugging/logging.
    /// </summary>
    string BackendName { get; }

    /// <summary>
    /// Creates a new viewport state instance.
    /// </summary>
    IViewportState CreateViewportState();

    /// <summary>
    /// Creates a render context for the specified viewport.
    /// </summary>
    IRenderContext CreateRenderContext(IViewportState viewport);

    /// <summary>
    /// Creates a hit tester for the graph.
    /// </summary>
    IGraphHitTester CreateHitTester(IReadOnlyViewportState viewport);
}
```

### 5.2 Backend Factory Pattern

```csharp
// FlowGraph.Core/Backends/CanvasBackendFactory.cs
namespace FlowGraph.Core.Backends;

/// <summary>
/// Factory for creating backend-specific implementations.
/// Allows switching backends at runtime or for testing.
/// </summary>
public static class CanvasBackendFactory
{
    private static ICanvasBackend? _defaultBackend;

    /// <summary>
    /// Registers the default backend implementation.
    /// Called by backend assemblies during initialization.
    /// </summary>
    public static void RegisterDefaultBackend(ICanvasBackend backend)
    {
        _defaultBackend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    /// <summary>
    /// Gets the default backend, throwing if none is registered.
    /// </summary>
    public static ICanvasBackend Default =>
        _defaultBackend ?? throw new InvalidOperationException(
            "No canvas backend registered. Ensure a backend assembly " +
            "(e.g., FlowGraph.Avalonia) is loaded and initialized.");
}
```

---

## 6. Migration Path

### 6.1 Current State Analysis

| Component             | Current Behavior   | Target Behavior              |
| --------------------- | ------------------ | ---------------------------- |
| `NodeVisualManager`   | ✅ Canvas coords   | ✅ Keep as-is                |
| `EdgeVisualManager`   | ✅ Canvas coords   | ✅ Keep as-is                |
| `ResizeHandleManager` | ✅ Canvas coords   | ✅ Keep as-is                |
| `ShapeVisualManager`  | ❌ Screen coords   | ⚠️ Fix to canvas coords      |
| `DirectGraphRenderer` | ⚠️ Self-transforms | ✅ Mark as `SelfTransformed` |
| `RenderContext`       | ⚠️ Implicit space  | ✅ Explicit via attributes   |

### 6.2 Phase 1: Documentation & Attributes

1. Add `[CoordinateSpace]` attributes to all existing APIs
2. Update XML docs to explicitly state coordinate spaces
3. Add runtime debug assertions to validate coordinates

### 6.3 Phase 2: Extract Core Interfaces

1. Create `FlowGraph.Abstractions` project (or expand `FlowGraph.Core`)
2. Define platform-agnostic interfaces (`ICoordinateTransformer`, `IRenderContext`, etc.)
3. Keep existing implementations, just have them implement new interfaces

### 6.4 Phase 3: Fix Inconsistencies

1. Fix `ShapeVisualManager` to use canvas coordinates
2. Add `LayerTransformMode` to all render layers
3. Implement `IGraphHitTester` with explicit coordinate space

### 6.5 Phase 4: Backend Abstraction (Future)

1. Extract Avalonia-specific code to `FlowGraph.Avalonia`
2. Define `ICanvasBackend` and implement for Avalonia
3. Create reference implementations for other backends

---

## 7. Patterns from Established Libraries

### 7.1 react-diagrams Pattern Summary

```typescript
// CanvasModel - central viewport state
class CanvasModel {
  offsetX: number; // Pan offset X
  offsetY: number; // Pan offset Y
  zoom: number; // Zoom level
}

// CanvasEngine - coordinate transforms
class CanvasEngine {
  getRelativeMousePoint(event): Point {
    // Converts screen coords to canvas coords
    return {
      x: (event.clientX - model.offsetX) / model.zoom,
      y: (event.clientY - model.offsetY) / model.zoom,
    };
  }
}

// LayerModel - transform participation flag
class LayerModel {
  options: {
    transformed: boolean; // If true, layer follows zoom/pan
  };
}
```

**Key Insight**: Each layer declares `transformed` boolean to opt in/out of viewport transforms.

### 7.2 Konva.js Pattern Summary

```javascript
// Stage - top-level container
stage.getPointerPosition(); // Returns screen coords (no transform)

// Node - any shape/group
node.getRelativePointerPosition(); // Returns coords relative to node

// Hit Testing
stage.getIntersection({ x, y }); // Returns Shape at screen position
```

**Key Insight**: Clear distinction between "pointer position" (screen) and "relative position" (local).

### 7.3 mxGraph Pattern Summary

```javascript
// View manages transforms
mxGraphView {
    scale: number;
    translate: { x, y };

    getGraphBounds(): mxRectangle;  // In canvas coords
    getState(cell): mxCellState;    // Includes both bounds and screen position
}

// CellRenderer handles actual drawing
mxCellRenderer {
    redraw(state, force, rendering);  // State contains both coordinate systems
}
```

**Key Insight**: State objects carry both canvas and screen coordinates, avoiding repeated transforms.

---

## 8. Validation & Debugging

### 8.1 Debug Helpers

```csharp
// FlowGraph.Core/Diagnostics/CoordinateDebug.cs
namespace FlowGraph.Core.Diagnostics;

public static class CoordinateDebug
{
    /// <summary>
    /// Validates that a point is within reasonable bounds for the specified space.
    /// </summary>
    [Conditional("DEBUG")]
    public static void ValidateCoordinate(
        Point point,
        CoordinateSpace expectedSpace,
        IReadOnlyViewportState viewport,
        [CallerMemberName] string? caller = null)
    {
        var screenSize = viewport.ViewSize;

        if (expectedSpace == CoordinateSpace.Screen)
        {
            // Screen coords should generally be positive and within view bounds
            // (with some tolerance for drag operations)
            if (point.X < -1000 || point.Y < -1000 ||
                point.X > screenSize.Width + 1000 || point.Y > screenSize.Height + 1000)
            {
                Debug.WriteLine($"[CoordWarn] {caller}: Suspicious screen coordinate {point}, " +
                               $"view size is {screenSize}. Did you mean canvas coords?");
            }
        }
        else if (expectedSpace == CoordinateSpace.Canvas)
        {
            // Canvas coords at current zoom should map roughly to screen
            var screenPoint = viewport.CanvasToScreen(point);
            if (screenPoint.X < -10000 || screenPoint.Y < -10000 ||
                screenPoint.X > screenSize.Width + 10000 || screenPoint.Y > screenSize.Height + 10000)
            {
                Debug.WriteLine($"[CoordWarn] {caller}: Canvas point {point} maps to " +
                               $"far-off-screen {screenPoint}. Is this intentional?");
            }
        }
    }
}
```

---

## 9. Summary

| Contract                 | Purpose                            | Key Design Decision                                                       |
| ------------------------ | ---------------------------------- | ------------------------------------------------------------------------- |
| `ICoordinateTransformer` | Bidirectional coord transforms     | Explicit methods: `ScreenToCanvas`, `CanvasToScreen`, plus delta variants |
| `IViewportState`         | Zoom/pan state management          | Mutable interface extends read-only interface                             |
| `IRenderLayer`           | Layer transform participation      | `LayerTransformMode` enum declares behavior                               |
| `IElementRenderer<T, V>` | Backend-agnostic element rendering | Declares `InputCoordinateSpace`                                           |
| `IHitTestable<T>`        | Hit testing contract               | Declares `HitTestCoordinateSpace`                                         |
| `IGraphHitTester`        | Composite hit testing              | Priority-ordered, explicit coordinate space                               |
| `ICanvasBackend`         | Backend abstraction                | Factory pattern for cross-platform                                        |

The key insight from established libraries: **Explicit is better than implicit**. Every component that touches coordinates should declare which space it operates in, and the framework should provide clear transforms between spaces.
