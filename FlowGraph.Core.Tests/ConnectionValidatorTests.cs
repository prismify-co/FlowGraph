using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Validation;
using FlowGraph.Core;

namespace FlowGraph.Core.Tests;

public class ConnectionValidatorTests
{
    private static Graph CreateTestGraph()
    {
        var graph = new Graph();
        var node1 = new Node
        {
            Id = "node1",
            Type = "default",
            Outputs = [new Port { Id = "out1", Type = "string" }]
        };
        var node2 = new Node
        {
            Id = "node2",
            Type = "default",
            Inputs = [new Port { Id = "in1", Type = "string" }]
        };
        var node3 = new Node
        {
            Id = "node3",
            Type = "default",
            Inputs = [new Port { Id = "in1", Type = "number" }],
            Outputs = [new Port { Id = "out1", Type = "number" }]
        };
        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddNode(node3);
        return graph;
    }

    private static ConnectionContext CreateContext(Graph graph, string sourceId, string sourcePort, string targetId, string targetPort)
    {
        var sourceNode = graph.Nodes.First(n => n.Id == sourceId);
        var targetNode = graph.Nodes.First(n => n.Id == targetId);
        return new ConnectionContext
        {
            SourceNode = sourceNode,
            SourcePort = sourceNode.Outputs.First(p => p.Id == sourcePort),
            TargetNode = targetNode,
            TargetPort = targetNode.Inputs.First(p => p.Id == targetPort),
            Graph = graph
        };
    }

    #region DefaultConnectionValidator Tests

    [Fact]
    public void DefaultValidator_AllowsAllConnections()
    {
        var graph = CreateTestGraph();
        var validator = new DefaultConnectionValidator();
        var context = CreateContext(graph, "node1", "out1", "node2", "in1");

        var result = validator.Validate(context);

        Assert.True(result.IsValid);
    }

    #endregion

    #region TypeMatchingConnectionValidator Tests

