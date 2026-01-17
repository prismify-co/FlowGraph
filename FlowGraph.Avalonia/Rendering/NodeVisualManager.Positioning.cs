using Avalonia.Controls;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Position update and coordinate calculation methods.
/// </summary>
public partial class NodeVisualManager
{
    /// <summary>
    /// Updates the position of a node visual on the canvas.
    /// </summary>
    /// <param name="node">The node whose position changed.</param>
    public void UpdateNodePosition(Node node)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            // Use canvas coordinates directly (transform handles zoom/pan)
            Canvas.SetLeft(control, node.Position.X);
            Canvas.SetTop(control, node.Position.Y);
        }

        UpdatePortPositions(node);
    }

    /// <summary>
    /// Updates all existing node visuals to their current screen positions.
    /// This is an optimized path for viewport changes (pan/zoom) that avoids
    /// recreating the visual tree. Only updates positions, not visual properties.
    /// NOTE: With transform-based rendering, this method is no longer needed for viewport changes.
    /// It's kept for compatibility but should only be called when nodes actually move in canvas space.
    /// </summary>
    /// <param name="graph">The graph containing the nodes.</param>
    public void UpdateAllNodePositions(Graph graph)
    {
        foreach (var node in graph.Elements.Nodes)
        {
            if (_nodeVisuals.TryGetValue(node.Id, out var control))
            {
                // Use canvas coordinates directly
                Canvas.SetLeft(control, node.Position.X);
                Canvas.SetTop(control, node.Position.Y);
            }
        }

        // Update all port positions
        foreach (var node in graph.Elements.Nodes)
        {
            UpdatePortPositions(node);
        }
    }

    /// <summary>
    /// Updates the positions of all ports for a node using GraphRenderModel.
    /// </summary>
    /// <param name="node">The node whose ports need updating.</param>
    public void UpdatePortPositions(Node node)
    {
        var scale = _renderContext.Scale;

        for (int i = 0; i < node.Inputs.Count; i++)
        {
            var port = node.Inputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var renderer = _portRendererRegistry.GetRenderer(port);
                var portSize = renderer.GetSize(port, node, _renderContext.Settings) ?? _renderContext.Settings.PortSize;
                var scaledPortSize = portSize * scale;

                var canvasPos = _model.GetPortPositionByIndex(node, i, node.Inputs.Count, false);
                // Use canvas coordinates directly
                Canvas.SetLeft(portVisual, canvasPos.X - scaledPortSize / 2);
                Canvas.SetTop(portVisual, canvasPos.Y - scaledPortSize / 2);
            }
        }

        for (int i = 0; i < node.Outputs.Count; i++)
        {
            var port = node.Outputs[i];
            if (_portVisuals.TryGetValue((node.Id, port.Id), out var portVisual))
            {
                var renderer = _portRendererRegistry.GetRenderer(port);
                var portSize = renderer.GetSize(port, node, _renderContext.Settings) ?? _renderContext.Settings.PortSize;
                var scaledPortSize = portSize * scale;

                var canvasPos = _model.GetPortPositionByIndex(node, i, node.Outputs.Count, true);
                // Use canvas coordinates directly
                Canvas.SetLeft(portVisual, canvasPos.X - scaledPortSize / 2);
                Canvas.SetTop(portVisual, canvasPos.Y - scaledPortSize / 2);
            }
        }
    }

    /// <summary>
    /// Updates the selection visual state of a node.
    /// </summary>
    /// <param name="node">The node to update.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void UpdateNodeSelection(Node node, ThemeResources theme)
    {
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            var scale = _renderContext.Scale;
            var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
            var context = new NodeRenderContext
            {
                Theme = theme,
                Settings = _renderContext.Settings,
                Scale = scale
            };

            renderer.UpdateSelection(control, node, context);
        }
    }

    /// <summary>
    /// Updates the size of a node visual.
    /// </summary>
    /// <param name="node">The node to update.</param>
    /// <param name="theme">Theme resources for styling.</param>
    public void UpdateNodeSize(Node node, ThemeResources theme)
    {
        System.Diagnostics.Debug.WriteLine($"[NodeVisualManager.UpdateNodeSize] Node={node.Id}, HasVisual={_nodeVisuals.ContainsKey(node.Id)}");
        if (_nodeVisuals.TryGetValue(node.Id, out var control))
        {
            var scale = _renderContext.Scale;
            var renderer = _nodeRendererRegistry.GetRenderer(node.Type);
            var context = new NodeRenderContext
            {
                Theme = theme,
                Settings = _renderContext.Settings,
                Scale = scale
            };

            var bounds = _model.GetNodeBounds(node);
            System.Diagnostics.Debug.WriteLine($"[NodeVisualManager.UpdateNodeSize] Bounds={bounds.Width}x{bounds.Height}, Renderer={renderer.GetType().Name}");
            renderer.UpdateSize(control, node, context, bounds.Width, bounds.Height);
            System.Diagnostics.Debug.WriteLine($"[NodeVisualManager.UpdateNodeSize] After UpdateSize: control.Width={control.Width}, control.Height={control.Height}");

#if DEBUG
            // Validate that composite renderers properly handle resize
            ValidateResizeImplementation(renderer, control, node);
#endif
        }
    }

