using FlowGraph.Avalonia;
using FlowGraph.Core;
using FlowGraph.Core.Commands;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Test implementation of IGraphContext for unit testing.
/// </summary>
internal class TestGraphContext : IGraphContext
{
    public Graph? Graph { get; set; }
}

public class GroupManagerTests
{
    private static (Graph graph, GroupManager manager, CommandHistory history) CreateTestSetup()
    {
        var graph = new Graph();
        var history = new CommandHistory();
        var settings = FlowCanvasSettings.Default;
        var context = new TestGraphContext { Graph = graph };
        var manager = new GroupManager(context, history, settings);
        return (graph, manager, history);
    }

    private static void AddTestNodes(Graph graph)
    {
        graph.AddNode(TestHelpers.CreateNode("node1", x: 100, y: 100, width: 150, height: 60));
        graph.AddNode(TestHelpers.CreateNode("node2", x: 300, y: 100, width: 150, height: 60));
        graph.AddNode(TestHelpers.CreateNode("node3", x: 200, y: 200, width: 150, height: 60));
    }

    #region CreateGroup Tests

    [Fact]
    public void CreateGroup_WithValidNodes_CreatesGroup()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);

        var group = manager.CreateGroup(["node1", "node2"], "Test Group");

        Assert.NotNull(group);
        Assert.True(group.IsGroup);
        Assert.Equal("Test Group", group.Label);
    }

    [Fact]
    public void CreateGroup_SetsParentGroupIdOnChildren()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);

        var group = manager.CreateGroup(["node1", "node2"]);

        Assert.NotNull(group);
        Assert.Equal(group.Id, graph.Elements.Nodes.First(n => n.Id == "node1").ParentGroupId);
        Assert.Equal(group.Id, graph.Elements.Nodes.First(n => n.Id == "node2").ParentGroupId);
    }

    [Fact]
    public void CreateGroup_WithSingleNode_ReturnsNull()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);

        var group = manager.CreateGroup(["node1"]);

        Assert.Null(group);
    }

    #endregion

    #region Collapse Tests

    [Fact]
    public void ToggleCollapse_TogglesCollapsedState()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        manager.ToggleCollapse(group!.Id);

        Assert.True(group.IsCollapsed);
    }

    [Fact]
    public void SetCollapsed_SetsCorrectState()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        manager.SetCollapsed(group!.Id, true);

        Assert.True(group.IsCollapsed);
    }

    [Fact]
    public void CollapseAll_CollapsesAllGroups()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group1 = manager.CreateGroup(["node1", "node2"]);

        graph.AddNode(TestHelpers.CreateNode("node4", x: 400, y: 100));
        graph.AddNode(TestHelpers.CreateNode("node5", x: 500, y: 100));
        var group2 = manager.CreateGroup(["node4", "node5"]);

        manager.CollapseAll();

        Assert.True(group1!.IsCollapsed);
        Assert.True(group2!.IsCollapsed);
    }

    [Fact]
    public void ExpandAll_ExpandsAllGroups()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);
        manager.SetCollapsed(group!.Id, true);

        manager.ExpandAll();

        Assert.False(group.IsCollapsed);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void IsNodeVisible_ReturnsTrueForUngroupedNode()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);

        var isVisible = manager.IsNodeVisible("node1");

        Assert.True(isVisible);
    }

    [Fact]
    public void IsNodeVisible_ReturnsTrueForExpandedGroupChild()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        manager.CreateGroup(["node1", "node2"]);

        var isVisible = manager.IsNodeVisible("node1");

        Assert.True(isVisible);
    }

    [Fact]
    public void IsNodeVisible_ReturnsFalseForCollapsedGroupChild()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);
        manager.SetCollapsed(group!.Id, true);

        var isVisible = manager.IsNodeVisible("node1");

        Assert.False(isVisible);
    }

    [Fact]
    public void GetVisibleNodes_ExcludesCollapsedGroupChildren()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);
        manager.SetCollapsed(group!.Id, true);

        var visibleNodes = manager.GetVisibleNodes().ToList();

        Assert.DoesNotContain(visibleNodes, n => n.Id == "node1");
        Assert.DoesNotContain(visibleNodes, n => n.Id == "node2");
        Assert.Contains(visibleNodes, n => n.Id == "node3");
        Assert.Contains(visibleNodes, n => n.Id == group.Id); // Group itself is visible
    }

    #endregion

    #region AddNodesToGroup Tests

    [Fact]
    public void AddNodesToGroup_AddsNodesToExistingGroup()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        manager.AddNodesToGroup(group!.Id, ["node3"]);

        Assert.Equal(group.Id, graph.Elements.Nodes.First(n => n.Id == "node3").ParentGroupId);
    }

    [Fact]
    public void AddNodesToGroup_RaisesNodesAddedToGroupEvent()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        string? eventGroupId = null;
        manager.NodesAddedToGroup += (s, e) => eventGroupId = e.GroupId;

        manager.AddNodesToGroup(group!.Id, ["node3"]);

        Assert.Equal(group.Id, eventGroupId);
    }

    #endregion

    #region RemoveNodesFromGroup Tests

    [Fact]
    public void RemoveNodesFromGroup_ClearsParentGroupId()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2", "node3"]);

        manager.RemoveNodesFromGroup(["node3"]);

        Assert.Null(graph.Elements.Nodes.First(n => n.Id == "node3").ParentGroupId);
    }

    #endregion

    #region GetGroupAtPoint Tests

    [Fact]
    public void GetGroupAtPoint_ReturnsGroupIdWhenPointIsInsideGroup()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        // Point inside the group bounds
        var groupId = manager.GetGroupAtPoint(new Point(150, 120));

        Assert.Equal(group!.Id, groupId);
    }

    [Fact]
    public void GetGroupAtPoint_ReturnsNullWhenPointIsOutsideGroups()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        manager.CreateGroup(["node1", "node2"]);

        // Point far outside all groups
        var groupId = manager.GetGroupAtPoint(new Point(1000, 1000));

        Assert.Null(groupId);
    }

    [Fact]
    public void GetGroupAtPoint_ReturnsNullForCollapsedGroup()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);
        manager.SetCollapsed(group!.Id, true);

        // Point that would be inside the group if expanded
        var groupId = manager.GetGroupAtPoint(new Point(150, 120));

        // Collapsed groups don't accept drops
        Assert.Null(groupId);
    }

    #endregion

    #region AutoResize Tests

    [Fact]
    public void AutoResizeGroup_ResizesToFitChildren()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        // Move a node to change bounds
        var node2 = graph.Elements.Nodes.First(n => n.Id == "node2");
        node2.Position = new Point(500, 300);

        manager.AutoResizeGroup(group!.Id);

        // Group should have resized to fit the new node position
        Assert.True(group.Width > 400); // Should be wider now
        Assert.True(group.Height > 200); // Should be taller now
    }

    #endregion

    #region GetAllGroups Tests

    [Fact]
    public void GetAllGroups_ReturnsOnlyGroupNodes()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        var groups = manager.GetAllGroups().ToList();

        Assert.Single(groups);
        Assert.Equal(group!.Id, groups[0].Id);
    }

    #endregion

    #region GetGroupChildren Tests

    [Fact]
    public void GetGroupChildren_ReturnsDirectChildren()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        var children = manager.GetGroupChildren(group!.Id).ToList();

        Assert.Equal(2, children.Count);
        Assert.Contains(children, n => n.Id == "node1");
        Assert.Contains(children, n => n.Id == "node2");
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ToggleCollapse_RaisesGroupCollapsedChangedEvent()
    {
        var (graph, manager, _) = CreateTestSetup();
        AddTestNodes(graph);
        var group = manager.CreateGroup(["node1", "node2"]);

        bool eventRaised = false;
        bool? eventIsCollapsed = null;
        manager.GroupCollapsedChanged += (s, e) =>
        {
            eventRaised = true;
            eventIsCollapsed = e.IsCollapsed;
        };

        manager.ToggleCollapse(group!.Id);

        Assert.True(eventRaised);
        Assert.True(eventIsCollapsed);
    }

    #endregion
}
