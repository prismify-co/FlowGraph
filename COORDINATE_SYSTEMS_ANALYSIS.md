# Coordinate System Transformation Issues - Root Cause Analysis

## Problem Summary

Inconsistent coordinate transformations between user clicks and actual element positions on the canvas, particularly affecting shape (comment) selection in retained rendering mode.

## Root Causes

### 1. **Multiple Overlapping Coordinate Systems**

The codebase has **4 distinct coordinate systems** that are used inconsistently:

1. **Screen/Window Coordinates** - Browser/OS window space
2. **RootPanel Coordinates** - Top-level untransformed panel (viewport space)
3. **MainCanvas Coordinates** - Canvas with MatrixTransform applied
4. **Logical Canvas Coordinates** - The actual graph element positions

```
Window/Screen
    └── RootPanel (viewport space, no transform)
        └── MainCanvas (canvas space, WITH MatrixTransform)
            └── Elements (positioned in logical canvas coordinates)
```

### 2. **Dual Rendering Modes with Different Coordinate Handling**

#### **Direct Rendering Mode** (>100 elements)

- Elements drawn directly to canvas via Skia
- No visual tree, no automatic transforms
- **Must use**: `e.GetPosition(_rootPanel)` → manual `ViewportToCanvas()` conversion
- Coordinate flow: Screen → Viewport → Manual Transform → Canvas

#### **Retained/Visual Tree Mode** (<100 elements)

- Elements are actual Avalonia controls in visual tree
- MatrixTransform on MainCanvas handles viewport
- **Must use**: `e.GetPosition(_mainCanvas)` → automatic inverse transform
- Coordinate flow: Screen → **Avalonia applies inverse transform automatically** → Canvas

### 3. **The MatrixTransform Confusion**

The MainCanvas has a `RenderTransform` that applies:

```csharp
// Forward transform (canvas → viewport)
x' = x * zoom + offsetX
y' = y * zoom + offsetY
```

**Critical misunderstanding**: When you call `e.GetPosition(_mainCanvas)`, Avalonia **automatically applies the INVERSE transform**:

```csharp
// Inverse transform (applied by Avalonia when using GetPosition(_mainCanvas))
x = (x' - offsetX) / zoom
y = (y' - offsetY) / zoom
```

### 4. **Inconsistent API Usage Patterns**

The codebase mixes these patterns inconsistently:

**Pattern A** (Correct for Direct Rendering):

```csharp
var screenPos = e.GetPosition(_rootPanel);           // Viewport coords
var canvasPos = _viewport.ViewportToCanvas(screenPos); // Manual transform
```

**Pattern B** (Correct for Retained Rendering):

```csharp
var canvasPos = e.GetPosition(_mainCanvas);  // Already canvas coords (auto-transformed)
```

**Pattern C** (WRONG - what we had for shapes):

```csharp
var screenPos = e.GetPosition(_rootPanel);             // Viewport coords
var canvasPos = _viewport.ViewportToCanvas(screenPos); // Manual transform
// But in retained mode, this gives WRONG coords because
// it doesn't account for how Avalonia's GetPosition works with transforms!
```

## Specific Issues Found

### Issue 1: Shape Hit Testing (Fixed in commit 801c774)

**Before:**

```csharp
// FlowCanvas.Input.cs - OnRootPanelPointerPressed
var screenPosRaw = e.GetPosition(_rootPanel);
var canvasPos = _viewport.ViewportToCanvas(screenPosRaw);
// Used canvasPos for shape bounds checking
```

**Problem**: In retained mode, this double-transforms the coordinates:

1. `GetPosition(_rootPanel)` gives viewport coords
2. Manual `ViewportToCanvas()` applies transform
3. But shapes are positioned using `Canvas.SetLeft/Top` which means they're transformed again by MainCanvas's RenderTransform
4. Result: Click at (140, 110) → calculated as canvas (-20, -26) → misses shape at canvas (50, 30)

**After:**

```csharp
var canvasPos = e.GetPosition(_mainCanvas);
// Avalonia automatically applies inverse transform → correct canvas coords
```

**Why this works**: When you click at screen (140, 110) near a shape at canvas (50, 30):

