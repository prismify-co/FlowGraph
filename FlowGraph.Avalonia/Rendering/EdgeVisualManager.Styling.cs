// EdgeVisualManager.Styling.cs
// Partial class containing edge styling logic for EdgeStyle support

using Avalonia.Collections;
using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace FlowGraph.Avalonia.Rendering;

public partial class EdgeVisualManager
{
    /// <summary>
    /// Applies EdgeStyle to a path visual.
    /// </summary>
    /// <param name="path">The path to style.</param>
    /// <param name="edge">The edge containing the style.</param>
    /// <param name="theme">Theme resources for defaults.</param>
    /// <param name="isSelected">Whether the edge is selected.</param>
    internal void ApplyEdgeStyle(AvaloniaPath path, Edge edge, ThemeResources theme, bool isSelected)
    {
        var style = edge.Style;
        var settings = _renderContext.Settings;

        // Determine stroke color
        IBrush strokeBrush;
        if (isSelected)
        {
            strokeBrush = theme.NodeSelectedBorder;
        }
        else if (style?.StrokeColor != null)
        {
            strokeBrush = CreateBrushFromHex(style.StrokeColor) ?? theme.EdgeStroke;
        }
        else
        {
            strokeBrush = theme.EdgeStroke;
        }

        // Apply opacity
        if (style?.Opacity is < 1.0)
        {
            path.Opacity = style.Opacity;
        }
        else
        {
            path.Opacity = 1.0;
        }

        // Determine stroke width
        double strokeWidth;
        if (isSelected)
        {
            strokeWidth = settings.EdgeSelectedStrokeThickness;
        }
        else if (style?.StrokeWidth != null)
        {
            strokeWidth = style.StrokeWidth.Value;
        }
        else
        {
            strokeWidth = settings.EdgeStrokeThickness;
        }

        path.Stroke = strokeBrush;
        path.StrokeThickness = strokeWidth;

        // Apply dash pattern
        path.StrokeDashArray = GetDashArray(style);

        // Apply glow effect
        if (style?.Glow == true && !isSelected)
        {
            var glowColor = style.GlowColor != null
                ? ParseColor(style.GlowColor)
                : (style.StrokeColor != null ? ParseColor(style.StrokeColor) : Colors.Cyan);

            var intensity = style.GlowIntensity;
            path.Effect = new ImmutableDropShadowEffect(
                offsetX: 0,
                offsetY: 0,
                blurRadius: intensity * 4,
                color: glowColor,
                opacity: 1.0);
        }
        else
        {
            path.Effect = null;
        }
    }

    /// <summary>
    /// Gets the stroke brush for an edge, considering its style and selection state.
    /// </summary>
    internal IBrush GetEdgeStrokeBrush(Edge edge, ThemeResources theme, bool isSelected)
    {
        if (isSelected)
        {
            return theme.NodeSelectedBorder;
        }

        var style = edge.Style;
        if (style?.StrokeColor != null)
        {
            return CreateBrushFromHex(style.StrokeColor) ?? theme.EdgeStroke;
        }

        return theme.EdgeStroke;
    }

    /// <summary>
    /// Gets the stroke width for an edge, considering its style and selection state.
    /// </summary>
    internal double GetEdgeStrokeWidth(Edge edge, bool isSelected)
    {
        var settings = _renderContext.Settings;

        if (isSelected)
        {
            return settings.EdgeSelectedStrokeThickness;
        }

        var style = edge.Style;
        if (style?.StrokeWidth != null)
        {
            return style.StrokeWidth.Value;
        }

        return settings.EdgeStrokeThickness;
    }

    /// <summary>
    /// Converts EdgeDashPattern to Avalonia dash array.
    /// </summary>
    private static AvaloniaList<double>? GetDashArray(EdgeStyle? style)
    {
        if (style == null)
            return null;

        return style.DashPattern switch
        {
            EdgeDashPattern.Solid => null,
            EdgeDashPattern.Dashed => new AvaloniaList<double> { 6, 3 },
            EdgeDashPattern.Dotted => new AvaloniaList<double> { 2, 2 },
            EdgeDashPattern.DashDot => new AvaloniaList<double> { 6, 3, 2, 3 },
            EdgeDashPattern.LongDash => new AvaloniaList<double> { 12, 4 },
            EdgeDashPattern.Custom when style.CustomDashArray is { Length: > 0 } =>
                new AvaloniaList<double>(style.CustomDashArray),
            _ => null
        };
    }

    /// <summary>
    /// Creates a brush from a hex color string.
    /// </summary>
    private static IBrush? CreateBrushFromHex(string hex)
    {
        try
        {
            var color = Color.Parse(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a hex color string to a Color.
    /// </summary>
    private static Color ParseColor(string hex)
    {
        try
        {
            return Color.Parse(hex);
        }
        catch
        {
            return Colors.Gray;
        }
    }
}
