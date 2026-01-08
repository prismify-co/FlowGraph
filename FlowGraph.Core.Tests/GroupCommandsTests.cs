using FlowGraph.Core;
using FlowGraph.Core.Commands;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Tests;

public class GroupCommandsTests
{
    private static Graph CreateTestGraph()
    {
        var graph = new Graph();
        var node1 = TestHelpers.CreateNode("node1", type: "default", x: 100, y: 100);
        var node2 = TestHelpers.CreateNode("node2", type: "default", x: 300, y: 100);
        var node3 = TestHelpers.CreateNode("node3", type: "default", x: 200, y: 200);
        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddNode(node3);
        return graph;
    }

    #region GroupNodesCommand Tests

    [Fact]
    public void GroupNodesCommand_CreatesGroupNode()
    {
        var graph = CreateTestGraph();
        var command = new GroupNodesCommand(graph, ["node1", "node2"], "TestGroup");

        command.Execute();

        var groups = graph.Nodes.Where(n => n.IsGroup).ToList();
        Assert.Single(groups);
        Assert.Equal("TestGroup", groups[0].Label);
    }

    [Fact]
    public void GroupNodesCommand_SetsParentGroupIdOnChildren()
    {
        var graph = CreateTestGraph();
        var command = new GroupNodesCommand(graph, ["node1", "node2"]);

        command.Execute();

        var group = graph.Nodes.First(n => n.IsGroup);
        var node1 = graph.Nodes.First(n => n.Id == "node1");
        var node2 = graph.Nodes.First(n => n.Id == "node2");

        Assert.Equal(group.Id, node1.ParentGroupId);
        Assert.Equal(group.Id, node2.ParentGroupId);
    }

    [Fact]
    public void GroupNodesCommand_DoesNotAffectUnselectedNodes()
    {
        var graph = CreateTestGraph();
        var command = new GroupNodesCommand(graph, ["node1", "node2"]);

        command.Execute();

        var node3 = graph.Nodes.First(n => n.Id == "node3");
        Assert.Null(node3.ParentGroupId);
    }

    [Fact]
    public void GroupNodesCommand_Undo_RemovesGroupAndRestoresParents()
    {
        var graph = CreateTestGraph();
        var command = new GroupNodesCommand(graph, ["node1", "node2"]);
        command.Execute();
        var groupId = graph.Nodes.First(n => n.IsGroup).Id;

        command.Undo();

        Assert.DoesNotContain(graph.Nodes, n => n.Id == groupId);
        Assert.Null(graph.Nodes.First(n => n.Id == "node1").ParentGroupId);
        Assert.Null(graph.Nodes.First(n => n.Id == "node2").ParentGroupId);
    }

    [Fact]
    public void GroupNodesCommand_CalculatesCorrectBounds()
    {
        var graph = CreateTestGraph();
        var command = new GroupNodesCommand(graph, ["node1", "node2", "node3"]);

        command.Execute();

        var group = graph.Nodes.First(n => n.IsGroup);
        // Group should encompass all nodes with padding
        Assert.True(group.Position.X < 100); // Less than node1's X
        Assert.True(group.Position.Y < 100); // Less than node1's Y (with header)
        Assert.True(group.Width > 200); // Should span from node1 to node2 at least
    }

    #endregion

    #region UngroupNodesCommand Tests

    [Fact]
    public void UngroupNodesCommand_RemovesGroup()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var groupId = graph.Nodes.First(n => n.IsGroup).Id;

        var ungroupCommand = new UngroupNodesCommand(graph, groupId);
        ungroupCommand.Execute();

