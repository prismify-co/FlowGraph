using Avalonia;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering;
using Xunit;

namespace FlowGraph.Core.Tests.Rendering;

public class RenderContextTests
{
    [Fact]
    public void Constructor_WithNullSettings_UsesDefaults()
    {
        var context = new RenderContext(null);
        
        Assert.NotNull(context.Settings);
        Assert.Equal(FlowCanvasSettings.Default.NodeWidth, context.Settings.NodeWidth);
    }

    [Fact]
    public void Constructor_WithSettings_UsesProvidedSettings()
    {
        var settings = new FlowCanvasSettings { NodeWidth = 200 };
        var context = new RenderContext(settings);
        
        Assert.Equal(200, context.Settings.NodeWidth);
    }

    [Fact]
    public void Scale_WithNoViewport_ReturnsOne()
    {
        var context = new RenderContext();
        
        Assert.Equal(1.0, context.Scale);
    }

    [Fact]
    public void Scale_WithViewport_ReturnsZoom()
    {
        var context = new RenderContext();
        var viewport = new ViewportState();
        viewport.SetZoom(2.0);
        context.SetViewport(viewport);
        
        Assert.Equal(2.0, context.Scale);
    }

    [Fact]
    public void CanvasToScreen_WithNoViewport_ReturnsOriginalPoint()
    {
        var context = new RenderContext();
        
        var result = context.CanvasToScreen(100, 200);
        
        Assert.Equal(100, result.X);
        Assert.Equal(200, result.Y);
    }

    [Fact]
    public void CanvasToScreen_WithViewport_TransformsCorrectly()
    {
        var context = new RenderContext();
        var viewport = new ViewportState();
        viewport.SetZoom(2.0);
        viewport.Pan(50, 50);
        context.SetViewport(viewport);
        
        var result = context.CanvasToScreen(100, 100);
        
        // With zoom 2.0 and offset (50, 50): screen = canvas * zoom + offset
        // screen = 100 * 2.0 + 50 = 250
        Assert.Equal(250, result.X);
        Assert.Equal(250, result.Y);
    }

    [Fact]
    public void ScreenToCanvas_WithNoViewport_ReturnsOriginalPoint()
    {
        var context = new RenderContext();
        
        var result = context.ScreenToCanvas(100, 200);
        
        Assert.Equal(100, result.X);
        Assert.Equal(200, result.Y);
    }

    [Fact]
    public void ScaleValue_WithNoViewport_ReturnsSameValue()
    {
        var context = new RenderContext();
        
        Assert.Equal(10.0, context.ScaleValue(10.0));
    }

    [Fact]
    public void ScaleValue_WithViewport_ScalesValue()
    {
        var context = new RenderContext();
        var viewport = new ViewportState();
        viewport.SetZoom(2.5);
        context.SetViewport(viewport);
        
        Assert.Equal(25.0, context.ScaleValue(10.0));
    }

    [Fact]
    public void SetViewport_CanBeCalledWithNull()
    {
        var context = new RenderContext();
        var viewport = new ViewportState();
        context.SetViewport(viewport);
        
        context.SetViewport(null);
        
        Assert.Null(context.Viewport);
        Assert.Equal(1.0, context.Scale);
    }
}
