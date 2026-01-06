using FlowGraph.Avalonia;
using FlowGraph.Core;
using Xunit;

namespace FlowGraph.Core.Tests;

public class GroupProxyManagerTests
{
    private (Graph graph, GroupProxyManager manager) CreateTestSetup()
    {
        var graph = new Graph();
        var manager = new GroupProxyManager(() => graph);
        return (graph, manager);
    }

    private void CreateNodesWithGroup(Graph graph)
    {
        // Create nodes
        var node1 = new Node
        {
            Id = "node1",
            Type = "default",
            Position = new Point(100, 100),
            Inputs = [new Port { Id = "in1", Type = "default" }],
            Outputs = [new Port { Id = "out1", Type = "default" }]
        };
        var node2 = new Node
        {
            Id = "node2",
            Type = "default",
            Position = new Point(200, 100),
            Inputs = [new Port { Id = "in2", Type = "default" }],
            Outputs = [new Port { Id = "out2", Type = "default" }]
        };
        var node3 = new Node
        {
            Id = "node3",
            Type = "default",
            Position = new Point(400, 100),
            Inputs = [new Port { Id = "in3", Type = "default" }],
            Outputs = [new Port { Id = "out3", Type = "default" }]
        };

        // Create a group containing node1 and node2
        var group = new Node
        {
            Id = "group1",
            Type = "group",
            IsGroup = true,
            Position = new Point(50, 50),
            Width = 300,
            Height = 200
        };

        graph.Nodes.Add(node1);
        graph.Nodes.Add(node2);
        graph.Nodes.Add(node3);
        graph.Nodes.Add(group);

        // Set parent group
        node1.ParentGroupId = "group1";
        node2.ParentGroupId = "group1";

        // Create edges:
        // Internal edge: node1 -> node2
        graph.AddEdge(new Edge
        {
            Id = "edge_internal",
            Source = "node1",
            SourcePort = "out1",
            Target = "node2",
            TargetPort = "in2"
        });

        // Crossing edge: node2 -> node3 (from inside group to outside)
        graph.AddEdge(new Edge
        {
            Id = "edge_crossing_out",
            Source = "node2",
            SourcePort = "out2",
            Target = "node3",
            TargetPort = "in3"
        });

        // Crossing edge: node3 -> node1 (from outside group to inside)
        graph.AddEdge(new Edge
        {
            Id = "edge_crossing_in",
            Source = "node3",
            SourcePort = "out3",
            Target = "node1",
            TargetPort = "in1"
        });
    }

    [Fact]
    public void OnGroupCollapsed_CreatesProxyPorts()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");

        var group = graph.Nodes.First(n => n.Id == "group1");
        
        // Should have created proxy ports for crossing edges
        Assert.NotEmpty(group.Inputs);  // For edge_crossing_in
        Assert.NotEmpty(group.Outputs); // For edge_crossing_out
    }

    [Fact]
    public void OnGroupCollapsed_ReroutesEdgesToGroup()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");

        // edge_crossing_out should now have group1 as source
        var crossingOut = graph.Edges.First(e => e.Id == "edge_crossing_out");
        Assert.Equal("group1", crossingOut.Source);
        Assert.Equal("node3", crossingOut.Target); // Target unchanged

        // edge_crossing_in should now have group1 as target
        var crossingIn = graph.Edges.First(e => e.Id == "edge_crossing_in");
        Assert.Equal("node3", crossingIn.Source); // Source unchanged
        Assert.Equal("group1", crossingIn.Target);
    }

    [Fact]
    public void OnGroupCollapsed_InternalEdgesUnchanged()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");

        // Internal edge should remain unchanged
        var internalEdge = graph.Edges.First(e => e.Id == "edge_internal");
        Assert.Equal("node1", internalEdge.Source);
        Assert.Equal("node2", internalEdge.Target);
    }

    [Fact]
    public void OnGroupExpanded_RestoresOriginalEdges()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");
        manager.OnGroupExpanded("group1");

        // Edges should be restored to original
        var crossingOut = graph.Edges.First(e => e.Id == "edge_crossing_out");
        Assert.Equal("node2", crossingOut.Source);
        Assert.Equal("out2", crossingOut.SourcePort);

        var crossingIn = graph.Edges.First(e => e.Id == "edge_crossing_in");
        Assert.Equal("node1", crossingIn.Target);
        Assert.Equal("in1", crossingIn.TargetPort);
    }

    [Fact]
    public void OnGroupExpanded_RemovesProxyPorts()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");
        manager.OnGroupExpanded("group1");

        var group = graph.Nodes.First(n => n.Id == "group1");
        
        // Proxy ports should be removed
        Assert.Empty(group.Inputs);
        Assert.Empty(group.Outputs);
    }

    [Fact]
    public void IsProxyEdge_ReturnsTrueForProxiedEdges()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");

        Assert.True(manager.IsProxyEdge("edge_crossing_out"));
        Assert.True(manager.IsProxyEdge("edge_crossing_in"));
        Assert.False(manager.IsProxyEdge("edge_internal"));
    }

    [Fact]
    public void GetProxyPorts_ReturnsProxyPortsForGroup()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");

        var proxyPorts = manager.GetProxyPorts("group1");
        Assert.Equal(2, proxyPorts.Count); // One input, one output proxy
    }

    [Fact]
    public void Clear_RemovesAllProxyState()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");
        manager.Clear();

        Assert.False(manager.IsProxyEdge("edge_crossing_out"));
        Assert.Empty(manager.GetProxyPorts("group1"));
    }

    [Fact]
    public void OnGroupCollapsed_RaisesProxyStateChangedEvent()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        bool eventRaised = false;
        manager.ProxyStateChanged += (s, e) => eventRaised = true;

        manager.OnGroupCollapsed("group1");

        Assert.True(eventRaised);
    }
}
