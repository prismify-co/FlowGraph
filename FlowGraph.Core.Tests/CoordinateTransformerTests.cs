using FlowGraph.Core.Rendering;
using FlowGraph.Avalonia;
using AvaloniaSize = Avalonia.Size;
using AvaloniaPoint = Avalonia.Point;

// Tests for deprecated methods - suppress obsolete warnings intentionally
#pragma warning disable CS0618

namespace FlowGraph.Core.Tests;

/// <summary>
/// Tests for the ICoordinateTransformer contract implementation.
/// These tests verify the coordinate transformation formulas:
/// - ScreenToCanvas: canvasPoint = (screenPoint - offset) / zoom
/// - CanvasToScreen: screenPoint = canvasPoint * zoom + offset
/// </summary>
public class CoordinateTransformerTests
{
    private ICoordinateTransformer CreateTransformer(double zoom = 1.0, double offsetX = 0, double offsetY = 0)
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        
        if (offsetX != 0 || offsetY != 0)
        {
            viewport.SetOffset(offsetX, offsetY);
        }
        
        if (Math.Abs(zoom - 1.0) > 0.001)
        {
            // Set zoom without a center to avoid offset adjustments
            viewport.SetOffset(0, 0);
            viewport.SetZoom(zoom, new AvaloniaPoint(0, 0));
            viewport.SetOffset(offsetX, offsetY);
        }
        
