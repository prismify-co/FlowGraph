using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Rendering.PortRenderers;

/// <summary>
/// Default port renderer that creates circular port visuals.
/// This is the standard port appearance used by FlowGraph.
/// </summary>
public class DefaultPortRenderer : IPortRenderer
{
  /// <summary>
  /// Shared instance for convenience.
  /// </summary>
  public static DefaultPortRenderer Instance { get; } = new();

  /// <inheritdoc />
  public virtual Control CreatePortVisual(Port port, Node node, PortRenderContext context)
  {
    var size = GetSize(port, node, context.Settings) ?? context.Settings.PortSize;
    var scaledSize = size * context.Scale;

    var ellipse = new Ellipse
    {
      Width = scaledSize,
      Height = scaledSize,
      Fill = context.Theme.PortBackground,
      Stroke = context.Theme.PortBorder,
      StrokeThickness = 2,
      Cursor = new Cursor(StandardCursorType.Cross),
      Tag = (node, port, context.IsOutput)
    };

    return ellipse;
  }

  /// <inheritdoc />
  public virtual void UpdateState(Control visual, Port port, Node node, PortRenderContext context, PortVisualState state)
  {
    if (visual is not Ellipse ellipse) return;

    // Update visual appearance based on state
    if (state.IsHovered || state.IsValidDropTarget)
    {
      ellipse.Fill = context.Theme.PortHoverBackground ?? context.Theme.PortHover;
      ellipse.Stroke = context.Theme.PortHoverBorder ?? context.Theme.PortHover;
    }
    else if (state.IsDragging)
    {
      ellipse.Fill = context.Theme.PortHover;
      ellipse.Stroke = context.Theme.PortHover;
    }
    else
    {
      ellipse.Fill = context.Theme.PortBackground;
      ellipse.Stroke = context.Theme.PortBorder;
    }
  }

  /// <inheritdoc />
  public virtual double? GetSize(Port port, Node node, FlowCanvasSettings settings)
  {
    return null; // Use default from settings
  }
}