- Shape is rendered at screen: `50 * 1.0 + 160 = 210` (but you're clicking at 140!)
- Actually, the shape VISUAL is at canvas (50, 30) in the transformed space
- `GetPosition(_mainCanvas)` gives you the canvas coords directly, accounting for the transform
- Click at screen (140, 110) → `GetPosition(_mainCanvas)` → canvas (73, 102) → HITS shape at (50-270, 30-170)

### Issue 2: Box Selection Ignored Shapes (Fixed in commit 801c774)

**Before:**

```csharp
// BoxSelectingState.UpdateSelection
foreach (var node in graph.Elements.Nodes) { ... }
// No loop for shapes!
```

**Problem**: Box selection only checked nodes, completely ignored shapes.

**After**: Added shape selection loop with proper `ShapeVisualManager.UpdateSelection()` calls.

### Issue 3: Context Menu Coordinate Handling (Still exists)

In `HandleContextMenuRequest`:

```csharp
var screenPos = e.GetPosition(_rootPanel);
var canvasPos = _viewport.ViewportToCanvas(screenPos);
```

This works for Direct Rendering but is **potentially wrong for retained mode** - should be:

```csharp
var canvasPos = _useDirectRendering
    ? _viewport.ViewportToCanvas(e.GetPosition(_rootPanel))
    : e.GetPosition(_mainCanvas);
```

## Architectural Issues

### 1. **No Clear Abstraction Layer**

The code has `InputCoordinatesAdapter` which tries to abstract this:

```csharp
public CanvasPoint GetPointerCanvasPosition(PointerEventArgs e)
{
    if (IsDirectRenderingMode && _rootPanel != null)
    {
        var viewportPos = e.GetPosition(_rootPanel);
        var canvasPos = _viewport.ViewportToCanvas(viewportPos);
        return new CanvasPoint(canvasPos.X, canvasPos.Y);
    }
    else if (_mainCanvas != null)
    {
        var canvasPos = e.GetPosition(_mainCanvas);
        return new CanvasPoint(canvasPos.X, canvasPos.Y);
    }
}
```

**But**: This abstraction is **not used consistently**. Many places still do manual coordinate conversion.

### 2. **Inconsistent Terminology**

The codebase uses these terms interchangeably:

- "Screen" vs "Viewport" (sometimes mean the same thing)
- "Canvas" vs "Logical Canvas" (not clearly distinguished)
- `ScreenToCanvas` vs `ViewportToCanvas` (both do the same thing)

### 3. **Transform Mode Not Always Available**

Code that needs to choose the right approach often doesn't have access to `_useDirectRendering`:

```csharp
// In some input states, we don't know which mode we're in!
var canvasPos = ??? // Should we use _rootPanel or _mainCanvas?
```

## Recommended Fixes

### Short Term (Patches)

1. ✅ **DONE**: Fix shape hit testing to use `GetPosition(_mainCanvas)` in retained mode
2. ✅ **DONE**: Add shape selection to box selection
3. **TODO**: Fix context menu coordinate handling
4. **TODO**: Audit all `GetPosition` calls for correctness

### Medium Term (Refactoring)

1. **Enforce use of `InputCoordinatesAdapter`**: Make it the ONLY way to get coordinates

   ```csharp
   // Everywhere in input handling:
   var canvasPos = context.Coordinates.GetPointerCanvasPosition(e);
   ```

2. **Remove direct `GetPosition` calls**: Ban calling `e.GetPosition()` directly outside of the adapter

3. **Centralize mode detection**: Put `_useDirectRendering` in `InputStateContext` so all states have access

### Long Term (Architecture)

1. **Single Source of Truth**: Create a `CoordinateSystem` class that owns all transformations

   ```csharp
   public class CoordinateSystem
   {
       public Point PointerToCanvas(PointerEventArgs e);
       public Point PointerToViewport(PointerEventArgs e);
       public Point CanvasToViewport(Point canvas);
       public Point ViewportToCanvas(Point viewport);
   }
   ```

2. **Type Safety**: Use distinct types for each coordinate space

   ```csharp
   public readonly struct CanvasPoint { public double X, Y; }
   public readonly struct ViewportPoint { public double X, Y; }
   // Compiler prevents mixing them up!
   ```

3. **Eliminate Manual Transforms**: All coordinate conversions go through the abstraction layer

4. **Document Transform Chain**: Every rendering path should document its coordinate flow:
   ```
   Input Event → Viewport Space → Canvas Space → Element Logic
   ```

## Why This Is Hard to Get Right

1. **Avalonia's implicit behavior**: `GetPosition(_mainCanvas)` silently applies inverse transform
2. **Mode-dependent behavior**: Same API call means different things in different modes
3. **Historical code**: Mix of old patterns (manual transform) and new patterns (abstraction)
4. **Performance pressure**: Direct rendering mode needed for large graphs, forcing dual code paths
5. **Leaky abstraction**: MatrixTransform is UI framework detail that leaks into business logic

## Key Takeaway

The fundamental issue is **dual rendering modes with fundamentally different coordinate handling**, combined with **insufficient abstraction** to hide this complexity from input handling code.

**The correct pattern going forward:**

```csharp
// ALWAYS use the abstraction
var canvasPos = context.Coordinates.GetPointerCanvasPosition(e);

// NEVER do this manually
var bad = _viewport.ViewportToCanvas(e.GetPosition(_rootPanel)); // ❌
```
