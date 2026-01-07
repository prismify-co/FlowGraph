using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;

namespace FlowGraph.Avalonia.Rendering.NodeRenderers;

/// <summary>
/// Renders a color picker input node.
/// Uses Avalonia's built-in ColorView for full color selection with hex input fallback.
/// The selected color is persisted in Node.Data as a uint (ARGB).
/// </summary>
public class ColorPickerNodeRenderer : DataNodeRendererBase
{
    private static readonly IBrush HeaderBackground = new SolidColorBrush(Color.FromRgb(255, 240, 245));
    private static readonly IBrush HeaderBorder = new SolidColorBrush(Color.FromRgb(255, 182, 193));
    private static readonly Color DefaultColor = Color.FromRgb(255, 0, 113); // Hot pink (#ff0071)

    /// <inheritdoc />
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => 160;

    /// <inheritdoc />
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => 100;

    /// <inheritdoc />
    public override Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        return CreateDataBoundVisual(node, null, context);
    }

    /// <inheritdoc />
    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var scale = context.Scale;
        var baseWidth = node.Width ?? GetWidth(node, context.Settings) ?? context.Settings.NodeWidth;
        var baseHeight = node.Height ?? GetHeight(node, context.Settings) ?? context.Settings.NodeHeight;

        // Main container - scaled dimensions
        var border = new Border
        {
            Width = baseWidth * scale,
            Height = baseHeight * scale,
            Background = HeaderBackground,
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : HeaderBorder,
            BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2),
            CornerRadius = new CornerRadius(12 * scale),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2 * scale,
                OffsetY = 2 * scale,
                Blur = 8 * scale,
                Color = Color.FromArgb(40, 0, 0, 0)
            }),
            Cursor = new Cursor(StandardCursorType.Hand),
            Tag = node,
            ClipToBounds = true
        };

        // Content at base size inside a Viewbox for uniform scaling
        var content = new StackPanel
        {
            Spacing = 8,
            Width = baseWidth - 24,  // Account for margins
            VerticalAlignment = VerticalAlignment.Center
        };

        // Title
        content.Children.Add(new TextBlock
        {
            Text = node.Label ?? "shape color",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 50, 80))
        });

        // Color row
        var colorRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        // Get initial color from Node.Data, processor, or default
        var initialColor = GetColorFromNode(node, processor);

        // Color preview border (we'll manually show the flyout)
        var colorPreview = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(initialColor),
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            Tag = "ColorPreview",
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        // Hex input
        var hexInput = new TextBox
        {
            Text = ColorToHex(initialColor),
            Width = 80,
            FontSize = 11,
            FontFamily = new FontFamily("Consolas, Monaco, monospace"),
            Padding = new Thickness(4, 2),
            Tag = "HexInput"
        };

        // Stop propagation on hex input to prevent node dragging
        hexInput.PointerPressed += (s, e) => e.Handled = true;

        // Create ColorView for the flyout
        var colorView = new ColorView
        {
            Color = initialColor,
            IsAlphaVisible = false,
            IsColorSpectrumVisible = true,
            IsColorComponentsVisible = false,
            IsColorPaletteVisible = false,
            IsHexInputVisible = true,
            Width = 250,
            Tag = "ColorView"
        };

        // Create flyout with ColorView
        var flyout = new Flyout
        {
            Content = colorView,
            Placement = PlacementMode.Bottom
        };

        // Helper to save color to node and processor
        void SaveColor(Color newColor)
        {
            // Persist to Node.Data so it survives re-renders
            node.Data = newColor.ToUInt32();

            // Also update processor if available
            if (processor is InputNodeProcessor<Color> cp)
            {
                cp.Value = newColor;
            }
            else if (processor is InputNodeProcessor<uint> up)
            {
                up.Value = newColor.ToUInt32();
            }
        }

        // Event: Update from ColorView
        colorView.PropertyChanged += (s, e) =>
        {
            if (e.Property == ColorView.ColorProperty && colorView.Color is Color newColor)
            {
                colorPreview.Background = new SolidColorBrush(newColor);
                hexInput.Text = ColorToHex(newColor);
                SaveColor(newColor);
            }
        };

        // Event: Open flyout on preview click - MUST stop propagation
        colorPreview.PointerPressed += (s, e) =>
        {
            e.Handled = true; // Prevent node dragging
            flyout.ShowAt(colorPreview);
        };

        // Event: Update from hex input
        void UpdateFromHex()
        {
            var hex = hexInput.Text?.Trim() ?? "";
            if (!hex.StartsWith("#")) hex = "#" + hex;

            if (TryParseColor(hex, out var color))
            {
                colorPreview.Background = new SolidColorBrush(color);
                colorView.Color = color;
                SaveColor(color);
            }
        }

        hexInput.LostFocus += (s, e) => UpdateFromHex();
        hexInput.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                UpdateFromHex();
                e.Handled = true;
            }
        };

        colorRow.Children.Add(colorPreview);
        colorRow.Children.Add(hexInput);
        content.Children.Add(colorRow);

        // Use Viewbox for uniform scaling
        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            Child = content,
            Margin = new Thickness(10 * scale)
        };

        border.Child = viewbox;
        return border;
    }

    /// <summary>
    /// Gets the color from the node's data, processor, or returns the default color.
    /// </summary>
    private static Color GetColorFromNode(Node node, INodeProcessor? processor)
    {
        // First try Node.Data (persisted color)
        if (node.Data is uint uintData)
        {
            return Color.FromUInt32(uintData);
        }
        if (node.Data is Color colorData)
        {
            return colorData;
        }

        // Then try processor
        if (processor is InputNodeProcessor<Color> colorProcessor)
        {
            return colorProcessor.Value;
        }
        if (processor is InputNodeProcessor<uint> uintProcessor)
        {
            return Color.FromUInt32(uintProcessor.Value);
        }

        // Default color
        return DefaultColor;
    }

    /// <inheritdoc />
    public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
    {
        if (visual is not Border border) return;

        var colorPreview = FindByTag<Border>(border, "ColorPreview");
        var hexInput = FindByTag<TextBox>(border, "HexInput");

        Color? color = null;

        if (processor.OutputValues.TryGetValue("color", out var port))
        {
            if (port.Value is Color c) color = c;
            else if (port.Value is uint u) color = Color.FromUInt32(u);
        }
        else if (processor.OutputValues.TryGetValue("out", out var outPort))
        {
            if (outPort.Value is Color c) color = c;
            else if (outPort.Value is uint u) color = Color.FromUInt32(u);
        }

        if (color.HasValue)
        {
            colorPreview?.SetValue(Border.BackgroundProperty, new SolidColorBrush(color.Value));
            if (hexInput != null)
            {
                hexInput.Text = ColorToHex(color.Value);
            }
        }
    }

    private static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}".ToLowerInvariant();
    }

    private static bool TryParseColor(string hex, out Color color)
    {
        try
        {
            if (Color.TryParse(hex, out color))
                return true;

            // Try without #
            if (!hex.StartsWith("#") && Color.TryParse("#" + hex, out color))
                return true;

            color = Colors.Black;
            return false;
        }
        catch
        {
            color = Colors.Black;
            return false;
        }
    }
}