        Assert.DoesNotContain(graph.Nodes, n => n.Id == groupId);
    }

    [Fact]
    public void UngroupNodesCommand_ClearsParentGroupId()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var groupId = graph.Nodes.First(n => n.IsGroup).Id;

        var ungroupCommand = new UngroupNodesCommand(graph, groupId);
        ungroupCommand.Execute();

        Assert.Null(graph.Nodes.First(n => n.Id == "node1").ParentGroupId);
        Assert.Null(graph.Nodes.First(n => n.Id == "node2").ParentGroupId);
    }

    [Fact]
    public void UngroupNodesCommand_Undo_RestoresGroup()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var groupId = graph.Nodes.First(n => n.IsGroup).Id;

        var ungroupCommand = new UngroupNodesCommand(graph, groupId);
        ungroupCommand.Execute();
        ungroupCommand.Undo();

        Assert.Contains(graph.Nodes, n => n.Id == groupId && n.IsGroup);
        Assert.Equal(groupId, graph.Nodes.First(n => n.Id == "node1").ParentGroupId);
    }

    #endregion

    #region ToggleGroupCollapseCommand Tests

    [Fact]
    public void ToggleGroupCollapseCommand_TogglesCollapsedState()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var group = graph.Nodes.First(n => n.IsGroup);

        var toggleCommand = new ToggleGroupCollapseCommand(graph, group.Id);
        toggleCommand.Execute();

        Assert.True(group.IsCollapsed);
    }

    [Fact]
    public void ToggleGroupCollapseCommand_TogglesBackWhenCalledTwice()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var group = graph.Nodes.First(n => n.IsGroup);

        var toggle1 = new ToggleGroupCollapseCommand(graph, group.Id);
        toggle1.Execute();
        var toggle2 = new ToggleGroupCollapseCommand(graph, group.Id);
        toggle2.Execute();

        Assert.False(group.IsCollapsed);
    }

    [Fact]
    public void ToggleGroupCollapseCommand_Undo_RestoresPreviousState()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var group = graph.Nodes.First(n => n.IsGroup);

        var toggleCommand = new ToggleGroupCollapseCommand(graph, group.Id);
        toggleCommand.Execute();
        toggleCommand.Undo();

        Assert.False(group.IsCollapsed);
    }

    #endregion

    #region AddNodesToGroupCommand Tests

    [Fact]
    public void AddNodesToGroupCommand_AddsNodeToGroup()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var groupId = graph.Nodes.First(n => n.IsGroup).Id;

        var addCommand = new AddNodesToGroupCommand(graph, groupId, ["node3"]);
        addCommand.Execute();

        Assert.Equal(groupId, graph.Nodes.First(n => n.Id == "node3").ParentGroupId);
    }

    [Fact]
    public void AddNodesToGroupCommand_Undo_RestoresPreviousParent()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2"]);
        groupCommand.Execute();
        var groupId = graph.Nodes.First(n => n.IsGroup).Id;

        var addCommand = new AddNodesToGroupCommand(graph, groupId, ["node3"]);
        addCommand.Execute();
        addCommand.Undo();

        Assert.Null(graph.Nodes.First(n => n.Id == "node3").ParentGroupId);
    }

    #endregion

    #region RemoveNodesFromGroupCommand Tests

    [Fact]
    public void RemoveNodesFromGroupCommand_RemovesNodeFromGroup()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2", "node3"]);
        groupCommand.Execute();

        var removeCommand = new RemoveNodesFromGroupCommand(graph, ["node3"]);
        removeCommand.Execute();

        Assert.Null(graph.Nodes.First(n => n.Id == "node3").ParentGroupId);
    }

    [Fact]
    public void RemoveNodesFromGroupCommand_Undo_RestoresParentGroup()
    {
        var graph = CreateTestGraph();
        var groupCommand = new GroupNodesCommand(graph, ["node1", "node2", "node3"]);
        groupCommand.Execute();
        var groupId = graph.Nodes.First(n => n.IsGroup).Id;

        var removeCommand = new RemoveNodesFromGroupCommand(graph, ["node3"]);
        removeCommand.Execute();
        removeCommand.Undo();

        Assert.Equal(groupId, graph.Nodes.First(n => n.Id == "node3").ParentGroupId);
    }

    #endregion
}
