using Avalonia;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// DirectCanvasRenderer partial - Group rendering methods.
/// </summary>
public partial class DirectCanvasRenderer
{
  private void DrawGroup(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
  {
    var canvasBounds = _model.GetNodeBounds(group);
    var screenBounds = CanvasToScreen(canvasBounds, zoom, offsetX, offsetY);
    var cornerRadius = CanvasRenderModel.GroupBorderRadius * zoom;

    // Background fill
    var bgGeometry = CreateRoundedRectGeometry(screenBounds, cornerRadius);
    context.DrawGeometry(_theme!.GroupBackground, null, bgGeometry);

    // Header background (if enabled)
    if (_settings.ShowGroupHeaderBackground)
    {
      var headerHeight = CanvasRenderModel.GroupHeaderHeight * zoom;
      var headerBounds = new Rect(screenBounds.X, screenBounds.Y, screenBounds.Width, headerHeight);
      var headerGeometry = CreateRoundedRectGeometry(headerBounds, cornerRadius);
      context.DrawGeometry(_theme.GroupHeaderBackground, null, headerGeometry);
    }

    // Border
    var borderBrush = group.IsSelected ? _theme.NodeSelectedBorder : _theme.GroupBorder;
    var borderPen = new Pen(borderBrush, CanvasRenderModel.GroupDashedStrokeThickness * zoom);
    if (!group.IsSelected)
    {
      borderPen.DashStyle = new DashStyle(new double[] { 4, 2 }, 0);
    }

    var borderGeometry = CreateRoundedRectGeometry(screenBounds, cornerRadius);
    context.DrawGeometry(null, borderPen, borderGeometry);

    // Header
    DrawGroupHeader(context, group, zoom, offsetX, offsetY);

    // Ports
    DrawGroupPorts(context, group, canvasBounds, zoom, offsetX, offsetY);
  }

  private void DrawCollapsedGroup(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
  {
    // Same as DrawGroup but with collapsed height
    DrawGroup(context, group, zoom, offsetX, offsetY);
  }

  private void DrawGroupHeader(DrawingContext context, Node group, double zoom, double offsetX, double offsetY)
  {
    // Collapse button
    var buttonBounds = _model.GetGroupCollapseButtonBounds(group);
    var screenButtonBounds = CanvasToScreen(buttonBounds, zoom, offsetX, offsetY);

    var buttonBrush = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
    context.DrawRectangle(buttonBrush, null, screenButtonBounds, 3 * zoom, 3 * zoom);

    // Collapse indicator
    var indicatorText = group.IsCollapsed ? "+" : "-";
    var indicatorFontSize = 12 * zoom;
    var indicatorFormatted = new FormattedText(
        indicatorText,
        System.Globalization.CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
        indicatorFontSize,
        _theme!.GroupLabelText);

    var indicatorX = screenButtonBounds.X + (screenButtonBounds.Width - indicatorFormatted.Width) / 2;
    var indicatorY = screenButtonBounds.Y + (screenButtonBounds.Height - indicatorFormatted.Height) / 2;
    context.DrawText(indicatorFormatted, new AvaloniaPoint(indicatorX, indicatorY));

    // Label (skip if being edited)
    if (_editingNodeId != group.Id)
    {
      var label = group.Label ?? "Group";
      var fontSize = 11 * zoom;

      // Get color from theme, with fallback for non-SolidColorBrush
      var labelBrush = _theme.GroupLabelText;
      if (labelBrush is SolidColorBrush solidBrush)
      {
        labelBrush = new SolidColorBrush(solidBrush.Color, 0.9);
      }

      var formattedText = new FormattedText(
          label,
          System.Globalization.CultureInfo.CurrentCulture,
          FlowDirection.LeftToRight,
          new Typeface(_typeface.FontFamily, FontStyle.Normal, FontWeight.Medium, FontStretch.Normal),
          fontSize,
          labelBrush);

      var labelPos = _model.GetGroupLabelPosition(group);
      var screenLabelPos = CanvasToScreen(labelPos, zoom, offsetX, offsetY);

      // Adjust Y to center with button
      var textY = screenButtonBounds.Y + (screenButtonBounds.Height - formattedText.Height) / 2;

      context.DrawText(formattedText, new AvaloniaPoint(screenLabelPos.X, textY));
    }
  }

  private void DrawGroupPorts(DrawingContext context, Node group, Rect canvasBounds, double zoom, double offsetX, double offsetY)
  {
    var portSize = _settings.PortSize * zoom;

    // Input ports
    for (int i = 0; i < group.Inputs.Count; i++)
    {
      var port = group.Inputs[i];
      var canvasPos = _model.GetPortPositionByIndex(group, i, group.Inputs.Count, false);
      var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

      var isHovered = _hoveredPort.HasValue &&
                     _hoveredPort.Value.nodeId == group.Id &&
                     _hoveredPort.Value.portId == port.Id;

      var brush = isHovered ? _theme!.PortHover : _portBrush;
      context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
    }

    // Output ports
    for (int i = 0; i < group.Outputs.Count; i++)
    {
      var port = group.Outputs[i];
      var canvasPos = _model.GetPortPositionByIndex(group, i, group.Outputs.Count, true);
      var screenPos = CanvasToScreen(canvasPos, zoom, offsetX, offsetY);

      var isHovered = _hoveredPort.HasValue &&
                     _hoveredPort.Value.nodeId == group.Id &&
                     _hoveredPort.Value.portId == port.Id;

      var brush = isHovered ? _theme!.PortHover : _portBrush;
      context.DrawEllipse(brush, _portPen, screenPos, portSize / 2, portSize / 2);
    }
  }
}
