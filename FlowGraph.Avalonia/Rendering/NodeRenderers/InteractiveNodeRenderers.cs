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
/// Renders a slider input node. Value persisted in Node.Data as double.
/// </summary>
public class SliderNodeRenderer : DataNodeRendererBase
{
    public double Minimum { get; set; } = 0;
    public double Maximum { get; set; } = 100;
    public double TickFrequency { get; set; } = 1;

    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.NodeWidthWide;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => DesignTokens.NodeHeightTall;

    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var baseVisual = base.CreateNodeVisual(node, context);
        if (baseVisual is not Border border) return baseVisual;

        var scale = context.Scale;
        var initialValue = GetSliderValue(node, processor, Minimum);

        var content = new StackPanel { Spacing = DesignTokens.SpacingBase, VerticalAlignment = VerticalAlignment.Center };

        content.Children.Add(new TextBlock
        {
            Text = node.Label ?? "Slider",
            FontWeight = FontWeight.SemiBold,
            FontSize = DesignTokens.FontSizeBase,
            Foreground = context.Theme.NodeText
        });

        var slider = new Slider
        {
            Minimum = Minimum,
            Maximum = Maximum,
            Value = initialValue,
            TickFrequency = TickFrequency,
            IsSnapToTickEnabled = TickFrequency > 0,
            Width = DesignTokens.SliderWidth,
            Tag = "Slider"
        };

        var valueText = new TextBlock
        {
            Text = initialValue.ToString("F1"),
            FontSize = DesignTokens.FontSizeSm,
            Foreground = context.Theme.NodeText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Tag = "ValueText"
        };

        slider.PointerPressed += (s, e) => e.Handled = true;
        slider.PropertyChanged += (s, e) =>
        {
            if (e.Property == RangeBase.ValueProperty)
            {
                valueText.Text = slider.Value.ToString("F1");
                node.Data = slider.Value;
                if (processor is InputNodeProcessor<double> dp) dp.Value = slider.Value;
            }
        };

        content.Children.Add(slider);
        content.Children.Add(valueText);

        border.Child = new Viewbox { Stretch = Stretch.Uniform, Child = content, Margin = new Thickness(DesignTokens.SpacingMd * scale) };
        border.ClipToBounds = true;
        return border;
    }

    private static double GetSliderValue(Node node, INodeProcessor? processor, double defaultValue)
    {
        if (node.Data is double d) return d;
        if (processor is InputNodeProcessor<double> p) return p.Value;
        return defaultValue;
    }

    public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
    {
        if (visual is not Border border) return;
        var slider = FindByTag<Slider>(border, "Slider");
        var valueText = FindByTag<TextBlock>(border, "ValueText");
        if (processor.OutputValues.TryGetValue("out", out var port) && port.Value is double value)
        {
            if (slider != null) slider.Value = value;
            if (valueText != null) valueText.Text = value.ToString("F1");
        }
    }
}

/// <summary>
/// Renders a numeric input node. Value persisted in Node.Data as double.
/// </summary>
public class NumberInputNodeRenderer : DataNodeRendererBase
{
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.InputWidthWide;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => 90;

    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var baseVisual = base.CreateNodeVisual(node, context);
        if (baseVisual is not Border border) return baseVisual;

        var scale = context.Scale;
        var initialValue = node.Data is double d ? d : (processor is InputNodeProcessor<double> p ? p.Value : 0);

        var content = new StackPanel { Spacing = DesignTokens.SpacingBase, VerticalAlignment = VerticalAlignment.Center };

        content.Children.Add(new TextBlock
        {
            Text = node.Label ?? "Number",
            FontWeight = FontWeight.SemiBold,
            FontSize = DesignTokens.FontSizeBase,
            Foreground = context.Theme.NodeText
        });

        var textBox = new TextBox
        {
            Text = initialValue.ToString(),
            Width = DesignTokens.InputWidthNarrow,
            FontSize = DesignTokens.FontSizeBase,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Tag = "NumberInput"
        };

        textBox.PointerPressed += (s, e) => e.Handled = true;
        textBox.LostFocus += (s, e) =>
        {
            if (double.TryParse(textBox.Text, out var value))
            {
                node.Data = value;
                if (processor is InputNodeProcessor<double> dp) dp.Value = value;
            }
            else textBox.Text = (node.Data is double dv ? dv : 0).ToString();
        };

        content.Children.Add(textBox);

        border.Child = new Viewbox { Stretch = Stretch.Uniform, Child = content, Margin = new Thickness(DesignTokens.SpacingMd * scale) };
        border.ClipToBounds = true;
        return border;
    }
}

