using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Core.Tests;

public class NodeCommandsTests
{
    [Fact]
    public void AddNodeCommand_Execute_AddsNodeToGraph()
    {
        var graph = new Graph();
        var node = new Node { Type = "test" };
        var command = new AddNodeCommand(graph, node);

        command.Execute();

        Assert.Single(graph.Nodes);
        Assert.Same(node, graph.Nodes[0]);
    }

    [Fact]
    public void AddNodeCommand_Undo_RemovesNodeFromGraph()
    {
        var graph = new Graph();
        var node = new Node { Type = "test" };
        var command = new AddNodeCommand(graph, node);
        command.Execute();

        command.Undo();

        Assert.Empty(graph.Nodes);
    }

    [Fact]
    public void RemoveNodeCommand_Execute_RemovesNodeFromGraph()
    {
        var graph = new Graph();
        var node = new Node { Type = "test" };
        graph.AddNode(node);
        var command = new RemoveNodeCommand(graph, node);

        command.Execute();

        Assert.Empty(graph.Nodes);
    }

    [Fact]
    public void RemoveNodeCommand_Undo_RestoresNodeAndEdges()
    {
        var graph = new Graph();
        var node1 = new Node 
        { 
            Type = "test1",
            Outputs = [new Port { Id = "out", Type = "data" }]
        };
        var node2 = new Node 
        { 
            Type = "test2",
            Inputs = [new Port { Id = "in", Type = "data" }]
        };
        graph.AddNode(node1);
        graph.AddNode(node2);
        
        var edge = new Edge 
        { 
            Source = node1.Id, 
            Target = node2.Id,
            SourcePort = "out",
            TargetPort = "in"
        };
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
        var node1 = new Node { Type = "test1" };
        var node2 = new Node { Type = "test2" };
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
        var node1 = new Node { Type = "test1" };
        var node2 = new Node { Type = "test2" };
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
        var node = new Node 
        { 
            Type = "test",
            Position = new Point(0, 0)
        };
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
        var node = new Node 
        { 
            Type = "test",
            Position = new Point(0, 0)
        };
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
        var node1 = new Node { Id = "n1", Type = "test1", Position = new Point(0, 0) };
        var node2 = new Node { Id = "n2", Type = "test2", Position = new Point(100, 100) };
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
