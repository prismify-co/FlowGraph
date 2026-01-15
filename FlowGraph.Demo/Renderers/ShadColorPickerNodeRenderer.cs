using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;

namespace FlowGraph.Demo.Renderers;

/// <summary>
/// A ShadUI-styled color picker node renderer that displays a color swatch 
/// which opens a Card-wrapped ColorSpectrum flyout when clicked.
/// Uses the base class's proper resize behavior (no Viewbox).
/// </summary>
public class ShadColorPickerNodeRenderer : WhiteHeaderedNodeRendererBase
{
  private static readonly Color DefaultColor = Color.FromRgb(255, 0, 113); // Hot pink (#ff0071)

  // Static registry to store processor mappings (survives visual tree rebuilds)
  private static readonly Dictionary<string, INodeProcessor> ProcessorRegistry = new();

  /// <inheritdoc />
  public override double? GetWidth(Node node, FlowCanvasSettings settings) => 160;

  /// <inheritdoc />
  public override double? GetHeight(Node node, FlowCanvasSettings settings) => 100;

  /// <inheritdoc />
  protected override string GetDefaultLabel() => "Color Picker";

  /// <inheritdoc />
  protected override double ContentVerticalPadding => 8;

  /// <inheritdoc />
  protected override Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context)
  {
    // Get initial color from Node.Data, processor, or default
    var initialColor = GetColorFromNode(node, processor);

    // Main layout
    var panel = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      Spacing = 8,
      HorizontalAlignment = HorizontalAlignment.Center,
      VerticalAlignment = VerticalAlignment.Center
    };

    // Color swatch button with shadow (ShadUI style)
    var colorSwatch = new Border
    {
      Width = 40,
      Height = 40,
      CornerRadius = new CornerRadius(8),
      Background = new SolidColorBrush(initialColor),
      BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
      BorderThickness = new Thickness(1),
      BoxShadow = new BoxShadows(new BoxShadow
      {
        OffsetX = 0,
        OffsetY = 2,
        Blur = 4,
        Color = Color.FromArgb(40, 0, 0, 0)
      }),
      Tag = "ColorSwatch",
      Cursor = new Cursor(StandardCursorType.Hand)
    };

    // Hex input with ShadUI styling
    var hexInput = new TextBox
    {
      Text = ColorToHex(initialColor),
      Width = 70,
      FontSize = 11,
      FontFamily = new FontFamily("Consolas, Monaco, monospace"),
      Padding = new Thickness(6, 4),
      Tag = "HexInput",
      VerticalAlignment = VerticalAlignment.Center
    };

    // Stop propagation on hex input
    hexInput.PointerPressed += (s, e) => e.Handled = true;

    // Color spectrum for saturation/value selection
    var colorSpectrum = new ColorSpectrum
    {
      Color = initialColor,
      MinWidth = 256,
      MinHeight = 200,
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Stretch
    };

    var initialHsv = initialColor.ToHsv();
    byte initialAlpha = initialColor.A;

    // Create styled slider with gradient background
    Border CreateGradientSlider(string label, double min, double max, double initialValue,
      IBrush gradientBrush, Action<double> onValueChanged, out Slider slider)
    {
      slider = new Slider
      {
        Orientation = Orientation.Horizontal,
        Minimum = min,
        Maximum = max,
        Value = initialValue,
        Height = 16,
        Margin = new Thickness(0),
        HorizontalAlignment = HorizontalAlignment.Stretch
      };

      var sliderInstance = slider;
      slider.PropertyChanged += (s, e) =>
      {
        if (e.Property == Slider.ValueProperty)
          onValueChanged(sliderInstance.Value);
      };

      // Gradient track background
      var gradientTrack = new Border
      {
        Background = gradientBrush,
        CornerRadius = new CornerRadius(4),
        Height = 12,
        Margin = new Thickness(0, 2, 0, 0),
        HorizontalAlignment = HorizontalAlignment.Stretch
      };

      // Overlay slider on gradient
      var sliderContainer = new Grid
      {
        Height = 16,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Children = { gradientTrack, slider }
      };

      var labelText = new TextBlock
      {
        Text = label,
        FontSize = 11,
        Foreground = new SolidColorBrush(Color.Parse("#888888")),
        Margin = new Thickness(0, 0, 0, 2)
      };

      var container = new Border
      {
        Margin = new Thickness(0, 8, 0, 0),
        Child = new StackPanel
        {
          Orientation = Orientation.Vertical,
          Children = { labelText, sliderContainer }
        }
      };

      return container;
    }

    // Rainbow gradient for hue
    var hueGradient = new LinearGradientBrush
    {
      StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
      EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
      GradientStops =
      {
        new GradientStop(Color.FromRgb(255, 0, 0), 0),      // Red
        new GradientStop(Color.FromRgb(255, 255, 0), 0.17), // Yellow
        new GradientStop(Color.FromRgb(0, 255, 0), 0.33),   // Green
        new GradientStop(Color.FromRgb(0, 255, 255), 0.5),  // Cyan
        new GradientStop(Color.FromRgb(0, 0, 255), 0.67),   // Blue
        new GradientStop(Color.FromRgb(255, 0, 255), 0.83), // Magenta
        new GradientStop(Color.FromRgb(255, 0, 0), 1)       // Red (wrap)
      }
    };

    // Hue slider
    var hueContainer = CreateGradientSlider("Hue", 0, 360, initialHsv.H, hueGradient,
      value =>
      {
        var currentHsv = colorSpectrum.HsvColor;
        colorSpectrum.HsvColor = new HsvColor(currentHsv.A, value, currentHsv.S, currentHsv.V);
      }, out var hueSlider);

    // Alpha gradient (checkerboard + transparency)
    var alphaGradient = new LinearGradientBrush
    {
      StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
      EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
      GradientStops =
      {
        new GradientStop(Color.FromArgb(0, 255, 255, 255), 0),
        new GradientStop(initialColor, 1)
      }
    };

    // Alpha slider
    var alphaContainer = CreateGradientSlider("Opacity", 0, 255, initialAlpha, alphaGradient,
      value =>
      {
        // Alpha changes don't affect spectrum, update color directly
      }, out var alphaSlider);

    // Preview swatch showing current color
    var previewSwatch = new Border
    {
      Width = 48,
      Height = 48,
      CornerRadius = new CornerRadius(6),
      Background = new SolidColorBrush(initialColor),
      BorderBrush = new SolidColorBrush(Color.Parse("#e0e0e0")),
      BorderThickness = new Thickness(1),
      Margin = new Thickness(0, 0, 12, 0)
    };

    // Hex input for flyout
    var flyoutHexInput = new TextBox
    {
      Text = ColorToHex(initialColor),
      Width = 90,
      FontSize = 12,
      FontFamily = new FontFamily("Consolas, Monaco, monospace"),
      Padding = new Thickness(8, 6),
      VerticalAlignment = VerticalAlignment.Center
    };

    // RGB display
    var rgbText = new TextBlock
    {
      Text = $"RGB({initialColor.R}, {initialColor.G}, {initialColor.B})",
      FontSize = 11,
      Foreground = new SolidColorBrush(Color.Parse("#666666")),
      VerticalAlignment = VerticalAlignment.Center,
      Margin = new Thickness(8, 0, 0, 0)
    };

    // Preview row
    var previewRow = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      Margin = new Thickness(0, 12, 0, 0),
      Children = { previewSwatch, flyoutHexInput, rgbText }
    };

    // Stack all components
    var pickerPanel = new StackPanel
    {
      Orientation = Orientation.Vertical,
      Width = 280,
      Children = { colorSpectrum, hueContainer, alphaContainer, previewRow }
    };

    // Wrap in a ShadUI Card
    var cardContent = new ShadUI.Card
    {
      Padding = new Thickness(16),
      HasShadow = true,
      HorizontalAlignment = HorizontalAlignment.Center,
      Content = pickerPanel
    };

    // Track color changes from spectrum
    colorSpectrum.PropertyChanged += (s, e) =>
    {
      if (e.Property == ColorSpectrum.ColorProperty)
      {
        var spectrumColor = colorSpectrum.Color;
        var alpha = (byte)alphaSlider.Value;
        var newColor = Color.FromArgb(alpha, spectrumColor.R, spectrumColor.G, spectrumColor.B);

        // Update all UI
        colorSwatch.Background = new SolidColorBrush(newColor);
        previewSwatch.Background = new SolidColorBrush(newColor);
        hexInput.Text = ColorToHex(newColor);
        flyoutHexInput.Text = ColorToHex(newColor);
        rgbText.Text = $"RGB({newColor.R}, {newColor.G}, {newColor.B})";

        // Update alpha gradient to show current color
        alphaGradient.GradientStops[1] = new GradientStop(newColor, 1);

        SaveColor(newColor);
      }
    };

    // Alpha slider change handler  
    alphaSlider.PropertyChanged += (s, e) =>
    {
      if (e.Property == Slider.ValueProperty)
      {
        var spectrumColor = colorSpectrum.Color;
        var alpha = (byte)alphaSlider.Value;
        var newColor = Color.FromArgb(alpha, spectrumColor.R, spectrumColor.G, spectrumColor.B);

        colorSwatch.Background = new SolidColorBrush(newColor);
        previewSwatch.Background = new SolidColorBrush(newColor);
        hexInput.Text = ColorToHex(newColor);
        flyoutHexInput.Text = ColorToHex(newColor);
        SaveColor(newColor);
      }
    };

    // Flyout hex input handler
    flyoutHexInput.LostFocus += (s, e) =>
    {
      var hex = flyoutHexInput.Text?.Trim() ?? "";
      if (!hex.StartsWith("#")) hex = "#" + hex;
      if (TryParseColor(hex, out var color))
      {
        colorSpectrum.Color = color;
        alphaSlider.Value = color.A;
        hueSlider.Value = color.ToHsv().H;
      }
    };

    // Create flyout
    var flyout = new Flyout
    {
      Content = cardContent,
      Placement = PlacementMode.RightEdgeAlignedTop
    };

    // Container with node tag
    var container = new Border { Tag = node };

    // Helper to save color
    void SaveColor(Color newColor)
    {
      node.Data = newColor.ToUInt32();
      if (ProcessorRegistry.TryGetValue(node.Id, out var currentProcessor))
      {
        if (currentProcessor is InputNodeProcessor<Color> cp)
          cp.Value = newColor;
        else if (currentProcessor is InputNodeProcessor<uint> up)
          up.Value = newColor.ToUInt32();
      }
    }

    // Open flyout on swatch click
    colorSwatch.PointerPressed += (s, e) =>
    {
      e.Handled = true;
      flyout.ShowAt(colorSwatch);
    };

    // Hex input handlers
    void UpdateFromHex()
    {
      var hex = hexInput.Text?.Trim() ?? "";
      if (!hex.StartsWith("#")) hex = "#" + hex;
      if (TryParseColor(hex, out var color))
      {
        colorSwatch.Background = new SolidColorBrush(color);
        colorSpectrum.Color = color;
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

    panel.Children.Add(colorSwatch);
    panel.Children.Add(hexInput);
    container.Child = panel;

    return container;
  }

  private static string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

  private static bool TryParseColor(string hex, out Color color)
  {
    try
    {
      color = Color.Parse(hex);
      return true;
    }
    catch
    {
      color = default;
      return false;
    }
  }

  private static Color GetColorFromNode(Node node, INodeProcessor? processor)
  {
    if (node.Data is uint uintData) return Color.FromUInt32(uintData);
    if (node.Data is Color colorData) return colorData;
    if (processor is InputNodeProcessor<Color> colorProcessor) return colorProcessor.Value;
    if (processor is InputNodeProcessor<uint> uintProcessor) return Color.FromUInt32(uintProcessor.Value);
    return DefaultColor;
  }

  /// <inheritdoc />
  public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
  {
    if (visual is not Border border) return;

    var colorSwatch = FindByTag<Border>(border, "ColorSwatch");
    var hexInput = FindByTag<TextBox>(border, "HexInput");

    Color? color = null;
    if (processor is InputNodeProcessor<Color> colorProcessor)
      color = colorProcessor.Value;
    else if (processor is InputNodeProcessor<uint> uintProcessor)
      color = Color.FromUInt32(uintProcessor.Value);

    if (color.HasValue)
    {
      if (colorSwatch != null)
        colorSwatch.Background = new SolidColorBrush(color.Value);
      if (hexInput != null)
        hexInput.Text = ColorToHex(color.Value);
    }
  }

  /// <inheritdoc />
  public override void OnProcessorAttached(Control visual, INodeProcessor processor)
  {
    // Use the base class helper to get the node (handles ResizableVisual tag structure)
    var node = GetNodeFromVisual(visual);
    if (node != null)
    {
      ProcessorRegistry[node.Id] = processor;
      UpdateFromPortValues(visual, processor);
    }
  }

  private static new T? FindByTag<T>(Control parent, string tag) where T : Control
  {
    if (parent is T typed && typed.Tag?.ToString() == tag) return typed;
    if (parent is Decorator decorator && decorator.Child != null)
      return FindByTag<T>(decorator.Child, tag);
    if (parent is Panel panel)
    {
      foreach (var child in panel.Children)
        if (child is Control control)
        {
          var found = FindByTag<T>(control, tag);
          if (found != null) return found;
        }
    }
    if (parent is ContentControl cc && cc.Content is Control content)
      return FindByTag<T>(content, tag);
    return null;
  }
}