/// <summary>
/// Renders a color display output node.
/// Shows a large color swatch with the received color value.
/// </summary>
public class ColorDisplayNodeRenderer : DataNodeRendererBase
{
    /// <inheritdoc />
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => 120;

    /// <inheritdoc />
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => 120;

    /// <inheritdoc />
    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var scale = context.Scale;
        var baseWidth = node.Width ?? GetWidth(node, context.Settings) ?? context.Settings.NodeWidth;
        var baseHeight = node.Height ?? GetHeight(node, context.Settings) ?? context.Settings.NodeHeight;

        var border = new Border
        {
            Width = baseWidth * scale,
            Height = baseHeight * scale,
            Background = context.Theme.NodeBackground,
            BorderBrush = node.IsSelected ? context.Theme.NodeSelectedBorder : context.Theme.NodeBorder,
            BorderThickness = node.IsSelected ? new Thickness(3) : new Thickness(2),
            CornerRadius = new CornerRadius(12 * scale),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 2 * scale,
                OffsetY = 2 * scale,
                Blur = 8 * scale,
                Color = Color.FromArgb(40, 0, 0, 0)
            }),
            Tag = node,
            ClipToBounds = true
        };

        var content = new StackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        content.Children.Add(new TextBlock
        {
            Text = node.Label ?? "Color",
            FontWeight = FontWeight.SemiBold,
            FontSize = 11,
            Foreground = context.Theme.NodeText,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var colorSwatch = new Border
        {
            Width = 60,
            Height = 60,
            CornerRadius = new CornerRadius(8),
            Background = Brushes.Gray,
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            Tag = "ColorSwatch"
        };

        content.Children.Add(colorSwatch);

        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            Child = content,
            Margin = new Thickness(8 * scale)
        };

        border.Child = viewbox;

        if (processor != null) UpdateFromPortValues(border, processor);
        return border;
    }

    /// <inheritdoc />
    public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
    {
        if (visual is not Border border) return;
        var colorSwatch = FindByTag<Border>(border, "ColorSwatch");
        if (colorSwatch == null) return;

        var firstInput = processor.InputValues.Values.FirstOrDefault();
        if (firstInput?.Value is Color color)
            colorSwatch.Background = new SolidColorBrush(color);
        else if (firstInput?.Value is uint uintColor)
            colorSwatch.Background = new SolidColorBrush(Color.FromUInt32(uintColor));
    }
}
