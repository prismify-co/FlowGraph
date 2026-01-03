using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Core.Tests;

public class CommandHistoryTests
{
    [Fact]
    public void Execute_AddsCommandToUndoStack()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        var node = new Node { Type = "test" };
        var command = new AddNodeCommand(graph, node);

        history.Execute(command);

        Assert.True(history.CanUndo);
        Assert.Equal(1, history.UndoCount);
    }

    [Fact]
    public void Execute_ClearsRedoStack()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        var node1 = new Node { Type = "test1" };
        var node2 = new Node { Type = "test2" };

        history.Execute(new AddNodeCommand(graph, node1));
        history.Undo();
        Assert.True(history.CanRedo);

        history.Execute(new AddNodeCommand(graph, node2));
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_MovesCommandToRedoStack()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        var node = new Node { Type = "test" };

        history.Execute(new AddNodeCommand(graph, node));
        history.Undo();

        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Redo_MovesCommandBackToUndoStack()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        var node = new Node { Type = "test" };

        history.Execute(new AddNodeCommand(graph, node));
        history.Undo();
        history.Redo();

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Clear_RemovesAllCommands()
    {
        var history = new CommandHistory();
        var graph = new Graph();

        history.Execute(new AddNodeCommand(graph, new Node { Type = "test1" }));
        history.Execute(new AddNodeCommand(graph, new Node { Type = "test2" }));
        history.Undo();

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void NextUndoDescription_ReturnsCorrectDescription()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        var node = new Node { Type = "TestType" };

        history.Execute(new AddNodeCommand(graph, node));

        Assert.Equal("Add TestType node", history.NextUndoDescription);
    }

    [Fact]
    public void NextRedoDescription_ReturnsCorrectDescription()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        var node = new Node { Type = "TestType" };

        history.Execute(new AddNodeCommand(graph, node));
        history.Undo();

        Assert.Equal("Add TestType node", history.NextRedoDescription);
    }

    [Fact]
    public void HistoryChanged_RaisedOnExecute()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        var eventRaised = false;
        history.HistoryChanged += (_, _) => eventRaised = true;

        history.Execute(new AddNodeCommand(graph, new Node { Type = "test" }));

        Assert.True(eventRaised);
    }

    [Fact]
    public void HistoryChanged_RaisedOnUndo()
    {
        var history = new CommandHistory();
        var graph = new Graph();
        history.Execute(new AddNodeCommand(graph, new Node { Type = "test" }));

        var eventRaised = false;
        history.HistoryChanged += (_, _) => eventRaised = true;

        history.Undo();

        Assert.True(eventRaised);
    }

    [Fact]
    public void MaxHistorySize_TrimsOldCommands()
    {
        var history = new CommandHistory(maxHistorySize: 3);
        var graph = new Graph();

        for (int i = 0; i < 5; i++)
        {
            history.Execute(new AddNodeCommand(graph, new Node { Type = $"test{i}" }));
        }

        Assert.Equal(3, history.UndoCount);
    }
}