    [Fact]
    public void TypeMatchingValidator_AllowsMatchingTypes()
    {
        var graph = CreateTestGraph();
        var validator = new TypeMatchingConnectionValidator();
        var context = CreateContext(graph, "node1", "out1", "node2", "in1"); // string -> string

        var result = validator.Validate(context);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void TypeMatchingValidator_RejectsMismatchedTypes()
    {
        var graph = CreateTestGraph();
        var validator = new TypeMatchingConnectionValidator();
        var context = CreateContext(graph, "node1", "out1", "node3", "in1"); // string -> number

        var result = validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Contains("Type mismatch", result.Message);
    }

    [Fact]
    public void TypeMatchingValidator_AllowsAnyType()
    {
        var graph = new Graph();
        var node1 = new Node
        {
            Id = "node1",
            Outputs = [new Port { Id = "out1", Type = "any" }]
        };
        var node2 = new Node
        {
            Id = "node2",
            Inputs = [new Port { Id = "in1", Type = "string" }]
        };
        graph.AddNode(node1);
        graph.AddNode(node2);

        var validator = new TypeMatchingConnectionValidator();
        var context = new ConnectionContext
        {
            SourceNode = node1,
            SourcePort = node1.Outputs[0],
            TargetNode = node2,
            TargetPort = node2.Inputs[0],
            Graph = graph
        };

        var result = validator.Validate(context);

        Assert.True(result.IsValid);
    }

    #endregion

    #region NoDuplicateConnectionValidator Tests

    [Fact]
    public void NoDuplicateValidator_AllowsNewConnection()
    {
        var graph = CreateTestGraph();
        var validator = new NoDuplicateConnectionValidator();
        var context = CreateContext(graph, "node1", "out1", "node2", "in1");

        var result = validator.Validate(context);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NoDuplicateValidator_RejectsDuplicateConnection()
    {
        var graph = CreateTestGraph();
        graph.AddEdge(new Edge
        {
            Source = "node1",
            SourcePort = "out1",
            Target = "node2",
            TargetPort = "in1"
        });

        var validator = new NoDuplicateConnectionValidator();
        var context = CreateContext(graph, "node1", "out1", "node2", "in1");

        var result = validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Contains("already exists", result.Message);
    }

    #endregion

    #region NoSelfConnectionValidator Tests

    [Fact]
    public void NoSelfConnectionValidator_AllowsConnectionToDifferentNode()
    {
        var graph = CreateTestGraph();
        var validator = new NoSelfConnectionValidator();
        var context = CreateContext(graph, "node1", "out1", "node2", "in1");

        var result = validator.Validate(context);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NoSelfConnectionValidator_RejectsSelfConnection()
    {
        var graph = new Graph();
        var node = new Node
        {
            Id = "node1",
            Inputs = [new Port { Id = "in1", Type = "string" }],
            Outputs = [new Port { Id = "out1", Type = "string" }]
        };
        graph.AddNode(node);

        var validator = new NoSelfConnectionValidator();
        var context = new ConnectionContext
        {
            SourceNode = node,
            SourcePort = node.Outputs[0],
            TargetNode = node,
            TargetPort = node.Inputs[0],
            Graph = graph
        };

        var result = validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Contains("itself", result.Message);
    }

    #endregion

    #region NoCycleConnectionValidator Tests

    [Fact]
    public void NoCycleValidator_AllowsNonCyclicConnection()
    {
        var graph = CreateTestGraph();
        var validator = new NoCycleConnectionValidator();
        var context = CreateContext(graph, "node1", "out1", "node2", "in1");

        var result = validator.Validate(context);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NoCycleValidator_RejectsCyclicConnection()
    {
        var graph = new Graph();
        var node1 = new Node
        {
            Id = "node1",
            Inputs = [new Port { Id = "in1", Type = "any" }],
            Outputs = [new Port { Id = "out1", Type = "any" }]
        };
        var node2 = new Node
        {
            Id = "node2",
            Inputs = [new Port { Id = "in1", Type = "any" }],
            Outputs = [new Port { Id = "out1", Type = "any" }]
        };
        graph.AddNode(node1);
        graph.AddNode(node2);
        
        // Add edge from node1 -> node2
        graph.AddEdge(new Edge
        {
            Source = "node1",
            SourcePort = "out1",
            Target = "node2",
            TargetPort = "in1"
        });

        var validator = new NoCycleConnectionValidator();
        // Try to add edge from node2 -> node1 (would create cycle)
        var context = new ConnectionContext
        {
            SourceNode = node2,
            SourcePort = node2.Outputs[0],
            TargetNode = node1,
            TargetPort = node1.Inputs[0],
            Graph = graph
        };

        var result = validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Contains("cycle", result.Message);
    }

    [Fact]
    public void NoCycleValidator_DetectsIndirectCycle()
    {
        var graph = new Graph();
        var node1 = new Node { Id = "node1", Inputs = [new Port { Id = "in", Type = "any" }], Outputs = [new Port { Id = "out", Type = "any" }] };
        var node2 = new Node { Id = "node2", Inputs = [new Port { Id = "in", Type = "any" }], Outputs = [new Port { Id = "out", Type = "any" }] };
        var node3 = new Node { Id = "node3", Inputs = [new Port { Id = "in", Type = "any" }], Outputs = [new Port { Id = "out", Type = "any" }] };
        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddNode(node3);
        
        // node1 -> node2 -> node3
        graph.AddEdge(new Edge { Source = "node1", SourcePort = "out", Target = "node2", TargetPort = "in" });
        graph.AddEdge(new Edge { Source = "node2", SourcePort = "out", Target = "node3", TargetPort = "in" });

        var validator = new NoCycleConnectionValidator();
        // Try node3 -> node1 (would create cycle)
        var context = new ConnectionContext
        {
            SourceNode = node3,
            SourcePort = node3.Outputs[0],
            TargetNode = node1,
            TargetPort = node1.Inputs[0],
            Graph = graph
        };

        var result = validator.Validate(context);

        Assert.False(result.IsValid);
    }

    #endregion

    #region CompositeConnectionValidator Tests

    [Fact]
    public void CompositeValidator_PassesWhenAllValidatorsPass()
    {
        var graph = CreateTestGraph();
        var validator = new CompositeConnectionValidator()
            .Add(new NoSelfConnectionValidator())
            .Add(new NoDuplicateConnectionValidator());

        var context = CreateContext(graph, "node1", "out1", "node2", "in1");

        var result = validator.Validate(context);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CompositeValidator_FailsWhenAnyValidatorFails()
    {
        var graph = CreateTestGraph();
        graph.AddEdge(new Edge
        {
            Source = "node1",
            SourcePort = "out1",
            Target = "node2",
            TargetPort = "in1"
        });

        var validator = new CompositeConnectionValidator()
            .Add(new NoSelfConnectionValidator())
            .Add(new NoDuplicateConnectionValidator());

        var context = CreateContext(graph, "node1", "out1", "node2", "in1");

        var result = validator.Validate(context);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompositeValidator_CreateStandard_IncludesExpectedValidators()
    {
        var validator = CompositeConnectionValidator.CreateStandard();
        var graph = new Graph();
        var node = new Node
        {
            Id = "node1",
            Inputs = [new Port { Id = "in1", Type = "string" }],
            Outputs = [new Port { Id = "out1", Type = "string" }]
        };
        graph.AddNode(node);

        // Test self-connection (should fail)
        var context = new ConnectionContext
        {
            SourceNode = node,
            SourcePort = node.Outputs[0],
            TargetNode = node,
            TargetPort = node.Inputs[0],
            Graph = graph
        };

        var result = validator.Validate(context);

        Assert.False(result.IsValid);
    }

    #endregion
}
