# Pan Performance Analysis

## Problem

Panning is "passing grade but noticeable in terms of not so smooth" from 100-5000 nodes. User expects "buttery smoothness."

## Root Cause Identified

### Issue 1: Grid Re-Rendering on Every Pan

**Location**: `ApplyViewportTransforms()` in FlowCanvas.axaml.cs (lines 1013-1023)

```csharp
else if (offsetChanged)
{
    // Pan-only change - update transform and grid
    _lastOffsetX = _viewport.OffsetX;
    _lastOffsetY = _viewport.OffsetY;

    // Grid background is on separate untransformed canvas, must re-render
    RenderGrid();  // <-- PROBLEM: Called on EVERY pan!

    // Update custom background renderers (they render to GridCanvas which has no transform)
    RenderCustomBackgrounds();  // <-- PROBLEM: Also called on every pan!
}
```

**Why This is Problematic**:

- `RenderGrid()` is called on **every single pan movement**
- `GridRenderer` already has smart transform-based panning with padding
- It only needs to re-render when zoom changes or pan exceeds padding bounds
- But we're forcing it to render on every offset change!

The `GridRenderer` itself is optimized:

- Uses `TranslateTransform` for O(1) panning
- Only calls `ForceFullRender()` when truly needed (zoom change or padding exceeded)
- Has 200px padding to allow smooth panning without re-render

**But** we're bypassing this optimization by calling `RenderGrid()` unconditionally on every pan!

### Issue 2: Custom Background Renderers on Every Pan

Same issue - `RenderCustomBackgrounds()` is called on every pan, even though most custom backgrounds should use transforms.

## Performance Impact

**Current Behavior (every pan frame)**:

1. PanningState updates offset → `SetOffset()`
2. ViewportState.SetOffset() → `OnViewportChanged()`
3. OnViewportStateChanged() → `ApplyViewportTransforms()`
4. ApplyViewportTransforms() detects offsetChanged → `RenderGrid()`
5. GridRenderer.Render() → Re-calculates all dot positions → DrawingContext draws 100s of dots
6. RenderCustomBackgrounds() → Iterates all custom renderers

This happens **60+ times per second** during smooth panning!

**Expected Behavior**:

1. PanningState updates offset → `SetOffset()`
2. ViewportState.SetOffset() → `OnViewportChanged()`
3. OnViewportStateChanged() → `ApplyViewportTransforms()`
4. Apply matrix transform to MainCanvas ✓
5. Apply InvalidateVisual to DirectRenderer ✓
6. **Grid just updates its TranslateTransform** (O(1), no re-render) ✓
7. Custom background renderers use their transforms (if applicable)

## Solution

Remove the unconditional `RenderGrid()` and `RenderCustomBackgrounds()` calls from the pan-only branch.

The `GridRenderer.Render()` method is already called via:

- Initial setup in `OnGraphChanged()`
- Viewport subscription in `GridRenderer.Render()`
- Grid's own `InvalidateVisual()` when needed

### Changes Needed

**File**: `FlowGraph.Avalonia/FlowCanvas.axaml.cs`

**Before** (lines 1013-1023):

```csharp
else if (offsetChanged)
{
    // Pan-only change - update transform and grid
    _lastOffsetX = _viewport.OffsetX;
    _lastOffsetY = _viewport.OffsetY;

    // Grid background is on separate untransformed canvas, must re-render
    RenderGrid();

    // Update custom background renderers (they render to GridCanvas which has no transform)
    RenderCustomBackgrounds();
}
```

**After**:

```csharp
else if (offsetChanged)
{
    // Pan-only change - just update the stored offset values
    // Grid and custom backgrounds handle their own transforms/updates
    _lastOffsetX = _viewport.OffsetX;
    _lastOffsetY = _viewport.OffsetY;
}
```

## Why GridRenderer Doesn't Need Explicit Calls

Looking at `GridRenderer.Render()` (lines 100-120):

1. Grid has viewport subscription via its own `Render()` method
2. Grid already uses `TranslateTransform` for O(1) panning
3. Grid only calls `ForceFullRender()` → `InvalidateVisual()` when needed:
   - Zoom changed
   - Pan exceeded 200px padding
   - Size changed

The grid control is smart enough to handle its own updates!

## Expected Performance Gain

**Before**: 60+ grid re-renders per second during pan
**After**: 0 grid re-renders during smooth pan (until padding exceeded)

This should provide the "buttery smooth" panning the user expects.
