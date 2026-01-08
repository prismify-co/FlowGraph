using FlowGraph.Core;
using FlowGraph.Core.Commands;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Tests;

public class NodeCommandsTests
{
    [Fact]
    public void AddNodeCommand_Execute_AddsNodeToGraph()
    {
        var graph = new Graph();
        var node = TestHelpers.CreateNode("n1", type: "test");
        var command = new AddNodeCommand(graph, node);

        command.Execute();

        Assert.Single(graph.Nodes);
        Assert.Same(node, graph.Nodes[0]);
    }

    [Fact]
    public void AddNodeCommand_Undo_RemovesNodeFromGraph()
    {
        var graph = new Graph();
        var node = TestHelpers.CreateNode("n1", type: "test");
        var command = new AddNodeCommand(graph, node);
        command.Execute();

        command.Undo();

        Assert.Empty(graph.Nodes);
    }

    [Fact]
    public void RemoveNodeCommand_Execute_RemovesNodeFromGraph()
    {
        var graph = new Graph();
        var node = TestHelpers.CreateNode("n1", type: "test");
        graph.AddNode(node);
        var command = new RemoveNodeCommand(graph, node);

        command.Execute();

        Assert.Empty(graph.Nodes);
    }

    [Fact]
    public void RemoveNodeCommand_Undo_RestoresNodeAndEdges()
    {
        var graph = new Graph();
        var node1 = TestHelpers.CreateNode("n1", type: "test1",
            outputs: [new Port { Id = "out", Type = "data" }]);
        var node2 = TestHelpers.CreateNode("n2", type: "test2",
            inputs: [new Port { Id = "in", Type = "data" }]);
        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge = TestHelpers.CreateEdge("e1", node1.Id, node2.Id, "out", "in");
        graph.AddEdge(edge);

        var command = new RemoveNodeCommand(graph, node1);
        command.Execute();

        command.Undo();

        Assert.Equal(2, graph.Nodes.Count);
        Assert.Single(graph.Edges);
    }

    [Fact]
    public void RemoveNodesCommand_Execute_RemovesMultipleNodes()
    {
        var graph = new Graph();
        var node1 = TestHelpers.CreateNode("n1", type: "test1");
        var node2 = TestHelpers.CreateNode("n2", type: "test2");
        graph.AddNode(node1);
        graph.AddNode(node2);

        var command = new RemoveNodesCommand(graph, [node1, node2]);
        command.Execute();

        Assert.Empty(graph.Nodes);
    }

    [Fact]
    public void RemoveNodesCommand_Undo_RestoresAllNodes()
    {
        var graph = new Graph();
        var node1 = TestHelpers.CreateNode("n1", type: "test1");
        var node2 = TestHelpers.CreateNode("n2", type: "test2");
        graph.AddNode(node1);
        graph.AddNode(node2);

        var command = new RemoveNodesCommand(graph, [node1, node2]);
        command.Execute();
        command.Undo();

        Assert.Equal(2, graph.Nodes.Count);
    }

    [Fact]
    public void MoveNodesCommand_Execute_MovesNode()
    {
        var graph = new Graph();
        var node = TestHelpers.CreateNode("n1", type: "test", x: 0, y: 0);
        graph.AddNode(node);

        var command = new MoveNodesCommand(graph, node, new Point(0, 0), new Point(100, 100));
        command.Execute();

        Assert.Equal(100, node.Position.X);
        Assert.Equal(100, node.Position.Y);
    }

    [Fact]
    public void MoveNodesCommand_Undo_RestoresPosition()
    {
        var graph = new Graph();
        var node = TestHelpers.CreateNode("n1", type: "test", x: 0, y: 0);
        graph.AddNode(node);

        var command = new MoveNodesCommand(graph, node, new Point(0, 0), new Point(100, 100));
        command.Execute();
        command.Undo();

        Assert.Equal(0, node.Position.X);
        Assert.Equal(0, node.Position.Y);
    }

    [Fact]
    public void MoveNodesCommand_MovesMultipleNodes()
    {
        var graph = new Graph();
        var node1 = TestHelpers.CreateNode("n1", type: "test1", x: 0, y: 0);
        var node2 = TestHelpers.CreateNode("n2", type: "test2", x: 100, y: 100);
        graph.AddNode(node1);
        graph.AddNode(node2);

        var oldPositions = new Dictionary<string, Point>
        {
            { "n1", new Point(0, 0) },
            { "n2", new Point(100, 100) }
        };
        var newPositions = new Dictionary<string, Point>
        {
            { "n1", new Point(50, 50) },
            { "n2", new Point(150, 150) }
        };

        var command = new MoveNodesCommand(graph, oldPositions, newPositions);
        command.Execute();

        Assert.Equal(50, node1.Position.X);
        Assert.Equal(150, node2.Position.X);
    }
}
