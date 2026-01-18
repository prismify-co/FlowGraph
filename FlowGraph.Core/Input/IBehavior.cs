using FlowGraph.Core.Input;

namespace FlowGraph.Core.Input;

/// <summary>
/// Framework-agnostic representation of a graph input event.
/// Wraps hit test results and modifier state without exposing UI framework types.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction allows behaviors written against FlowGraph.Core to work
/// with any UI framework (Avalonia, WPF, MAUI, etc.) without modification.
/// </para>
/// <para>
/// <b>Design Rationale:</b>
/// <list type="bullet">
/// <item>Hit test results are already framework-agnostic (Point, Node, Edge, etc.)</item>
/// <item>Modifier keys map to a simple flags enum</item>
/// <item>Click count and button info are primitive types</item>
/// </list>
/// </para>
/// </remarks>
public interface IGraphInputEvent
{
  /// <summary>
  /// The result of hit testing at the input position.
  /// </summary>
  GraphHitTestResult HitResult { get; }

  /// <summary>
  /// Position in canvas coordinates where the input occurred.
  /// </summary>
  Point CanvasPosition { get; }

  /// <summary>
  /// Position in viewport/screen coordinates where the input occurred.
  /// </summary>
  Point ViewportPosition { get; }

  /// <summary>
  /// Keyboard modifiers held during the input.
  /// </summary>
  InputModifiers Modifiers { get; }

  /// <summary>
  /// Which mouse button triggered this event (for press/release events).
  /// </summary>
  MouseButton Button { get; }

  /// <summary>
  /// Number of consecutive clicks (1 = single, 2 = double, etc.).
  /// </summary>
  int ClickCount { get; }

  /// <summary>
  /// Gets or sets whether this event has been handled.
  /// Setting to true prevents further processing.
  /// </summary>
  bool Handled { get; set; }

  /// <summary>
  /// Delta values for wheel/scroll events. Zero for press/release.
  /// </summary>
  Point WheelDelta { get; }
}

/// <summary>
/// Keyboard modifier flags (framework-agnostic).
/// </summary>
[Flags]
public enum InputModifiers
{
  None = 0,
  Alt = 1,
  Control = 2,
  Shift = 4,
  Meta = 8  // Windows key / Command key
}

/// <summary>
/// Mouse button identifiers.
/// </summary>
public enum MouseButton
{
  None,
  Left,
  Middle,
  Right,
  XButton1,
  XButton2
}

/// <summary>
/// Result of a behavior's input handling.
/// </summary>
/// <param name="Handled">Whether the behavior handled this input.</param>
/// <param name="SuppressDefault">If true, skip default processing entirely.</param>
public record BehaviorResult(bool Handled, bool SuppressDefault = false)
{
  /// <summary>
  /// The behavior did not handle this input.
  /// </summary>
  public static BehaviorResult NotHandled { get; } = new(false, false);

  /// <summary>
  /// The behavior handled this input, continue with default processing.
  /// </summary>
  public static BehaviorResult HandledContinue { get; } = new(true, false);

  /// <summary>
  /// The behavior handled this input, skip default processing.
  /// </summary>
  public static BehaviorResult HandledSuppress { get; } = new(true, true);
}

/// <summary>
/// A behavior that can observe and optionally intercept graph input events.
/// </summary>
/// <remarks>
/// <para>
/// Behaviors provide an extensibility mechanism for customizing graph interactions
/// without modifying the core input handling logic. They are inspired by the
/// Behavior pattern from Blazor.Diagrams.
/// </para>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
/// <item>Before core processing: <see cref="OnBeforePointerPressed"/> etc.</item>
/// <item>Core processing (InputProcessor) runs if not suppressed</item>
/// <item>After core processing: <see cref="OnAfterPointerPressed"/> etc.</item>
/// </list>
/// </para>
/// <para>
/// <b>Performance (Gemini Optimization):</b>
/// Behaviors can specify which states they're active in via <see cref="ActiveInStates"/>.
/// The dispatcher skips behaviors that aren't relevant to the current state,
/// avoiding iteration overhead on high-frequency events like PointerMoved.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// <list type="bullet">
/// <item>Custom selection modes (marquee, lasso)</item>
/// <item>Snap-to-grid visualization</item>
/// <item>Connection validation feedback</item>
/// <item>Accessibility enhancements</item>
/// <item>Analytics/logging</item>
/// </list>
/// </para>
/// </remarks>
public interface IBehavior
{
  /// <summary>
  /// Display name for debugging and UI purposes.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Whether this behavior is currently active.
  /// Disabled behaviors receive no events.
  /// </summary>
  bool IsEnabled { get; set; }

  /// <summary>
  /// Priority relative to other behaviors. Higher = called first.
  /// Default behaviors use 0. Use negative for "always last" behaviors.
  /// </summary>
  int Priority { get; }