        return viewport;
    }

    #region ScreenToCanvas Tests

    [Fact]
    public void ScreenToCanvas_AtDefaultState_ReturnsIdentity()
    {
        var transformer = CreateTransformer();
        
        var result = transformer.ScreenToCanvas(100, 200);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(200, result.Y, 0.001);
    }

    [Fact]
    public void ScreenToCanvas_WithOffset_SubtractsOffset()
    {
        var transformer = CreateTransformer(zoom: 1.0, offsetX: 50, offsetY: 100);
        
        // Formula: canvasPoint = (screenPoint - offset) / zoom
        // canvas = (150 - 50) / 1 = 100
        var result = transformer.ScreenToCanvas(150, 300);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(200, result.Y, 0.001);
    }

    [Fact]
    public void ScreenToCanvas_WithZoom_DividesByZoom()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        // Set zoom towards origin to avoid offset changes
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        ICoordinateTransformer transformer = viewport;
        
        // Formula: canvasPoint = (screenPoint - offset) / zoom
        // With zoom=2 and offset=0: canvas = 200 / 2 = 100
        var result = transformer.ScreenToCanvas(200, 400);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(200, result.Y, 0.001);
    }

    [Fact]
    public void ScreenToCanvas_WithZoomAndOffset_AppliesBothTransforms()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        viewport.SetOffset(100, 50);
        ICoordinateTransformer transformer = viewport;
        
        // Formula: canvasPoint = (screenPoint - offset) / zoom
        // canvas.X = (300 - 100) / 2 = 100
        // canvas.Y = (250 - 50) / 2 = 100
        var result = transformer.ScreenToCanvas(300, 250);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(100, result.Y, 0.001);
    }

    [Fact]
    public void ScreenToCanvas_WithNegativeOffset_HandlesCorrectly()
    {
        var transformer = CreateTransformer(zoom: 1.0, offsetX: -100, offsetY: -50);
        
        // Formula: canvasPoint = (screenPoint - offset) / zoom
        // canvas = (100 - (-100)) / 1 = 200
        var result = transformer.ScreenToCanvas(100, 100);
        
        Assert.Equal(200, result.X, 0.001);
        Assert.Equal(150, result.Y, 0.001);
    }

    #endregion

    #region CanvasToScreen Tests

    [Fact]
    public void CanvasToScreen_AtDefaultState_ReturnsIdentity()
    {
        var transformer = CreateTransformer();
        
        var result = transformer.CanvasToScreen(100, 200);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(200, result.Y, 0.001);
    }

    [Fact]
    public void CanvasToScreen_WithOffset_AddsOffset()
    {
        var transformer = CreateTransformer(zoom: 1.0, offsetX: 50, offsetY: 100);
        
        // Formula: screenPoint = canvasPoint * zoom + offset
        // screen = 100 * 1 + 50 = 150
        var result = transformer.CanvasToScreen(100, 200);
        
        Assert.Equal(150, result.X, 0.001);
        Assert.Equal(300, result.Y, 0.001);
    }

    [Fact]
    public void CanvasToScreen_WithZoom_MultipliesByZoom()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        ICoordinateTransformer transformer = viewport;
        
        // Formula: screenPoint = canvasPoint * zoom + offset
        // With zoom=2 and offset=0: screen = 100 * 2 = 200
        var result = transformer.CanvasToScreen(100, 200);
        
        Assert.Equal(200, result.X, 0.001);
        Assert.Equal(400, result.Y, 0.001);
    }

    [Fact]
    public void CanvasToScreen_WithZoomAndOffset_AppliesBothTransforms()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        viewport.SetOffset(100, 50);
        ICoordinateTransformer transformer = viewport;
        
        // Formula: screenPoint = canvasPoint * zoom + offset
        // screen.X = 100 * 2 + 100 = 300
        // screen.Y = 100 * 2 + 50 = 250
        var result = transformer.CanvasToScreen(100, 100);
        
        Assert.Equal(300, result.X, 0.001);
        Assert.Equal(250, result.Y, 0.001);
    }

    #endregion

    #region Round-trip Tests

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 200)]
    [InlineData(-50, -100)]
    [InlineData(1000, 500)]
    public void ScreenToCanvas_ThenCanvasToScreen_ReturnsOriginal(double x, double y)
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(1.5, new AvaloniaPoint(0, 0));
        viewport.SetOffset(200, 150);
        ICoordinateTransformer transformer = viewport;
        
        var canvas = transformer.ScreenToCanvas(x, y);
        var screen = transformer.CanvasToScreen(canvas.X, canvas.Y);
        
        Assert.Equal(x, screen.X, 0.001);
        Assert.Equal(y, screen.Y, 0.001);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 200)]
    [InlineData(-50, -100)]
    [InlineData(500, 300)]
    public void CanvasToScreen_ThenScreenToCanvas_ReturnsOriginal(double x, double y)
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(0.5, new AvaloniaPoint(0, 0));
        viewport.SetOffset(-100, 50);
        ICoordinateTransformer transformer = viewport;
        
        var screen = transformer.CanvasToScreen(x, y);
        var canvas = transformer.ScreenToCanvas(screen.X, screen.Y);
        
        Assert.Equal(x, canvas.X, 0.001);
        Assert.Equal(y, canvas.Y, 0.001);
    }

    #endregion

    #region Delta Transform Tests

    [Fact]
    public void ScreenToCanvasDelta_AtDefaultZoom_ReturnsIdentity()
    {
        var transformer = CreateTransformer();
        
        var result = transformer.ScreenToCanvasDelta(100, 50);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(50, result.Y, 0.001);
    }

    [Fact]
    public void ScreenToCanvasDelta_WithZoom_DividesByZoom()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        ICoordinateTransformer transformer = viewport;
        
        // Delta transforms only apply zoom, not offset
        // canvasDelta = screenDelta / zoom = 100 / 2 = 50
        var result = transformer.ScreenToCanvasDelta(100, 50);
        
        Assert.Equal(50, result.X, 0.001);
        Assert.Equal(25, result.Y, 0.001);
    }

    [Fact]
    public void ScreenToCanvasDelta_IgnoresOffset()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetOffset(500, 300); // Large offset should not affect delta
        ICoordinateTransformer transformer = viewport;
        
        var result = transformer.ScreenToCanvasDelta(100, 50);
        
        // Offset should have no effect on delta transforms
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(50, result.Y, 0.001);
    }

    [Fact]
    public void CanvasToScreenDelta_WithZoom_MultipliesByZoom()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        ICoordinateTransformer transformer = viewport;
        
        // screenDelta = canvasDelta * zoom = 50 * 2 = 100
        var result = transformer.CanvasToScreenDelta(50, 25);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(50, result.Y, 0.001);
    }

    [Fact]
    public void DeltaTransforms_AreInverses()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(1.5, new AvaloniaPoint(0, 0));
        viewport.SetOffset(200, 150);
        ICoordinateTransformer transformer = viewport;
        
        var screenDelta = new Point(100, 50);
        var canvasDelta = transformer.ScreenToCanvasDelta(screenDelta.X, screenDelta.Y);
        var backToScreen = transformer.CanvasToScreenDelta(canvasDelta.X, canvasDelta.Y);
        
        Assert.Equal(screenDelta.X, backToScreen.X, 0.001);
        Assert.Equal(screenDelta.Y, backToScreen.Y, 0.001);
    }

    #endregion

    #region Extension Method Tests

    [Fact]
    public void ScreenToCanvas_PointOverload_WorksCorrectly()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        viewport.SetOffset(100, 50);
        ICoordinateTransformer transformer = viewport;
        
        var screenPoint = new Point(300, 250);
        var result = transformer.ScreenToCanvas(screenPoint);
        
        Assert.Equal(100, result.X, 0.001);
        Assert.Equal(100, result.Y, 0.001);
    }

    [Fact]
    public void CanvasToScreen_PointOverload_WorksCorrectly()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        viewport.SetOffset(100, 50);
        ICoordinateTransformer transformer = viewport;
        
        var canvasPoint = new Point(100, 100);
        var result = transformer.CanvasToScreen(canvasPoint);
        
        Assert.Equal(300, result.X, 0.001);
        Assert.Equal(250, result.Y, 0.001);
    }

    [Fact]
    public void CanvasToScreen_RectOverload_TransformsCorrectly()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0, new AvaloniaPoint(0, 0));
        viewport.SetOffset(100, 50);
        ICoordinateTransformer transformer = viewport;
        
        var canvasRect = new FlowGraph.Core.Elements.Rect(100, 100, 50, 30);
        var result = transformer.CanvasToScreen(canvasRect);
        
        // Position: canvas * zoom + offset
        Assert.Equal(300, result.X, 0.001);  // 100 * 2 + 100
        Assert.Equal(250, result.Y, 0.001);  // 100 * 2 + 50
        // Size: canvas * zoom (delta transform)
        Assert.Equal(100, result.Width, 0.001);  // 50 * 2
        Assert.Equal(60, result.Height, 0.001);  // 30 * 2
    }

    #endregion
}
