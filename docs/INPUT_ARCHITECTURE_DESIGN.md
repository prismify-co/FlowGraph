# FlowGraph Input Architecture Design

## Executive Summary

This document proposes a **hybrid input architecture** that combines:

- **InputProcessor pattern** (Nodify-inspired) as the core state machine
- **Behavior pattern** (Blazor.Diagrams-inspired) as an optional extensibility layer

The goal is to eliminate the fragile pattern-matching in `IdleState` while maintaining the clean state machine approach and enabling user-defined behaviors.

---

## Problem Analysis

### Current Architecture (What We Have)

```
FlowCanvas.Input.cs                 InputStateMachine
       │                                   │
       │ OnRootPanelPointerPressed()       │
       │                                   │
       ├─► Hit test (direct or visual)     │
       │                                   │
       │   Returns: Control with Tag       │
       │                                   │
       └─► _inputStateMachine              │
              .HandlePointerPressed(e, hitElement)
                       │
                       ▼
               IdleState.HandlePointerPressed()
                       │
         ┌─────────────┼─────────────┐
         │             │             │
    Pattern Match on Tag:            │
    ─ Node?           ─ Edge?        │
    ─ (Node,Port,bool)? ─ (Node,ResizeHandlePosition)?
    ─ (ShapeElement,ResizeHandlePosition)?
    ─ ShapeElement?
```

**The Problem:** Every new element type or sub-element requires:

1. New pattern in `IdleState`
2. New handler method
3. Easy to forget (the resize handle bug we just fixed)

### Why Pattern Matching is Fragile

```csharp
// IdleState.cs - Current approach (fragile)
if (source?.Tag is (Node resizeNode, ResizeHandlePosition handlePos))
    return HandleResizeHandleClick(...);

if (source?.Tag is (ShapeElement resizeShape, ResizeHandlePosition shapeHandlePos))  // <-- EASY TO FORGET!
    return HandleShapeResizeHandleClick(...);
```

Each element type needs its own pattern. Miss one = silent failure (falls through to canvas click).

---

## Proposed Architecture

### Layer 1: Core State Machine (InputProcessor Pattern)

The state machine remains the core orchestrator, but instead of pattern matching on Tags, it dispatches to **registered InputProcessors**.

```
                    ┌─────────────────────────────────────────────┐
                    │              InputStateMachine               │
                    │  ┌─────────────────────────────────────┐    │
                    │  │         InputDispatcher              │    │
                    │  │                                      │    │
                    │  │  HitTest() ──► GraphHitTestResult    │    │
                    │  │       │                              │    │
                    │  │       ▼                              │    │
                    │  │  Route to InputProcessor             │    │
                    │  │       │                              │    │
                    │  │       ▼                              │    │
                    │  │  Processor handles OR               │    │
                    │  │  Processor transitions state         │    │
                    │  └─────────────────────────────────────┘    │
                    │                                             │
                    │  States: Idle, Dragging, Panning, etc.     │
                    └─────────────────────────────────────────────┘
```

### Layer 2: InputProcessors (Per-Element Handlers)

Each element type has a dedicated processor that knows how to handle its input:

```csharp
// FlowGraph.Avalonia/Input/Processors/IInputProcessor.cs
public interface IInputProcessor
{
    /// <summary>
    /// The hit target type(s) this processor handles.
    /// </summary>
    IReadOnlyList<HitTargetType> HandledTypes { get; }

    /// <summary>
    /// Priority for handling. Higher = checked first.
    /// Resize handles > Ports > Nodes > Edges > Canvas
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Handles pointer pressed on this element type.
    /// </summary>
    InputProcessorResult HandlePointerPressed(
        InputStateContext context,
        GraphHitTestResult hitResult,
        PointerPressedEventArgs e);

    // Similar for Move, Release, etc.
}

public record InputProcessorResult(
    IInputState? NewState,      // Transition to new state?
    bool Handled,               // Stop propagation?
    bool ContinueToNext = false // Also let next processor handle?
);
```

### Layer 3: Behaviors (Optional Extensibility)

