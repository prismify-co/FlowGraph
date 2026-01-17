using Avalonia;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// DirectCanvasRenderer partial - Node rendering methods.
/// </summary>
public partial class DirectCanvasRenderer
{
  private void DrawNode(DrawingContext context, Node node, double zoom, double offsetX, double offsetY, bool showLabels, bool showPorts, bool useSimplified)
  {
    var canvasBounds = _model.GetNodeBounds(node);
    var screenBounds = CanvasToScreen(canvasBounds, zoom, offsetX, offsetY);

    // Check if custom renderer exists and implements IDirectNodeRenderer
    if (!useSimplified && _nodeRenderers != null)
    {
      var renderer = _nodeRenderers.GetRenderer(node.Type);
      if (renderer is IDirectNodeRenderer directRenderer)
      {
        var background = GetNodeBackground(node);
        var borderPen = node.IsSelected ? _nodeSelectedPen : _nodeBorderPen;

        var renderContext = new DirectNodeRenderContext
        {
          ScreenBounds = screenBounds,
          Zoom = zoom,
          IsSelected = node.IsSelected,
          IsEditing = _editingNodeId == node.Id,
          Background = background,
          BorderPen = borderPen,
          TextBrush = _theme!.NodeText,
          Theme = _theme,
          Settings = _settings,
          Model = _model
        };

        directRenderer.DrawNode(context, node, renderContext);
        if (showPorts)
          DrawNodePorts(context, node, canvasBounds, zoom, offsetX, offsetY);
        return;
      }
    }

    // Simplified or default drawing
    var cornerRadius = useSimplified ? 0 : CanvasRenderModel.NodeCornerRadius * zoom;
    var defaultBackground = GetNodeBackground(node);

    // Draw rounded rectangle (or sharp rect if simplified)
    if (useSimplified)
    {
      // Simple rectangle - faster than rounded
      context.DrawRectangle(defaultBackground, node.IsSelected ? _nodeSelectedPen : _nodeBorderPen, screenBounds);
    }
    else
    {
      var geometry = CreateRoundedRectGeometry(screenBounds, cornerRadius);
      context.DrawGeometry(defaultBackground, node.IsSelected ? _nodeSelectedPen : _nodeBorderPen, geometry);
    }

    // Draw label (skip if being edited or LOD disabled)
    if (showLabels && _editingNodeId != node.Id)
    {
      var label = node.Label ?? node.Type ?? node.Id;
      if (!string.IsNullOrEmpty(label))
      {
        DrawCenteredText(context, label, screenBounds, 10 * zoom, _theme!.NodeText);
      }
    }

    // Draw ports (if LOD allows)
    if (showPorts)
      DrawNodePorts(context, node, canvasBounds, zoom, offsetX, offsetY);
  }

  private IBrush? GetNodeBackground(Node node)
  {
    return node.Type?.ToLowerInvariant() switch
    {
      "input" => _nodeInputBackground,
      "output" => _nodeOutputBackground,
      _ => _nodeBackground
    };
  }

  private void DrawNodePorts(DrawingContext context, Node node, Rect canvasBounds, double zoom, double offsetX, double offsetY)
  {
    var portSize = _settings.PortSize * zoom;

    // Input ports
    for (int i = 0; i < node.Inputs.Count; i++)
    {
      var port = node.Inputs[i];
      var canvasPos = _model.GetPortPositionByIndex(node, i, node.Inputs.Count, false);
      var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

      var isHovered = _hoveredPort.HasValue &&
                     _hoveredPort.Value.nodeId == node.Id &&
                     _hoveredPort.Value.portId == port.Id;

      var brush = isHovered ? _theme!.PortHover : _portBrush;
      context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
    }

    // Output ports
    for (int i = 0; i < node.Outputs.Count; i++)
    {
      var port = node.Outputs[i];
      var canvasPos = _model.GetPortPositionByIndex(node, i, node.Outputs.Count, true);
      var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

      var isHovered = _hoveredPort.HasValue &&
                     _hoveredPort.Value.nodeId == node.Id &&
                     _hoveredPort.Value.portId == port.Id;

      var brush = isHovered ? _theme!.PortHover : _portBrush;
      context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
    }
  }

  private void DrawResizeHandles(DrawingContext context, Node node, double zoom, double offsetX, double offsetY)
  {
    var handleSize = CanvasRenderModel.ResizeHandleSize * zoom;

    foreach (var (_, center) in _model.GetResizeHandlePositions(node))
    {
      var screenCenter = CanvasToScreen(center, zoom, offsetX, offsetY);
      var rect = new Rect(
          screenCenter.X - handleSize / 2,
          screenCenter.Y - handleSize / 2,
          handleSize,
          handleSize);
      context.DrawRectangle(_resizeHandleFill, _resizeHandlePen, rect);
    }
  }
}
