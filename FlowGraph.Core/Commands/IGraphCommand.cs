namespace FlowGraph.Core.Commands;

/// <summary>
/// Interface for reversible commands that support undo/redo.
/// </summary>
public interface IGraphCommand
{
    /// <summary>
    /// Gets a human-readable description of the command.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    void Execute();

    /// <summary>
    /// Reverses the command (undo).
    /// </summary>
    void Undo();
}
