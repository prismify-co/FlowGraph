using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Provides theme-aware resource access for FlowCanvas components.
/// </summary>
public class ThemeResources
{
    private readonly StyledElement _element;

    public ThemeResources(StyledElement element)
    {
        _element = element;
    }

    /// <summary>
    /// Gets a theme resource with a fallback value.
    /// </summary>
    public T GetResource<T>(string key, T fallback) where T : class
    {
        if (_element.TryGetResource(key, _element.ActualThemeVariant, out var resource) && resource is T typedResource)
        {
            return typedResource;
        }
        return fallback;
    }

    /// <summary>
    /// Gets a brush resource with a fallback color.
    /// </summary>
    public IBrush GetBrush(string key, string fallbackColor)
    {
        return GetResource<IBrush>(key, new SolidColorBrush(Color.Parse(fallbackColor)));
    }

    // Common brush accessors
    public IBrush Background => GetBrush("FlowCanvasBackground", "#1E1E1E");
    public IBrush GridColor => GetBrush("FlowCanvasGridColor", "#333333");
    public IBrush NodeBackground => GetBrush("FlowCanvasNodeBackground", "#2D2D30");
    public IBrush NodeBorder => GetBrush("FlowCanvasNodeBorder", "#4682B4");
    public IBrush NodeSelectedBorder => GetBrush("FlowCanvasNodeSelectedBorder", "#FF6B00");
    public IBrush NodeText => GetBrush("FlowCanvasNodeText", "#FFFFFF");
    public IBrush EdgeStroke => GetBrush("FlowCanvasEdgeStroke", "#808080");
    public IBrush PortBackground => GetBrush("FlowCanvasPortBackground", "#4682B4");
    public IBrush PortBorder => GetBrush("FlowCanvasPortBorder", "#FFFFFF");
    public IBrush PortHover => GetBrush("FlowCanvasPortHover", "#FF6B00");

    // Group-specific colors - React Flow inspired translucent style
    // Light theme: lavender/purple tint, Dark theme: subtle highlight
    public IBrush GroupBackground => GetBrush("FlowCanvasGroupBackground", 
        IsLightTheme ? "#20845EC2" : "#18FFFFFF"); // Translucent purple/white
    
    public IBrush GroupBorder => GetBrush("FlowCanvasGroupBorder",
        IsLightTheme ? "#845EC2" : "#606060"); // Purple/gray dashed border
    
    public IBrush GroupLabelText => GetBrush("FlowCanvasGroupLabelText",
        IsLightTheme ? "#5A4080" : "#B0B0B0"); // Darker purple text / light gray
    
    // Legacy properties (kept for backward compatibility)
    public IBrush GroupHeaderBackground => GetBrush("FlowCanvasGroupHeaderBackground", 
        IsLightTheme ? "#30845EC2" : "#3C3C3C");
    public IBrush GroupHeaderText => GetBrush("FlowCanvasGroupHeaderText", "#FFFFFF");
    public IBrush GroupHeaderHover => GetBrush("FlowCanvasGroupHeaderHover", "#505050");
    public IBrush GroupBodyBackground => GetBrush("FlowCanvasGroupBodyBackground", "#252526");

    /// <summary>
    /// Determines if the current theme is light or dark.
    /// </summary>
    private bool IsLightTheme
    {
        get
        {
            // Check if we're in a light theme by examining the background color
            if (_element.TryGetResource("FlowCanvasBackground", _element.ActualThemeVariant, out var bg) && 
                bg is SolidColorBrush brush)
            {
                // If background is light (high luminance), we're in light theme
                var color = brush.Color;
                var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                return luminance > 0.5;
            }
            
            // Also check the actual theme variant
            var variant = _element.ActualThemeVariant;
            return variant == ThemeVariant.Light;
        }
    }
}