  /// <summary>
  /// The input state names this behavior is active in.
  /// If null or empty, the behavior is active in all states.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Gemini Optimization:</b> Behaviors that are only relevant in specific states
  /// (e.g., a selection box behavior only during Idle or BoxSelecting) should specify
  /// those states here. The dispatcher will skip the behavior entirely in other states,
  /// avoiding unnecessary method calls on high-frequency events.
  /// </para>
  /// <para>
  /// State names match <c>IInputState.Name</c> (e.g., "Idle", "Dragging", "Panning").
  /// </para>
  /// </remarks>
  IReadOnlySet<string>? ActiveInStates { get; }

  /// <summary>
  /// Checks if this behavior should be invoked for the given state.
  /// Default: active in all states if <see cref="ActiveInStates"/> is null/empty.
  /// </summary>
  bool IsActiveInState(string stateName) =>
      ActiveInStates == null || ActiveInStates.Count == 0 || ActiveInStates.Contains(stateName);

  #region Pointer Events

  /// <summary>
  /// Called before pointer pressed is processed.
  /// Return <see cref="BehaviorResult.HandledSuppress"/> to prevent default handling.
  /// </summary>
  BehaviorResult OnBeforePointerPressed(IGraphInputEvent e);

  /// <summary>
  /// Called after pointer pressed has been processed.
  /// </summary>
  void OnAfterPointerPressed(IGraphInputEvent e);

  /// <summary>
  /// Called before pointer moved is processed.
  /// </summary>
  BehaviorResult OnBeforePointerMoved(IGraphInputEvent e);

  /// <summary>
  /// Called after pointer moved has been processed.
  /// </summary>
  void OnAfterPointerMoved(IGraphInputEvent e);

  /// <summary>
  /// Called before pointer released is processed.
  /// </summary>
  BehaviorResult OnBeforePointerReleased(IGraphInputEvent e);

  /// <summary>
  /// Called after pointer released has been processed.
  /// </summary>
  void OnAfterPointerReleased(IGraphInputEvent e);

  #endregion

  #region Wheel Events

  /// <summary>
  /// Called before wheel/scroll is processed.
  /// </summary>
  BehaviorResult OnBeforePointerWheel(IGraphInputEvent e);

  /// <summary>
  /// Called after wheel/scroll has been processed.
  /// </summary>
  void OnAfterPointerWheel(IGraphInputEvent e);

  #endregion

  #region Keyboard Events

  /// <summary>
  /// Called before key down is processed.
  /// </summary>
  BehaviorResult OnBeforeKeyDown(IGraphInputEvent e);

  /// <summary>
  /// Called after key down has been processed.
  /// </summary>
  void OnAfterKeyDown(IGraphInputEvent e);

  #endregion

  #region Lifecycle

  /// <summary>
  /// Called when the behavior is attached to a graph canvas.
  /// </summary>
  void OnAttached(Graph graph);

  /// <summary>
  /// Called when the behavior is detached from a graph canvas.
  /// </summary>
  void OnDetached();

  #endregion
}

/// <summary>
/// Base class for behaviors with default no-op implementations.
/// </summary>
public abstract class BehaviorBase : IBehavior
{
  public abstract string Name { get; }

  public bool IsEnabled { get; set; } = true;

  public virtual int Priority => 0;

  /// <summary>
  /// Override to restrict this behavior to specific states.
  /// Return null (default) to be active in all states.
  /// </summary>
  public virtual IReadOnlySet<string>? ActiveInStates => null;

  public bool IsActiveInState(string stateName) =>
      ActiveInStates == null || ActiveInStates.Count == 0 || ActiveInStates.Contains(stateName);

  // Default: don't intercept anything
  public virtual BehaviorResult OnBeforePointerPressed(IGraphInputEvent e) => BehaviorResult.NotHandled;
  public virtual void OnAfterPointerPressed(IGraphInputEvent e) { }

  public virtual BehaviorResult OnBeforePointerMoved(IGraphInputEvent e) => BehaviorResult.NotHandled;
  public virtual void OnAfterPointerMoved(IGraphInputEvent e) { }

  public virtual BehaviorResult OnBeforePointerReleased(IGraphInputEvent e) => BehaviorResult.NotHandled;
  public virtual void OnAfterPointerReleased(IGraphInputEvent e) { }

  public virtual BehaviorResult OnBeforePointerWheel(IGraphInputEvent e) => BehaviorResult.NotHandled;
  public virtual void OnAfterPointerWheel(IGraphInputEvent e) { }

  public virtual BehaviorResult OnBeforeKeyDown(IGraphInputEvent e) => BehaviorResult.NotHandled;
  public virtual void OnAfterKeyDown(IGraphInputEvent e) { }

  public virtual void OnAttached(Graph graph) { }
  public virtual void OnDetached() { }
}
