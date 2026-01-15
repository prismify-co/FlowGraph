using FlowGraph.Core.Rendering;
using FlowGraph.Avalonia;
using AvaloniaSize = Avalonia.Size;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Tests for the IViewportState contract implementation.
/// </summary>
public class ViewportStateInterfaceTests
{
    private (ViewportState concrete, IViewportState iface) CreateViewport()
    {
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        return (viewport, viewport);
    }

    #region IReadOnlyViewportState Tests

    [Fact]
    public void ViewSize_ReturnsCorrectSize()
    {
        var (_, iface) = CreateViewport();
        
        var size = iface.ViewSize;
        
        Assert.Equal(1000, size.Width);
        Assert.Equal(600, size.Height);
    }

    [Fact]
    public void GetVisibleCanvasRect_AtDefaultState_ReturnsFullViewInCanvasCoords()
    {
        var (_, iface) = CreateViewport();
        
        var rect = iface.GetVisibleCanvasRect();
        
        Assert.Equal(0, rect.X, 0.001);
        Assert.Equal(0, rect.Y, 0.001);
        Assert.Equal(1000, rect.Width, 0.001);
        Assert.Equal(600, rect.Height, 0.001);
    }

    [Fact]
    public void GetVisibleCanvasRect_WithZoom_ReturnsScaledArea()
    {
        var (concrete, iface) = CreateViewport();
        concrete.SetZoom(2.0, new AvaloniaPoint(500, 300)); // Zoom toward center
        
        var rect = iface.GetVisibleCanvasRect();
        
        // At 2x zoom centered on (500,300), visible area should be:
        // Width: 1000/2 = 500, Height: 600/2 = 300
        // Centered on (500, 300), so X: 250, Y: 150
        Assert.Equal(500, rect.Width, 0.1);
        Assert.Equal(300, rect.Height, 0.1);
    }

    [Fact]
    public void ViewportChanged_FiresOnZoom()
    {
        var (concrete, iface) = CreateViewport();
        var eventFired = false;
        iface.ViewportChanged += (s, e) => eventFired = true;
        
        concrete.SetZoom(2.0);
        
        Assert.True(eventFired);
    }

    [Fact]
    public void ViewportChanged_FiresOnPan()
    {
        var (concrete, iface) = CreateViewport();
        var eventFired = false;
        iface.ViewportChanged += (s, e) => eventFired = true;
        
        concrete.Pan(100, 50);
        
        Assert.True(eventFired);
    }

    #endregion

    #region IViewportState.SetZoom Tests

    [Fact]
    public void SetZoom_ThroughInterface_ChangesZoom()
    {
        var (concrete, iface) = CreateViewport();
        
        iface.SetZoom(2.0);
        
        Assert.Equal(2.0, concrete.Zoom, 0.001);
    }

    [Fact]
    public void SetZoom_WithZoomCenter_ZoomsTowardPoint()
    {
        var (concrete, iface) = CreateViewport();
        var zoomCenter = new Point(200, 100);
        
        // Before zoom, screen point (200, 100) maps to canvas (200, 100)
        var canvasBefore = iface.ScreenToCanvas(200, 100);
        
        iface.SetZoom(2.0, zoomCenter);
        
        // After zoom, the same screen point should map to the same canvas point
        var canvasAfter = iface.ScreenToCanvas(200, 100);
        
        Assert.Equal(canvasBefore.X, canvasAfter.X, 0.1);
        Assert.Equal(canvasBefore.Y, canvasAfter.Y, 0.1);
    }

    [Fact]
    public void SetZoom_ClampsToMinMax()
    {
        var (concrete, iface) = CreateViewport();
        
        iface.SetZoom(0.01); // Below minimum
        Assert.True(concrete.Zoom >= FlowCanvasSettings.Default.MinZoom);
        
        iface.SetZoom(100); // Above maximum
        Assert.True(concrete.Zoom <= FlowCanvasSettings.Default.MaxZoom);
    }

    #endregion

    #region IViewportState.Pan Tests

    [Fact]
    public void Pan_ThroughInterface_MovesViewport()
    {
        var (concrete, iface) = CreateViewport();
        var initialOffset = (concrete.OffsetX, concrete.OffsetY);
        
        iface.Pan(100, 50);
        
        Assert.Equal(initialOffset.OffsetX + 100, concrete.OffsetX, 0.001);
        Assert.Equal(initialOffset.OffsetY + 50, concrete.OffsetY, 0.001);
    }

