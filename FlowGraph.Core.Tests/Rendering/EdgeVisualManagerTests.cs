using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using Xunit;

namespace FlowGraph.Core.Tests.Rendering;

public class EdgeVisualManagerTests
{
    private (RenderContext, NodeVisualManager) CreateManagers()
    {
        var context = new RenderContext(new FlowCanvasSettings());
        var nodeManager = new NodeVisualManager(context);
        return (context, nodeManager);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var (_, nodeManager) = CreateManagers();
        Assert.Throws<ArgumentNullException>(() => new EdgeVisualManager(null!, nodeManager));
    }

    [Fact]
    public void Constructor_WithNullNodeManager_ThrowsArgumentNullException()
    {
        var (context, _) = CreateManagers();
        Assert.Throws<ArgumentNullException>(() => new EdgeVisualManager(context, null!));
    }

    [Fact]
    public void GetEdgeVisual_WhenNotRendered_ReturnsNull()
    {
        var (context, nodeManager) = CreateManagers();
        var manager = new EdgeVisualManager(context, nodeManager);
        
        Assert.Null(manager.GetEdgeVisual("nonexistent"));
    }

    [Fact]
    public void GetEdgeVisiblePath_WhenNotRendered_ReturnsNull()
    {
        var (context, nodeManager) = CreateManagers();
        var manager = new EdgeVisualManager(context, nodeManager);
        
        Assert.Null(manager.GetEdgeVisiblePath("nonexistent"));
    }

    [Fact]
    public void GetEdgeMarkers_WhenNotRendered_ReturnsNull()
    {
        var (context, nodeManager) = CreateManagers();
        var manager = new EdgeVisualManager(context, nodeManager);
        
        Assert.Null(manager.GetEdgeMarkers("nonexistent"));
    }

    [Fact]
    public void GetEdgeLabel_WhenNotRendered_ReturnsNull()
    {
        var (context, nodeManager) = CreateManagers();
        var manager = new EdgeVisualManager(context, nodeManager);
        
        Assert.Null(manager.GetEdgeLabel("nonexistent"));
    }

    [Fact]
    public void GetEdgeEndpointHandles_WhenNotRendered_ReturnsNulls()
    {
        var (context, nodeManager) = CreateManagers();
        var manager = new EdgeVisualManager(context, nodeManager);
        
        var (source, target) = manager.GetEdgeEndpointHandles("nonexistent");
        
        Assert.Null(source);
        Assert.Null(target);
    }

    [Fact]
    public void Clear_ClearsAllTracking()
    {
        var (context, nodeManager) = CreateManagers();
        var manager = new EdgeVisualManager(context, nodeManager);
        
        // Just verify it doesn't throw
        manager.Clear();
        
        Assert.Null(manager.GetEdgeVisual("any"));
        Assert.Null(manager.GetEdgeVisiblePath("any"));
    }
}
