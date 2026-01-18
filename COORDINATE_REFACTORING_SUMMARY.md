# Coordinate Transformation Refactoring Summary

**Date:** January 18, 2026  
**Issue:** Inconsistent coordinate transformations causing shape selection bugs  
**Solution:** Consistently use InputCoordinatesAdapter abstraction layer

## Background

The coordinate transformation bugs stemmed from bypassing the existing `InputCoordinatesAdapter` abstraction and doing manual coordinate transformations in `FlowCanvas.Input.cs`. This led to:

1. **Double transformation bug** in retained mode: Using `e.GetPosition(_rootPanel)` + manual `ViewportToCanvas()` didn't account for Avalonia's automatic inverse transform application
2. **Inconsistent patterns**: Mix of manual and abstracted coordinate handling
3. **Confusion about coordinate spaces**: Unclear which coordinate system was being used where

## Key Learning from Nodify Library

Investigation of the Nodify-Avalonia port revealed their approach:

```cs
// Nodify's pattern - ALWAYS use the transformed element
protected override void OnPointerMoved(PointerEventArgs e)
{
    MouseLocation = e.GetPosition(ItemsHost);  // ItemsHost has ViewportTransform applied
    ...
}
```

**Key Insight:** Avalonia **automatically applies the inverse transform** when you call `GetPosition()` on a transformed element. Don't try to manually transform coordinates.

## Architecture Before Refactoring

```
FlowCanvas.Input.cs
├── RIGHT ✓ InputStateBase uses InputCoordinatesAdapter
├── WRONG ✗ Direct calls to e.GetPosition(_rootPanel) + manual ViewportToCanvas()
├── WRONG ✗ Mix of viewport and canvas coordinates without clear documentation
└── WRONG ✗ Context menu handling bypassed abstraction layer
```

## Changes Made

### 1. Context Menu Coordinate Handling
**Before:**
```cs
var screenPos = e.GetPosition(_rootPanel);
var canvasPos = _viewport.ViewportToCanvas(screenPos);
var canvasPoint = new Core.Point(canvasPos.X, canvasPos.Y);
```

**After:**
```cs
// Use the coordinate adapter to get canvas position - it handles both rendering modes
var canvasPos = _inputContext.Coordinates.GetPointerCanvasPosition(e);
var canvasPoint = new Core.Point(canvasPos.X, canvasPos.Y);
```

### 2. Hit Testing Coordinate Handling
**Before:**
```cs
if (_useDirectRendering && _directRenderer != null)
{
    hitElement = PerformDirectRenderingHitTest(screenPos.X, screenPos.Y);
}
else
{
    var canvasPosForHit = _rootPanel != null && _mainCanvas != null
        ? e.GetPosition(_mainCanvas)
        : screenPos;
    hitElement = _mainCanvas?.InputHitTest(canvasPosForHit) as Control;
}
```

**After:**
```cs
if (_useDirectRendering && _directRenderer != null)
{
    // Get viewport position for DirectRenderer
    var viewportPos = _inputContext.Coordinates.GetPointerViewportPosition(e);
    hitElement = PerformDirectRenderingHitTest(viewportPos.X, viewportPos.Y);
}
else
{
    // In visual tree mode, get canvas position for hit testing
    // GetPosition(mainCanvas) automatically applies inverse transform
    var canvasPosForHit = _mainCanvas != null
        ? e.GetPosition(_mainCanvas)
        : default;
    hitElement = _mainCanvas?.InputHitTest(canvasPosForHit) as Control;
}
```

### 3. Hover Detection Coordinate Handling
**Before:**
```cs
var screenPos = e.GetPosition(_rootPanel);
var resizeHit = _directRenderer.HitTestResizeHandle(screenPos.X, screenPos.Y);
```

**After:**
```cs
// Get viewport position for DirectRenderer - it expects screen coordinates
var viewportPos = _inputContext.Coordinates.GetPointerViewportPosition(e);
var resizeHit = _directRenderer.HitTestResizeHandle(viewportPos.X, viewportPos.Y);
```

### 4. Pointer Pressed Coordinate Handling
**Before:**
```cs
var screenPos = e.GetPosition(_rootPanel);
var rightClickHit = PerformDirectRenderingHitTest(screenPos.X, screenPos.Y);
```

**After:**
```cs
var viewportPos = _inputContext.Coordinates.GetPointerViewportPosition(e);
var rightClickHit = PerformDirectRenderingHitTest(viewportPos.X, viewportPos.Y);
```

## Architecture After Refactoring

```
FlowCanvas.Input.cs
├── ✓ All coordinate conversions use InputCoordinatesAdapter
├── ✓ Clear separation: GetPointerCanvasPosition() vs GetPointerViewportPosition()
├── ✓ DirectRenderer always receives viewport coordinates
├── ✓ Visual tree always uses e.GetPosition(_mainCanvas) for canvas coordinates
└── ✓ No manual ViewportToCanvas() calls outside of abstraction layer
```

## Benefits

1. **Consistency**: All coordinate transformations go through one abstraction layer
2. **Clarity**: Method names clearly indicate coordinate space (Canvas vs Viewport)
3. **Maintainability**: Future changes to coordinate handling only need to update InputCoordinatesAdapter
4. **Correctness**: Leverages Avalonia's automatic inverse transform application
5. **Mode Transparency**: Input states don't need to know about rendering modes

## How InputCoordinatesAdapter Works

```cs
public CanvasPoint GetPointerCanvasPosition(PointerEventArgs e)
{
    if (IsDirectRenderingMode && _rootPanel != null)
    {
        // Direct mode: Get viewport coords, then transform to canvas
        var viewportPos = e.GetPosition(_rootPanel);
        var canvasPos = _viewport.ViewportToCanvas(viewportPos);
        return new CanvasPoint(canvasPos.X, canvasPos.Y);
    }
    else if (_mainCanvas != null)
    {
        // Visual tree mode: GetPosition(mainCanvas) auto-applies inverse transform
        var canvasPos = e.GetPosition(_mainCanvas);
        return new CanvasPoint(canvasPos.X, canvasPos.Y);
    }
    return CanvasPoint.Zero;
}
```

## Testing Verification

- ✅ Build succeeds with no errors
- ✅ Shape selection works in retained mode (original fix preserved)
- ✅ Box selection works for shapes
- ✅ Context menu coordinates are correct
- ✅ Hover detection works in direct rendering mode

## Related Documentation

- [COORDINATE_SYSTEMS_ANALYSIS.md](./COORDINATE_SYSTEMS_ANALYSIS.md) - Root cause analysis
- Commit 801c774 - Original shape selection fix
- Nodify-Avalonia reference: https://github.com/BAndysc/nodify-avalonia

## Future Recommendations

1. Consider adding coordinate type wrappers (ViewportPoint, CanvasPoint) to prevent mix-ups at compile time
2. Document the coordinate systems in InputCoordinatesAdapter XML comments
3. Add unit tests for coordinate transformations
4. Consider renaming "screen coordinates" to "viewport coordinates" for consistency
