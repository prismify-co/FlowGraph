using Avalonia.Input;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using CoreMouseButton = FlowGraph.Core.Input.MouseButton;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Coordinates input handling between behaviors, processors, and the state machine.
/// </summary>
/// <remarks>
/// <para>
/// The InputDispatcher is the central coordinator for the hybrid input architecture:
/// </para>
/// <code>
///                   Input Event (PointerPressed, etc.)
///                              │
///                              ▼
///                 ┌───────────────────────┐
///                 │    InputDispatcher    │
///                 └───────────────────────┘
///                              │
///          ┌───────────────────┼───────────────────┐
///          ▼                   ▼                   ▼
///    ┌──────────┐       ┌────────────┐      ┌──────────┐
///    │ Behaviors │       │ HitTester  │      │ State    │
///    │ (before)  │       │            │      │ Machine  │
///    └──────────┘       └────────────┘      └──────────┘
///          │                   │                   │
///          │                   ▼                   │
///          │            GraphHitTestResult         │
///          │                   │                   │
///          │                   ▼                   │
///          │         ┌─────────────────┐           │
///          │         │ InputProcessor  │           │
///          │         │ (by hit type)   │           │
///          │         └─────────────────┘           │
///          │                   │                   │
///          │                   ▼                   │
///          │            State Transition?──────────┘
///          │                   │
///          ▼                   ▼
///    ┌──────────┐        Result/New State
///    │ Behaviors │
///    │ (after)   │
///    └──────────┘
/// </code>
/// <para>
/// <b>Performance Optimizations (Gemini):</b>
/// <list type="bullet">
/// <item><see cref="HitTargetType"/> is a [Flags] enum for O(1) bitmask checks</item>
/// <item>Processor lookup uses pre-computed Dictionary for O(1) dispatch</item>
/// <item>Behaviors support state-aware filtering via <see cref="IBehavior.ActiveInStates"/></item>
/// <item>Processor list sorted once on registration, not on every dispatch</item>
/// </list>
/// </para>
/// <para>
/// <b>Pointer Capture (Critical for Drag Operations):</b>
/// When a processor starts a drag operation (e.g., NodeProcessor → DraggingState),
/// it MUST capture the pointer via <c>e.Pointer.Capture(control)</c>. This ensures
/// the interaction state continues to receive events even if the mouse moves
/// outside the element's visual bounds. Without capture, fast mouse movements
/// would cause hit tests to return "Canvas" instead of the element being dragged.
/// </para>
/// </remarks>
public class InputDispatcher
{
    private readonly List<IInputProcessor> _processors = new();
    private readonly List<IBehavior> _behaviors = new();
    private IGraphHitTester? _hitTester;
    private Graph? _graph;
    
    // Gemini optimization: Pre-computed lookup for O(1) dispatch
    // Maps each HitTargetType to its highest-priority processor
    private readonly Dictionary<HitTargetType, IInputProcessor> _processorLookup = new();
    private bool _lookupDirty = true;
    
    /// <summary>
    /// Registers an input processor.
    /// </summary>
    public void RegisterProcessor(IInputProcessor processor)
    {
        _processors.Add(processor);
        // Keep sorted by priority descending (Gemini: sort once, not on every dispatch)
        _processors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        _lookupDirty = true;
    }
    
    /// <summary>
    /// Unregisters an input processor.
    /// </summary>
    public bool UnregisterProcessor(IInputProcessor processor)
    {
        var removed = _processors.Remove(processor);
        if (removed) _lookupDirty = true;
        return removed;
    }
    
    /// <summary>
    /// Rebuilds the processor lookup dictionary for O(1) dispatch.
    /// Called lazily when needed after processor registration changes.
    /// </summary>
    private void RebuildLookupIfNeeded()
    {
        if (!_lookupDirty) return;
        
        _processorLookup.Clear();
        
        // For each possible HitTargetType flag value, find the highest-priority processor
        // that can handle it. Since _processors is already sorted by priority descending,
        // the first match wins.
        foreach (HitTargetType targetType in Enum.GetValues<HitTargetType>())
        {
            if (targetType == HitTargetType.None || targetType == HitTargetType.All ||
                targetType == HitTargetType.Draggable || targetType == HitTargetType.Selectable)
                continue; // Skip meta/combination values
            
            var processor = _processors.FirstOrDefault(p => p.CanHandle(targetType));
            if (processor != null)
            {
                _processorLookup[targetType] = processor;
            }
        }
        
        _lookupDirty = false;
    }
    
    /// <summary>
    /// Registers a behavior.
    /// </summary>
    public void RegisterBehavior(IBehavior behavior)
    {
        _behaviors.Add(behavior);
        _behaviors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        if (_graph != null)
        {
            behavior.OnAttached(_graph);
        }
    }
    
