using Avalonia.Controls;
using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Manages input state transitions and delegates events to the current state.
/// </summary>
public class InputStateMachine
{
    private IInputState _currentState;
    private readonly InputStateContext _context;

    /// <summary>
    /// Gets the current state name for debugging.
    /// </summary>
    public string CurrentStateName => _currentState.Name;

    /// <summary>
    /// Gets the context shared across all states.
    /// </summary>
    public InputStateContext Context => _context;

    public InputStateMachine(InputStateContext context)
    {
        _context = context;
        _currentState = IdleState.Instance;
    }

    /// <summary>
    /// Transitions to a new state.
    /// </summary>
    public void TransitionTo(IInputState newState)
    {
        var oldStateName = _currentState.Name;
        
        _currentState.Exit(_context);
        _currentState = newState;
        _currentState.Enter(_context);

        _context.RaiseStateChanged(oldStateName, newState.Name);
    }

    /// <summary>
    /// Forces a return to the idle state.
    /// </summary>
    public void Reset()
    {
        TransitionTo(IdleState.Instance);
    }

    #region Event Handlers

    public bool HandlePointerPressed(PointerPressedEventArgs e, Control? source)
    {
        var result = _currentState.HandlePointerPressed(_context, e, source);
        ProcessResult(result);
        return result.Handled;
    }

    public bool HandlePointerMoved(PointerEventArgs e)
    {
        var result = _currentState.HandlePointerMoved(_context, e);
        ProcessResult(result);
        return result.Handled;
    }

    public bool HandlePointerReleased(PointerReleasedEventArgs e)
    {
        var result = _currentState.HandlePointerReleased(_context, e);
        ProcessResult(result);
        return result.Handled;
    }

    public bool HandlePointerWheel(PointerWheelEventArgs e)
    {
        var result = _currentState.HandlePointerWheel(_context, e);
        ProcessResult(result);
        return result.Handled;
    }

    public bool HandleKeyDown(KeyEventArgs e)
    {
        var result = _currentState.HandleKeyDown(_context, e);
        ProcessResult(result);
        return result.Handled;
    }

    public void Update(double deltaTime)
    {
        _currentState.Update(_context, deltaTime);
    }

    #endregion

    private void ProcessResult(StateTransitionResult result)
    {
        if (result.NewState != null)
        {
            TransitionTo(result.NewState);
        }
    }
}