Behaviors are user-facing extensions that can observe and optionally intercept input:

```csharp
// FlowGraph.Core/Input/IBehavior.cs (framework-agnostic)
public interface IBehavior
{
    /// <summary>
    /// Display name for debugging/UI.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this behavior is currently active.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Called before standard processing. Can intercept.
    /// </summary>
    BehaviorResult OnPointerPressed(IGraphInputEvent e);

    /// <summary>
    /// Called after standard processing completes.
    /// </summary>
    void OnPointerPressedComplete(IGraphInputEvent e);
}

public record BehaviorResult(bool Handled, bool SuppressDefault = false);
```

---

## Detailed Design

### 1. GraphHitTestResult Integration

The existing `IGraphHitTester` + `GraphHitTestResult` in FlowGraph.Core is **perfect** for this:

```csharp
// Already exists in FlowGraph.Core/Input/IGraphHitTester.cs
public class GraphHitTestResult
{
    public HitTargetType TargetType { get; init; }
    public object? Target { get; init; }
    public Point CanvasPosition { get; init; }

    // Typed accessors
    public Node? Node => ...;
    public Edge? Edge => ...;
    public Port? Port => ...;
    public ResizeHandlePosition? ResizeHandle => ...;
}
```

**This eliminates pattern matching!** The hit test result already knows what type was hit.

### 2. InputDispatcher (New)

Replaces the pattern matching in IdleState:

```csharp
// FlowGraph.Avalonia/Input/InputDispatcher.cs
public class InputDispatcher
{
    private readonly IGraphHitTester _hitTester;
    private readonly List<IInputProcessor> _processors;
    private readonly List<IBehavior> _behaviors;

    public InputDispatcher(IGraphHitTester hitTester)
    {
        _hitTester = hitTester;
        _processors = new List<IInputProcessor>();
        _behaviors = new List<IBehavior>();

        // Register built-in processors in priority order
        RegisterProcessor(new ResizeHandleProcessor());  // Priority: 100
        RegisterProcessor(new PortProcessor());           // Priority: 90
        RegisterProcessor(new NodeProcessor());           // Priority: 80
        RegisterProcessor(new EdgeProcessor());           // Priority: 70
        RegisterProcessor(new ShapeProcessor());          // Priority: 60
        RegisterProcessor(new CanvasProcessor());         // Priority: 0
    }

    public StateTransitionResult Dispatch(
        InputStateContext context,
        PointerPressedEventArgs e,
        Point canvasPosition)
    {
        // 1. Hit test using framework-agnostic interface
        var hitResult = _hitTester.HitTest(canvasPosition);

        // 2. Let behaviors intercept first
        foreach (var behavior in _behaviors.Where(b => b.IsEnabled))
        {
            var behaviorResult = behavior.OnPointerPressed(
                new GraphInputEvent(hitResult, e.KeyModifiers));

            if (behaviorResult.SuppressDefault)
                return StateTransitionResult.Stay(behaviorResult.Handled);
        }

        // 3. Find processor for this hit type
        var processor = _processors
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault(p => p.HandledTypes.Contains(hitResult.TargetType));

        if (processor == null)
            return StateTransitionResult.Unhandled();

        // 4. Let processor handle
        var result = processor.HandlePointerPressed(context, hitResult, e);

        // 5. Notify behaviors of completion
        foreach (var behavior in _behaviors.Where(b => b.IsEnabled))
        {
            behavior.OnPointerPressedComplete(
                new GraphInputEvent(hitResult, e.KeyModifiers));
        }

        return new StateTransitionResult(result.NewState, result.Handled);
    }
}
```

### 3. Built-in InputProcessors

Each processor is self-contained and focused:

