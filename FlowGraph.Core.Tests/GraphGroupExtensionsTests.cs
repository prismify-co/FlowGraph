using FlowGraph.Core;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Tests;

public class GraphGroupExtensionsTests
{
    private static Graph CreateTestGraphWithGroups()
    {
        var graph = new Graph();

        // Create a group
        var group = TestHelpers.CreateNode("group1", isGroup: true, label: "Group 1", x: 50, y: 50);

        // Create nodes in the group
        var node1 = TestHelpers.CreateNode("node1", parentGroupId: "group1", x: 100, y: 100);
        var node2 = TestHelpers.CreateNode("node2", parentGroupId: "group1", x: 200, y: 100);

        // Create node outside group
        var node3 = TestHelpers.CreateNode("node3", x: 400, y: 100);

        graph.AddNode(group);
        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddNode(node3);

        // Add edges
        graph.AddEdge(TestHelpers.CreateEdge("edge1", "node1", "node2", "out", "in")); // Internal
        graph.AddEdge(TestHelpers.CreateEdge("edge2", "node2", "node3", "out", "in")); // Crossing

        return graph;
    }

    [Fact]
    public void GetGroupChildren_ReturnsDirectChildren()
    {
        var graph = CreateTestGraphWithGroups();

        var children = graph.GetGroupChildren("group1").ToList();

        Assert.Equal(2, children.Count);
        Assert.Contains(children, n => n.Id == "node1");
        Assert.Contains(children, n => n.Id == "node2");
    }

    [Fact]
    public void GetGroupChildren_DoesNotReturnUnrelatedNodes()
    {
        var graph = CreateTestGraphWithGroups();

        var children = graph.GetGroupChildren("group1").ToList();

        Assert.DoesNotContain(children, n => n.Id == "node3");
    }

    [Fact]
    public void GetParentGroup_ReturnsParentGroup()
    {
        var graph = CreateTestGraphWithGroups();
        var node1 = graph.Elements.Nodes.First(n => n.Id == "node1");

        var parent = graph.GetParentGroup(node1);

        Assert.NotNull(parent);
        Assert.Equal("group1", parent.Id);
    }

    [Fact]
    public void GetParentGroup_ReturnsNullForTopLevelNode()
    {
        var graph = CreateTestGraphWithGroups();
        var node3 = graph.Elements.Nodes.First(n => n.Id == "node3");

        var parent = graph.GetParentGroup(node3);

        Assert.Null(parent);
    }

    [Fact]
    public void GetGroups_ReturnsOnlyGroupNodes()
    {
        var graph = CreateTestGraphWithGroups();

        var groups = graph.GetGroups().ToList();

        Assert.Single(groups);
        Assert.Equal("group1", groups[0].Id);
    }

    [Fact]
    public void GetTopLevelNodes_ReturnsNodesWithoutParent()
    {
        var graph = CreateTestGraphWithGroups();

        var topLevel = graph.GetTopLevelNodes().ToList();

        Assert.Equal(2, topLevel.Count); // group1 and node3
        Assert.Contains(topLevel, n => n.Id == "group1");
        Assert.Contains(topLevel, n => n.Id == "node3");
    }

    [Fact]
    public void IsEdgeInternalToGroup_ReturnsTrueForInternalEdge()
    {
        var graph = CreateTestGraphWithGroups();
        var internalEdge = graph.Elements.Edges.First(e => e.Id == "edge1");

        var isInternal = graph.IsEdgeInternalToGroup(internalEdge, "group1");

        Assert.True(isInternal);
    }

    [Fact]
    public void IsEdgeInternalToGroup_ReturnsFalseForCrossingEdge()
    {
        var graph = CreateTestGraphWithGroups();
        var crossingEdge = graph.Elements.Edges.First(e => e.Id == "edge2");

        var isInternal = graph.IsEdgeInternalToGroup(crossingEdge, "group1");

        Assert.False(isInternal);
    }

    [Fact]
    public void IsEdgeCrossingGroup_ReturnsTrueForCrossingEdge()
    {
        var graph = CreateTestGraphWithGroups();
        var crossingEdge = graph.Elements.Edges.First(e => e.Id == "edge2");

        var isCrossing = graph.IsEdgeCrossingGroup(crossingEdge, "group1");

        Assert.True(isCrossing);
    }