    [Fact]
    public void Pan_MultipleTimesAccumulates()
    {
        var (concrete, iface) = CreateViewport();
        
        iface.Pan(100, 50);
        iface.Pan(100, 50);
        
        Assert.Equal(200, concrete.OffsetX, 0.001);
        Assert.Equal(100, concrete.OffsetY, 0.001);
    }

    [Fact]
    public void Pan_NegativeValues_MovesOppositeDirection()
    {
        var (concrete, iface) = CreateViewport();
        
        iface.Pan(-100, -50);
        
        Assert.Equal(-100, concrete.OffsetX, 0.001);
        Assert.Equal(-50, concrete.OffsetY, 0.001);
    }

    #endregion

    #region IViewportState.SetOffset Tests

    [Fact]
    public void SetOffset_ThroughInterface_SetsAbsoluteOffset()
    {
        var (concrete, iface) = CreateViewport();
        iface.Pan(100, 100); // Set some initial offset
        
        iface.SetOffset(50, 25);
        
        Assert.Equal(50, concrete.OffsetX, 0.001);
        Assert.Equal(25, concrete.OffsetY, 0.001);
    }

    #endregion

    #region IViewportState.CenterOn Tests

    [Fact]
    public void CenterOn_ThroughInterface_CentersOnCanvasPoint()
    {
        var (concrete, iface) = CreateViewport();
        var canvasPoint = new Point(400, 200);
        
        iface.CenterOn(canvasPoint);
        
        // The center of the visible rect should now be at (400, 200)
        var visibleRect = iface.GetVisibleCanvasRect();
        var centerX = visibleRect.X + visibleRect.Width / 2;
        var centerY = visibleRect.Y + visibleRect.Height / 2;
        
        Assert.Equal(400, centerX, 0.1);
        Assert.Equal(200, centerY, 0.1);
    }

    [Fact]
    public void CenterOn_WithNegativeCoords_Works()
    {
        var (concrete, iface) = CreateViewport();
        var canvasPoint = new Point(-200, -100);
        
        iface.CenterOn(canvasPoint);
        
        var visibleRect = iface.GetVisibleCanvasRect();
        var centerX = visibleRect.X + visibleRect.Width / 2;
        var centerY = visibleRect.Y + visibleRect.Height / 2;
        
        Assert.Equal(-200, centerX, 0.1);
        Assert.Equal(-100, centerY, 0.1);
    }

    #endregion

    #region IViewportState.FitToBounds Tests

    [Fact]
    public void FitToBounds_ThroughInterface_ShowsEntireBounds()
    {
        var (concrete, iface) = CreateViewport();
        var bounds = new FlowGraph.Core.Elements.Rect(100, 100, 400, 200);
        
        iface.FitToBounds(bounds, padding: 50);
        
        var visible = iface.GetVisibleCanvasRect();
        
        // The bounds should be fully contained in the visible rect (with some padding)
        Assert.True(visible.X <= bounds.X);
        Assert.True(visible.Y <= bounds.Y);
        Assert.True(visible.X + visible.Width >= bounds.X + bounds.Width);
        Assert.True(visible.Y + visible.Height >= bounds.Y + bounds.Height);
    }

    [Fact]
    public void FitToBounds_CentersTheBounds()
    {
        var (concrete, iface) = CreateViewport();
        var bounds = new FlowGraph.Core.Elements.Rect(0, 0, 200, 100);
        
        iface.FitToBounds(bounds, padding: 0);
        
        var visible = iface.GetVisibleCanvasRect();
        var boundsCenter = bounds.Center;
        var visibleCenterX = visible.X + visible.Width / 2;
        var visibleCenterY = visible.Y + visible.Height / 2;
        
        Assert.Equal(boundsCenter.X, visibleCenterX, 1);
        Assert.Equal(boundsCenter.Y, visibleCenterY, 1);
    }

    #endregion

    #region IViewportState.Reset Tests

    [Fact]
    public void Reset_ResetsToDefaultState()
    {
        var (concrete, iface) = CreateViewport();
        
        // Change the viewport state
        iface.SetZoom(2.0);
        iface.Pan(500, 300);
        
        // Reset
        iface.Reset();
        
        Assert.Equal(1.0, concrete.Zoom, 0.001);
        Assert.Equal(0, concrete.OffsetX, 0.001);
        Assert.Equal(0, concrete.OffsetY, 0.001);
    }

