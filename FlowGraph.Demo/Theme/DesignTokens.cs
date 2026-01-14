using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace FlowGraph.Demo.Theme;

/// <summary>
/// Design tokens for the FlowGraph demo - follows system theme.
/// </summary>
public static class DesignTokens
{
    // Accent colors (consistent across themes)
    public static readonly Color AccentPrimary = Color.Parse("#6366F1");
    public static readonly Color AccentSuccess = Color.Parse("#22C55E");
    public static readonly Color AccentWarning = Color.Parse("#EAB308");
    public static readonly Color AccentError = Color.Parse("#EF4444");

    // Spacing
    public const double SpaceXs = 4;
    public const double SpaceSm = 8;
    public const double SpaceMd = 12;
    public const double SpaceLg = 16;
    public const double SpaceXl = 24;
    public const double Space2Xl = 32;

    // Typography
    public const double FontSizeXs = 11;
    public const double FontSizeSm = 13;
    public const double FontSizeMd = 14;
    public const double FontSizeLg = 16;
    public const double FontSizeXl = 20;
    public const double FontSize2Xl = 24;

    // Radius
    public const double RadiusSm = 4;
    public const double RadiusMd = 6;
    public const double RadiusLg = 8;

    // Layout
    public const double SidebarWidth = 200;

    // Helper methods
    public static SolidColorBrush Brush(Color color) => new(color);
    public static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));

    /// <summary>
    /// Creates a styled button.
    /// </summary>
    public static Button CreateButton(string text, Action onClick, bool isPrimary = false)
    {
        var btn = new Button
        {
            Content = text,
            Padding = new Thickness(SpaceMd, SpaceSm),
            Margin = new Thickness(0, 0, SpaceSm, SpaceSm),
            CornerRadius = new CornerRadius(RadiusMd),
            FontSize = FontSizeSm,
            Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand)
        };
        
        if (isPrimary)
        {
            btn.Background = Brush(AccentPrimary);
            btn.Foreground = Brushes.White;
        }
        
        btn.Click += (_, _) => onClick();
        return btn;
    }

    /// <summary>
    /// Creates a section header with title and description.
    /// </summary>
    public static Border CreateSectionHeader(string title, string? description = null)
    {
        var panel = new StackPanel { Spacing = SpaceXs };

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = FontSizeXl,
            FontWeight = FontWeight.Bold
        });

        if (!string.IsNullOrEmpty(description))
        {
            panel.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = FontSizeMd,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            });
        }

        return new Border
        {
            Padding = new Thickness(SpaceLg, SpaceMd),
            Margin = new Thickness(0, 0, 0, SpaceSm),
            Child = panel
        };
    }

    /// <summary>
    /// Creates a status bar.
    /// </summary>
    public static Border CreateStatusBar(out TextBlock statusText)
    {
        statusText = new TextBlock
        {
            Text = "Ready",
            FontSize = FontSizeSm,
            Opacity = 0.7
        };

        return new Border
        {
            Padding = new Thickness(SpaceMd, SpaceSm),
            Child = statusText
        };
    }

    /// <summary>
    /// Creates a styled canvas container.
    /// </summary>
    public static Border CreateCanvasContainer(Control canvas)
    {
        return new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(RadiusLg),
            ClipToBounds = true,
            Child = canvas
        };
    }
}
