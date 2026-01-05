using Avalonia.Controls.Shapes;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using Xunit;

namespace FlowGraph.Core.Tests.Rendering;

public class GraphRendererTests
{
    [Fact]
    public void Constructor_Default_CreatesAllManagers()
    {
        var renderer = new GraphRenderer();
        
        Assert.NotNull(renderer.RenderContext);
        Assert.NotNull(renderer.Nodes);
        Assert.NotNull(renderer.Edges);
        Assert.NotNull(renderer.ResizeHandles);
        Assert.NotNull(renderer.NodeRenderers);
    }

    [Fact]
    public void Constructor_WithSettings_UsesSettings()
    {
        var settings = new FlowCanvasSettings { NodeWidth = 200 };
        var renderer = new GraphRenderer(settings);
        
        Assert.Equal(200, renderer.RenderContext.Settings.NodeWidth);
    }

    [Fact]
    public void Constructor_WithNodeRendererRegistry_UsesRegistry()
    {
        var registry = new NodeRendererRegistry();
        var renderer = new GraphRenderer(null, registry);
        
        Assert.Same(registry, renderer.NodeRenderers);
    }

    [Fact]
    public void SetViewport_SetsOnRenderContext()
    {
        var renderer = new GraphRenderer();
        var viewport = new ViewportState();
        viewport.SetZoom(2.0);
        
        renderer.SetViewport(viewport);
        
        Assert.Equal(2.0, renderer.RenderContext.Scale);
    }

    [Fact]
    public void GetNodeVisual_DelegatesToNodeManager()
    {
        var renderer = new GraphRenderer();
        
        // Not rendered, should return null
        Assert.Null(renderer.GetNodeVisual("test"));
    }

    [Fact]
    public void GetPortVisual_DelegatesToNodeManager()
    {
        var renderer = new GraphRenderer();
        
        Assert.Null(renderer.GetPortVisual("node", "port"));
    }

    [Fact]
    public void GetEdgeVisual_DelegatesToEdgeManager()
    {
        var renderer = new GraphRenderer();
        
        Assert.Null(renderer.GetEdgeVisual("test"));
    }

    [Fact]
    public void GetEdgeVisiblePath_DelegatesToEdgeManager()
    {
        var renderer = new GraphRenderer();
        
        Assert.Null(renderer.GetEdgeVisiblePath("test"));
    }

    [Fact]
    public void GetEdgeMarkers_DelegatesToEdgeManager()
    {
        var renderer = new GraphRenderer();
        
        Assert.Null(renderer.GetEdgeMarkers("test"));
    }

    [Fact]
    public void GetEdgeLabel_DelegatesToEdgeManager()
    {
        var renderer = new GraphRenderer();
        
        Assert.Null(renderer.GetEdgeLabel("test"));
    }

    [Fact]
    public void GetEdgeEndpointHandles_DelegatesToEdgeManager()
    {
        var renderer = new GraphRenderer();
        
        var (source, target) = renderer.GetEdgeEndpointHandles("test");
        Assert.Null(source);
        Assert.Null(target);
    }

    [Fact]
    public void GetNodeDimensions_DelegatesToNodeManager()
    {
        var settings = new FlowCanvasSettings { NodeWidth = 150, NodeHeight = 80 };
        var renderer = new GraphRenderer(settings);
        var node = new Node();
        
        var (width, height) = renderer.GetNodeDimensions(node);
        
        Assert.Equal(150, width);
        Assert.Equal(80, height);
    }

    [Fact]
    public void IsNodeVisible_DelegatesToNodeManager()
    {
        var renderer = new GraphRenderer();
        var graph = new Graph();
        var node = new Node { Id = "test" };
        graph.AddNode(node);
        
        Assert.True(renderer.IsNodeVisible(graph, node));
    }

    [Fact]
    public void GetPortYCanvas_DelegatesToNodeManager()
    {
        var renderer = new GraphRenderer();
        
        var y = renderer.GetPortYCanvas(0, 0, 1, 100);
        
        Assert.Equal(50, y);
    }

    [Fact]
    public void GetPortY_DelegatesToNodeManager()
    {
        var renderer = new GraphRenderer();
        
        var y = renderer.GetPortY(0, 0, 1);
        
        // Default height is 80, so center is 40
        Assert.Equal(40, y);
    }

    [Fact]
    public void Clear_ClearsAllManagers()
    {
        var renderer = new GraphRenderer();
        
        // Just verify it doesn't throw
        renderer.Clear();
    }
}