/// <summary>
/// Renders a text input node. Value persisted in Node.Data as string.
/// </summary>
public class TextInputNodeRenderer : DataNodeRendererBase
{
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.NodeWidthWide;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => 90;

    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var baseVisual = base.CreateNodeVisual(node, context);
        if (baseVisual is not Border border) return baseVisual;

        var scale = context.Scale;
        var initialValue = node.Data as string ?? (processor is InputNodeProcessor<string> p ? p.Value ?? "" : "");

        var content = new StackPanel { Spacing = DesignTokens.SpacingBase, VerticalAlignment = VerticalAlignment.Center };

        content.Children.Add(new TextBlock
        {
            Text = node.Label ?? "Text",
            FontWeight = FontWeight.SemiBold,
            FontSize = DesignTokens.FontSizeBase,
            Foreground = context.Theme.NodeText
        });

        var textBox = new TextBox { Text = initialValue, Width = DesignTokens.InputWidthDefault, FontSize = DesignTokens.FontSizeBase, Tag = "TextInput" };

        textBox.PointerPressed += (s, e) => e.Handled = true;
        textBox.TextChanged += (s, e) =>
        {
            node.Data = textBox.Text ?? "";
            if (processor is InputNodeProcessor<string> sp) sp.Value = textBox.Text ?? "";
        };

        content.Children.Add(textBox);

        border.Child = new Viewbox { Stretch = Stretch.Uniform, Child = content, Margin = new Thickness(DesignTokens.SpacingMd * scale) };
        border.ClipToBounds = true;
        return border;
    }
}

/// <summary>
/// Renders a checkbox/toggle node. Value persisted in Node.Data as bool.
/// </summary>
public class ToggleNodeRenderer : DataNodeRendererBase
{
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.SliderWidth;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => DesignTokens.NodeHeightDefault;

    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var baseVisual = base.CreateNodeVisual(node, context);
        if (baseVisual is not Border border) return baseVisual;

        var scale = context.Scale;
        var initialValue = node.Data is bool b ? b : (processor is InputNodeProcessor<bool> p ? p.Value : false);

        var checkBox = new CheckBox
        {
            Content = node.Label ?? "Toggle",
            FontSize = DesignTokens.FontSizeBase,
            IsChecked = initialValue,
            Tag = "Toggle"
        };

        checkBox.PointerPressed += (s, e) => e.Handled = true;
        checkBox.IsCheckedChanged += (s, e) =>
        {
            var newValue = checkBox.IsChecked ?? false;
            node.Data = newValue;
            if (processor is InputNodeProcessor<bool> bp) bp.Value = newValue;
        };

        border.Child = new Viewbox
        {
            Stretch = Stretch.Uniform,
            Child = checkBox,
            Margin = new Thickness(DesignTokens.SpacingMd * scale),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        border.ClipToBounds = true;
        return border;
    }
}

/// <summary>
/// Renders a dropdown node. Value persisted in Node.Data as DropdownData.
/// </summary>
public class DropdownNodeRenderer : DataNodeRendererBase
{
    public IList<string> Options { get; set; } = new List<string>();

    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.InputWidthWide;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => DesignTokens.NodeHeightTall;

    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var baseVisual = base.CreateNodeVisual(node, context);
        if (baseVisual is not Border border) return baseVisual;

        var scale = context.Scale;
        var (options, selectedValue) = GetDropdownData(node, processor);

        var content = new StackPanel { Spacing = DesignTokens.SpacingBase, VerticalAlignment = VerticalAlignment.Center };

        content.Children.Add(new TextBlock
        {
            Text = node.Label ?? "Select",
            FontWeight = FontWeight.SemiBold,
            FontSize = DesignTokens.FontSizeBase,
            Foreground = context.Theme.NodeText
        });

        var comboBox = new ComboBox { ItemsSource = options, Width = DesignTokens.ComboBoxWidth, FontSize = DesignTokens.FontSizeSm, Tag = "Dropdown" };
        comboBox.PointerPressed += (s, e) => e.Handled = true;

