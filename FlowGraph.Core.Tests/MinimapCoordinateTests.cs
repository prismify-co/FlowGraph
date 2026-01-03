using FlowGraph.Avalonia;
using AvaloniaSize = Avalonia.Size;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Tests that verify the minimap coordinate transformation logic.
/// These tests simulate what the minimap should do and verify expected positions.
/// </summary>
public class MinimapCoordinateTests
{
    // Simulates the minimap's coordinate transformation
    private class MinimapSimulator
    {
        public double Scale { get; private set; }
        public double ExtentX { get; private set; }
        public double ExtentY { get; private set; }
        
        public double MinimapWidth { get; }
        public double MinimapHeight { get; }

        // For debugging
        public AvaloniaRect ItemsExtent { get; private set; }
        public AvaloniaRect ViewportBounds { get; private set; }
        public AvaloniaRect FinalExtent { get; private set; }

        public MinimapSimulator(double minimapWidth, double minimapHeight)
        {
            MinimapWidth = minimapWidth;
            MinimapHeight = minimapHeight;
        }

        /// <summary>
        /// Calculate the extent and scale based on nodes and viewport.
        /// This is what the minimap should do.
        /// </summary>
        public void Calculate(AvaloniaRect itemsExtent, AvaloniaRect viewportBounds)
        {
            ItemsExtent = itemsExtent;
            ViewportBounds = viewportBounds;

            // Extent = union of items and viewport
            var extent = itemsExtent;
            if (viewportBounds.Width > 0 && viewportBounds.Height > 0)
            {
                extent = extent.Union(viewportBounds);
            }

            // Add padding
            extent = extent.Inflate(20);
            FinalExtent = extent;

            // Calculate scale to fit
            var scaleX = MinimapWidth / extent.Width;
            var scaleY = MinimapHeight / extent.Height;
            Scale = Math.Min(scaleX, scaleY);

            // Store extent origin
            ExtentX = extent.X;
            ExtentY = extent.Y;

            // Center the content
            var scaledWidth = extent.Width * Scale;
            var scaledHeight = extent.Height * Scale;
            var offsetX = (MinimapWidth - scaledWidth) / 2;
            var offsetY = (MinimapHeight - scaledHeight) / 2;

            ExtentX -= offsetX / Scale;
            ExtentY -= offsetY / Scale;
        }

        public AvaloniaPoint CanvasToMinimap(double canvasX, double canvasY)
        {
            return new AvaloniaPoint(
                (canvasX - ExtentX) * Scale,
                (canvasY - ExtentY) * Scale
            );
        }

        public AvaloniaPoint MinimapToCanvas(double minimapX, double minimapY)
        {
            return new AvaloniaPoint(
                minimapX / Scale + ExtentX,
                minimapY / Scale + ExtentY
            );
        }

        public string GetDebugInfo()
        {
            return $@"
MinimapSize: {MinimapWidth} x {MinimapHeight}
ItemsExtent: {ItemsExtent}
ViewportBounds: {ViewportBounds}
FinalExtent: {FinalExtent}
Scale: {Scale}
ExtentOrigin: ({ExtentX}, {ExtentY})";
        }
    }

    [Fact]
    public void ViewportRect_ShouldCoverNode_WhenNodeIsCentered()
    {
        // Arrange
        var minimap = new MinimapSimulator(200, 150);
        
        // Single node at (100, 100) with size 150x80
        var nodeRect = new AvaloniaRect(100, 100, 150, 80);
        var itemsExtent = nodeRect;

        // Viewport centered on the node (node center = 175, 140)
        // With a 1000x600 view at zoom=1, viewport would be:
        // X = 175 - 500 = -325, Y = 140 - 300 = -160
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.CenterOn(new AvaloniaPoint(175, 140)); // Center on node center
        var viewportBounds = viewport.GetVisibleRect();

        // Act
        minimap.Calculate(itemsExtent, viewportBounds);

        // Get node position in minimap
        var nodeInMinimap = minimap.CanvasToMinimap(100, 100);
        var nodeEndInMinimap = minimap.CanvasToMinimap(250, 180);

        // Get viewport position in minimap
        var viewportInMinimap = minimap.CanvasToMinimap(viewportBounds.X, viewportBounds.Y);
        var viewportEndInMinimap = minimap.CanvasToMinimap(viewportBounds.Right, viewportBounds.Bottom);

        // Assert - viewport should contain the node
        Assert.True(viewportInMinimap.X <= nodeInMinimap.X, 
            $"Viewport left ({viewportInMinimap.X}) should be <= node left ({nodeInMinimap.X})");
        Assert.True(viewportInMinimap.Y <= nodeInMinimap.Y, 
            $"Viewport top ({viewportInMinimap.Y}) should be <= node top ({nodeInMinimap.Y})");
        Assert.True(viewportEndInMinimap.X >= nodeEndInMinimap.X, 
            $"Viewport right ({viewportEndInMinimap.X}) should be >= node right ({nodeEndInMinimap.X})");
        Assert.True(viewportEndInMinimap.Y >= nodeEndInMinimap.Y, 
            $"Viewport bottom ({viewportEndInMinimap.Y}) should be >= node bottom ({nodeEndInMinimap.Y})");
    }

