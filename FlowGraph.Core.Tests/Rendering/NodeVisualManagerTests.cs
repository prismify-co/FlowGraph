using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using Xunit;

namespace FlowGraph.Core.Tests.Rendering;

public class NodeVisualManagerTests
{
    private RenderContext CreateContext() => new RenderContext(new FlowCanvasSettings());

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NodeVisualManager(null!));
    }

    [Fact]
    public void Constructor_WithNullRegistry_CreatesDefaultRegistry()
    {
        var context = CreateContext();
        var manager = new NodeVisualManager(context, null);
        
        Assert.NotNull(manager.NodeRenderers);
    }

    [Fact]
    public void GetNodeVisual_WhenNotRendered_ReturnsNull()
    {
        var context = CreateContext();
        var manager = new NodeVisualManager(context);
        
        Assert.Null(manager.GetNodeVisual("nonexistent"));
    }

    [Fact]
    public void GetPortVisual_WhenNotRendered_ReturnsNull()
    {
        var context = CreateContext();
        var manager = new NodeVisualManager(context);
        
        Assert.Null(manager.GetPortVisual("node", "port"));
    }

    [Fact]
    public void GetNodeDimensions_WithExplicitDimensions_ReturnsNodeDimensions()
    {
        var context = CreateContext();
        var manager = new NodeVisualManager(context);
        var node = new Node
        {
            Width = 200,
            Height = 100
        };
        
        var (width, height) = manager.GetNodeDimensions(node);
        
        Assert.Equal(200, width);
        Assert.Equal(100, height);
    }

    [Fact]
    public void GetNodeDimensions_WithoutExplicitDimensions_ReturnsDefaultDimensions()
    {
        var settings = new FlowCanvasSettings { NodeWidth = 150, NodeHeight = 80 };
        var context = new RenderContext(settings);
        var manager = new NodeVisualManager(context);
        var node = new Node { Type = "unknown" };
        
        var (width, height) = manager.GetNodeDimensions(node);
        
        Assert.Equal(150, width);
        Assert.Equal(80, height);
    }

    [Fact]
    public void IsNodeVisible_NodeWithNoParent_ReturnsTrue()
    {
        var graph = new Graph();
        var node = new Node { Id = "test" };
        graph.AddNode(node);
        
        Assert.True(NodeVisualManager.IsNodeVisible(graph, node));
    }

    [Fact]
    public void IsNodeVisible_NodeInCollapsedGroup_ReturnsFalse()
    {
        var graph = new Graph();
        var group = new Node { Id = "group", IsGroup = true, IsCollapsed = true };
        var node = new Node { Id = "test", ParentGroupId = "group" };
        graph.AddNode(group);
        graph.AddNode(node);
        
        Assert.False(NodeVisualManager.IsNodeVisible(graph, node));
    }

    [Fact]
    public void IsNodeVisible_NodeInExpandedGroup_ReturnsTrue()
    {
        var graph = new Graph();
        var group = new Node { Id = "group", IsGroup = true, IsCollapsed = false };
        var node = new Node { Id = "test", ParentGroupId = "group" };
        graph.AddNode(group);
        graph.AddNode(node);
        
        Assert.True(NodeVisualManager.IsNodeVisible(graph, node));
    }

    [Fact]
    public void IsNodeVisible_NodeInNestedCollapsedGroup_ReturnsFalse()
    {
        var graph = new Graph();
        var outerGroup = new Node { Id = "outer", IsGroup = true, IsCollapsed = true };
        var innerGroup = new Node { Id = "inner", IsGroup = true, IsCollapsed = false, ParentGroupId = "outer" };
        var node = new Node { Id = "test", ParentGroupId = "inner" };
        graph.AddNode(outerGroup);
        graph.AddNode(innerGroup);
        graph.AddNode(node);
        
        Assert.False(NodeVisualManager.IsNodeVisible(graph, node));
    }

    [Fact]
    public void GetPortYCanvas_SinglePort_ReturnsCentered()
    {
        var context = CreateContext();
        var manager = new NodeVisualManager(context);
        
        var y = manager.GetPortYCanvas(0, 0, 1, 100);
        
        Assert.Equal(50, y); // Centered at height/2
    }

    [Fact]
    public void GetPortYCanvas_MultiplePorts_DistributesEvenly()
    {
        var context = CreateContext();
        var manager = new NodeVisualManager(context);
        
        var y1 = manager.GetPortYCanvas(0, 0, 3, 100);
        var y2 = manager.GetPortYCanvas(0, 1, 3, 100);
        var y3 = manager.GetPortYCanvas(0, 2, 3, 100);
        
        Assert.Equal(25, y1);  // 100 / 4 * 1
        Assert.Equal(50, y2);  // 100 / 4 * 2
        Assert.Equal(75, y3);  // 100 / 4 * 3
    }

    [Fact]
    public void Clear_ClearsAllTracking()
    {
        var context = CreateContext();
        var manager = new NodeVisualManager(context);
        
        // Just verify it doesn't throw
        manager.Clear();
        
        Assert.Null(manager.GetNodeVisual("any"));
        Assert.Null(manager.GetPortVisual("any", "any"));
    }
}