    [Fact]
    public void IsEdgeCrossingGroup_ReturnsFalseForInternalEdge()
    {
        var graph = CreateTestGraphWithGroups();
        var internalEdge = graph.Elements.Edges.First(e => e.Id == "edge1");

        var isCrossing = graph.IsEdgeCrossingGroup(internalEdge, "group1");

        Assert.False(isCrossing);
    }

    [Fact]
    public void GetEdgesInternalToGroup_ReturnsOnlyInternalEdges()
    {
        var graph = CreateTestGraphWithGroups();

        var internalEdges = graph.GetEdgesInternalToGroup("group1").ToList();

        Assert.Single(internalEdges);
        Assert.Equal("edge1", internalEdges[0].Id);
    }

    [Fact]
    public void GetEdgesCrossingGroup_ReturnsOnlyCrossingEdges()
    {
        var graph = CreateTestGraphWithGroups();

        var crossingEdges = graph.GetEdgesCrossingGroup("group1").ToList();

        Assert.Single(crossingEdges);
        Assert.Equal("edge2", crossingEdges[0].Id);
    }

    [Fact]
    public void CalculateGroupBounds_ReturnsCorrectBounds()
    {
        var graph = CreateTestGraphWithGroups();

        var (topLeft, width, height) = graph.CalculateGroupBounds("group1",
            padding: 20, headerHeight: 30, defaultNodeWidth: 150, defaultNodeHeight: 60);

        // Node1 at (100, 100), Node2 at (200, 100)
        // With 150 width, node2 extends to 350
        // With padding 20, left should be 80, right should be 370
        Assert.Equal(80, topLeft.X);
        Assert.Equal(50, topLeft.Y); // 100 - 20 padding - 30 header
        Assert.True(width >= 290); // 350 - 100 + 40 padding
    }

    [Fact]
    public void GetGroupChildrenRecursive_IncludesNestedChildren()
    {
        var graph = new Graph();

        // Outer group
        var outerGroup = TestHelpers.CreateNode("outer", isGroup: true);

        // Inner group (child of outer)
        var innerGroup = TestHelpers.CreateNode("inner", isGroup: true, parentGroupId: "outer");

        // Node in inner group
        var nestedNode = TestHelpers.CreateNode("nested", parentGroupId: "inner");

        // Direct child of outer
        var directChild = TestHelpers.CreateNode("direct", parentGroupId: "outer");

        graph.AddNode(outerGroup);
        graph.AddNode(innerGroup);
        graph.AddNode(nestedNode);
        graph.AddNode(directChild);

        var allChildren = graph.GetGroupChildrenRecursive("outer").ToList();

        Assert.Equal(3, allChildren.Count);
        Assert.Contains(allChildren, n => n.Id == "inner");
        Assert.Contains(allChildren, n => n.Id == "nested");
        Assert.Contains(allChildren, n => n.Id == "direct");
    }

    [Fact]
    public void GetAncestorGroups_ReturnsAllAncestors()
    {
        var graph = new Graph();
        var outer = TestHelpers.CreateNode("outer", isGroup: true);
        var inner = TestHelpers.CreateNode("inner", isGroup: true, parentGroupId: "outer");
        var node = TestHelpers.CreateNode("node", parentGroupId: "inner");

        graph.AddNode(outer);
        graph.AddNode(inner);
        graph.AddNode(node);

        var ancestors = graph.GetAncestorGroups(node).ToList();

        Assert.Equal(2, ancestors.Count);
        Assert.Equal("inner", ancestors[0].Id);
        Assert.Equal("outer", ancestors[1].Id);
    }

    [Fact]
    public void IsDescendantOf_ReturnsTrueForNestedNode()
    {
        var graph = new Graph();
        var outer = TestHelpers.CreateNode("outer", isGroup: true);
        var inner = TestHelpers.CreateNode("inner", isGroup: true, parentGroupId: "outer");
        var node = TestHelpers.CreateNode("node", parentGroupId: "inner");

        graph.AddNode(outer);
        graph.AddNode(inner);
        graph.AddNode(node);

        Assert.True(graph.IsDescendantOf(node, "outer"));
        Assert.True(graph.IsDescendantOf(node, "inner"));
    }

    [Fact]
    public void IsDescendantOf_ReturnsFalseForUnrelatedNode()
    {
        var graph = CreateTestGraphWithGroups();
        var node3 = graph.Elements.Nodes.First(n => n.Id == "node3");

        Assert.False(graph.IsDescendantOf(node3, "group1"));
    }
}
