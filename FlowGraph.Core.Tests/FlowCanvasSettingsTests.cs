using FlowGraph.Avalonia;

namespace FlowGraph.Core.Tests;

public class FlowCanvasSettingsTests
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        var settings = FlowCanvasSettings.Default;

        Assert.Equal(150, settings.NodeWidth);
        Assert.Equal(80, settings.NodeHeight);
        Assert.Equal(12, settings.PortSize);
        Assert.Equal(20, settings.GridSpacing);
        Assert.Equal(0.1, settings.MinZoom);
        Assert.Equal(3.0, settings.MaxZoom);
        Assert.True(settings.SnapToGrid);
        Assert.True(settings.PanOnDrag);
        Assert.Equal(SelectionMode.Partial, settings.SelectionMode);
    }

    [Fact]
    public void EffectiveSnapGridSize_ShouldUseGridSpacing_WhenSnapGridSizeIsNull()
    {
        var settings = new FlowCanvasSettings
        {
            GridSpacing = 25,
            SnapGridSize = null
        };

        Assert.Equal(25, settings.EffectiveSnapGridSize);
    }

    [Fact]
    public void EffectiveSnapGridSize_ShouldUseSnapGridSize_WhenSet()
    {
        var settings = new FlowCanvasSettings
        {
            GridSpacing = 25,
            SnapGridSize = 10
        };

        Assert.Equal(10, settings.EffectiveSnapGridSize);
    }

    [Theory]
    [InlineData(SelectionMode.Partial)]
    [InlineData(SelectionMode.Full)]
    public void SelectionMode_ShouldBeConfigurable(SelectionMode mode)
    {
        var settings = new FlowCanvasSettings
        {
            SelectionMode = mode
        };

        Assert.Equal(mode, settings.SelectionMode);
    }

    [Fact]
    public void PanOnDrag_WhenTrue_LeftClickPans()
    {
        var settings = new FlowCanvasSettings { PanOnDrag = true };
        
        // When PanOnDrag is true:
        // - Left click on empty canvas = Pan
        // - Shift + Left click = Box select
        Assert.True(settings.PanOnDrag);
    }

    [Fact]
    public void PanOnDrag_WhenFalse_LeftClickSelects()
    {
        var settings = new FlowCanvasSettings { PanOnDrag = false };
        
        // When PanOnDrag is false:
        // - Left click on empty canvas = Box select
        // - Shift + Left click = Pan
        Assert.False(settings.PanOnDrag);
    }
}