```csharp
// FlowGraph.Avalonia/Input/Processors/NodeProcessor.cs
public class NodeProcessor : IInputProcessor
{
    public IReadOnlyList<HitTargetType> HandledTypes => new[] { HitTargetType.Node };
    public int Priority => 80;

    public InputProcessorResult HandlePointerPressed(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerPressedEventArgs e)
    {
        var node = hit.Node!;  // Safe - we only handle Node hits
        var graph = context.Graph;

        if (e.ClickCount == 2 && context.Settings.EnableNodeLabelEditing)
        {
            // Double-click: edit label
            context.RaiseNodeLabelEditRequested(node, hit.CanvasPosition);
            return new InputProcessorResult(null, Handled: true);
        }

        // Handle selection
        HandleSelection(context, node, e.KeyModifiers);

        // Start drag if selected and draggable
        if (node.IsSelected && node.IsDraggable && !context.Settings.IsReadOnly)
        {
            var dragState = new DraggingState(graph, ...);
            return new InputProcessorResult(dragState, Handled: true);
        }

        return new InputProcessorResult(null, Handled: true);
    }
}
```

```csharp
// FlowGraph.Avalonia/Input/Processors/ResizeHandleProcessor.cs
public class ResizeHandleProcessor : IInputProcessor
{
    // Handles BOTH node and shape resize handles!
    public IReadOnlyList<HitTargetType> HandledTypes => new[] { HitTargetType.ResizeHandle };
    public int Priority => 100;  // Highest - always check first

    public InputProcessorResult HandlePointerPressed(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerPressedEventArgs e)
    {
        if (context.Settings.IsReadOnly)
            return new InputProcessorResult(null, Handled: false);

        var handlePosition = hit.ResizeHandle!.Value;

        // Check if it's a node or shape resize
        if (hit.ResizeHandleOwner != null)
        {
            // Node resize
            var node = hit.ResizeHandleOwner;
            var state = new ResizingState(node, handlePosition, ...);
            return new InputProcessorResult(state, Handled: true);
        }

        // Shape resize - need to extend GraphHitTestResult for shapes
        if (hit.Target is ShapeResizeHandleHitInfo shapeInfo)
        {
            var state = new ResizingShapeState(shapeInfo.Shape, handlePosition, ...);
            return new InputProcessorResult(state, Handled: true);
        }

        return new InputProcessorResult(null, Handled: false);
    }
}
```

### 4. Example Behaviors (User Extensibility)

```csharp
// Example: Custom behavior for multi-select with marquee
public class MarqueeSelectionBehavior : IBehavior
{
    public string Name => "Marquee Selection";
    public bool IsEnabled { get; set; } = true;

    public BehaviorResult OnPointerPressed(IGraphInputEvent e)
    {
        // Only intercept Alt+Click on canvas
        if (e.HitResult.IsCanvasHit && e.Modifiers.HasFlag(KeyModifiers.Alt))
        {
            // Start custom marquee mode
            StartMarquee(e.CanvasPosition);
            return new BehaviorResult(Handled: true, SuppressDefault: true);
        }

        return new BehaviorResult(Handled: false);
    }
}

// Example: Behavior that adds snap-to-grid feedback
public class SnapToGridBehavior : IBehavior
{
    public string Name => "Snap to Grid";
    public bool IsEnabled { get; set; } = true;

    public BehaviorResult OnPointerPressed(IGraphInputEvent e)
    {
        // Don't intercept, just observe
        return new BehaviorResult(Handled: false);
    }

    public void OnPointerPressedComplete(IGraphInputEvent e)
    {
        // After node click, we might want to show snap guides
        if (e.HitResult.Node != null)
        {
            ShowSnapGuides(e.HitResult.Node);
        }
    }
}
```

---

## Migration Strategy

### Phase 1: Add Infrastructure (Non-Breaking)

1. Create `IInputProcessor` interface
2. Create `InputDispatcher` class
3. Create processor implementations that mirror current behavior
4. Wire up alongside existing code (A/B testable)

### Phase 2: Migrate IdleState

1. Replace pattern matching in `IdleState.HandlePointerPressed` with dispatcher
2. Keep all existing state classes (DraggingState, etc.)
3. Test thoroughly

### Phase 3: Add Behavior Support

1. Create `IBehavior` interface in FlowGraph.Core
2. Create `IGraphInputEvent` abstraction
3. Add behavior registration to InputDispatcher
4. Document extensibility API

