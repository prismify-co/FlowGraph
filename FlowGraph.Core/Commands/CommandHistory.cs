namespace FlowGraph.Core.Commands;

/// <summary>
/// Manages command history for undo/redo functionality.
/// </summary>
public class CommandHistory
{
    private readonly Stack<IGraphCommand> _undoStack = new();
    private readonly Stack<IGraphCommand> _redoStack = new();
    private readonly int _maxHistorySize;
    private bool _isExecutingCommand;

    /// <summary>
    /// Creates a new command history with the specified maximum size.
    /// </summary>
    /// <param name="maxHistorySize">Maximum number of commands to keep in history. 0 = unlimited.</param>
    public CommandHistory(int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Event raised when the undo/redo state changes.
    /// </summary>
    public event EventHandler? HistoryChanged;

    /// <summary>
    /// Gets whether there are commands that can be undone.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether there are commands that can be redone.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets the number of commands in the undo stack.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Gets the number of commands in the redo stack.
    /// </summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Gets the description of the next command to undo, or null if none.
    /// </summary>
    public string? NextUndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <summary>
    /// Gets the description of the next command to redo, or null if none.
    /// </summary>
    public string? NextRedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Executes a command and adds it to the history.
    /// </summary>
    public void Execute(IGraphCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_isExecutingCommand)
        {
            // Prevent recursive command execution
            return;
        }

        try
        {
            _isExecutingCommand = true;
            command.Execute();
            
            _undoStack.Push(command);
            _redoStack.Clear(); // Clear redo stack when new command is executed

            // Trim history if needed
            TrimHistory();
            
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isExecutingCommand = false;
        }
    }

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    /// <returns>True if a command was undone, false if there was nothing to undo.</returns>
    public bool Undo()
    {
        if (!CanUndo || _isExecutingCommand)
            return false;

        try
        {
            _isExecutingCommand = true;
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        finally
        {
            _isExecutingCommand = false;
        }
    }

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    /// <returns>True if a command was redone, false if there was nothing to redo.</returns>
    public bool Redo()
    {
        if (!CanRedo || _isExecutingCommand)
            return false;

        try
        {
            _isExecutingCommand = true;
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        finally
        {
            _isExecutingCommand = false;
        }
    }

    /// <summary>
    /// Clears all command history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TrimHistory()
    {
        if (_maxHistorySize <= 0) return;

        // Remove oldest commands if we exceed the limit
        while (_undoStack.Count > _maxHistorySize)
        {
            // Convert to list, remove from bottom, convert back
            var commands = _undoStack.ToList();
            commands.RemoveAt(commands.Count - 1);
            _undoStack.Clear();
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                _undoStack.Push(commands[i]);
            }
        }
    }
}
