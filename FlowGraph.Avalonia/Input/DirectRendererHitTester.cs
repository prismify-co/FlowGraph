using Avalonia;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using FlowGraph.Core.Rendering;

// Aliases to resolve namespace ambiguities
using CoreResizeHandlePosition = FlowGraph.Core.Input.ResizeHandlePosition;
using CorePoint = FlowGraph.Core.Point;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Adapts <see cref="DirectCanvasRenderer"/> hit testing to the <see cref="IGraphHitTester"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This adapter bridges the gap between the existing DirectCanvasRenderer hit testing
/// (which returns separate results per element type) and the new unified IGraphHitTester
/// interface (which returns a single GraphHitTestResult).
/// </para>
/// <para>
/// <b>Coordinate Space:</b>
/// DirectCanvasRenderer expects screen/viewport coordinates and converts internally.
/// This adapter expects canvas coordinates and converts to screen coordinates for the renderer.
/// </para>
/// <para>
/// <b>Priority Order:</b>
/// <list type="number">
/// <item>Resize handles (highest - small targets on top)</item>
/// <item>Edge endpoint handles (for reconnection)</item>
/// <item>Ports (small targets)</item>
/// <item>Nodes</item>
/// <item>Edges</item>
/// <item>Shapes (lowest - background elements)</item>
/// </list>
/// </para>
/// </remarks>
public class DirectRendererHitTester : IGraphHitTester
{
  private readonly Func<DirectCanvasRenderer?> _getRenderer;
  private readonly ViewportState _viewport;

  /// <summary>
  /// Creates a new hit tester that wraps a DirectCanvasRenderer.
  /// </summary>
  /// <param name="getRenderer">Function to get the current renderer (may be null if not initialized).</param>
  /// <param name="viewport">The viewport state for coordinate conversion.</param>
  public DirectRendererHitTester(
      Func<DirectCanvasRenderer?> getRenderer,
      ViewportState viewport)
  {
    _getRenderer = getRenderer ?? throw new ArgumentNullException(nameof(getRenderer));
    _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
  }

  /// <inheritdoc/>
  public CoordinateSpace InputCoordinateSpace => CoordinateSpace.Canvas;

  /// <inheritdoc/>
  public GraphHitTestResult HitTest(CorePoint canvasPosition)
  {
    var renderer = _getRenderer();
    if (renderer == null)
    {
      return new GraphHitTestResult
      {
        TargetType = HitTargetType.Canvas,
        CanvasPosition = canvasPosition
      };
    }

    // Convert canvas to screen/viewport coordinates for DirectCanvasRenderer
    var avaloniaCanvasPos = new global::Avalonia.Point(canvasPosition.X, canvasPosition.Y);
    var screenPos = _viewport.CanvasToViewport(avaloniaCanvasPos);
    var screenX = screenPos.X;
    var screenY = screenPos.Y;

    // Check in priority order (same as PerformDirectRenderingHitTest)

    // 1. Edge endpoint handles (for reconnection)
    var endpointHit = renderer.HitTestEdgeEndpointHandle(screenX, screenY);
    if (endpointHit.HasValue)
    {
      return new GraphHitTestResult
      {
        TargetType = HitTargetType.Waypoint, // Using Waypoint for edge endpoints
        Target = new EdgeEndpointHitInfo(endpointHit.Value.edge, endpointHit.Value.isSource),
        CanvasPosition = canvasPosition
      };
    }

    // 2. Node resize handles (highest priority for precise targets)
    var resizeHit = renderer.HitTestResizeHandle(screenX, screenY);
    if (resizeHit.HasValue)
    {
      var coreHandlePosition = ConvertResizeHandlePosition(resizeHit.Value.position);
      return new GraphHitTestResult
      {
        TargetType = HitTargetType.ResizeHandle,
        Target = new ResizeHandleHitInfo(resizeHit.Value.node, coreHandlePosition),
        CanvasPosition = canvasPosition
      };
    }

    // 3. Shape resize handles (before shapes, so handles take priority)
    var shapeResizeHit = renderer.HitTestShapeResizeHandle(screenX, screenY);
    if (shapeResizeHit.HasValue)
    {
      var coreHandlePosition = ConvertResizeHandlePosition(shapeResizeHit.Value.position);
      return new GraphHitTestResult
      {
        TargetType = HitTargetType.ShapeResizeHandle,
        Target = new ShapeResizeHandleHitInfo(shapeResizeHit.Value.shape, coreHandlePosition),
        CanvasPosition = canvasPosition
      };
    }

    // 4. Ports
    var portHit = renderer.HitTestPort(screenX, screenY);
    if (portHit.HasValue)
    {
      return new GraphHitTestResult
      {
        TargetType = HitTargetType.Port,
        Target = new PortHitInfo(portHit.Value.node, portHit.Value.port, !portHit.Value.isOutput),
        CanvasPosition = canvasPosition
      };
    }

    // 4. Nodes
    var nodeHit = renderer.HitTestNode(screenX, screenY);
    if (nodeHit != null)
    {
      return new GraphHitTestResult
      {
        TargetType = nodeHit.IsGroup ? HitTargetType.Group : HitTargetType.Node,
        Target = nodeHit,
        CanvasPosition = canvasPosition
      };
    }

    // 5. Edges (behind nodes)
    var edgeHit = renderer.HitTestEdge(screenX, screenY);
    if (edgeHit != null)
    {
      return new GraphHitTestResult
      {
        TargetType = HitTargetType.Edge,
        Target = edgeHit,
        CanvasPosition = canvasPosition
      };
    }

    // 6. Shapes (lowest priority - background elements)
    var shapeHit = renderer.HitTestShape(screenX, screenY);
    if (shapeHit != null)
    {
      return new GraphHitTestResult
      {
        TargetType = HitTargetType.Shape,
        Target = shapeHit,
        CanvasPosition = canvasPosition
      };
    }

    // Nothing hit - empty canvas
    return new GraphHitTestResult
    {
      TargetType = HitTargetType.Canvas,
      CanvasPosition = canvasPosition
    };
  }

