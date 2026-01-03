using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Core.Tests;

public class EdgeCommandsTests
{
    [Fact]
    public void AddEdgeCommand_Execute_AddsEdgeToGraph()
    {
        var graph = new Graph();
        var node1 = new Node { Outputs = [new Port { Id = "out", Type = "data" }] };
        var node2 = new Node { Inputs = [new Port { Id = "in", Type = "data" }] };
        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out",
            TargetPort = "in"
        };
        var command = new AddEdgeCommand(graph, edge);

        command.Execute();

        Assert.Single(graph.Edges);
    }

    [Fact]
    public void AddEdgeCommand_Undo_RemovesEdgeFromGraph()
    {
        var graph = new Graph();
        var node1 = new Node { Outputs = [new Port { Id = "out", Type = "data" }] };
        var node2 = new Node { Inputs = [new Port { Id = "in", Type = "data" }] };
        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out",
            TargetPort = "in"
        };
        var command = new AddEdgeCommand(graph, edge);
        command.Execute();

        command.Undo();

        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void RemoveEdgeCommand_Execute_RemovesEdgeFromGraph()
    {
        var graph = new Graph();
        var node1 = new Node { Outputs = [new Port { Id = "out", Type = "data" }] };
        var node2 = new Node { Inputs = [new Port { Id = "in", Type = "data" }] };
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

        var command = new RemoveEdgeCommand(graph, edge);
        command.Execute();

        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void RemoveEdgeCommand_Undo_RestoresEdge()
    {
        var graph = new Graph();
        var node1 = new Node { Outputs = [new Port { Id = "out", Type = "data" }] };
        var node2 = new Node { Inputs = [new Port { Id = "in", Type = "data" }] };
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

        var command = new RemoveEdgeCommand(graph, edge);
        command.Execute();
        command.Undo();

        Assert.Single(graph.Edges);
    }

    [Fact]
    public void RemoveEdgesCommand_Execute_RemovesMultipleEdges()
    {
        var graph = new Graph();
        var node1 = new Node { Outputs = [new Port { Id = "out1", Type = "data" }, new Port { Id = "out2", Type = "data" }] };
        var node2 = new Node { Inputs = [new Port { Id = "in1", Type = "data" }, new Port { Id = "in2", Type = "data" }] };
        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge1 = new Edge { Source = node1.Id, Target = node2.Id, SourcePort = "out1", TargetPort = "in1" };
        var edge2 = new Edge { Source = node1.Id, Target = node2.Id, SourcePort = "out2", TargetPort = "in2" };
        graph.AddEdge(edge1);
        graph.AddEdge(edge2);

        var command = new RemoveEdgesCommand(graph, [edge1, edge2]);
        command.Execute();

        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void RemoveEdgesCommand_Undo_RestoresAllEdges()
    {
        var graph = new Graph();
        var node1 = new Node { Outputs = [new Port { Id = "out1", Type = "data" }, new Port { Id = "out2", Type = "data" }] };
        var node2 = new Node { Inputs = [new Port { Id = "in1", Type = "data" }, new Port { Id = "in2", Type = "data" }] };
        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge1 = new Edge { Source = node1.Id, Target = node2.Id, SourcePort = "out1", TargetPort = "in1" };
        var edge2 = new Edge { Source = node1.Id, Target = node2.Id, SourcePort = "out2", TargetPort = "in2" };
        graph.AddEdge(edge1);
        graph.AddEdge(edge2);

        var command = new RemoveEdgesCommand(graph, [edge1, edge2]);
        command.Execute();
        command.Undo();

        Assert.Equal(2, graph.Edges.Count);
    }
}