### Phase 4: Cleanup

1. Remove deprecated pattern matching code
2. Document migration guide for custom extensions
3. Update existing states to use hit results directly

---

## Benefits of This Design

| Aspect                      | Current               | Proposed                       |
| --------------------------- | --------------------- | ------------------------------ |
| **Adding new element type** | Add pattern + handler | Implement IInputProcessor      |
| **Forgetting a pattern**    | Silent failure        | Won't compile (must register)  |
| **Custom user behaviors**   | Subclass states       | Add IBehavior                  |
| **Testing**                 | Mock entire states    | Mock individual processors     |
| **Hit testing**             | Duplicated in states  | Centralized in IGraphHitTester |

---

## Performance Optimizations (Gemini Review)

Based on code review feedback, the following optimizations have been implemented:

### 1. HitTargetType Bitmask ([Flags] Enum)

```csharp
[Flags]
public enum HitTargetType
{
    None = 0,
    Canvas = 1 << 0,
    Node = 1 << 1,
    Edge = 1 << 2,
    Port = 1 << 3,
    ResizeHandle = 1 << 4,
    // etc.

    // Common combinations for processor registration
    All = Canvas | Node | Edge | Port | ...,
    Draggable = Node | Shape | Group,
    Selectable = Node | Edge | Shape | Group
}
```

**Benefit:** O(1) type checking via bitwise AND instead of O(N) list iteration:

```csharp
// Before: O(N) list iteration
processor.HandledTypes.Contains(hitResult.TargetType)

// After: O(1) bitmask check
(processor.HandledTypes & hitResult.TargetType) != 0
```

### 2. Pre-computed Processor Lookup

```csharp
public class InputDispatcher
{
    // Sorted once on registration
    private readonly List<IInputProcessor> _processors;

    // O(1) lookup by target type
    private readonly Dictionary<HitTargetType, IInputProcessor> _processorLookup;

    private void RebuildLookupIfNeeded()
    {
        // Map each HitTargetType to its highest-priority processor
        foreach (HitTargetType type in Enum.GetValues<HitTargetType>())
        {
            var processor = _processors.FirstOrDefault(p => p.CanHandle(type));
            if (processor != null)
                _processorLookup[type] = processor;
        }
    }
}
```

**Benefit:** Sort once on registration, O(1) dispatch instead of O(N) search.

### 3. State-Aware Behavior Filtering

```csharp
public interface IBehavior
{
    /// <summary>
    /// The input state names this behavior is active in.
    /// If null/empty, active in all states.
    /// </summary>
    IReadOnlySet<string>? ActiveInStates { get; }

    bool IsActiveInState(string stateName) =>
        ActiveInStates == null || ActiveInStates.Contains(stateName);
}
```

**Benefit:** Behaviors that are only relevant in specific states (e.g., selection box during Idle)
are skipped entirely in other states, avoiding unnecessary iteration on high-frequency events.

### 4. Pointer Capture for Drag Operations

When a processor starts a drag operation, it **MUST** capture the pointer:

```csharp
e.Pointer.Capture(context.RootPanel);
```

**Why:** If the user moves the mouse faster than the frame rate, hit tests might
return "Canvas" instead of the element being dragged. Pointer capture ensures
the interaction state continues receiving events regardless of visual bounds.

---

## Open Questions

1. **Should Behaviors be in Core or Avalonia?**
   - Core: More reusable, but needs framework-agnostic event types
   - Avalonia: Simpler, but ties users to Avalonia

2. **How to handle processor conflicts?**
   - Priority ordering (current proposal)
   - Explicit chain-of-responsibility
   - Composite pattern

3. **Should processors own their states?**
   - Nodify: Yes (NodeInputProcessor has NodeIdleState, NodeDraggingState)
   - Simpler: No, shared state pool (current approach)

---

## Next Steps

1. Review this design
2. Decide on open questions
3. Create skeleton interfaces
4. Implement Phase 1 alongside existing code
5. A/B test before committing to migration