        if (options.Count > 0)
        {
            comboBox.SelectedItem = !string.IsNullOrEmpty(selectedValue) && options.Contains(selectedValue)
                ? selectedValue : options[0];

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem is string selected)
                {
                    node.Data = new DropdownData { Options = options, SelectedValue = selected };
                    if (processor is InputNodeProcessor<string> sp) sp.Value = selected;
                }
            };
        }

        content.Children.Add(comboBox);

        border.Child = new Viewbox { Stretch = Stretch.Uniform, Child = content, Margin = new Thickness(DesignTokens.SpacingMd * scale) };
        border.ClipToBounds = true;
        return border;
    }

    private (IList<string> options, string selectedValue) GetDropdownData(Node node, INodeProcessor? processor)
    {
        if (node.Data is DropdownData dd) return (dd.Options, dd.SelectedValue);
        if (node.Data is IList<string> list) return (list, processor is InputNodeProcessor<string> sp ? sp.Value ?? "" : "");
        return (Options, processor is InputNodeProcessor<string> sp2 ? sp2.Value ?? "" : "");
    }
}

public class DropdownData
{
    public IList<string> Options { get; set; } = new List<string>();
    public string SelectedValue { get; set; } = "";
}

/// <summary>
/// Renders a display/output node.
/// </summary>
public class DisplayNodeRenderer : DataNodeRendererBase
{
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => 160;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => 90;

    public override Control CreateDataBoundVisual(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var baseVisual = base.CreateNodeVisual(node, context);
        if (baseVisual is not Border border) return baseVisual;

        var scale = context.Scale;

        var content = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

        content.Children.Add(new TextBlock
        {
            Text = node.Label ?? "Output",
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            Foreground = context.Theme.NodeText
        });

        var valueDisplay = new Border
        {
            Background = context.Theme.InteractiveValueBackground,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4),
            Child = new TextBlock
            {
                Text = "�",
                FontSize = 14,
                FontWeight = FontWeight.Medium,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = "ValueDisplay"
            },
            Tag = "ValueContainer"
        };

        content.Children.Add(valueDisplay);

        border.Child = new Viewbox { Stretch = Stretch.Uniform, Child = content, Margin = new Thickness(8 * scale) };
        border.ClipToBounds = true;

        if (processor != null) UpdateFromPortValues(border, processor);
        return border;
    }

    public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
    {
        if (visual is not Border border) return;
        var valueDisplay = FindByTag<TextBlock>(border, "ValueDisplay");
        if (valueDisplay == null) return;

        var firstInput = processor.InputValues.Values.FirstOrDefault();
        valueDisplay.Text = firstInput?.Value switch
        {
            double d => d.ToString("F2"),
            float f => f.ToString("F2"),
            bool b => b ? "True" : "False",
            null => "�",
            var v => v.ToString() ?? "�"
        };
    }
}

/// <summary>
/// Renders a radio button group node using the white headered base.
/// Value persisted in Node.Data as RadioButtonData.
/// </summary>
public class RadioButtonNodeRenderer : WhiteHeaderedNodeRendererBase
{
    // Static registry to store processor mappings (survives visual tree rebuilds)
    private static readonly Dictionary<string, INodeProcessor> ProcessorRegistry = new();

    public IList<string> Options { get; set; } = new List<string> { "cube", "pyramid" };

    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.InputWidthWide;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => DesignTokens.NodeHeightExpanded;

    protected override string GetDefaultLabel() => "shape type";

    protected override Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var (options, selectedValue) = GetRadioButtonData(node, processor);

        // Create container Border with Tag = node for processor lookup (like ZoomSlider)
        var container = new Border { Tag = node };
        var radioPanel = new StackPanel { Spacing = DesignTokens.SpacingBase };

        foreach (var option in options)
        {
            var radio = new RadioButton
            {
                Content = option,
                FontSize = DesignTokens.FontSizeSm,
                IsChecked = option == selectedValue,
                GroupName = node.Id,
                Foreground = context.Theme.InteractiveMutedText,
                Tag = option
            };

            radio.PointerPressed += (s, e) => e.Handled = true;
            radio.IsCheckedChanged += (s, e) =>
            {
                if (radio.IsChecked == true)
                {
                    node.Data = new RadioButtonData { Options = options, SelectedValue = option };
                    if (ProcessorRegistry.TryGetValue(node.Id, out var currentProcessor) &&
                        currentProcessor is InputNodeProcessor<string> sp)
                    {
                        sp.Value = option;
                    }
                }
            };

            radioPanel.Children.Add(radio);
        }

