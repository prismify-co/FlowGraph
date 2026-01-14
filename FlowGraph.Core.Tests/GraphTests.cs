// CS0618: Suppress obsolete warnings - these tests verify backward-compatible
// CollectionChanged events on the obsolete Graph.Nodes and Graph.Edges properties.
#pragma warning disable CS0618

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
    public void Nodes_CollectionChanged_ShouldFireOnAdd()
    {
        var graph = new Graph();
        var eventFired = false;

        graph.Nodes.CollectionChanged += (s, e) =>
        {
            eventFired = true;
            Assert.Equal(System.Collections.Specialized.NotifyCollectionChangedAction.Add, e.Action);
        };

        graph.AddNode(new Node { Type = "Test" });

        Assert.True(eventFired);
    }

    [Fact]
    public void Edges_CollectionChanged_ShouldFireOnAdd()
    {
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node1);
        graph.AddNode(node2);

        var eventFired = false;
        graph.Edges.CollectionChanged += (s, e) =>
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
    public void Edges_CollectionChanged_FiresBeforeCollectionIsUpdated()
    {
        // IMPORTANT: This test documents the behavior that CollectionChanged fires
        // BEFORE the collection is actually modified. This is why FlowCanvas.DataBinding
        // uses Dispatcher.UIThread.Post() to defer rendering - the collection must be
        // fully updated before we can accurately iterate over it for rendering.
        //
        // This is a regression test to ensure we don't accidentally "fix" the collection
        // behavior without also updating the UI code that depends on deferred rendering.
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node1);
        graph.AddNode(node2);

        var countDuringAddEvent = -1;
        var countDuringRemoveEvent = -1;

        graph.Edges.CollectionChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                countDuringAddEvent = graph.Elements.Edges.Count();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                countDuringRemoveEvent = graph.Elements.Edges.Count();
            }
        };

        var edge = new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out1",
            TargetPort = "in1"
        };

        // Add edge - event fires before collection is updated
        graph.AddEdge(edge);

        // The count during the Add event is 0 (not yet added), but after AddEdge returns it's 1
        Assert.Equal(0, countDuringAddEvent); // Event fires BEFORE add
        Assert.Single(graph.Elements.Edges); // But collection IS updated after method returns

        // Remove edge - event fires before collection is updated  
        graph.RemoveEdge(edge.Id);

        // The count during the Remove event is 1 (not yet removed), but after RemoveEdge returns it's 0
        Assert.Equal(1, countDuringRemoveEvent); // Event fires BEFORE remove
        Assert.Empty(graph.Elements.Edges); // But collection IS updated after method returns
    }

    [Fact]
    public void Edges_CollectionChanged_NewItemsContainsAddedEdge()
    {
        // Even though the collection count is stale during the event,
        // the NewItems in the event args should contain the edge being added.
        // UI code can use this for incremental updates instead of full re-renders.
        var graph = new Graph();
        var node1 = new Node { Type = "Source" };
        var node2 = new Node { Type = "Target" };
        node1.Outputs.Add(new Port { Id = "out1", Type = "data" });
        node2.Inputs.Add(new Port { Id = "in1", Type = "data" });
        graph.AddNode(node1);
        graph.AddNode(node2);

        Edge? edgeFromEvent = null;

        graph.Edges.CollectionChanged += (s, e) =>
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
    public void Edges_CollectionChanged_OldItemsContainsRemovedEdge()
    {
        // Even though the collection count is stale during the event,
        // the OldItems in the event args should contain the edge being removed.
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

        graph.Edges.CollectionChanged += (s, e) =>
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
}
