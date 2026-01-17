using FlowGraph.Core;
using FlowGraph.Core.Events;
using Xunit;

namespace FlowGraph.Core.Tests.Rendering;

/// <summary>
/// Tests for the cache invalidation infrastructure that DirectCanvasRenderer relies on.
/// These tests verify that the Node and Graph events fire correctly, which ensures
/// the DirectCanvasRenderer's event-based safety net will work properly.
/// 
/// Note: DirectCanvasRenderer itself requires Avalonia runtime context (ThemeResources),
/// so we test the underlying event infrastructure that makes cache invalidation work.
/// </summary>
public class CacheInvalidationInfrastructureTests
{
    #region Node BoundsChanged Event Tests

    [Fact]
    public void Node_PositionChange_RaisesBoundsChanged()
    {
        // Arrange
        var node = new Node { Type = "Test", Position = new Point(0, 0) };
        BoundsChangedEventArgs? args = null;
        node.BoundsChanged += (s, e) => args = e;
        
        // Act
        node.Position = new Point(100, 200);
        
        // Assert
        Assert.NotNull(args);
        Assert.Equal(100, args.NewPosition.X);
        Assert.Equal(200, args.NewPosition.Y);
        Assert.True(args.PositionOnly);
    }

    [Fact]
    public void Node_SizeChange_RaisesBoundsChanged()
    {
        // Arrange
        var node = new Node { Type = "Test" };
        BoundsChangedEventArgs? args = null;
        node.BoundsChanged += (s, e) => args = e;
        
        // Act
        node.State.Width = 200;
        
        // Assert
        Assert.NotNull(args);
        Assert.Equal(200, args.NewWidth);
        Assert.False(args.PositionOnly);
    }

    [Fact]
    public void Node_IndividualXChange_RaisesBoundsChanged()
    {
        // Arrange
        var node = new Node { Type = "Test" };
        var eventCount = 0;
        node.BoundsChanged += (s, e) => eventCount++;
        
        // Act
        node.State.X = 50;
        
        // Assert
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void Node_IndividualYChange_RaisesBoundsChanged()
    {
        // Arrange
        var node = new Node { Type = "Test" };
        var eventCount = 0;
        node.BoundsChanged += (s, e) => eventCount++;
        
        // Act
        node.State.Y = 75;
        
        // Assert
        Assert.Equal(1, eventCount);
    }

    #endregion

    #region Graph NodeBoundsChanged Aggregate Event Tests

    [Fact]
    public void Graph_NodeBoundsChanged_FiresWhenNodeMoves()
    {
        // Arrange - this is the key event DirectCanvasRenderer subscribes to
        var graph = new Graph();
        var node = new Node { Type = "Test", Position = new Point(0, 0) };
        graph.AddNode(node);
        
        NodeBoundsChangedEventArgs? args = null;
        graph.NodeBoundsChanged += (s, e) => args = e;
        
        // Act
        node.Position = new Point(100, 100);
        
        // Assert
        Assert.NotNull(args);
        Assert.Same(node, args.Node);
    }

    [Fact]
    public void Graph_NodeBoundsChanged_FiresWhenNodeResizes()
    {
        // Arrange
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);
        
        NodeBoundsChangedEventArgs? args = null;
        graph.NodeBoundsChanged += (s, e) => args = e;
        
        // Act
        node.State.Width = 300;
        
        // Assert
        Assert.NotNull(args);
        Assert.Same(node, args.Node);
    }

    [Fact]
    public void Graph_NodeBoundsChanged_UsesLazySubscription()
    {
        // Arrange - lazy subscription means no overhead when no subscribers
        var graph = new Graph();
        var node = new Node { Type = "Test", Position = new Point(0, 0) };
        graph.AddNode(node);
        
        // Move node BEFORE subscribing - should not crash
        node.Position = new Point(50, 50);
        
        // Now subscribe
        var eventCount = 0;
        graph.NodeBoundsChanged += (s, e) => eventCount++;
        
        // Act - move again after subscribing
        node.Position = new Point(100, 100);
        
        // Assert - should fire now
        Assert.True(eventCount > 0);
    }

    [Fact]
    public void Graph_NodeBoundsChanged_AutoSubscribesToNewNodes()
    {
        // Arrange - subscribe first, then add node
        var graph = new Graph();
        var eventCount = 0;
        graph.NodeBoundsChanged += (s, e) => eventCount++;
        
        // Add node AFTER subscribing
        var node = new Node { Type = "Test", Position = new Point(0, 0) };
        graph.AddNode(node);
        
        // Act
        node.Position = new Point(100, 100);
        
        // Assert - event should fire for newly added node
        Assert.True(eventCount > 0);
    }

