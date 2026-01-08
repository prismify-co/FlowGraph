using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;
using FlowGraph.ThreeD.Abstractions;
using FlowGraph.ThreeD.Avalonia;
using FlowGraph.ThreeD.Integration;

namespace FlowGraph.Demo.Renderers;

/// <summary>
/// Renders an output display node with 3D graphics that respond to input port values.
/// This demonstrates the FlowGraph.3D integration with the data flow system.
/// </summary>
public class Output3DNodeRenderer : WhiteHeaderedNodeRendererBase
{
  // Static registry to store Scene3DControl references by node ID.
  // This survives visual tree rebuilds during pan/zoom operations.
  private static readonly Dictionary<string, Scene3DControl> Scene3DRegistry = new();

  public override double? GetWidth(Node node, FlowCanvasSettings settings) => 220;
  public override double? GetHeight(Node node, FlowCanvasSettings settings) => 200;

  protected override string GetDefaultLabel() => "3D Output";

  protected override double ContentVerticalPadding => 10;

  protected override Control CreateContent(Node node, INodeProcessor? processor, NodeRenderContext context)
  {
    var baseWidth = node.Width ?? GetWidth(node, context.Settings) ?? context.Settings.NodeWidth;
    var baseHeight = node.Height ?? GetHeight(node, context.Settings) ?? context.Settings.NodeHeight;

    var scene3D = new Scene3DControl
    {
      Width = baseWidth - 40,
      Height = baseHeight - 80,
      Parameters = SceneParameters.Default
    };

    // Register in static registry for later retrieval in UpdateFromPortValues
    Scene3DRegistry[node.Id] = scene3D;

    var container = new Border
    {
      Width = baseWidth - 40,
      Height = baseHeight - 80,
      Background = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
      CornerRadius = new CornerRadius(8),
      ClipToBounds = true,
      Child = scene3D
    };

    return container;
  }

  public override void UpdateFromPortValues(Control visual, INodeProcessor processor)
  {
    var nodeId = processor.Node?.Id;
    if (string.IsNullOrEmpty(nodeId)) return;
    if (!Scene3DRegistry.TryGetValue(nodeId, out var scene3D)) return;

    var builder = new SceneParametersBuilder();

    // Get color from input port
    if (processor.InputValues.TryGetValue("color", out var colorPort) &&
        colorPort.Value is global::Avalonia.Media.Color avaloniaColor)
    {
      builder.WithColor(avaloniaColor);
    }

    // Get shape type from input port
    if (processor.InputValues.TryGetValue("shape", out var shapePort) &&
        shapePort.Value is string shapeName)
    {
      builder.WithShapeType(shapeName);
    }

    // Get zoom from input port - convert from 0-100 slider range to zoom factor
    if (processor.InputValues.TryGetValue("zoom", out var zoomPort))
    {
      var zoomValue = zoomPort.Value switch
      {
        double d => d,
        float f => (double)f,
        int i => (double)i,
        _ => 50.0
      };
      // Convert 0-100 range to 0.5-2.0 zoom range
      var zoomFactor = 0.5f + (float)(zoomValue / 100.0 * 1.5);
      builder.WithZoom(zoomFactor);
    }

    scene3D.Parameters = builder.Build();
  }
}
