namespace FlowGraph.Core.Commands;

/// <summary>
/// Command that groups multiple commands into a single undoable operation.
/// </summary>
public class CompositeCommand : IGraphCommand
{
    private readonly List<IGraphCommand> _commands;

    public string Description { get; }

    /// <summary>
    /// Creates a composite command with a custom description.
    /// </summary>
    public CompositeCommand(string description, IEnumerable<IGraphCommand> commands)
    {
        Description = description;
        _commands = commands.ToList();
        
        if (_commands.Count == 0)
            throw new ArgumentException("At least one command must be specified", nameof(commands));
    }

    /// <summary>
    /// Creates a composite command with a custom description.
    /// </summary>
    public CompositeCommand(string description, params IGraphCommand[] commands)
        : this(description, commands.AsEnumerable())
    {
    }

    public void Execute()
    {
        foreach (var command in _commands)
        {
            command.Execute();
        }
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
