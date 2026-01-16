using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Clipboard operations (copy, cut, paste, duplicate).
/// </summary>
public partial class FlowCanvas
{
    #region Undo/Redo

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    public void Undo() => CommandHistory.Undo();

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    public void Redo() => CommandHistory.Redo();

    #endregion

    #region Clipboard Operations

    /// <summary>
    /// Copies selected nodes to the clipboard.
    /// </summary>
    public void Copy()
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Elements.Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Count == 0) return;

        _clipboardManager.Copy(selectedNodes, Graph.Elements.Edges);
    }

    /// <summary>
    /// Cuts selected nodes to the clipboard.
    /// </summary>
    public void Cut()
    {
        if (Graph == null) return;

        Copy();
        _selectionManager.DeleteSelected();
    }

    /// <summary>
    /// Pastes nodes from the clipboard.
    /// </summary>
    public void Paste()
    {
        if (Graph == null || !_clipboardManager.HasContent) return;

        // Deselect current selection - OPTIMIZED: only iterate selected nodes
        foreach (var node in Graph.Elements.Nodes.Where(n => n.IsSelected))
        {
            node.IsSelected = false;
        }

        // Calculate paste position (center of viewport or with offset from original)
        var viewCenter = _viewport.ViewportToCanvas(new global::Avalonia.Point(
            _viewport.ViewSize.Width / 2,
            _viewport.ViewSize.Height / 2));
        var pastePosition = new Core.Point(viewCenter.X, viewCenter.Y);

        var (pastedNodes, pastedEdges) = _clipboardManager.Paste(Graph, pastePosition);

        if (pastedNodes.Count > 0)
        {
            var command = new PasteCommand(Graph, pastedNodes, pastedEdges);
            CommandHistory.Execute(new AlreadyExecutedCommand(command));
        }
    }

    /// <summary>
    /// Duplicates selected nodes in place.
    /// </summary>
    public void Duplicate()
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Elements.Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Count == 0) return;

        // Deselect current selection
        foreach (var node in selectedNodes)
        {
            node.IsSelected = false;
        }

        // Duplicate with small offset
        var offset = new Core.Point(20, 20);
        var (duplicatedNodes, duplicatedEdges) = _clipboardManager.Duplicate(
            Graph, selectedNodes, Graph.Elements.Edges, offset);

        if (duplicatedNodes.Count > 0)
        {
            var command = new DuplicateCommand(Graph, duplicatedNodes, duplicatedEdges);
            CommandHistory.Execute(new AlreadyExecutedCommand(command));
        }
    }

    #endregion
}
