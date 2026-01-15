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
}
