namespace FlowGraph.Core.Commands;

/// <summary>
/// Wraps a command that has already been executed, skipping the initial Execute call.
/// Used when an action was performed through UI and we just need to track it for undo/redo.
/// </summary>
public class AlreadyExecutedCommand : IGraphCommand
{
    private readonly IGraphCommand _innerCommand;
    private bool _firstExecute = true;

    public string Description => _innerCommand.Description;

    public AlreadyExecutedCommand(IGraphCommand innerCommand)
    {
        _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
    }

    public void Execute()
    {
        // Skip the first execute since the action already happened
        if (_firstExecute)
        {
            _firstExecute = false;
            return;
        }

        _innerCommand.Execute();
    }

    public void Undo()
    {
        _innerCommand.Undo();
    }
}
