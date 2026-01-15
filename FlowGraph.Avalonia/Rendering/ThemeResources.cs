using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Provides theme-aware resource access for FlowCanvas components.
/// <para>
/// All colors can be customized by defining resources in your App.axaml:
/// </para>
/// <code>
/// &lt;Application.Resources&gt;
///     &lt;SolidColorBrush x:Key="FlowCanvasNodeBackground" Color="#2D2D30"/&gt;
///     &lt;SolidColorBrush x:Key="FlowCanvasNodeBorder" Color="#4682B4"/&gt;
///     &lt;!-- etc. --&gt;
/// &lt;/Application.Resources&gt;
/// </code>
/// <para>
/// Available resource keys:
/// </para>
/// <list type="bullet">
///     <item><description>Canvas: FlowCanvasBackground, FlowCanvasGridColor</description></item>
///     <item><description>Node: FlowCanvasNodeBackground, FlowCanvasNodeBorder, FlowCanvasNodeSelectedBorder, FlowCanvasNodeText</description></item>
///     <item><description>Input Node: FlowCanvasInputNodeBackground, FlowCanvasInputNodeBorder, FlowCanvasInputNodeText, FlowCanvasInputNodeIcon</description></item>
///     <item><description>Output Node: FlowCanvasOutputNodeBackground, FlowCanvasOutputNodeBorder, FlowCanvasOutputNodeText, FlowCanvasOutputNodeIcon</description></item>
///     <item><description>Edge: FlowCanvasEdgeStroke, FlowCanvasEdgeSelectedStroke</description></item>
///     <item><description>Port: FlowCanvasPortBackground, FlowCanvasPortBorder, FlowCanvasPortHover, FlowCanvasPortValidConnection, FlowCanvasPortInvalidConnection</description></item>
///     <item><description>Group: FlowCanvasGroupBackground, FlowCanvasGroupBorder, FlowCanvasGroupLabelText</description></item>
///     <item><description>Selection: FlowCanvasSelectionBoxFill, FlowCanvasSelectionBoxStroke</description></item>
///     <item><description>Minimap: FlowCanvasMinimapBackground, FlowCanvasMinimapViewportFill, FlowCanvasMinimapViewportStroke</description></item>
/// </list>
/// </summary>
public class ThemeResources
{
    private readonly StyledElement _element;

    // Cache for performance - reset when theme changes
    private bool _isLightTheme;
    private bool _themeDetected;

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
    /// Tries to get a resource, returning null if not found.
    /// </summary>
    public T? TryGetResource<T>(string key) where T : class
    {
        if (_element.TryGetResource(key, _element.ActualThemeVariant, out var resource) && resource is T typedResource)
        {
            return typedResource;
        }
        return null;
    }

    /// <summary>
    /// Gets a brush resource with a fallback color.
    /// </summary>
    public IBrush GetBrush(string key, string fallbackColor)
    {
        return GetResource<IBrush>(key, new SolidColorBrush(Color.Parse(fallbackColor)));
    }

    /// <summary>
    /// Tries to get a brush resource, returning null if not found.
    /// </summary>
    public IBrush? TryGetBrush(string key)
    {
        return TryGetResource<IBrush>(key);
    }

    /// <summary>
    /// Gets a color resource with a fallback value.
    /// </summary>
    public Color GetColor(string key, string fallbackColor)
    {
        if (_element.TryGetResource(key, _element.ActualThemeVariant, out var resource))
        {
            if (resource is Color color) return color;
            if (resource is SolidColorBrush brush) return brush.Color;
        }
        return Color.Parse(fallbackColor);
    }

    #region Canvas

    public IBrush Background => GetBrush("FlowCanvasBackground",
        IsLightTheme ? "#F5F5F5" : "#1E1E1E");

    public IBrush GridColor => GetBrush("FlowCanvasGridColor",
        IsLightTheme ? "#E0E0E0" : "#333333");

    #endregion

    #region Default Node

    public IBrush NodeBackground => GetBrush("FlowCanvasNodeBackground",
        IsLightTheme ? "#FFFFFF" : "#2D2D30");

    public IBrush NodeBorder => GetBrush("FlowCanvasNodeBorder", "#4682B4");

