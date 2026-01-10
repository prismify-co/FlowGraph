using FlowGraph.Avalonia;
using FlowGraph.Core;
using FlowGraph.Core.Models;
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
        var node1 = TestHelpers.CreateNode("node1", type: "default", x: 100, y: 100,
            inputs: [new Port { Id = "in1", Type = "default" }],
            outputs: [new Port { Id = "out1", Type = "default" }]);
        var node2 = TestHelpers.CreateNode("node2", type: "default", x: 200, y: 100,
            inputs: [new Port { Id = "in2", Type = "default" }],
            outputs: [new Port { Id = "out2", Type = "default" }]);
        var node3 = TestHelpers.CreateNode("node3", type: "default", x: 400, y: 100,
            inputs: [new Port { Id = "in3", Type = "default" }],
            outputs: [new Port { Id = "out3", Type = "default" }]);

        // Create a group containing node1 and node2
        var group = TestHelpers.CreateNode("group1", type: "group", isGroup: true,
            x: 50, y: 50, width: 300, height: 200);

        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddNode(node3);
        graph.AddNode(group);

        // Set parent group
        node1.ParentGroupId = "group1";
        node2.ParentGroupId = "group1";

        // Create edges:
        // Internal edge: node1 -> node2
        graph.AddEdge(TestHelpers.CreateEdge("edge_internal", "node1", "node2", "out1", "in2"));

        // Crossing edge: node2 -> node3 (from inside group to outside)
        graph.AddEdge(TestHelpers.CreateEdge("edge_crossing_out", "node2", "node3", "out2", "in3"));

        // Crossing edge: node3 -> node1 (from outside group to inside)
        graph.AddEdge(TestHelpers.CreateEdge("edge_crossing_in", "node3", "node1", "out3", "in1"));
    }

    [Fact]
    public void OnGroupCollapsed_CreatesProxyPorts()
    {
        var (graph, manager) = CreateTestSetup();
        CreateNodesWithGroup(graph);

        manager.OnGroupCollapsed("group1");

        var group = graph.Elements.Nodes.First(n => n.Id == "group1");

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
        var crossingOut = graph.Elements.Edges.First(e => e.Id == "edge_crossing_out");
        Assert.Equal("group1", crossingOut.Source);
        Assert.Equal("node3", crossingOut.Target); // Target unchanged

        // edge_crossing_in should now have group1 as target
        var crossingIn = graph.Elements.Edges.First(e => e.Id == "edge_crossing_in");
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
        var internalEdge = graph.Elements.Edges.First(e => e.Id == "edge_internal");
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
        var crossingOut = graph.Elements.Edges.First(e => e.Id == "edge_crossing_out");
        Assert.Equal("node2", crossingOut.Source);
        Assert.Equal("out2", crossingOut.SourcePort);

        var crossingIn = graph.Elements.Edges.First(e => e.Id == "edge_crossing_in");
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

        var group = graph.Elements.Nodes.First(n => n.Id == "group1");

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