  /// <inheritdoc/>
  public GraphHitTestResult HitTest(
      CorePoint position,
      double nodeTolerance = 0,
      double edgeTolerance = 5,
      double portTolerance = 8)
  {
    // DirectCanvasRenderer doesn't support custom tolerances - use default
    return HitTest(position);
  }

  /// <inheritdoc/>
  public GraphHitTestResult HitTest(CorePoint position, params HitTargetType[] targetTypes)
  {
    var result = HitTest(position);

    // If no filter specified, return everything
    if (targetTypes == null || targetTypes.Length == 0)
    {
      return result;
    }

    // Check if the hit type matches any of the requested types
    // Using bitwise AND since HitTargetType is now a flags enum
    foreach (var type in targetTypes)
    {
      if ((result.TargetType & type) != 0)
      {
        return result;
      }
    }

    // Hit type doesn't match filter - return canvas hit
    return new GraphHitTestResult
    {
      TargetType = HitTargetType.Canvas,
      CanvasPosition = position
    };
  }

  /// <summary>
  /// Converts from Rendering.ResizeHandlePosition to Core.Input.ResizeHandlePosition.
  /// </summary>
  private static CoreResizeHandlePosition ConvertResizeHandlePosition(Rendering.ResizeHandlePosition renderPos)
  {
    return renderPos switch
    {
      Rendering.ResizeHandlePosition.TopLeft => CoreResizeHandlePosition.TopLeft,
      Rendering.ResizeHandlePosition.Top => CoreResizeHandlePosition.TopCenter,
      Rendering.ResizeHandlePosition.TopRight => CoreResizeHandlePosition.TopRight,
      Rendering.ResizeHandlePosition.Left => CoreResizeHandlePosition.MiddleLeft,
      Rendering.ResizeHandlePosition.Right => CoreResizeHandlePosition.MiddleRight,
      Rendering.ResizeHandlePosition.BottomLeft => CoreResizeHandlePosition.BottomLeft,
      Rendering.ResizeHandlePosition.Bottom => CoreResizeHandlePosition.BottomCenter,
      Rendering.ResizeHandlePosition.BottomRight => CoreResizeHandlePosition.BottomRight,
      _ => CoreResizeHandlePosition.BottomRight
    };
  }
}

/// <summary>
/// Hit info for an edge endpoint handle (source or target connection point).
/// </summary>
public record EdgeEndpointHitInfo(Edge Edge, bool IsSource);
