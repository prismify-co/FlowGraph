using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;
using System.Diagnostics;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Input event handling.
/// </summary>
public partial class FlowCanvas
{
    #region Input Event Handlers

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_inputStateMachine.HandleKeyDown(e))
        {
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _inputStateMachine.HandlePointerWheel(e);
    }

    private void OnRootPanelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var sw = Stopwatch.StartNew();

        var point = e.GetCurrentPoint(_rootPanel);
        var screenPos = e.GetPosition(_rootPanel);

        Debug.WriteLine($"[Input] PointerPressed at ({screenPos.X:F0}, {screenPos.Y:F0}), DirectRendering={_useDirectRendering}, RightButton={point.Properties.IsRightButtonPressed}");

        // Handle right-click for context menu
        if (point.Properties.IsRightButtonPressed)
        {
            Debug.WriteLine($"[Input] Right-click detected, performing hit test...");

            // In direct rendering mode, do hit testing first to find what was clicked
            if (_useDirectRendering && _directRenderer != null)
            {
                // DirectRenderer expects screen coordinates (relative to _rootPanel)
                // It calls ScreenToCanvas internally
                var rightClickHit = PerformDirectRenderingHitTest(screenPos.X, screenPos.Y);
                Debug.WriteLine($"[Input] Right-click hit test result: {rightClickHit?.Tag?.GetType().Name ?? "null"}");

                var hitNode = Rendering.NodeRenderers.ResizableVisual.GetNodeFromTag(rightClickHit?.Tag);
                if (hitNode != null)
                {
                    HandleContextMenuRequest(e, rightClickHit!, hitNode);
                }
                else if (rightClickHit?.Tag is Edge edge)
                {
                    HandleContextMenuRequest(e, rightClickHit, edge);
                }
                else
                {
                    // Empty canvas
                    HandleContextMenuRequest(e, null, null);
                }
            }
            else
            {
                // Normal rendering - use visual tree hit testing
                HandleContextMenuRequest(e, null, null);
            }
            return;
        }

        // Update context with current graph
        _inputContext.Graph = Graph;

        // Determine the source control for state handling
        Control? hitElement = null;

        // In direct rendering mode, use coordinate-based hit testing
        if (_useDirectRendering && _directRenderer != null)
        {
            // DirectRenderer expects screen coordinates (relative to _rootPanel)
            // It calls ScreenToCanvas internally
            var hitSw = Stopwatch.StartNew();
            hitElement = PerformDirectRenderingHitTest(screenPos.X, screenPos.Y);
            hitSw.Stop();
            Debug.WriteLine($"[Input] DirectHitTest took {hitSw.ElapsedMilliseconds}ms, hit={hitElement?.Tag?.GetType().Name ?? "null"}");
        }
        else
        {
            // Get position relative to the canvas (not rootPanel) for hit testing
            var canvasPos = _rootPanel != null && _mainCanvas != null
                ? e.GetPosition(_mainCanvas)
                : screenPos;

            var rawHit = _mainCanvas?.InputHitTest(canvasPos);
            hitElement = rawHit as Control;

            // Debug: log what we actually hit
            if (rawHit == null)
            {
                Debug.WriteLine($"[Input] VisualTreeHitTest returned null - nothing at ({screenPos.X:F0}, {screenPos.Y:F0})");

                // Debug: Check node bounds
                if (Graph != null && _graphRenderer != null)
                {
                    Debug.WriteLine($"[Input]   Canvas has {_mainCanvas?.Children.Count} children, checking node visuals:");
                    foreach (var node in Graph.Elements.Nodes.Take(5))
                    {
                        var visual = _graphRenderer.GetNodeVisual(node.Id);
                        if (visual != null)
                        {
                            var left = Canvas.GetLeft(visual);
                            var top = Canvas.GetTop(visual);
                            Debug.WriteLine($"[Input]   Node {node.Id}: Visual at ({left:F0},{top:F0}), Size=({visual.Bounds.Width:F0}x{visual.Bounds.Height:F0}), IsHitTestVisible={visual.IsHitTestVisible}");
                        }
                        else
                        {
                            Debug.WriteLine($"[Input]   Node {node.Id}: NO VISUAL");
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine($"[Input] VisualTreeHitTest hit: {rawHit.GetType().Name}, Tag={GetTagDescription(rawHit)}, IsHitTestVisible={(rawHit as Control)?.IsHitTestVisible}");

                // Walk up tree to find a valid target (node, edge, port, resize handle, etc.)
                var current = rawHit as Control;
                int depth = 0;
                bool foundValidTarget = false;

                while (current != null && depth < 20)
                {
                    var tag = current.Tag;

                    // Check if this is a valid target
                    if (Rendering.NodeRenderers.ResizableVisual.GetNodeFromTag(tag) != null || // Node
                        tag is Edge || // Edge
                        tag is (Node, Port, bool) || // Port
                        tag is (Node, ResizeHandlePosition)) // Resize handle
                    {
                        hitElement = current;
                        foundValidTarget = true;
                        Debug.WriteLine($"[Input]   Found valid target at depth {depth}: {current.GetType().Name}, Tag={GetTagDescription(current)}");
                        break;
                    }

                    Debug.WriteLine($"[Input]   Parent[{depth}]: {current.GetType().Name}, Tag={GetTagDescription(current)}");
                    current = current.Parent as Control;
                    depth++;
                }

                // If we didn't find a valid target, treat as canvas click (set to null)
                if (!foundValidTarget)
                {
                    Debug.WriteLine($"[Input]   No valid target found in tree, treating as canvas click");
                    hitElement = null;
                }
            }
        }

        var stateSw = Stopwatch.StartNew();
        _inputStateMachine.HandlePointerPressed(e, hitElement);
        stateSw.Stop();

        sw.Stop();
        Debug.WriteLine($"[Input] StateMachine took {stateSw.ElapsedMilliseconds}ms, Total={sw.ElapsedMilliseconds}ms");

        Focus();
    }

    private static int _pointerMoveCount = 0;
    private static long _totalPointerMoveMs = 0;
    private long _lastHoverHitTestTicks = 0;
    private const long HoverThrottleMs = 50; // Throttle hover hit testing to 20 FPS (50ms intervals)

    private void OnRootPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _inputContext.Graph = Graph;

        // OPTIMIZATION: Skip expensive hover hit testing during states that don't need it
        // (Panning, Dragging, BoxSelecting, Connecting, Reconnecting, Resizing, Animating)
        var currentState = _inputStateMachine.CurrentStateName;
        var needsHoverDetection = currentState == "Idle";

        // OPTIMIZATION: Throttle hover hit testing to reduce CPU load on large graphs
        // Hover feedback at 20 FPS is plenty responsive for cursor changes
        var currentTicks = Environment.TickCount64;
        var timeSinceLastHoverTest = currentTicks - _lastHoverHitTestTicks;
        var shouldDoHoverTest = needsHoverDetection && timeSinceLastHoverTest >= HoverThrottleMs;

        // Track hover states and cursor in direct rendering mode (only when idle and throttled)
        if (shouldDoHoverTest && _useDirectRendering && _directRenderer != null && _rootPanel != null)
        {
            // CRITICAL: Use screenPos (relative to _rootPanel), NOT canvasPos!
            // DirectRenderer methods expect screen coordinates and call ScreenToCanvas internally.
            // Using canvasPos would cause double-transformation at non-1.0 zoom levels.
            var screenPos = e.GetPosition(_rootPanel);

            // Check resize handles first (highest priority for cursor)
            var resizeHit = _directRenderer.HitTestResizeHandle(screenPos.X, screenPos.Y);
            if (resizeHit.HasValue)
            {
                _rootPanel.Cursor = GetResizeCursor(resizeHit.Value.position);
                _directRenderer.ClearHoveredPort();
                _directRenderer.ClearHoveredEndpointHandle();
            }
            else
            {
                // Check port hover
                var portHit = _directRenderer.HitTestPort(screenPos.X, screenPos.Y);
                if (portHit.HasValue)
                {
                    _rootPanel.Cursor = new Cursor(StandardCursorType.Hand);
                    _directRenderer.SetHoveredPort(portHit.Value.node.Id, portHit.Value.port.Id);
                    _directRenderer.ClearHoveredEndpointHandle();
                }
                else
                {
                    _directRenderer.ClearHoveredPort();

                    // Check edge endpoint handle hover
                    var endpointHit = _directRenderer.HitTestEdgeEndpointHandle(screenPos.X, screenPos.Y);
                    if (endpointHit.HasValue)
                    {
                        _rootPanel.Cursor = new Cursor(StandardCursorType.Hand);
                        _directRenderer.SetHoveredEndpointHandle(endpointHit.Value.edge.Id, endpointHit.Value.isSource);
                    }
                    else
                    {
                        _directRenderer.ClearHoveredEndpointHandle();

                        // Check if hovering a node
                        var nodeHit = _directRenderer.HitTestNode(screenPos.X, screenPos.Y);
                        if (nodeHit != null)
                        {
                            _rootPanel.Cursor = new Cursor(StandardCursorType.Hand);
                        }
                        else
                        {
                            // Check if hovering a shape
                            var shapeHit = _directRenderer.HitTestShape(screenPos.X, screenPos.Y);
                            if (shapeHit != null)
                            {
                                _rootPanel.Cursor = new Cursor(StandardCursorType.Hand);
                            }
                            else
                            {
                                // Default cursor
                                _rootPanel.Cursor = Cursor.Default;
                            }
                        }
                    }
                }
            }

            // Update throttle timestamp after hover test
            _lastHoverHitTestTicks = currentTicks;
        }

        _inputStateMachine.HandlePointerMoved(e);

        sw.Stop();
        _pointerMoveCount++;
        _totalPointerMoveMs += sw.ElapsedMilliseconds;
        if (_pointerMoveCount % 100 == 0)
        {
            Debug.WriteLine($"[Input] PointerMoved: last100avg={_totalPointerMoveMs}ms total, count={_pointerMoveCount}");
            _totalPointerMoveMs = 0;
        }
    }

    /// <summary>
    /// Gets the appropriate resize cursor for the given handle position.
    /// </summary>
    private static Cursor GetResizeCursor(Rendering.ResizeHandlePosition position)
    {
        return position switch
        {
            Rendering.ResizeHandlePosition.TopLeft => new Cursor(StandardCursorType.TopLeftCorner),
            Rendering.ResizeHandlePosition.TopRight => new Cursor(StandardCursorType.TopRightCorner),
            Rendering.ResizeHandlePosition.BottomLeft => new Cursor(StandardCursorType.BottomLeftCorner),
            Rendering.ResizeHandlePosition.BottomRight => new Cursor(StandardCursorType.BottomRightCorner),
            Rendering.ResizeHandlePosition.Top => new Cursor(StandardCursorType.TopSide),
            Rendering.ResizeHandlePosition.Bottom => new Cursor(StandardCursorType.BottomSide),
            Rendering.ResizeHandlePosition.Left => new Cursor(StandardCursorType.LeftSide),
            Rendering.ResizeHandlePosition.Right => new Cursor(StandardCursorType.RightSide),
            _ => Cursor.Default
        };
    }

    private void OnRootPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerReleased(e);
    }

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control)
        {
            var node = Rendering.NodeRenderers.ResizableVisual.GetNodeFromTag(control.Tag);
            if (node == null) return;

            var point = e.GetCurrentPoint(control);

            if (point.Properties.IsRightButtonPressed)
            {
                HandleContextMenuRequest(e, control, node);
                return;
            }

            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, control);
            Focus();
        }
    }

    private void OnNodePointerMoved(object? sender, PointerEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerMoved(e);
    }

    private void OnNodePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerReleased(e);
    }

    private void OnPortPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control portVisual)
        {
            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, portVisual);
        }
    }

    private void OnPortPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Shape portShape && _theme != null)
        {
            portShape.Fill = _theme.PortHover;
        }
    }

    private void OnPortPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Shape portShape && _theme != null)
        {
            portShape.Fill = _theme.PortBackground;
        }
    }

    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is global::Avalonia.Controls.Shapes.Path edgePath && edgePath.Tag is Edge edge)
        {
            var point = e.GetCurrentPoint(edgePath);

            if (point.Properties.IsRightButtonPressed)
            {
                HandleContextMenuRequest(e, edgePath, edge);
                return;
            }

            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, edgePath);
            Focus();
        }
    }

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e, Node node, Rendering.ResizeHandlePosition position)
    {
        if (sender is Rectangle handle)
        {
            _inputContext.Graph = Graph;
            _inputStateMachine.HandlePointerPressed(e, handle);
        }
    }

    private void OnResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerMoved(e);
    }

    private void OnResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _inputContext.Graph = Graph;
        _inputStateMachine.HandlePointerReleased(e);
    }

    /// <summary>
    /// Performs hit testing for direct rendering mode by checking coordinates against node/port/edge positions.
    /// Returns a dummy Control with the appropriate Tag set for the input state machine.
    /// </summary>
    private Control? PerformDirectRenderingHitTest(double screenX, double screenY)
    {
        if (_directRenderer == null) return null;

        // Check edge endpoint handles first (for selected edges)
        var endpointHit = _directRenderer.HitTestEdgeEndpointHandle(screenX, screenY);
        if (endpointHit.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"[HitTest] Edge endpoint handle hit: {(endpointHit.Value.isSource ? "source" : "target")} on edge {endpointHit.Value.edge.Id}");
            // Create a dummy ellipse with the endpoint handle info as tag
            var dummyHandle = new Ellipse { Tag = (endpointHit.Value.edge, endpointHit.Value.isSource) };
            return dummyHandle;
        }

        // Check resize handles (they're on top of everything else)
        var resizeHit = _directRenderer.HitTestResizeHandle(screenX, screenY);
        if (resizeHit.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"[HitTest] Resize handle hit: {resizeHit.Value.position} on node {resizeHit.Value.node.Id}");
            // Create a dummy rectangle with the resize handle info as tag
            var dummyHandle = new Rectangle { Tag = (resizeHit.Value.node, resizeHit.Value.position) };
            return dummyHandle;
        }

        // Check ports (they're smaller targets on top of nodes)
        var portHit = _directRenderer.HitTestPort(screenX, screenY);
        if (portHit.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"[HitTest] Port hit: {portHit.Value.port.Id} on node {portHit.Value.node.Id}");
            // Create a dummy ellipse with the port info as tag
            var dummyPort = new Ellipse { Tag = (portHit.Value.node, portHit.Value.port, portHit.Value.isOutput) };
            return dummyPort;
        }

        // Check nodes
        var nodeHit = _directRenderer.HitTestNode(screenX, screenY);
        if (nodeHit != null)
        {
            System.Diagnostics.Debug.WriteLine($"[HitTest] Node hit: {nodeHit.Id}");
            // Create a dummy control with the node as tag
            var dummyNode = new Border { Tag = nodeHit };
            return dummyNode;
        }

        // Check edges AFTER nodes - edges are visually behind nodes
        var edgeHit = _directRenderer.HitTestEdge(screenX, screenY);
        if (edgeHit != null)
        {
            System.Diagnostics.Debug.WriteLine($"[HitTest] Edge hit: {edgeHit.Id}");
            // Create a dummy path with the edge as tag
            var dummyEdge = new global::Avalonia.Controls.Shapes.Path { Tag = edgeHit };
            return dummyEdge;
        }

        // Check shapes AFTER edges - shapes have lowest ZIndex (typically background elements)
        var shapeHit = _directRenderer.HitTestShape(screenX, screenY);
        if (shapeHit != null)
        {
            System.Diagnostics.Debug.WriteLine($"[HitTest] Shape hit: {shapeHit.Id}");
            // Create a dummy control with the shape as tag
            var dummyShape = new ContentControl { Tag = shapeHit };
            return dummyShape;
        }

        System.Diagnostics.Debug.WriteLine($"[HitTest] No hit at ({screenX:F0}, {screenY:F0})");
        return null;
    }

    #endregion

    #region Context Menu

    /// <summary>
    /// Handles right-click context menu requests for nodes, edges, or empty canvas.
    /// </summary>
    private void HandleContextMenuRequest(PointerPressedEventArgs e, Control? target, object? targetObject)
    {
        var screenPos = e.GetPosition(_rootPanel);
        var canvasPos = _viewport.ViewportToCanvas(screenPos);
        var canvasPoint = new Core.Point(canvasPos.X, canvasPos.Y);

        // In direct rendering mode, target may be a dummy control - use _rootPanel for positioning
        var menuAnchor = target ?? _rootPanel;

        if (targetObject is Node node)
        {
            // Only change selection if the clicked node is NOT already selected
            // This preserves multi-selection for grouping etc.
            if (!node.IsSelected && Graph != null)
            {
                // OPTIMIZED: Only deselect nodes that are actually selected
                foreach (var n in Graph.Elements.Nodes.Where(n => n.IsSelected))
                    n.IsSelected = false;
                node.IsSelected = true;
            }
            _contextMenu.Show(menuAnchor!, e, canvasPoint);
        }
        else if (targetObject is Edge edge)
        {
            // Only change selection if the clicked edge is NOT already selected
            if (!edge.IsSelected && Graph != null)
            {
                // OPTIMIZED: Only deselect items that are actually selected
                foreach (var n in Graph.Elements.Nodes.Where(n => n.IsSelected))
                    n.IsSelected = false;
                foreach (var ed in Graph.Elements.Edges.Where(ed => ed.IsSelected))
                    ed.IsSelected = false;
                edge.IsSelected = true;
            }
            _contextMenu.Show(menuAnchor!, e, canvasPoint);
        }
        else
        {
            // Empty canvas or need to hit test
            Control? hitElement = null;

            if (_useDirectRendering && _directRenderer != null)
            {
                // CRITICAL: Use screenPos, not canvasPos!
                // DirectRenderer methods expect screen coordinates and call ScreenToCanvas internally.
                hitElement = PerformDirectRenderingHitTest(screenPos.X, screenPos.Y);
            }
            else
            {
                var canvasPosForHit = _rootPanel != null && _mainCanvas != null
                    ? e.GetPosition(_mainCanvas)
                    : screenPos;
                hitElement = _mainCanvas?.InputHitTest(canvasPosForHit) as Control;
            }

            var hitNode = Rendering.NodeRenderers.ResizableVisual.GetNodeFromTag(hitElement?.Tag);
            if (hitNode != null)
            {
                // Only change selection if the clicked node is NOT already selected
                if (!hitNode.IsSelected)
                {
                    // OPTIMIZED: Only deselect nodes that are actually selected
                    foreach (var n in (Graph?.Elements.Nodes ?? []).Where(n => n.IsSelected))
                        n.IsSelected = false;
                    hitNode.IsSelected = true;
                }
                _contextMenu.Show(_rootPanel!, e, canvasPoint);
            }
            else if (hitElement?.Tag is Edge hitEdge)
            {
                // Only change selection if the clicked edge is NOT already selected
                if (!hitEdge.IsSelected)
                {
                    // OPTIMIZED: Only deselect edges that are actually selected
                    foreach (var ed in (Graph?.Elements.Edges ?? []).Where(ed => ed.IsSelected))
                        ed.IsSelected = false;
                    hitEdge.IsSelected = true;
                }
                _contextMenu.Show(_rootPanel!, e, canvasPoint);
            }
            else
            {
                // Empty canvas
                _contextMenu.ShowCanvasMenu(this, canvasPoint);
            }
        }

        e.Handled = true;
    }

    private static string GetTagDescription(object? element)
    {
        if (element == null) return "null";
        var control = element as Control;
        if (control?.Tag == null) return "no-tag";

        // Check for Node (direct or in dictionary)
        var node = Rendering.NodeRenderers.ResizableVisual.GetNodeFromTag(control.Tag);
        if (node != null) return $"Node({node.Id})";

        if (control.Tag is Edge e) return $"Edge({e.Id})";
        if (control.Tag is Dictionary<string, object> dict) return $"Dict({dict.Count} keys)";
        return control.Tag.GetType().Name;
    }

    #endregion
}
