namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Design tokens for consistent styling across all FlowGraph components.
/// Inspired by React Flow's CSS variables and design system.
/// 
/// Usage:
/// - Reference these constants throughout renderers for consistency
/// - All values are in logical pixels (unscaled)
/// - Scale factor is applied by the renderer context when needed
/// </summary>
public static class DesignTokens
{
    #region Typography

    /// <summary>
    /// Extra small font size for compact UI elements.
    /// Used for: simplified nodes, secondary labels
    /// </summary>
    public const double FontSizeXs = 10;

    /// <summary>
    /// Small font size for supporting text.
    /// Used for: value displays, input hints, group labels
    /// </summary>
    public const double FontSizeSm = 11;

    /// <summary>
    /// Base/default font size for body text.
    /// Used for: node labels, form inputs, edge labels
    /// </summary>
    public const double FontSizeBase = 12;

    /// <summary>
    /// Medium font size for emphasis.
    /// Used for: node titles, output displays
    /// </summary>
    public const double FontSizeMd = 14;

    /// <summary>
    /// Large font size for icons and headings.
    /// Used for: styled node icons
    /// </summary>
    public const double FontSizeLg = 18;

    /// <summary>
    /// Extra large font size for prominent elements.
    /// Used for: preview placeholders
    /// </summary>
    public const double FontSizeXl = 24;

    #endregion

    #region Border Radius

    /// <summary>
    /// Small radius for tight corners.
    /// Used for: buttons, chips, tags
    /// </summary>
    public const double RadiusSm = 3;

    /// <summary>
    /// Base radius for standard components.
    /// Used for: input fields, color swatches
    /// </summary>
    public const double RadiusBase = 4;

    /// <summary>
    /// Medium radius for nodes and cards.
    /// Used for: default nodes, toolbar
    /// </summary>
    public const double RadiusMd = 6;

    /// <summary>
    /// Large radius for prominent containers.
    /// Used for: headered nodes, groups, preview areas
    /// </summary>
    public const double RadiusLg = 8;

    #endregion

    #region Border Thickness

    /// <summary>
    /// Thin border for subtle separators.
    /// Used for: header separators, input borders
    /// </summary>
    public const double BorderThin = 1;

    /// <summary>
    /// Standard border for most elements.
    /// Used for: node borders, port borders, selection
    /// </summary>
    public const double BorderBase = 2;

    /// <summary>
    /// Thick border for emphasis.
    /// Used for: selected nodes, active states
    /// </summary>
    public const double BorderThick = 3;

    #endregion

    #region Spacing

    /// <summary>
    /// Extra small spacing.
    /// </summary>
    public const double SpacingXs = 2;

    /// <summary>
    /// Small spacing.
    /// </summary>
    public const double SpacingSm = 4;

    /// <summary>
    /// Base spacing for padding and gaps.
    /// </summary>
    public const double SpacingBase = 6;

    /// <summary>
    /// Medium spacing.
    /// </summary>
    public const double SpacingMd = 8;

    /// <summary>
    /// Large spacing for section separation.
    /// </summary>
    public const double SpacingLg = 12;

    #endregion

    #region Node Dimensions

    /// <summary>
    /// Default node width.
    /// </summary>
    public const double NodeWidthDefault = 150;

    /// <summary>
    /// Narrow node width for simple inputs.
    /// </summary>
    public const double NodeWidthNarrow = 120;

    /// <summary>
    /// Wide node width for complex content.
    /// </summary>
    public const double NodeWidthWide = 180;

    /// <summary>
    /// Default node height.
    /// </summary>
    public const double NodeHeightDefault = 80;

    /// <summary>
    /// Compact node height.
    /// </summary>
    public const double NodeHeightCompact = 50;

    /// <summary>
    /// Tall node height for more content.
    /// </summary>
    public const double NodeHeightTall = 100;

    /// <summary>
    /// Expanded node height for radio buttons, multi-line content.
    /// </summary>
    public const double NodeHeightExpanded = 120;

    /// <summary>
    /// Large node height for output displays.
    /// </summary>
    public const double NodeHeightLarge = 200;

    /// <summary>
    /// Large node width for output displays.
    /// </summary>
    public const double NodeWidthLarge = 220;

    /// <summary>
    /// Minimum node width for resize.
    /// </summary>
    public const double NodeMinWidth = 60;

    /// <summary>
    /// Minimum node height for resize.
    /// </summary>
    public const double NodeMinHeight = 30;

    #endregion

    #region Interactive Elements

    /// <summary>
    /// Standard input field width.
    /// </summary>
    public const double InputWidthDefault = 140;

    /// <summary>
    /// Narrow input field width.
    /// </summary>
    public const double InputWidthNarrow = 100;

    /// <summary>
    /// Wide input field width.
    /// </summary>
    public const double InputWidthWide = 160;

    /// <summary>
    /// Standard slider width.
    /// </summary>
    public const double SliderWidth = 140;

    /// <summary>
    /// Standard combo box width.
    /// </summary>
    public const double ComboBoxWidth = 120;

    /// <summary>
    /// Color swatch small size.
    /// </summary>
    public const double ColorSwatchSm = 24;

    /// <summary>
    /// Color swatch large size.
    /// </summary>
    public const double ColorSwatchLg = 60;

    #endregion

    #region Group Constants (legacy - use from GraphRenderModel)

    // These are defined in GraphRenderModel for backwards compatibility:
    // - GroupHeaderHeight = 28
    // - MinGroupWidth = 200
    // - MinGroupHeight = 100
    // - GroupCollapseButtonSize = 18
    // - GroupBorderRadius = 8

    #endregion

    #region Shadows

    /// <summary>
    /// Standard shadow blur radius.
    /// </summary>
    public const double ShadowBlur = 8;

    /// <summary>
    /// Standard shadow offset X.
    /// </summary>
    public const double ShadowOffsetX = 0;

    /// <summary>
    /// Standard shadow offset Y.
    /// </summary>
    public const double ShadowOffsetY = 2;

    /// <summary>
    /// Standard shadow spread (negative for tighter shadow).
    /// </summary>
    public const double ShadowSpread = -2;

    #endregion
}
