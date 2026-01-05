using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using Xunit;

namespace FlowGraph.Core.Tests.Rendering;

public class ResizeHandleManagerTests
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
        Assert.Throws<ArgumentNullException>(() => new ResizeHandleManager(null!, nodeManager));
    }

    [Fact]
    public void Constructor_WithNullNodeManager_ThrowsArgumentNullException()
    {
        var (context, _) = CreateManagers();
        Assert.Throws<ArgumentNullException>(() => new ResizeHandleManager(context, null!));
    }

    [Fact]
    public void Clear_DoesNotThrow()
    {
        var (context, nodeManager) = CreateManagers();
        var manager = new ResizeHandleManager(context, nodeManager);
        
        // Just verify it doesn't throw
        manager.Clear();
    }
}
