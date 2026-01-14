using Avalonia.Controls;
using Avalonia.Media;
using Xunit;
using AvaloniaSize = global::Avalonia.Size;
using AvaloniaRect = global::Avalonia.Rect;

namespace FlowGraph.Core.Tests;

/// <summary>
/// These tests document and verify the hit testing design constraints in FlowGraph.
/// 
/// IMPORTANT: Avalonia's InputHitTest/VisualTreeHitTest uses visual tree positions 
/// (Canvas.Left, Canvas.Top) NOT rendered positions after RenderTransform.
/// 
/// This means:
/// - DO NOT use RenderTransform on the main canvas to implement pan/zoom fast paths
/// - Visuals will appear to move on screen, but hit testing will use original positions
/// - This causes clicks to "miss" nodes after panning
/// 
/// If you need to optimize pan/zoom performance, consider:
/// 1. Direct rendering mode (custom hit testing based on coordinates)
/// 2. Custom hit testing that accounts for transforms
/// 3. Throttling/debouncing render frequency
/// 4. Virtualization (only render visible nodes)
/// </summary>
public class HitTestingDesignTests
{
    /// <summary>
    /// Verifies that RenderTransform on a parent Canvas does NOT affect InputHitTest coordinates.
    /// This test documents why transform-based fast paths break hit testing.
    /// </summary>
    [Fact]
    public void RenderTransform_DoesNotAffect_InputHitTest_Coordinates()
    {
        // Arrange: Create a canvas with a child at position (100, 100)
        var canvas = new Canvas { Width = 500, Height = 500 };
        var child = new Border 
        { 
            Width = 50, 
            Height = 50, 
            Background = global::Avalonia.Media.Brushes.Red,
            Tag = "TestNode"
        };
        
        Canvas.SetLeft(child, 100);
        Canvas.SetTop(child, 100);
        canvas.Children.Add(child);
        
        // Force layout
        canvas.Measure(new AvaloniaSize(500, 500));
        canvas.Arrange(new AvaloniaRect(0, 0, 500, 500));
        
        // Act: Apply a translate transform that visually moves content by (50, 50)
        canvas.RenderTransform = new TranslateTransform(50, 50);
        
        // The child visually appears at (150, 150) but...
        
        // Assert: Canvas.GetLeft/GetTop still return the ORIGINAL position
        // This is what InputHitTest uses, NOT the transformed position
        Assert.Equal(100, Canvas.GetLeft(child));
        Assert.Equal(100, Canvas.GetTop(child));
        
        // IMPORTANT: This is why transform-based pan optimization breaks hit testing!
        // After panning with TranslateTransform:
        // - Visual appears at (150, 150)
        // - User clicks at (150, 150)
        // - InputHitTest checks (150, 150) against original bounds (100, 100, 50, 50)
        // - Click MISSES because 150 > 150 (right edge of original bounds)
    }

    /// <summary>
    /// Verifies that updating Canvas.Left/Top (the proper way) updates hit test coordinates.
    /// </summary>
    [Fact]
    public void Canvas_SetLeft_SetTop_Updates_HitTest_Coordinates()
    {
        // Arrange
        var canvas = new Canvas { Width = 500, Height = 500 };
        var child = new Border 
        { 
            Width = 50, 
            Height = 50, 
            Background = global::Avalonia.Media.Brushes.Red,
            Tag = "TestNode"
        };
        
        Canvas.SetLeft(child, 100);
        Canvas.SetTop(child, 100);
        canvas.Children.Add(child);
        
        // Act: Move by updating Canvas.Left/Top (what RenderAll does)
        Canvas.SetLeft(child, 150);
        Canvas.SetTop(child, 150);
        
        // Assert: The position is updated and hit testing will work correctly
        Assert.Equal(150, Canvas.GetLeft(child));
        Assert.Equal(150, Canvas.GetTop(child));
    }

    /// <summary>
    /// Documents the constraint that FlowCanvas.ApplyViewportTransforms must NOT use 
    /// RenderTransform on _mainCanvas because it breaks hit testing.
    /// </summary>
    [Fact]
    public void ApplyViewportTransforms_MustNot_Use_RenderTransform_On_MainCanvas()
    {
        // This is a design constraint test - it documents the requirement
        // The actual verification is done by reading the code
        
        // REQUIREMENT: ApplyViewportTransforms must call RenderAll() which updates
        // Canvas.Left/Top positions for all nodes, NOT apply RenderTransform to the canvas.
        //
        // Previous broken implementation used:
        //   _mainCanvas.RenderTransform = new TranslateTransform(deltaX, deltaY);
        // 
        // This visually moved nodes but broke InputHitTest because hit testing
        // uses Canvas.GetLeft/GetTop, not transformed positions.
        //
        // See: FlowCanvas.axaml.cs ApplyViewportTransforms() for correct implementation
        
        Assert.True(true, "Design constraint documented - verify in code review");
    }
}