    /// <summary>
    /// Unregisters a behavior.
    /// </summary>
    public bool UnregisterBehavior(IBehavior behavior)
    {
        if (_behaviors.Remove(behavior))
        {
            behavior.OnDetached();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Sets the hit tester implementation.
    /// </summary>
    public void SetHitTester(IGraphHitTester hitTester)
    {
        _hitTester = hitTester;
    }
    
    /// <summary>
    /// Sets the current graph (for behavior attachment).
    /// </summary>
    public void SetGraph(Graph? graph)
    {
        if (_graph == graph) return;
        
        // Detach from old graph
        if (_graph != null)
        {
            foreach (var behavior in _behaviors)
            {
                behavior.OnDetached();
            }
        }
        
        _graph = graph;
        
        // Attach to new graph
        if (_graph != null)
        {
            foreach (var behavior in _behaviors)
            {
                behavior.OnAttached(_graph);
            }
        }
    }
    
    /// <summary>
    /// Gets all registered processors (for debugging).
    /// </summary>
    public IReadOnlyList<IInputProcessor> Processors => _processors;
    
    /// <summary>
    /// Gets all registered behaviors (for debugging).
    /// </summary>
    public IReadOnlyList<IBehavior> Behaviors => _behaviors;

    #region Dispatch Methods
    
    /// <summary>
    /// Dispatches a pointer pressed event through the full pipeline.
    /// </summary>
    /// <param name="context">The input state context.</param>
    /// <param name="e">The pointer event args.</param>
    /// <param name="canvasPosition">Position in canvas coordinates.</param>
    /// <param name="viewportPosition">Position in viewport coordinates.</param>
    /// <param name="currentStateName">Name of the current state (for behavior filtering).</param>
    public StateTransitionResult DispatchPointerPressed(
        InputStateContext context,
        PointerPressedEventArgs e,
        Core.Point canvasPosition,
        Core.Point viewportPosition,
        string currentStateName = "Idle")
    {
        if (_hitTester == null)
            return StateTransitionResult.Unhandled();
        
        // 1. Perform hit test
        var hitResult = _hitTester.HitTest(canvasPosition);
        
        // 2. Create framework-agnostic event
        var inputEvent = CreateInputEvent(
            hitResult, canvasPosition, viewportPosition, e);
        
        // 3. Let behaviors intercept (before) - Gemini: state-aware filtering
        foreach (var behavior in GetActiveBehaviors(currentStateName))
        {
            var result = behavior.OnBeforePointerPressed(inputEvent);
            if (result.SuppressDefault)
            {
                return result.Handled 
                    ? StateTransitionResult.Stay(true) 
                    : StateTransitionResult.Unhandled();
            }
        }
        
        // 4. Find and invoke processor
        var processorResult = DispatchToProcessor(
            hitResult,
            processor => processor.HandlePointerPressed(context, hitResult, e));
        
        // 5. Let behaviors observe (after) - Gemini: state-aware filtering
        foreach (var behavior in GetActiveBehaviors(currentStateName))
        {
            behavior.OnAfterPointerPressed(inputEvent);
        }
        
        return new StateTransitionResult(processorResult.NewState, processorResult.Handled);
    }
    
    /// <summary>
    /// Dispatches a pointer moved event through the full pipeline.
    /// </summary>
    /// <param name="context">The input state context.</param>
    /// <param name="e">The pointer event args.</param>
    /// <param name="canvasPosition">Position in canvas coordinates.</param>
    /// <param name="viewportPosition">Position in viewport coordinates.</param>
    /// <param name="currentStateName">Name of the current state (for behavior filtering).</param>
    public StateTransitionResult DispatchPointerMoved(
        InputStateContext context,
        PointerEventArgs e,
        Core.Point canvasPosition,
        Core.Point viewportPosition,
        string currentStateName = "Idle")
    {
        if (_hitTester == null)
            return StateTransitionResult.Unhandled();
        
        var hitResult = _hitTester.HitTest(canvasPosition);
        var inputEvent = CreateInputEvent(
            hitResult, canvasPosition, viewportPosition, e);
        
        // Behaviors before - Gemini: state-aware filtering
        foreach (var behavior in GetActiveBehaviors(currentStateName))
        {
            var result = behavior.OnBeforePointerMoved(inputEvent);
            if (result.SuppressDefault)
            {
                return result.Handled 
                    ? StateTransitionResult.Stay(true) 
                    : StateTransitionResult.Unhandled();
            }
        }
        
        // Processor
        var processorResult = DispatchToProcessor(
            hitResult,
            processor => processor.HandlePointerMoved(context, hitResult, e));
        
        // Behaviors after
        foreach (var behavior in GetActiveBehaviors(currentStateName))
        {
            behavior.OnAfterPointerMoved(inputEvent);
        }
        
        return new StateTransitionResult(processorResult.NewState, processorResult.Handled);
    }
    
    /// <summary>
    /// Dispatches a pointer released event through the full pipeline.
    /// </summary>
    /// <param name="context">The input state context.</param>
    /// <param name="e">The pointer event args.</param>
    /// <param name="canvasPosition">Position in canvas coordinates.</param>
    /// <param name="viewportPosition">Position in viewport coordinates.</param>
    /// <param name="currentStateName">Name of the current state (for behavior filtering).</param>
    public StateTransitionResult DispatchPointerReleased(
        InputStateContext context,
        PointerReleasedEventArgs e,
        Core.Point canvasPosition,
        Core.Point viewportPosition,
        string currentStateName = "Idle")
    {
        if (_hitTester == null)
            return StateTransitionResult.Unhandled();
        
        var hitResult = _hitTester.HitTest(canvasPosition);
        var inputEvent = CreateInputEvent(
            hitResult, canvasPosition, viewportPosition, e);
        
        // Behaviors before
        foreach (var behavior in GetActiveBehaviors(currentStateName))
        {
            var result = behavior.OnBeforePointerReleased(inputEvent);
            if (result.SuppressDefault)
            {
                return result.Handled 
                    ? StateTransitionResult.Stay(true) 
                    : StateTransitionResult.Unhandled();
            }
        }
        
        // Processor
        var processorResult = DispatchToProcessor(
            hitResult,
            processor => processor.HandlePointerReleased(context, hitResult, e));
        
        // Behaviors after
        foreach (var behavior in GetActiveBehaviors(currentStateName))
        {
            behavior.OnAfterPointerReleased(inputEvent);
        }
        
        return new StateTransitionResult(processorResult.NewState, processorResult.Handled);
    }
    
    #endregion
    
    #region Private Helpers
    
    /// <summary>
    /// Gets the processor for the given hit type using O(1) dictionary lookup.
    /// </summary>
    private IInputProcessor? GetProcessor(HitTargetType targetType)
    {
        RebuildLookupIfNeeded();
        return _processorLookup.TryGetValue(targetType, out var processor) ? processor : null;
    }
    
    /// <summary>
    /// Gets behaviors that are enabled and active in the current state.
    /// </summary>
    private IEnumerable<IBehavior> GetActiveBehaviors(string currentStateName)
    {
        return _behaviors.Where(b => b.IsEnabled && b.IsActiveInState(currentStateName));
    }
    
    private InputProcessorResult DispatchToProcessor(
        GraphHitTestResult hitResult,
        Func<IInputProcessor, InputProcessorResult> handler)
    {
        // Gemini optimization: O(1) lookup instead of linear search
        var processor = GetProcessor(hitResult.TargetType);
        
        if (processor == null)
        {
            // No processor for this type - not handled
            return InputProcessorResult.NotHandled;
        }
        
        return handler(processor);
    }
    
    private static IGraphInputEvent CreateInputEvent(
        GraphHitTestResult hitResult,
        Core.Point canvasPosition,
        Core.Point viewportPosition,
        PointerEventArgs e)
    {
        var button = CoreMouseButton.None;
        var clickCount = 1;
        
        if (e is PointerPressedEventArgs pressed)
        {
            var point = pressed.GetCurrentPoint(null);
            button = GetMouseButton(point.Properties);
            clickCount = pressed.ClickCount;
        }
        else if (e is PointerReleasedEventArgs released)
        {
            var point = released.GetCurrentPoint(null);
            button = GetMouseButton(point.Properties);
        }
        
        return new GraphInputEventAdapter(
            hitResult,
            canvasPosition,
            viewportPosition,
            ConvertModifiers(e.KeyModifiers),
            button,
            clickCount);
    }
    
    private static InputModifiers ConvertModifiers(KeyModifiers keyModifiers)
    {
        var result = InputModifiers.None;
        
        if (keyModifiers.HasFlag(KeyModifiers.Alt))
            result |= InputModifiers.Alt;
        if (keyModifiers.HasFlag(KeyModifiers.Control))
            result |= InputModifiers.Control;
        if (keyModifiers.HasFlag(KeyModifiers.Shift))
            result |= InputModifiers.Shift;
        if (keyModifiers.HasFlag(KeyModifiers.Meta))
            result |= InputModifiers.Meta;
        
        return result;
    }
    
    private static CoreMouseButton GetMouseButton(PointerPointProperties props)
    {
        if (props.IsLeftButtonPressed) return CoreMouseButton.Left;
        if (props.IsRightButtonPressed) return CoreMouseButton.Right;
        if (props.IsMiddleButtonPressed) return CoreMouseButton.Middle;
        return CoreMouseButton.None;
    }
    
    #endregion
}

/// <summary>
/// Adapter that implements IGraphInputEvent using Avalonia event data.
/// </summary>
internal class GraphInputEventAdapter : IGraphInputEvent
{
    public GraphInputEventAdapter(
        GraphHitTestResult hitResult,
        Core.Point canvasPosition,
        Core.Point viewportPosition,
        InputModifiers modifiers,
        CoreMouseButton button,
        int clickCount)
    {
        HitResult = hitResult;
        CanvasPosition = canvasPosition;
        ViewportPosition = viewportPosition;
        Modifiers = modifiers;
        Button = button;
        ClickCount = clickCount;
    }
    
    public GraphHitTestResult HitResult { get; }
    public Core.Point CanvasPosition { get; }
    public Core.Point ViewportPosition { get; }
    public InputModifiers Modifiers { get; }
    public CoreMouseButton Button { get; }
    public int ClickCount { get; }
    public bool Handled { get; set; }
    public Core.Point WheelDelta => new Core.Point(0, 0);
}
