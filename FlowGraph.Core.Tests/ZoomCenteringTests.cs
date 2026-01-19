using Avalonia.Media;
using FlowGraph.Avalonia;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaSize = Avalonia.Size;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Tests to prevent regression of the zoom centering bug.
/// The bug was caused by missing RenderTransformOrigin="0,0" on MainCanvas,
/// causing Avalonia to apply transforms around the element's center (0.5, 0.5)
/// instead of the top-left corner.
/// </summary>
public class ZoomCenteringTests
{
  [Fact]
  public void SetZoom_ShouldKeepCanvasPointAtZoomCenterFixed()
  {
    // Arrange
    var viewport = new ViewportState();
    viewport.SetViewSize(new AvaloniaSize(1000, 600));
    viewport.SetOffset(100, 50);

    var zoomCenter = new AvaloniaPoint(500, 300); // Screen center
    var canvasPointBefore = viewport.ViewportToCanvas(zoomCenter);

    // Act - zoom in towards the center
    viewport.SetZoom(2.0, zoomCenter);

    // Assert - the same canvas point should still be at the zoom center
    var canvasPointAfter = viewport.ViewportToCanvas(zoomCenter);

    Assert.Equal(canvasPointBefore.X, canvasPointAfter.X, 0.001);
    Assert.Equal(canvasPointBefore.Y, canvasPointAfter.Y, 0.001);
  }

  [Fact]
  public void MultipleZoomOperations_ShouldKeepCenterPointFixed()
  {
    // Arrange
    var viewport = new ViewportState();
    viewport.SetViewSize(new AvaloniaSize(1000, 600));
    var zoomCenter = new AvaloniaPoint(500, 300);

    var initialCanvasPoint = viewport.ViewportToCanvas(zoomCenter);

    // Act - zoom in multiple times
    for (int i = 0; i < 10; i++)
    {
      viewport.ZoomIn(zoomCenter);
    }

    // Assert
    var finalCanvasPoint = viewport.ViewportToCanvas(zoomCenter);
    Assert.Equal(initialCanvasPoint.X, finalCanvasPoint.X, 0.01);
    Assert.Equal(initialCanvasPoint.Y, finalCanvasPoint.Y, 0.01);
  }

  [Fact]
  public void ZoomInThenOut_ShouldReturnToSameCanvasPoint()
  {
    // Arrange
    var viewport = new ViewportState();
    viewport.SetViewSize(new AvaloniaSize(1000, 600));
    viewport.SetOffset(200, 100);

    var zoomCenter = new AvaloniaPoint(500, 300);
    var initialCanvasPoint = viewport.ViewportToCanvas(zoomCenter);

    // Act - zoom in then out same number of times
    for (int i = 0; i < 5; i++)
      viewport.ZoomIn(zoomCenter);
    for (int i = 0; i < 5; i++)
      viewport.ZoomOut(zoomCenter);

    // Assert
    var finalCanvasPoint = viewport.ViewportToCanvas(zoomCenter);
    Assert.Equal(initialCanvasPoint.X, finalCanvasPoint.X, 0.01);
    Assert.Equal(initialCanvasPoint.Y, finalCanvasPoint.Y, 0.01);
  }

  [Fact]
  public void ApplyToTransformGroup_ShouldSetCorrectScaleAndTranslate()
  {
    // Arrange
    var viewport = new ViewportState();
    viewport.SetViewSize(new AvaloniaSize(1000, 600));
    viewport.SetOffset(150, 75);
    viewport.SetZoom(1.5, new AvaloniaPoint(500, 300));

    var scaleTransform = new ScaleTransform();
    var translateTransform = new TranslateTransform();

    // Act
    viewport.ApplyToTransformGroup(scaleTransform, translateTransform);

    // Assert
    Assert.Equal(viewport.Zoom, scaleTransform.ScaleX, 0.001);
    Assert.Equal(viewport.Zoom, scaleTransform.ScaleY, 0.001);
    Assert.Equal(viewport.OffsetX, translateTransform.X, 0.001);
    Assert.Equal(viewport.OffsetY, translateTransform.Y, 0.001);
  }

  [Fact]
  public void TransformGroup_ScaleThenTranslate_ProducesCorrectMapping()
  {
    // This test verifies the transform order is correct:
    // screenPos = canvasPos * scale + translate

    // Arrange
    var viewport = new ViewportState();
    viewport.SetViewSize(new AvaloniaSize(1000, 600));
    viewport.SetOffset(100, 50);
    viewport.SetZoom(2.0, new AvaloniaPoint(500, 300));

    var scaleTransform = new ScaleTransform();
    var translateTransform = new TranslateTransform();
    viewport.ApplyToTransformGroup(scaleTransform, translateTransform);

    // Create a TransformGroup with Scale first, then Translate (order matters!)
    var transformGroup = new TransformGroup();
    transformGroup.Children.Add(scaleTransform);
    transformGroup.Children.Add(translateTransform);

    // Pick a canvas point
    var canvasPoint = new AvaloniaPoint(200, 150);

    // Calculate expected screen position manually
    var expectedScreenX = canvasPoint.X * viewport.Zoom + viewport.OffsetX;
    var expectedScreenY = canvasPoint.Y * viewport.Zoom + viewport.OffsetY;

    // Act - transform through the group
    var screenPoint = transformGroup.Value.Transform(canvasPoint);

    // Assert
    Assert.Equal(expectedScreenX, screenPoint.X, 0.001);
    Assert.Equal(expectedScreenY, screenPoint.Y, 0.001);

    // Also verify it matches CanvasToViewport
    var expectedFromViewport = viewport.CanvasToViewport(canvasPoint);
    Assert.Equal(expectedFromViewport.X, screenPoint.X, 0.001);
    Assert.Equal(expectedFromViewport.Y, screenPoint.Y, 0.001);
  }
}
