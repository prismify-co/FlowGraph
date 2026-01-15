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
public class ColorPickerNodeRenderer : WhiteHeaderedNodeRendererBase
{
    private static readonly Color DefaultColor = Color.FromRgb(255, 0, 113); // Hot pink (#ff0071)

    // Static registry to store processor mappings (survives visual tree rebuilds)
    private static readonly Dictionary<string, INodeProcessor> ProcessorRegistry = new();

    /// <inheritdoc />
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => 160;

    /// <inheritdoc />
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => 100;

    /// <inheritdoc />
    protected override string GetDefaultLabel() => "shape color";

    /// <inheritdoc />
    protected override Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        // Color row
        var colorRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Get initial color from Node.Data, processor, or default
        var initialColor = GetColorFromNode(node, processor);

        // Color preview border (we'll manually show the flyout)
        var colorPreview = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(DesignTokens.RadiusBase),
            Background = new SolidColorBrush(initialColor),
            BorderBrush = context.Theme.HeaderedNodeBorder,
            BorderThickness = new Thickness(DesignTokens.BorderThin),
            Tag = "ColorPreview",
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        // Hex input
        var hexInput = new TextBox
        {
            Text = ColorToHex(initialColor),
            Width = 80,
            FontSize = DesignTokens.FontSizeSm,
            FontFamily = new FontFamily("Consolas, Monaco, monospace"),
            Padding = new Thickness(DesignTokens.SpacingSm, DesignTokens.SpacingXs),
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

        // Create a container Border with Tag = node for processor lookup (like ZoomSlider)
        var container = new Border { Tag = node };

        // Helper to save color to node and processor
        void SaveColor(Color newColor)
        {
            // Persist to Node.Data so it survives re-renders
            node.Data = newColor.ToUInt32();

            // Look up processor from static registry by node ID
            if (ProcessorRegistry.TryGetValue(node.Id, out var currentProcessor))
            {
                if (currentProcessor is InputNodeProcessor<Color> cp)
                    cp.Value = newColor;
                else if (currentProcessor is InputNodeProcessor<uint> up)
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

        container.Child = colorRow;
        return container;
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

    /// <inheritdoc />
    public override void OnProcessorAttached(Control visual, INodeProcessor processor)
    {
        base.OnProcessorAttached(visual, processor);
        ProcessorRegistry[processor.Node.Id] = processor;

        // Sync persisted Node.Data value to processor (important after visual tree rebuild)
        var color = GetColorFromNode(processor.Node, null);
        if (processor is InputNodeProcessor<Color> cp)
            cp.Value = color;
        else if (processor is InputNodeProcessor<uint> up)
            up.Value = color.ToUInt32();
    }
}

/// <summary>
/// Renders a color display output node.
/// Shows a large color swatch with the received color value.
/// </summary>
public class ColorDisplayNodeRenderer : WhiteHeaderedNodeRendererBase
{
    /// <inheritdoc />
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.NodeWidthNarrow;

    /// <inheritdoc />
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => DesignTokens.NodeHeightExpanded;

    /// <inheritdoc />
    protected override string GetDefaultLabel() => "Color";

    /// <inheritdoc />
    protected override Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var colorSwatch = new Border
        {
            Width = 60,
            Height = 60,
            CornerRadius = new CornerRadius(DesignTokens.RadiusLg),
            Background = Brushes.Gray,
            BorderBrush = context.Theme.HeaderedNodeBorder,
            BorderThickness = new Thickness(DesignTokens.BorderThin),
            Tag = "ColorSwatch",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        return colorSwatch;
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