    [Fact]
    public void ViewportRect_ShouldBeSmaller_WhenZoomedIn()
    {
        // Arrange
        var minimap = new MinimapSimulator(200, 150);
        var itemsExtent = new AvaloniaRect(100, 100, 600, 100); // Nodes from 100-700

        var viewport1x = new ViewportState();
        viewport1x.SetViewSize(new AvaloniaSize(1000, 600));
        viewport1x.SetZoom(1.0);
        var bounds1x = viewport1x.GetVisibleRect();

        var viewport2x = new ViewportState();
        viewport2x.SetViewSize(new AvaloniaSize(1000, 600));
        viewport2x.SetZoom(2.0);
        var bounds2x = viewport2x.GetVisibleRect();

        // Act - calculate for 1x zoom
        minimap.Calculate(itemsExtent, bounds1x);
        var vpWidth1x = bounds1x.Width * minimap.Scale;

        // Recalculate for 2x zoom
        minimap.Calculate(itemsExtent, bounds2x);
        var vpWidth2x = bounds2x.Width * minimap.Scale;

        // Assert - at 2x zoom, viewport in canvas coords is half the size
        // So viewport in minimap should also be smaller
        Assert.True(vpWidth2x < vpWidth1x, 
            $"Viewport at 2x zoom ({vpWidth2x}) should be smaller than at 1x ({vpWidth1x})");
    }

    [Fact]
    public void ViewportRect_ShouldMove_WhenPanning()
    {
        // Arrange
        var minimap = new MinimapSimulator(200, 150);
        var itemsExtent = new AvaloniaRect(0, 0, 800, 400);

        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(400, 300));

        // Position 1: viewport at origin
        viewport.SetOffset(0, 0);
        var bounds1 = viewport.GetVisibleRect();
        minimap.Calculate(itemsExtent, bounds1);
        var pos1 = minimap.CanvasToMinimap(bounds1.X, bounds1.Y);

        // Position 2: pan right by 200 canvas units
        viewport.SetOffset(200, 0); // Positive offset = canvas moves right = viewport shows left area
        var bounds2 = viewport.GetVisibleRect();
        minimap.Calculate(itemsExtent, bounds2);
        var pos2 = minimap.CanvasToMinimap(bounds2.X, bounds2.Y);