    public IBrush NodeSelectedBorder => GetBrush("FlowCanvasNodeSelectedBorder", "#FF6B00");

    public IBrush NodeText => GetBrush("FlowCanvasNodeText",
        IsLightTheme ? "#1E1E1E" : "#FFFFFF");

    #endregion

    #region Input Node (Green)

    public IBrush InputNodeBackground => GetBrush("FlowCanvasInputNodeBackground",
        IsLightTheme ? "#E8F5E9" : "#1B5E20");

    public IBrush InputNodeBorder => GetBrush("FlowCanvasInputNodeBorder", "#4CAF50");

    public IBrush InputNodeText => GetBrush("FlowCanvasInputNodeText",
        IsLightTheme ? "#1B5E20" : "#FFFFFF");

    public IBrush InputNodeIcon => GetBrush("FlowCanvasInputNodeIcon", "#4CAF50");

    #endregion

    #region Output Node (Red/Coral)

    public IBrush OutputNodeBackground => GetBrush("FlowCanvasOutputNodeBackground",
        IsLightTheme ? "#FFEBEE" : "#B71C1C");

    public IBrush OutputNodeBorder => GetBrush("FlowCanvasOutputNodeBorder", "#EF5350");

    public IBrush OutputNodeText => GetBrush("FlowCanvasOutputNodeText",
        IsLightTheme ? "#B71C1C" : "#FFFFFF");

    public IBrush OutputNodeIcon => GetBrush("FlowCanvasOutputNodeIcon", "#EF5350");

    #endregion

    #region Edges

    public IBrush EdgeStroke => GetBrush("FlowCanvasEdgeStroke", "#808080");

    public IBrush EdgeSelectedStroke => GetBrush("FlowCanvasEdgeSelectedStroke", "#FF6B00");

    #endregion

    #region Ports

    public IBrush PortBackground => GetBrush("FlowCanvasPortBackground", "#4682B4");

    public IBrush PortBorder => GetBrush("FlowCanvasPortBorder",
        IsLightTheme ? "#333333" : "#FFFFFF");

    public IBrush PortHover => GetBrush("FlowCanvasPortHover", "#FF6B00");

    /// <summary>
    /// Background color for hovered ports. Falls back to PortHover if not set.
    /// </summary>
    public IBrush? PortHoverBackground => TryGetBrush("FlowCanvasPortHoverBackground");

    /// <summary>
    /// Border color for hovered ports. Falls back to PortHover if not set.
    /// </summary>
    public IBrush? PortHoverBorder => TryGetBrush("FlowCanvasPortHoverBorder");

    public IBrush PortValidConnection => GetBrush("FlowCanvasPortValidConnection", "#22C55E");

    public IBrush PortInvalidConnection => GetBrush("FlowCanvasPortInvalidConnection", "#EF4444");

    #endregion

    #region Groups

    public IBrush GroupBackground => GetBrush("FlowCanvasGroupBackground",
        IsLightTheme ? "#20845EC2" : "#18FFFFFF");

    public IBrush GroupBorder => GetBrush("FlowCanvasGroupBorder",
        IsLightTheme ? "#845EC2" : "#606060");

    public IBrush GroupLabelText => GetBrush("FlowCanvasGroupLabelText",
        IsLightTheme ? "#5A4080" : "#B0B0B0");

    public IBrush GroupHeaderBackground => GetBrush("FlowCanvasGroupHeaderBackground",
        IsLightTheme ? "#30845EC2" : "#3C3C3C");

    // Legacy properties (kept for backward compatibility)
    public IBrush GroupHeaderText => GetBrush("FlowCanvasGroupHeaderText", "#FFFFFF");
    public IBrush GroupHeaderHover => GetBrush("FlowCanvasGroupHeaderHover", "#505050");
    public IBrush GroupBodyBackground => GetBrush("FlowCanvasGroupBodyBackground", "#252526");

    #endregion

    #region Selection

    public IBrush SelectionBoxFill => GetBrush("FlowCanvasSelectionBoxFill", "#204682B4");

    public IBrush SelectionBoxStroke => GetBrush("FlowCanvasSelectionBoxStroke", "#4682B4");

