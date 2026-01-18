using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Core.Input;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Result of an input processor's handling.
/// </summary>
/// <param name="NewState">Optional new state to transition to.</param>
/// <param name="Handled">Whether this input was handled (stops propagation to lower-priority processors).</param>
public record InputProcessorResult(IInputState? NewState, bool Handled)
{
    /// <summary>
    /// The processor did not handle this input. Continue to next processor.
    /// </summary>
    public static InputProcessorResult NotHandled { get; } = new(null, false);
    
    /// <summary>
    /// The processor handled this input but state remains unchanged.
    /// </summary>
    public static InputProcessorResult HandledStay { get; } = new(null, true);
    
    /// <summary>
    /// The processor handled this input and wants to transition to a new state.
    /// </summary>
    public static InputProcessorResult TransitionTo(IInputState newState) => new(newState, true);
}

/// <summary>
/// Processes input for a specific element type or interaction pattern.
/// </summary>
/// <remarks>
/// <para>
/// InputProcessors replace the pattern-matching approach in IdleState with
/// a more maintainable, extensible architecture. Each processor handles
/// input for one or more <see cref="HitTargetType"/>s.
/// </para>
/// <para>
/// <b>Priority Order (higher = checked first):</b>
/// <list type="bullet">
/// <item>100: Resize handles (small targets, always on top)</item>
/// <item>90: Ports (small targets need priority)</item>
/// <item>80: Nodes</item>
/// <item>70: Edges</item>
/// <item>60: Shapes (sticky notes, etc.)</item>
/// <item>50: Groups</item>
/// <item>0: Canvas (fallback)</item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Note (Gemini Optimization):</b>
/// HandledTypes is a bitmask enum, enabling O(1) type checking via bitwise AND
/// instead of O(N) list iteration. Critical for high-frequency events like PointerMoved.
/// </para>
/// <para>
/// <b>Design Rationale:</b>
/// <list type="bullet">
/// <item>Eliminates fragile pattern matching on Tag objects</item>
/// <item>Each element type's logic is self-contained</item>
/// <item>New element types = new processor, not touching IdleState</item>
/// <item>Testable in isolation</item>
/// </list>
/// </para>
/// </remarks>
public interface IInputProcessor
{
    /// <summary>
    /// The hit target types this processor handles (bitmask).
    /// Use bitwise OR to handle multiple types: <c>HitTargetType.Node | HitTargetType.Group</c>
    /// </summary>
    HitTargetType HandledTypes { get; }
    
    /// <summary>
    /// Priority for checking this processor. Higher = checked first.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Display name for debugging.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Checks if this processor handles the given target type.
    /// Default implementation uses bitwise AND for O(1) checking.
    /// </summary>
    bool CanHandle(HitTargetType targetType) => (HandledTypes & targetType) != 0;

    #region Pointer Events
    
    /// <summary>
    /// Handles pointer pressed on an element this processor is responsible for.
    /// </summary>
    /// <param name="context">The input state context.</param>
    /// <param name="hit">The hit test result (guaranteed to be a type from <see cref="HandledTypes"/>).</param>
    /// <param name="e">The Avalonia pointer event.</param>
    /// <returns>Result indicating whether handled and any state transition.</returns>
    InputProcessorResult HandlePointerPressed(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerPressedEventArgs e);
    
    /// <summary>
    /// Handles pointer moved. Called during idle state for hover effects.
    /// </summary>
    InputProcessorResult HandlePointerMoved(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerEventArgs e);
    
    /// <summary>
    /// Handles pointer released. Rarely needed at processor level.
    /// </summary>
    InputProcessorResult HandlePointerReleased(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerReleasedEventArgs e);
    
    #endregion
}

/// <summary>
/// Base class for input processors with default no-op implementations.
/// </summary>
public abstract class InputProcessorBase : IInputProcessor
{
    public abstract HitTargetType HandledTypes { get; }
    public abstract int Priority { get; }
    public abstract string Name { get; }
    
    /// <summary>
    /// Checks if this processor handles the given target type (O(1) bitmask check).
    /// </summary>
    public bool CanHandle(HitTargetType targetType) => (HandledTypes & targetType) != 0;
    
    public virtual InputProcessorResult HandlePointerPressed(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerPressedEventArgs e)
        => InputProcessorResult.NotHandled;
    
    public virtual InputProcessorResult HandlePointerMoved(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerEventArgs e)
        => InputProcessorResult.NotHandled;
    
    public virtual InputProcessorResult HandlePointerReleased(
        InputStateContext context,
        GraphHitTestResult hit,
        PointerReleasedEventArgs e)
        => InputProcessorResult.NotHandled;
    
    #region Helpers
    
    /// <summary>
    /// Captures the pointer on the root panel for drag operations.
    /// </summary>
    protected static void CapturePointer(PointerEventArgs e, Control? target)
    {
        if (target != null)
        {
            e.Pointer.Capture(target);
        }
    }
    
    /// <summary>
    /// Checks if Ctrl modifier is held.
    /// </summary>
    protected static bool IsCtrlHeld(PointerEventArgs e) 
        => e.KeyModifiers.HasFlag(KeyModifiers.Control);
    
    /// <summary>
    /// Checks if Shift modifier is held.
    /// </summary>
    protected static bool IsShiftHeld(PointerEventArgs e) 
        => e.KeyModifiers.HasFlag(KeyModifiers.Shift);
    
    /// <summary>
    /// Checks if this is a double-click.
    /// </summary>
    protected static bool IsDoubleClick(PointerPressedEventArgs e) 
        => e.ClickCount == 2;
    
    #endregion
}

/// <summary>
/// Standard priority constants for input processors.
/// </summary>
public static class InputProcessorPriority
{
    /// <summary>Resize handles - highest priority, small targets always on top.</summary>
    public const int ResizeHandle = 100;
    
    /// <summary>Ports - small targets need priority over nodes.</summary>
    public const int Port = 90;
    
    /// <summary>Nodes - standard graph elements.</summary>
    public const int Node = 80;
    
    /// <summary>Edge endpoints - handles for reconnection.</summary>
    public const int EdgeEndpoint = 75;
    
    /// <summary>Edges - connection lines.</summary>
    public const int Edge = 70;
    
    /// <summary>Shapes - sticky notes, annotations, etc.</summary>
    public const int Shape = 60;
    
    /// <summary>Groups - behind their contents.</summary>
    public const int Group = 50;
    
    /// <summary>Canvas - empty space, lowest priority fallback.</summary>
    public const int Canvas = 0;
}
