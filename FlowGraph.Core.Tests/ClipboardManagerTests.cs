using FlowGraph.Avalonia;
using FlowGraph.Core;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Tests;

public class ClipboardManagerTests
{
    private static Graph CreateTestGraph()
    {
        var graph = new Graph();
        var node1 = TestHelpers.CreateNode("node1", type: "default", x: 100, y: 100,
            inputs: [new Port { Id = "in1", Type = "string" }],
            outputs: [new Port { Id = "out1", Type = "string" }]);
        var node2 = TestHelpers.CreateNode("node2", type: "default", x: 300, y: 100,
            inputs: [new Port { Id = "in1", Type = "string" }],
            outputs: [new Port { Id = "out1", Type = "string" }]);
        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.AddEdge(TestHelpers.CreateEdge("edge1", "node1", "node2", "out1", "in1"));
        return graph;
    }

    [Fact]
    public void Copy_SetsHasContentToTrue()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodes = graph.Nodes.ToList();

        clipboard.Copy(nodes, graph.Edges);

        Assert.True(clipboard.HasContent);
    }

    [Fact]
    public void Copy_EmptyNodes_HasContentIsFalse()
    {
        var clipboard = new ClipboardManager();

        clipboard.Copy([], []);

        Assert.False(clipboard.HasContent);
    }

    [Fact]
    public void Paste_CreateNewNodesWithNewIds()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToCopy = graph.Nodes.ToList();
        clipboard.Copy(nodesToCopy, graph.Edges);

        var (pastedNodes, _) = clipboard.Paste(graph, new Point(0, 0));

        Assert.Equal(2, pastedNodes.Count);
        Assert.DoesNotContain(pastedNodes, n => n.Id == "node1" || n.Id == "node2");
    }

    [Fact]
    public void Paste_PreservesEdgesBetweenPastedNodes()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToCopy = graph.Nodes.ToList();
        clipboard.Copy(nodesToCopy, graph.Edges);

        var (pastedNodes, pastedEdges) = clipboard.Paste(graph, new Point(0, 0));

        Assert.Single(pastedEdges);
        var edge = pastedEdges[0];
        Assert.Contains(pastedNodes, n => n.Id == edge.Source);
        Assert.Contains(pastedNodes, n => n.Id == edge.Target);
    }

    [Fact]
    public void Paste_AdjustsPositionToPasteLocation()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToCopy = graph.Nodes.Take(1).ToList(); // Just node1 at (100, 100)
        clipboard.Copy(nodesToCopy, graph.Edges);

        var (pastedNodes, _) = clipboard.Paste(graph, new Point(500, 500));

        // Original center was at (100, 100), paste at (500, 500)
        // Should offset by 400, 400
        Assert.Single(pastedNodes);
        Assert.Equal(500, pastedNodes[0].Position.X);
        Assert.Equal(500, pastedNodes[0].Position.Y);
    }

    [Fact]
    public void Paste_SetsIsSelectedOnPastedNodes()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToCopy = graph.Nodes.ToList();
        clipboard.Copy(nodesToCopy, graph.Edges);

        var (pastedNodes, _) = clipboard.Paste(graph, new Point(0, 0));

        Assert.All(pastedNodes, n => Assert.True(n.IsSelected));
    }

    [Fact]
    public void Paste_MultipleTimes_CreatesUniqueNodes()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToCopy = graph.Nodes.Take(1).ToList();
        clipboard.Copy(nodesToCopy, graph.Edges);

        var (firstPaste, _) = clipboard.Paste(graph, new Point(200, 200));
        var (secondPaste, _) = clipboard.Paste(graph, new Point(300, 300));

        Assert.NotEqual(firstPaste[0].Id, secondPaste[0].Id);
        Assert.Equal(4, graph.Nodes.Count); // Original 2 + 2 pasted
    }

    [Fact]
    public void Duplicate_CreatesNodesWithOffset()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToDuplicate = graph.Nodes.Take(1).ToList();
        var originalPosition = nodesToDuplicate[0].Position;

        var (duplicated, _) = clipboard.Duplicate(graph, nodesToDuplicate, graph.Edges, new Point(20, 20));

        Assert.Single(duplicated);
        Assert.Equal(originalPosition.X + 20, duplicated[0].Position.X);
        Assert.Equal(originalPosition.Y + 20, duplicated[0].Position.Y);
    }

    [Fact]
    public void Duplicate_PreservesEdgesBetweenDuplicatedNodes()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToDuplicate = graph.Nodes.ToList();

        var (duplicated, duplicatedEdges) = clipboard.Duplicate(graph, nodesToDuplicate, graph.Edges, new Point(20, 20));

        Assert.Equal(2, duplicated.Count);
        Assert.Single(duplicatedEdges);
        Assert.Contains(duplicated, n => n.Id == duplicatedEdges[0].Source);
        Assert.Contains(duplicated, n => n.Id == duplicatedEdges[0].Target);
    }

    [Fact]
    public void Duplicate_SetsIsSelectedOnDuplicatedNodes()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        var nodesToDuplicate = graph.Nodes.ToList();

        var (duplicated, _) = clipboard.Duplicate(graph, nodesToDuplicate, graph.Edges, new Point(20, 20));

        Assert.All(duplicated, n => Assert.True(n.IsSelected));
    }

    [Fact]
    public void Clear_RemovesClipboardContent()
    {
        var clipboard = new ClipboardManager();
        var graph = CreateTestGraph();
        clipboard.Copy(graph.Nodes.ToList(), graph.Edges);

        clipboard.Clear();

        Assert.False(clipboard.HasContent);
    }

    [Fact]
    public void Copy_PreservesNodeProperties()
    {
        var clipboard = new ClipboardManager();
        var node = TestHelpers.CreateNode("test", type: "custom", x: 50, y: 50,
            width: 200, height: 150,
            inputs: [new Port { Id = "in1", Type = "number", Label = "Input" }],
            outputs: [new Port { Id = "out1", Type = "number", Label = "Output" }]);
        // Note: IsResizable is a definition-level capability, we'll handle it if needed
        var graph = new Graph();
        graph.AddNode(node);

        clipboard.Copy([node], []);
        var (pasted, _) = clipboard.Paste(graph, new Point(50, 50));

        var pastedNode = pasted[0];
        Assert.Equal("custom", pastedNode.Type);
        Assert.Equal(200, pastedNode.Width);
        Assert.Equal(150, pastedNode.Height);
        Assert.True(pastedNode.IsResizable);
        Assert.Single(pastedNode.Inputs);
        Assert.Single(pastedNode.Outputs);
        Assert.Equal("number", pastedNode.Inputs[0].Type);
        Assert.Equal("Input", pastedNode.Inputs[0].Label);
    }
}