    #endregion

    #region Headered Nodes (React Flow style)

    /// <summary>
    /// Header text color for headered nodes.
    /// </summary>
    public IBrush HeaderedNodeHeaderText => GetBrush("FlowCanvasHeaderedNodeHeaderText",
        IsLightTheme ? "#646464" : "#B0B0B0");

    /// <summary>
    /// Header background color for headered nodes (semi-transparent glassy effect).
    /// </summary>
    public IBrush HeaderedNodeHeaderBackground => GetBrush("FlowCanvasHeaderedNodeHeaderBackground",
        IsLightTheme ? "#80F8F8F8" : "#80303030");

    /// <summary>
    /// Body background color for headered nodes.
    /// </summary>
    public IBrush HeaderedNodeBodyBackground => GetBrush("FlowCanvasHeaderedNodeBodyBackground",
        IsLightTheme ? "#FFFFFF" : "#252526");

    /// <summary>
    /// Border color for headered nodes.
    /// </summary>
    public IBrush HeaderedNodeBorder => GetBrush("FlowCanvasHeaderedNodeBorder",
        IsLightTheme ? "#E6E6E6" : "#404040");

    /// <summary>
    /// Shadow color for headered nodes.
    /// </summary>
    public Color HeaderedNodeShadow => GetColor("FlowCanvasHeaderedNodeShadow",
        IsLightTheme ? "#1E000000" : "#1E000000");

    #endregion

    #region Interactive Nodes

    /// <summary>
    /// Background for value display containers (output nodes, etc.).
    /// </summary>
    public IBrush InteractiveValueBackground => GetBrush("FlowCanvasInteractiveValueBackground",
        IsLightTheme ? "#F0F0F0" : "#383838");

    /// <summary>
    /// Foreground for muted/placeholder text in interactive nodes.
    /// </summary>
    public IBrush InteractiveMutedText => GetBrush("FlowCanvasInteractiveMutedText",
        IsLightTheme ? "#505050" : "#909090");

    /// <summary>
    /// Background for preview areas in interactive nodes.
    /// </summary>
    public IBrush InteractivePreviewBackground => GetBrush("FlowCanvasInteractivePreviewBackground",
        IsLightTheme ? "#FAFAFA" : "#2A2A2A");

    /// <summary>
    /// Foreground for preview placeholder text.
    /// </summary>
    public IBrush InteractivePreviewText => GetBrush("FlowCanvasInteractivePreviewText",
        IsLightTheme ? "#C8C8C8" : "#505050");

    #endregion

    #region Minimap

    public IBrush MinimapBackground => GetBrush("FlowCanvasMinimapBackground",
        IsLightTheme ? "#F0F0F0" : "#252526");

    public IBrush MinimapViewportFill => GetBrush("FlowCanvasMinimapViewportFill", "#304682B4");

    public IBrush MinimapViewportStroke => GetBrush("FlowCanvasMinimapViewportStroke", "#4682B4");

    #endregion

    #region Theme Detection

    /// <summary>
    /// Determines if the current theme is light or dark.
    /// Cached for performance - call InvalidateThemeCache() if theme changes.
    /// </summary>
    public bool IsLightTheme
    {
        get
        {
            if (!_themeDetected)
            {
                _isLightTheme = DetectLightTheme();
                _themeDetected = true;
            }
            return _isLightTheme;
        }
    }

    /// <summary>
    /// Invalidates the cached theme detection.
    /// Call this when the theme changes.
    /// </summary>
    public void InvalidateThemeCache()
    {
        _themeDetected = false;
    }

    private bool DetectLightTheme()
    {
        // First check the actual theme variant
        var variant = _element.ActualThemeVariant;
        if (variant == ThemeVariant.Light)
            return true;
        if (variant == ThemeVariant.Dark)
            return false;

        // For "Default" variant, check if user defined a custom background
        if (_element.TryGetResource("FlowCanvasBackground", variant, out var bg) &&
            bg is SolidColorBrush brush)
        {
            var color = brush.Color;
            var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance > 0.5;
        }

        // Default to dark theme
        return false;
    }

    #endregion
}
