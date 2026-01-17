using FlowGraph.Core.Events;

namespace FlowGraph.Core.Tests;

public class GraphTests
{
    [Fact]
    public void Graph_ShouldStartEmpty()
    {
        var graph = new Graph();

        Assert.Empty(graph.Elements.Nodes);
        Assert.Empty(graph.Elements.Edges);
    }

    [Fact]
    public void AddNode_ShouldAddToCollection()
    {
        var graph = new Graph();
        var node = new Node { Type = "Test" };

        graph.AddNode(node);

        Assert.Single(graph.Elements.Nodes);
        Assert.Contains(node, graph.Elements.Nodes);
    }

    [Fact]
    public void AddNode_ShouldThrowForNull()
    {
        var graph = new Graph();

        Assert.Throws<ArgumentNullException>(() => graph.AddNode(null!));
    }

    [Fact]
    public void RemoveNode_ShouldRemoveFromCollection()
    {
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        graph.RemoveNode(node.Id);

        Assert.Empty(graph.Elements.Nodes);
    }

    [Fact]
    public void RemoveNode_ShouldRemoveConnectedEdges()
    {
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });

        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddEdge(new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        });

        Assert.Single(graph.Elements.Edges);

        graph.RemoveNode(node1.Id);

        Assert.Empty(graph.Elements.Edges);
        Assert.Single(graph.Elements.Nodes);
    }

    [Fact]
    public void RemoveNode_ShouldDoNothingForUnknownId()
    {
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        graph.RemoveNode("unknown-id");

        Assert.Single(graph.Elements.Nodes);
    }

    [Fact]
    public void AddEdge_ShouldAddToCollection()
    {
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });

        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };
        graph.AddEdge(edge);

        Assert.Single(graph.Elements.Edges);
        Assert.Contains(edge, graph.Elements.Edges);
    }

    [Fact]
    public void AddEdge_ShouldThrowForNull()
    {
        var graph = new Graph();

        Assert.Throws<ArgumentNullException>(() => graph.AddEdge(null!));
    }

    [Fact]
    public void AddEdge_ShouldThrowIfSourceNodeMissing()
    {
        var graph = new Graph();
        var node = new Node { Type = "Target" };
        node.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node);

        var edge = new Edge
        {
            Source = "missing-node",
            Target = node.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };

        Assert.Throws<InvalidOperationException>(() => graph.AddEdge(edge));
    }

    [Fact]
    public void AddEdge_ShouldThrowIfTargetNodeMissing()
    {
        var graph = new Graph();
        var node = new Node { Type = "Source" };
        node.Outputs.Add(new Port { Id = "out1", Type = "data" });
        graph.AddNode(node);

        var edge = new Edge
        {
            Source = node.Id,
            Target = "missing-node",
            SourcePort = "out1",
            TargetPort = "in1"
        };

        Assert.Throws<InvalidOperationException>(() => graph.AddEdge(edge));
    }

    [Fact]
    public void RemoveEdge_ShouldRemoveFromCollection()
    {
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });

        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };
        graph.AddEdge(edge);

        graph.RemoveEdge(edge.Id);

        Assert.Empty(graph.Elements.Edges);
    }

    [Fact]
    public void RemoveEdge_ShouldDoNothingForUnknownId()
    {
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });

        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };
        graph.AddEdge(edge);

        graph.RemoveEdge("unknown-id");

        Assert.Single(graph.Elements.Edges);
    }

    [Fact]
    public void NodesChanged_ShouldFireOnAdd()
    {
        var graph = new Graph();
        var eventFired = false;

        graph.NodesChanged += (s, e) =>
        {
            eventFired = true;
            Assert.Equal(System.Collections.Specialized.NotifyCollectionChangedAction.Add, e.Action);
        };

        graph.AddNode(new Node { Type = "Test" });

        Assert.True(eventFired);
    }

    [Fact]
    public void EdgesChanged_ShouldFireOnAdd()
    {
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node1);
        graph.AddNode(node2);

        var eventFired = false;
        graph.EdgesChanged += (s, e) =>
        {
            eventFired = true;
            Assert.Equal(System.Collections.Specialized.NotifyCollectionChangedAction.Add, e.Action);
        };

        graph.AddEdge(new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        });

        Assert.True(eventFired);
    }

    [Fact]
    public void EdgesChanged_CollectionIsUpdatedWhenEventFires()
    {
        // UPDATED: With the new single-source-of-truth architecture, Elements is always
        // up-to-date when events fire. EdgesChanged fires AFTER Elements is modified.
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node1);
        graph.AddNode(node2);

        var countDuringAddEvent = -1;
        var countDuringRemoveEvent = -1;

        graph.EdgesChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                countDuringAddEvent = graph.Edges.Count;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                countDuringRemoveEvent = graph.Edges.Count;
            }
        };

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };

        // Add edge - collection IS updated when event fires
        graph.AddEdge(edge);

        Assert.Equal(1, countDuringAddEvent); // Collection already has the edge
        Assert.Single(graph.Edges);

        // Remove edge - collection IS updated when event fires
        graph.RemoveEdge(edge.Id);

        Assert.Equal(0, countDuringRemoveEvent); // Collection already removed the edge
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void EdgesChanged_NewItemsContainsAddedEdge()
    {
        // NewItems in the event args contains the edge being added.
        // UI code can use this for incremental updates instead of full re-renders.
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node1);
        graph.AddNode(node2);

        Edge? edgeFromEvent = null;

        graph.EdgesChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                edgeFromEvent = e.NewItems?[0] as Edge;
            }
        };

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };
        graph.AddEdge(edge);

        Assert.NotNull(edgeFromEvent);
        Assert.Equal(edge.Id, edgeFromEvent!.Id);
    }

    [Fact]
    public void EdgesChanged_OldItemsContainsRemovedEdge()
    {
        // OldItems in the event args contains the edge being removed.
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node1);
        graph.AddNode(node2);

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };
        graph.AddEdge(edge);

        Edge? edgeFromEvent = null;

        graph.EdgesChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                edgeFromEvent = e.OldItems?[0] as Edge;
            }
        };

        graph.RemoveEdge(edge.Id);

        Assert.NotNull(edgeFromEvent);
        Assert.Equal(edge.Id, edgeFromEvent!.Id);
    }

    #region NodeBoundsChanged Event Tests

    [Fact]
    public void NodeBoundsChanged_ShouldFireWhenNodePositionChanges()
    {
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        Events.NodeBoundsChangedEventArgs? args = null;
        graph.NodeBoundsChanged += (s, e) => args = e;

        node.Position = new Point(100, 200);

        Assert.NotNull(args);
        Assert.Same(node, args.Node);
        Assert.Equal(100, args.BoundsChange.NewPosition.X);
        Assert.Equal(200, args.BoundsChange.NewPosition.Y);
    }

    [Fact]
    public void NodeBoundsChanged_ShouldFireWhenNodeSizeChanges()
    {
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        Events.NodeBoundsChangedEventArgs? args = null;
        graph.NodeBoundsChanged += (s, e) => args = e;

        node.State.Width = 200;

        Assert.NotNull(args);
        Assert.Same(node, args.Node);
        Assert.Equal(200, args.BoundsChange.NewWidth);
    }

    [Fact]
    public void NodeBoundsChanged_ShouldUseLazySubscription()
    {
        // When no subscribers, changing node position should not require subscription overhead
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        // No subscriber - this should work fine without any subscription management
        node.Position = new Point(100, 200);

        // Now add subscriber
        var eventCount = 0;
        graph.NodeBoundsChanged += (s, e) => eventCount++;

        // Should get events now (2 events: one for X, one for Y)
        node.Position = new Point(200, 300);
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void NodeBoundsChanged_ShouldSubscribeToNewNodesWhenHasSubscribers()
    {
        var graph = new Graph();
        var eventCount = 0;

        // Subscribe BEFORE adding node
        graph.NodeBoundsChanged += (s, e) => eventCount++;

        // Now add node
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        // Should get events for the new node (2 events: one for X, one for Y)
        node.Position = new Point(100, 200);
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void NodeBoundsChanged_ShouldUnsubscribeWhenNodeRemoved()
    {
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        var eventCount = 0;
        graph.NodeBoundsChanged += (s, e) => eventCount++;

        // Get initial events (2: one for X, one for Y)
        node.Position = new Point(100, 200);
        Assert.Equal(2, eventCount);

        // Remove node
        graph.RemoveNode(node.Id);

        // Should NOT get events after removal
        node.Position = new Point(200, 300);
        Assert.Equal(2, eventCount); // Still 2, not 4
    }

    [Fact]
    public void NodeBoundsChanged_ShouldCleanupWhenLastSubscriberRemoved()
    {
        var graph = new Graph();
        var node = new Node { Type = "Test" };
        graph.AddNode(node);

        var eventCount = 0;
        EventHandler<NodeBoundsChangedEventArgs> handler = (s, e) => eventCount++;
        
        graph.NodeBoundsChanged += handler;
        node.Position = new Point(100, 200);
        Assert.Equal(2, eventCount); // 2 events: one for X, one for Y

        // Remove subscriber
        graph.NodeBoundsChanged -= handler;

        // Graph should have unsubscribed from nodes (no way to verify directly,
        // but this tests the code path without errors)
        node.Position = new Point(200, 300);
        Assert.Equal(2, eventCount); // Still 2 because handler was removed
    }

    #endregion
}