#if DEBUG
    /// <summary>
    /// Debug-only validation to detect renderers that may have incomplete UpdateSize implementations.
    /// </summary>
    private static void ValidateResizeImplementation(INodeRenderer renderer, Control control, Node node)
    {
        // If renderer declares it has composite visuals but doesn't use ResizableVisual,
        // emit a debug warning (once per renderer type)
        if (renderer.HasCompositeVisual &&
            renderer is not IResizableNodeRenderer &&
            !ResizableVisual.HasResizeMetadata(control))
        {
            var rendererType = renderer.GetType().Name;
            System.Diagnostics.Debug.WriteLine(
                $"[WARNING] {rendererType} has HasCompositeVisual=true but doesn't use ResizableVisual. " +
                $"Child elements may not update correctly during resize. Node: {node.Id}");
        }
    }
#endif

    /// <summary>
    /// Gets the viewport position of a port using GraphRenderModel.
    /// </summary>
    /// <remarks>
    /// <b>WARNING:</b> This returns viewport-relative coordinates, NOT screen/control coordinates.
    /// For distance calculations with pointer events, prefer using <see cref="GetPortCanvasPosition"/>
    /// and comparing in canvas space, since pointer events give canvas coords via <c>e.GetPosition(MainCanvas)</c>.
    /// </remarks>
    /// <param name="node">The parent node.</param>
    /// <param name="port">The port.</param>
    /// <param name="isOutput">True if this is an output port.</param>
    /// <returns>The port position in viewport coordinates.</returns>
    public AvaloniaPoint GetPortViewportPosition(Node node, Port port, bool isOutput)
    {
        var canvasPos = _model.GetPortPosition(node, port, isOutput);
        return _renderContext.CanvasToViewport(canvasPos.X, canvasPos.Y);
    }

    /// <summary>
    /// Gets the screen position of a port using GraphRenderModel.
    /// </summary>
    [Obsolete("Use GetPortViewportPosition or GetPortCanvasPosition instead. 'Screen' terminology was ambiguous.")]
    public AvaloniaPoint GetPortScreenPosition(Node node, Port port, bool isOutput)
        => GetPortViewportPosition(node, port, isOutput);

    /// <summary>
    /// Gets the canvas position of a port using GraphRenderModel.
    /// Use this for drawing visual elements on MainCanvas (which uses MatrixTransform).
    /// </summary>
    /// <param name="node">The parent node.</param>
    /// <param name="port">The port.</param>
    /// <param name="isOutput">True if this is an output port.</param>
    /// <returns>The port position in canvas coordinates.</returns>
    public AvaloniaPoint GetPortCanvasPosition(Node node, Port port, bool isOutput)
    {
        return _model.GetPortPosition(node, port, isOutput);
    }

    /// <summary>
    /// Gets the dimensions for a node using GraphRenderModel.
    /// </summary>
    /// <param name="node">The node to get dimensions for.</param>
    /// <returns>The width and height of the node.</returns>
    public (double width, double height) GetNodeDimensions(Node node)
    {
        var bounds = _model.GetNodeBounds(node);
        return (bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Calculates the Y position for a port in canvas coordinates using GraphRenderModel.
    /// </summary>
    /// <param name="nodeY">Y position of the node.</param>
    /// <param name="portIndex">Index of the port.</param>
    /// <param name="totalPorts">Total number of ports.</param>
    /// <param name="nodeHeight">Optional node height override.</param>
    /// <returns>The Y position for the port.</returns>
    public double GetPortYCanvas(double nodeY, int portIndex, int totalPorts, double? nodeHeight = null)
    {
        var height = nodeHeight ?? _renderContext.Settings.NodeHeight;

        if (totalPorts == 1)
        {
            return nodeY + height / 2;
        }

        var spacing = height / (totalPorts + 1);
        return nodeY + spacing * (portIndex + 1);
    }
}