    [Fact]
    public void Graph_NodeBoundsChanged_StopsTrackingRemovedNodes()
    {
        // Arrange
        var graph = new Graph();
        var node = new Node { Type = "Test", Position = new Point(0, 0) };
        graph.AddNode(node);
        
        var eventCount = 0;
        graph.NodeBoundsChanged += (s, e) => eventCount++;
        
        // Verify events fire before removal
        node.Position = new Point(50, 50);
        var countBeforeRemoval = eventCount;
        Assert.True(countBeforeRemoval > 0);
        
        // Act - remove node
        graph.RemoveNode(node.Id);
        
        // Move node after removal
        node.Position = new Point(200, 200);
        
        // Assert - should NOT increase count after removal
        Assert.Equal(countBeforeRemoval, eventCount);
    }

    [Fact]
    public void Graph_NodeBoundsChanged_CleansUpWhenLastSubscriberRemoved()
    {
        // Arrange
        var graph = new Graph();
        var node = new Node { Type = "Test", Position = new Point(0, 0) };
        graph.AddNode(node);
        
        var eventCount = 0;
        EventHandler<NodeBoundsChangedEventArgs> handler = (s, e) => eventCount++;
        
        graph.NodeBoundsChanged += handler;
        node.Position = new Point(50, 50);
        var countBeforeUnsubscribe = eventCount;
        
        // Act - unsubscribe
        graph.NodeBoundsChanged -= handler;
        node.Position = new Point(100, 100);
        
        // Assert - count should not increase after unsubscribe
        Assert.Equal(countBeforeUnsubscribe, eventCount);
    }

    #endregion

    #region Multiple Nodes Tests

    [Fact]
    public void Graph_MultipleNodeMoves_AllFireEvents()
    {
        // Arrange
        var graph = new Graph();
        var nodes = new List<Node>();
        for (int i = 0; i < 5; i++)
        {
            var node = new Node { Type = "Test", Position = new Point(i * 100, 0) };
            nodes.Add(node);
            graph.AddNode(node);
        }
        
        var movedNodeIds = new HashSet<string>();
        graph.NodeBoundsChanged += (s, e) => movedNodeIds.Add(e.Node.Id);
        
        // Act - move all nodes
        foreach (var node in nodes)
        {
            node.Position = new Point(node.Position.X, 500);
        }
        
        // Assert - all nodes should have fired events
        foreach (var node in nodes)
        {
            Assert.Contains(node.Id, movedNodeIds);
        }
    }

    [Fact]
    public void Graph_ClearAndReAdd_EventsStillWork()
    {
        // Arrange - this simulates the stress test scenario
        var graph = new Graph();
        var oldNode = new Node { Type = "Test", Position = new Point(0, 0) };
        graph.AddNode(oldNode);
        
        var eventFiredForNewNode = false;
        graph.NodeBoundsChanged += (s, e) => 
        {
            if (e.Node.Id != oldNode.Id)
                eventFiredForNewNode = true;
        };
        
        // Act - remove old node and add new one
        graph.RemoveNode(oldNode.Id);
        
        var newNode = new Node { Type = "Test", Position = new Point(100, 100) };
        graph.AddNode(newNode);
        
        // Move the new node
        newNode.Position = new Point(200, 200);
        
        // Assert - event should fire for new node
        Assert.True(eventFiredForNewNode);
    }

    #endregion

    #region Graph Switch Simulation Tests

    [Fact]
    public void GraphSwitch_OldGraphEventsDoNotFire()
    {
        // Arrange - simulates switching from one graph to another
        var graph1 = new Graph();
        var node1 = new Node { Type = "Test", Position = new Point(0, 0) };
        graph1.AddNode(node1);
        
        var graph2 = new Graph();
        var node2 = new Node { Type = "Test", Position = new Point(100, 100) };
        graph2.AddNode(node2);
        
        // Subscribe to graph1
        var graph1EventCount = 0;
        EventHandler<NodeBoundsChangedEventArgs> handler = (s, e) => graph1EventCount++;
        graph1.NodeBoundsChanged += handler;
        
        // Move node1 to verify subscription works
        node1.Position = new Point(50, 50);
        Assert.Equal(2, graph1EventCount); // 2 events: X change, Y change
        
        // Act - "switch" to graph2 by unsubscribing from graph1
        graph1.NodeBoundsChanged -= handler;
        
        // Move node1 again - should NOT fire
        node1.Position = new Point(75, 75);
        
        // Assert - count should not have changed
        Assert.Equal(2, graph1EventCount);
    }

    #endregion
}