    [Fact]
    public void Reset_FiresViewportChanged()
    {
        var (concrete, iface) = CreateViewport();
        concrete.SetZoom(2.0);
        
        var eventFired = false;
        iface.ViewportChanged += (s, e) => eventFired = true;
        
        iface.Reset();
        
        Assert.True(eventFired);
    }

    #endregion

    #region Interface Compatibility Tests

    [Fact]
    public void ViewportState_IsAssignableToICoordinateTransformer()
    {
        var viewport = new ViewportState();
        
        Assert.IsAssignableFrom<ICoordinateTransformer>(viewport);
    }

    [Fact]
    public void ViewportState_IsAssignableToIReadOnlyViewportState()
    {
        var viewport = new ViewportState();
        
        Assert.IsAssignableFrom<IReadOnlyViewportState>(viewport);
    }

    [Fact]
    public void ViewportState_IsAssignableToIViewportState()
    {
        var viewport = new ViewportState();
        
        Assert.IsAssignableFrom<IViewportState>(viewport);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void GetVisibleCanvasRect_WithZeroViewSize_ReturnsZeroRect()
    {
        var viewport = new ViewportState();
        // Don't set view size - it defaults to (0, 0)
        IViewportState iface = viewport;

        var rect = iface.GetVisibleCanvasRect();

        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
    }

    [Fact]
    public void ScreenToCanvas_WithZeroViewSize_StillTransforms()
    {
        var viewport = new ViewportState();
        // Don't set view size
        ICoordinateTransformer transformer = viewport;

        // Should not throw - transforms are independent of view size
        var result = transformer.ScreenToCanvas(100, 200);

        Assert.Equal(100, result.X);
        Assert.Equal(200, result.Y);
    }

    [Fact]
    public void CenterOn_WithZeroViewSize_DoesNotThrow()
    {
        var viewport = new ViewportState();
        // Don't set view size
        IViewportState iface = viewport;

        // Should not throw - centering with zero view size is a no-op or uses 0 offsets
        var exception = Record.Exception(() => iface.CenterOn(new Point(100, 100)));

        Assert.Null(exception);
    }

    [Fact]
    public void SetZoom_AtMinZoom_TransformsCorrectly()
    {
        var (concrete, iface) = CreateViewport();
        var minZoom = FlowCanvasSettings.Default.MinZoom;
        
        iface.SetZoom(minZoom);
        
        // Transform should work correctly at minimum zoom
        var screenPoint = new Point(500, 300);
        var canvasPoint = iface.ScreenToCanvas(screenPoint);
        var backToScreen = iface.CanvasToScreen(canvasPoint);
        
        Assert.Equal(screenPoint.X, backToScreen.X, 0.1);
        Assert.Equal(screenPoint.Y, backToScreen.Y, 0.1);
    }

    [Fact]
    public void SetZoom_BelowMinZoom_ClampsToMin()
    {
        var (concrete, iface) = CreateViewport();
        var minZoom = FlowCanvasSettings.Default.MinZoom;
        
        iface.SetZoom(0.001); // Way below minimum
        
        Assert.Equal(minZoom, concrete.Zoom, 0.001);
    }

    [Fact]
    public void FitToBounds_WithZeroSizeBounds_DoesNotThrow()
    {
        var (concrete, iface) = CreateViewport();
        var zeroBounds = new FlowGraph.Core.Elements.Rect(100, 100, 0, 0);

        // Should not throw - zero-size bounds is an edge case
        var exception = Record.Exception(() => iface.FitToBounds(zeroBounds));

        Assert.Null(exception);
    }

    [Fact]
    public void Pan_WithZeroDeltas_StillFiresEvent()
    {
        // Note: Current implementation fires ViewportChanged on every Pan call,
        // even with zero deltas. This is consistent behavior - all operations notify.
        var (concrete, iface) = CreateViewport();
        iface.Pan(100, 50); // Set initial offset
        
        var eventFired = false;
        iface.ViewportChanged += (s, e) => eventFired = true;
        
        iface.Pan(0, 0); // Zero delta
        
        // Event fires even for zero delta - consistent notification behavior
        Assert.True(eventFired);
    }

    #endregion
}