        // Assert - viewport should have moved left in canvas coords (showing more left area)
        // bounds2.X should be less than bounds1.X
        Assert.True(bounds2.X < bounds1.X, 
            $"After panning right, viewport X ({bounds2.X}) should be < original ({bounds1.X})");
    }

    [Fact]
    public void NodePosition_ShouldRemainStable_WhenViewportChanges()
    {
        // Arrange
        var minimap = new MinimapSimulator(200, 150);
        
        // Three nodes
        var node1 = new AvaloniaRect(100, 100, 150, 80);
        var node2 = new AvaloniaRect(400, 150, 150, 80);
        var node3 = new AvaloniaRect(700, 100, 150, 80);
        
        var itemsExtent = node1.Union(node2).Union(node3);

        // Calculate with viewport centered on middle node
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(800, 500));
        viewport.CenterOn(new AvaloniaPoint(475, 190));
        var viewportBounds = viewport.GetVisibleRect();

        minimap.Calculate(itemsExtent, viewportBounds);

        // Get all positions
        var node1Pos = minimap.CanvasToMinimap(node1.X, node1.Y);
        var node2Pos = minimap.CanvasToMinimap(node2.X, node2.Y);
        var node3Pos = minimap.CanvasToMinimap(node3.X, node3.Y);
        var viewportPos = minimap.CanvasToMinimap(viewportBounds.X, viewportBounds.Y);

        // Assert - relative positions should make sense
        // Node 2 should be between node 1 and node 3
        Assert.True(node1Pos.X < node2Pos.X, "Node 1 should be left of Node 2");
        Assert.True(node2Pos.X < node3Pos.X, "Node 2 should be left of Node 3");

        // Viewport should overlap with node 2 (since we centered on it)
        var viewportEnd = minimap.CanvasToMinimap(viewportBounds.Right, viewportBounds.Bottom);
        var node2End = minimap.CanvasToMinimap(node2.Right, node2.Bottom);
        
        Assert.True(viewportPos.X <= node2Pos.X && viewportEnd.X >= node2End.X,
            "Viewport should contain Node 2 horizontally");
    }

    [Fact]
    public void ClickOnMinimap_ShouldNavigateToCorrectCanvasPosition()
    {
        // Arrange
        var minimap = new MinimapSimulator(200, 150);
        var itemsExtent = new AvaloniaRect(100, 100, 600, 100);

        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        var viewportBounds = viewport.GetVisibleRect();

        minimap.Calculate(itemsExtent, viewportBounds);

        // Act - click on a point in the minimap
        var minimapClickPoint = new AvaloniaPoint(100, 75); // Center of minimap
        var canvasPoint = minimap.MinimapToCanvas(minimapClickPoint.X, minimapClickPoint.Y);

        // Verify round-trip
        var backToMinimap = minimap.CanvasToMinimap(canvasPoint.X, canvasPoint.Y);

        // Assert
        Assert.Equal(minimapClickPoint.X, backToMinimap.X, 0.01);
        Assert.Equal(minimapClickPoint.Y, backToMinimap.Y, 0.01);
    }

    [Fact]
    public void ViewportCenter_ShouldAlignWithNodeCenter_WhenCenteredOnNode()
    {
        // Arrange
        var minimap = new MinimapSimulator(200, 150);
        
        // Node at (400, 150) with size 150x80, center at (475, 190)
        var nodeRect = new AvaloniaRect(400, 150, 150, 80);
        var nodeCenter = new AvaloniaPoint(475, 190);
        var itemsExtent = new AvaloniaRect(100, 100, 750, 130); // All three demo nodes

        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1000, 600));
        viewport.CenterOn(nodeCenter);
        var viewportBounds = viewport.GetVisibleRect();

        // Act
        minimap.Calculate(itemsExtent, viewportBounds);

        // Get viewport center in minimap coords
        var vpTopLeft = minimap.CanvasToMinimap(viewportBounds.X, viewportBounds.Y);
        var vpBottomRight = minimap.CanvasToMinimap(viewportBounds.Right, viewportBounds.Bottom);
        var vpCenterMinimap = new AvaloniaPoint(
            (vpTopLeft.X + vpBottomRight.X) / 2,
            (vpTopLeft.Y + vpBottomRight.Y) / 2
        );

        // Get node center in minimap coords
        var nodeCenterMinimap = minimap.CanvasToMinimap(nodeCenter.X, nodeCenter.Y);

        // Assert - viewport center should be at node center
        Assert.Equal(nodeCenterMinimap.X, vpCenterMinimap.X, 1.0);
        Assert.Equal(nodeCenterMinimap.Y, vpCenterMinimap.Y, 1.0);
    }

    [Fact]
    public void AllPositions_ShouldBeWithinMinimapBounds()
    {
        // Arrange
        var minimap = new MinimapSimulator(200, 150);
        var itemsExtent = new AvaloniaRect(100, 100, 600, 100);

        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(800, 500));
        viewport.CenterOn(new AvaloniaPoint(400, 150));
        var viewportBounds = viewport.GetVisibleRect();

        // Act
        minimap.Calculate(itemsExtent, viewportBounds);

        // Get all corners
        var itemsTopLeft = minimap.CanvasToMinimap(itemsExtent.X, itemsExtent.Y);
        var itemsBottomRight = minimap.CanvasToMinimap(itemsExtent.Right, itemsExtent.Bottom);
        var vpTopLeft = minimap.CanvasToMinimap(viewportBounds.X, viewportBounds.Y);
        var vpBottomRight = minimap.CanvasToMinimap(viewportBounds.Right, viewportBounds.Bottom);

        // Assert - everything should be within minimap bounds (with some tolerance for centering)
        void AssertInBounds(AvaloniaPoint p, string name)
        {
            Assert.True(p.X >= -5 && p.X <= minimap.MinimapWidth + 5, 
                $"{name} X ({p.X}) should be within minimap width");
            Assert.True(p.Y >= -5 && p.Y <= minimap.MinimapHeight + 5, 
                $"{name} Y ({p.Y}) should be within minimap height");
        }

        AssertInBounds(itemsTopLeft, "Items top-left");
        AssertInBounds(itemsBottomRight, "Items bottom-right");
        // Note: viewport can extend outside if panned to empty area
    }

    /// <summary>
    /// This test simulates the exact scenario from the demo app to debug the issue.
    /// </summary>
    [Fact]
    public void DemoAppScenario_ShouldHaveCorrectViewportPosition()
    {
        // Arrange - exact values from the demo app
        var minimap = new MinimapSimulator(198, 148); // 200-2 for border

        // Demo nodes: Input(100,100), Process(400,150), Output(700,100)
        var node1 = new AvaloniaRect(100, 100, 150, 80);  // Input
        var node2 = new AvaloniaRect(400, 150, 150, 80);  // Process  
        var node3 = new AvaloniaRect(700, 100, 150, 80);  // Output
        var itemsExtent = node1.Union(node2).Union(node3);

        // Simulate different viewport scenarios
        var viewport = new ViewportState();
        viewport.SetViewSize(new AvaloniaSize(1100, 700)); // Approximate window size

        // Scenario 1: Default view (offset 0,0, zoom 1.0)
        viewport.SetOffset(0, 0);
        viewport.SetZoom(1.0);
        var viewportBounds = viewport.GetVisibleRect();
        
        minimap.Calculate(itemsExtent, viewportBounds);

        // Output debug info
        var debugInfo = minimap.GetDebugInfo();
        
        // Calculate positions
        var node1InMinimap = minimap.CanvasToMinimap(100, 100);
        var node2InMinimap = minimap.CanvasToMinimap(400, 150);
        var node3InMinimap = minimap.CanvasToMinimap(700, 100);
        var vpInMinimap = minimap.CanvasToMinimap(viewportBounds.X, viewportBounds.Y);
        var vpEndInMinimap = minimap.CanvasToMinimap(viewportBounds.Right, viewportBounds.Bottom);

        // The viewport at (0,0) with size 1100x700 should show all nodes
        // Verify viewport contains all nodes
        Assert.True(vpInMinimap.X <= node1InMinimap.X, 
            $"Viewport should contain Node1. VP left: {vpInMinimap.X}, Node1 left: {node1InMinimap.X}\n{debugInfo}");
        Assert.True(vpEndInMinimap.X >= minimap.CanvasToMinimap(850, 0).X, 
            $"Viewport should extend past Node3. VP right: {vpEndInMinimap.X}, Node3 right: {minimap.CanvasToMinimap(850, 0).X}\n{debugInfo}");

        // Scenario 2: Zoomed to 100% and centered on Process node
        viewport.CenterOn(new AvaloniaPoint(475, 190)); // Center of Process node
        viewportBounds = viewport.GetVisibleRect();
        minimap.Calculate(itemsExtent, viewportBounds);

        vpInMinimap = minimap.CanvasToMinimap(viewportBounds.X, viewportBounds.Y);
        vpEndInMinimap = minimap.CanvasToMinimap(viewportBounds.Right, viewportBounds.Bottom);
        node2InMinimap = minimap.CanvasToMinimap(400, 150);
        var node2EndInMinimap = minimap.CanvasToMinimap(550, 230);

        // The viewport should contain the Process node
        Assert.True(vpInMinimap.X <= node2InMinimap.X && vpEndInMinimap.X >= node2EndInMinimap.X,
            $"Viewport should contain Process node horizontally.\nVP: ({vpInMinimap.X} to {vpEndInMinimap.X})\nNode2: ({node2InMinimap.X} to {node2EndInMinimap.X})\n{minimap.GetDebugInfo()}");
        Assert.True(vpInMinimap.Y <= node2InMinimap.Y && vpEndInMinimap.Y >= node2EndInMinimap.Y,
            $"Viewport should contain Process node vertically.\nVP: ({vpInMinimap.Y} to {vpEndInMinimap.Y})\nNode2: ({node2InMinimap.Y} to {node2EndInMinimap.Y})\n{minimap.GetDebugInfo()}");
    }
}