        container.Child = radioPanel;
        return container;
    }

    private (IList<string> options, string selectedValue) GetRadioButtonData(Node node, INodeProcessor? processor)
    {
        if (node.Data is RadioButtonData rd) return (rd.Options, rd.SelectedValue);
        if (node.Data is IList<string> list)
        {
            var sel = list.FirstOrDefault() ?? "";
            if (processor is InputNodeProcessor<string> sp && !string.IsNullOrEmpty(sp.Value)) sel = sp.Value;
            return (list, sel);
        }
        var defaultSel = Options.FirstOrDefault() ?? "";
        if (processor is InputNodeProcessor<string> sp2 && !string.IsNullOrEmpty(sp2.Value)) defaultSel = sp2.Value;
        return (Options, defaultSel);
    }

    public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
    {
        if (visual is not Border border) return;
        var value = processor.OutputValues.TryGetValue("out", out var port) && port.Value is string s ? s : "";

        // Find radio buttons via recursive search
        var radios = FindAll<RadioButton>(border);
        foreach (var radio in radios)
        {
            if (radio.Tag is string tag)
                radio.IsChecked = tag == value;
        }
    }

    /// <inheritdoc />
    public override void OnProcessorAttached(Control visual, INodeProcessor processor)
    {
        base.OnProcessorAttached(visual, processor);
        ProcessorRegistry[processor.Node.Id] = processor;

        // Sync persisted Node.Data value to processor (important after visual tree rebuild)
        var (_, selectedValue) = GetRadioButtonData(processor.Node, null);
        if (processor is InputNodeProcessor<string> sp)
            sp.Value = selectedValue;
    }
}

public class RadioButtonData
{
    public IList<string> Options { get; set; } = new List<string>();
    public string SelectedValue { get; set; } = "";
}

/// <summary>
/// Renders a zoom slider node using the white headered base.
/// Value persisted in Node.Data as double.
/// </summary>
public class ZoomSliderNodeRenderer : WhiteHeaderedNodeRendererBase
{
    // Static registry to store processor mappings (survives visual tree rebuilds)
    private static readonly Dictionary<string, INodeProcessor> ProcessorRegistry = new();

    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.InputWidthWide;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => 90;

    protected override string GetDefaultLabel() => "zoom level";

    protected override Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var initialValue = node.Data is double d ? d : (processor is InputNodeProcessor<double> p ? p.Value : 50.0);

        var container = new Border { Tag = node }; // Container for processor lookup

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = initialValue,
            Width = DesignTokens.ComboBoxWidth,
            Tag = "Slider"
        };

        slider.PointerPressed += (s, e) => e.Handled = true;
        slider.PropertyChanged += (s, e) =>
        {
            if (e.Property == RangeBase.ValueProperty)
            {
                node.Data = slider.Value;
                if (ProcessorRegistry.TryGetValue(node.Id, out var currentProcessor) &&
                    currentProcessor is InputNodeProcessor<double> dp)
                {
                    dp.Value = slider.Value;
                }
            }
        };

        container.Child = slider;
        return container;
    }

    public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
    {
        if (visual is not Border border) return;
        var slider = FindByTag<Slider>(border, "Slider");
        if (slider != null && processor.OutputValues.TryGetValue("out", out var port) && port.Value is double value)
            slider.Value = value;
    }

    /// <inheritdoc />
    public override void OnProcessorAttached(Control visual, INodeProcessor processor)
    {
        base.OnProcessorAttached(visual, processor);
        ProcessorRegistry[processor.Node.Id] = processor;

        // Sync persisted Node.Data value to processor (important after visual tree rebuild)
        var value = processor.Node.Data is double d ? d : 50.0;
        if (processor is InputNodeProcessor<double> dp)
            dp.Value = value;
    }
}

/// <summary>
/// Renders an output display node using the white headered base with large content area.
/// </summary>
public class OutputDisplayNodeRenderer : WhiteHeaderedNodeRendererBase
{
    public override double? GetWidth(Node node, FlowCanvasSettings settings) => DesignTokens.NodeWidthLarge;
    public override double? GetHeight(Node node, FlowCanvasSettings settings) => DesignTokens.NodeHeightLarge;

    protected override string GetDefaultLabel() => "output";

    protected override double ContentVerticalPadding => DesignTokens.SpacingLg;

    protected override Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context)
    {
        var baseWidth = node.Width ?? GetWidth(node, context.Settings) ?? context.Settings.NodeWidth;
        var baseHeight = node.Height ?? GetHeight(node, context.Settings) ?? context.Settings.NodeHeight;

        var contentArea = new Border
        {
            Width = baseWidth - 40,
            Height = baseHeight - 80,
            Background = context.Theme.InteractivePreviewBackground,
            CornerRadius = new CornerRadius(DesignTokens.RadiusLg),
            Tag = "ContentArea",
            Child = new TextBlock
            {
                Text = "Preview",
                FontSize = DesignTokens.FontSizeXl,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = context.Theme.InteractivePreviewText
            }
        };

        return contentArea;
    }

    public override void UpdateFromPortValues(Control visual, INodeProcessor processor) { }
}
