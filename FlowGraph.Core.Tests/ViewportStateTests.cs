using FlowGraph.Avalonia;
using AvaloniaSize = Avalonia.Size;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Core.Tests;

public class ViewportStateTests
{
    [Fact]
    public void GetVisibleRect_WithDefaultState_ReturnsCorrectBounds()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));

        var rect = viewport.GetVisibleRect();

        // At zoom=1 and offset=(0,0), visible rect should be (0,0) to (1000,600)
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(1000, rect.Width);
        Assert.Equal(600, rect.Height);
    }

    [Fact]
    public void GetVisibleRect_WithPan_ReturnsShiftedBounds()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetOffset(100, 50); // Panned right and down

        var rect = viewport.GetVisibleRect();

        // At zoom=1 and offset=(100,50):
        // topLeft = (0-100)/1, (0-50)/1 = (-100, -50)
        // bottomRight = (1000-100)/1, (600-50)/1 = (900, 550)
        Assert.Equal(-100, rect.X);
        Assert.Equal(-50, rect.Y);
        Assert.Equal(1000, rect.Width);
        Assert.Equal(600, rect.Height);
    }

    [Fact]
    public void GetVisibleRect_WithZoom_ReturnsScaledBounds()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetZoom(2.0); // Zoomed in 2x towards view center

        var rect = viewport.GetVisibleRect();

        // At zoom=2, zooming towards center (500, 300):
        // The center of the view should stay at the same canvas position
        // Before: center shows canvas point (500, 300)
        // After: center still shows canvas point (500, 300)
        // Visible area is half the size: 500x300
        // So visible rect should be (250, 150) to (750, 450)
        Assert.Equal(250, rect.X, 0.1);
        Assert.Equal(150, rect.Y, 0.1);
        Assert.Equal(500, rect.Width, 0.1);
        Assert.Equal(300, rect.Height, 0.1);
    }

    [Fact]
    public void GetVisibleRect_WithZoomAndPan_ReturnsCorrectBounds()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetOffset(200, 100); // First pan
        viewport.SetZoom(2.0); // Then zoom towards center

        var rect = viewport.GetVisibleRect();

        // After panning with offset (200, 100) at zoom=1:
        // Center of view shows canvas point ((500-200)/1, (300-100)/1) = (300, 200)
        // After zooming to 2x towards view center:
        // Center still shows canvas point (300, 200)
        // Visible area is 500x300
        // So visible rect should be (50, 50) to (550, 350)
        Assert.Equal(50, rect.X, 0.1);
        Assert.Equal(50, rect.Y, 0.1);
        Assert.Equal(500, rect.Width, 0.1);
        Assert.Equal(300, rect.Height, 0.1);
    }

    [Fact]
    public void CenterOn_PositionsViewportCorrectly()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        
        // Center on canvas point (400, 200)
        viewport.CenterOn(new AvaloniaPoint(400, 200));

        var rect = viewport.GetVisibleRect();

        // The point (400, 200) should be at the center of the visible rect
        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;

        Assert.Equal(400, centerX, 0.1);
        Assert.Equal(200, centerY, 0.1);
    }

    [Fact]
    public void ScreenToCanvas_RoundTrip_PreservesCoordinates()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetOffset(150, 75);
        viewport.SetZoom(1.5);

        var originalCanvas = new AvaloniaPoint(400, 300);
        var screen = viewport.CanvasToScreen(originalCanvas);
        var backToCanvas = viewport.ScreenToCanvas(screen);

        Assert.Equal(originalCanvas.X, backToCanvas.X, 0.001);
        Assert.Equal(originalCanvas.Y, backToCanvas.Y, 0.001);
    }

    [Fact]
    public void MinimapCoordinateTransformation_ShouldAlignNodesAndViewport()
    {
        // Simulate what the minimap does
        // Graph nodes at: (100,100), (400,150), (700,100)
        const double NodeWidth = 150;
        const double NodeHeight = 80;
        const double MinimapPadding = 50;

        // Graph bounds
        double graphMinX = 100 - MinimapPadding;  // 50
        double graphMinY = 100 - MinimapPadding;  // 50
        double graphMaxX = 700 + NodeWidth + MinimapPadding;  // 900
        double graphMaxY = 150 + NodeHeight + MinimapPadding; // 280

        double graphWidth = graphMaxX - graphMinX;   // 850
        double graphHeight = graphMaxY - graphMinY;  // 230

        // Minimap size
        double minimapWidth = 200;
        double minimapHeight = 150;

        // Calculate scale (same as minimap does)
        double scaleX = minimapWidth / graphWidth;
        double scaleY = minimapHeight / graphHeight;
        double scale = Math.Min(scaleX, scaleY);  // Should be height-limited

        // Calculate translation (same as minimap does)
        double graphCenterX = (graphMinX + graphMaxX) / 2;  // 475
        double graphCenterY = (graphMinY + graphMaxY) / 2;  // 165
        double minimapCenterX = minimapWidth / 2;   // 100
        double minimapCenterY = minimapHeight / 2;  // 75

        double translateX = minimapCenterX - graphCenterX * scale;
        double translateY = minimapCenterY - graphCenterY * scale;

        // Helper to transform canvas to minimap
        AvaloniaPoint CanvasToMinimap(double canvasX, double canvasY)
        {
            return new AvaloniaPoint(
                canvasX * scale + translateX,
                canvasY * scale + translateY
            );
        }

        // Now test: if Process node (400, 150) is centered in main view,
        // the viewport rect should cover the Process node in minimap

        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.CenterOn(new AvaloniaPoint(475, 190)); // Center on Process node center

        var visibleRect = viewport.GetVisibleRect();

        // Transform both Process node and viewport to minimap coordinates
        var processNodeMinimap = CanvasToMinimap(400, 150);
        var viewportTopLeftMinimap = CanvasToMinimap(visibleRect.X, visibleRect.Y);
        var viewportBottomRightMinimap = CanvasToMinimap(visibleRect.Right, visibleRect.Bottom);

        // The viewport rect in minimap should contain the Process node position
        Assert.True(viewportTopLeftMinimap.X <= processNodeMinimap.X, 
            $"Viewport left {viewportTopLeftMinimap.X} should be <= Process node X {processNodeMinimap.X}");
        Assert.True(viewportTopLeftMinimap.Y <= processNodeMinimap.Y,
            $"Viewport top {viewportTopLeftMinimap.Y} should be <= Process node Y {processNodeMinimap.Y}");
        Assert.True(viewportBottomRightMinimap.X >= processNodeMinimap.X + NodeWidth * scale,
            $"Viewport right {viewportBottomRightMinimap.X} should be >= Process node right {processNodeMinimap.X + NodeWidth * scale}");
        Assert.True(viewportBottomRightMinimap.Y >= processNodeMinimap.Y + NodeHeight * scale,
            $"Viewport bottom {viewportBottomRightMinimap.Y} should be >= Process node bottom {processNodeMinimap.Y + NodeHeight * scale}");
    }

    [Fact]
    public void MinimapViewport_ShouldShrinkWhenZoomedIn()
    {
        // This test verifies that when zoomed in, the viewport rect in minimap gets smaller
        const double NodeWidth = 150;
        const double MinimapPadding = 50;

        // Graph bounds (same as other test)
        double graphMinX = 100 - MinimapPadding;
        double graphMaxX = 700 + NodeWidth + MinimapPadding;
        double graphWidth = graphMaxX - graphMinX;

        double minimapWidth = 200;
        double scale = minimapWidth / graphWidth; // Simplified - assume width-limited

        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));

        // At 100% zoom
        viewport.SetZoom(1.0);
        var rect1x = viewport.GetVisibleRect();
        double viewportWidth1x = rect1x.Width * scale;

        // At 200% zoom
        viewport.SetZoom(2.0);
        var rect2x = viewport.GetVisibleRect();
        double viewportWidth2x = rect2x.Width * scale;

        // Viewport should be half the size when zoomed in 2x
        Assert.Equal(viewportWidth1x / 2, viewportWidth2x, 0.1);
    }

    [Fact]
    public void RepeatedZoomIn_ShouldKeepViewCenterStationary()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        
        // Start with offset (0,0), zoom 1.0
        // View center shows canvas point (500, 300)
        var initialCenterCanvas = viewport.ScreenToCanvas(new AvaloniaPoint(500, 300));
        Assert.Equal(500, initialCenterCanvas.X, 0.1);
        Assert.Equal(300, initialCenterCanvas.Y, 0.1);

        // Zoom in 7 times
        for (int i = 0; i < 7; i++)
        {
            viewport.ZoomIn();
        }

        // After zooming, the view center should still show the same canvas point
        var finalCenterCanvas = viewport.ScreenToCanvas(new AvaloniaPoint(500, 300));
        Assert.Equal(500, finalCenterCanvas.X, 0.1);
        Assert.Equal(300, finalCenterCanvas.Y, 0.1);
    }

    [Fact]
    public void ZoomIn_ThenZoomOut_ShouldReturnToOriginalState()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.SetOffset(100, 50);
        
        var originalOffsetX = viewport.OffsetX;
        var originalOffsetY = viewport.OffsetY;
        var originalZoom = viewport.Zoom;

        // Zoom in then out
        viewport.ZoomIn();
        viewport.ZoomOut();

        Assert.Equal(originalZoom, viewport.Zoom, 0.001);
        Assert.Equal(originalOffsetX, viewport.OffsetX, 0.1);
        Assert.Equal(originalOffsetY, viewport.OffsetY, 0.1);
    }
}
