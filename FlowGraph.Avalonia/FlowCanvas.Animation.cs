using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Avalonia.Animation;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Animation API and helpers.
/// </summary>
public partial class FlowCanvas
{
    #region Animation API

    /// <summary>
    /// Gets the animation manager for controlling animations.
    /// </summary>
    public AnimationManager Animations => _animationManager;

    /// <summary>
    /// Smoothly animates to fit all nodes into the viewport.
    /// </summary>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void FitToViewAnimated(double duration = 0.3, Func<double, double>? easing = null)
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var bounds = CalculateGraphBounds();
        var animation = ViewportAnimation.FitToBounds(_viewport, bounds, 50, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to center on the graph.
    /// </summary>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void CenterOnGraphAnimated(double duration = 0.3, Func<double, double>? easing = null)
    {
        if (Graph == null || Graph.Nodes.Count == 0) return;

        var bounds = CalculateGraphBounds();
        var center = new AvaloniaPoint(
            bounds.X + bounds.Width / 2,
            bounds.Y + bounds.Height / 2);

        var animation = ViewportAnimation.CenterOn(_viewport, center, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to center on a specific point.
    /// </summary>
    /// <param name="x">X coordinate in canvas coordinates.</param>
    /// <param name="y">Y coordinate in canvas coordinates.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void CenterOnAnimated(double x, double y, double duration = 0.3, Func<double, double>? easing = null)
    {
        var animation = ViewportAnimation.CenterOn(
            _viewport, 
            new AvaloniaPoint(x, y), 
            duration, 
            easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to center on a specific node.
    /// </summary>
    /// <param name="node">The node to center on.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void CenterOnNodeAnimated(Node node, double duration = 0.3, Func<double, double>? easing = null)
    {
        var (width, height) = _graphRenderer.GetNodeDimensions(node);
        var center = new AvaloniaPoint(
            node.Position.X + width / 2,
            node.Position.Y + height / 2);

        var animation = ViewportAnimation.CenterOn(_viewport, center, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates to a specific zoom level.
    /// </summary>
    /// <param name="targetZoom">Target zoom level.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.2).</param>
    /// <param name="easing">Optional easing function.</param>
    public void ZoomToAnimated(double targetZoom, double duration = 0.2, Func<double, double>? easing = null)
    {
        var animation = ViewportAnimation.ZoomTo(_viewport, targetZoom, null, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates a node to a new position.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="targetPosition">Target position in canvas coordinates.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void AnimateNodeTo(Node node, Core.Point targetPosition, double duration = 0.3, Func<double, double>? easing = null)
    {
        var animation = new NodeAnimation(node, targetPosition, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Smoothly animates multiple nodes to new positions.
    /// </summary>
    /// <param name="nodePositions">Dictionary mapping nodes to their target positions.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="easing">Optional easing function.</param>
    public void AnimateNodesTo(IReadOnlyDictionary<Node, Core.Point> nodePositions, double duration = 0.3, Func<double, double>? easing = null)
    {
        var animation = new MultiNodeAnimation(nodePositions, duration, easing);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Stops all running animations.
    /// </summary>
    public void StopAllAnimations()
    {
        _animationManager.StopAll();
    }

    #endregion

    #region Edge Animations

    /// <summary>
    /// Animates an edge with a fade-in effect.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgeFadeIn(Edge edge, double duration = 0.3, Action? onComplete = null)
    {
        var animation = EdgeFadeAnimation.FadeIn(
            edge,
            duration,
            (e, opacity) => UpdateEdgeOpacity(e, opacity),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates an edge with a fade-out effect.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgeFadeOut(Edge edge, double duration = 0.3, Action? onComplete = null)
    {
        var animation = EdgeFadeAnimation.FadeOut(
            edge,
            duration,
            (e, opacity) => UpdateEdgeOpacity(e, opacity),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates an edge with a pulse/highlight effect.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="pulseCount">Number of pulse cycles (default: 2).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgePulse(Edge edge, int pulseCount = 2, Action? onComplete = null)
    {
        var animation = new EdgePulseAnimation(
            edge,
            baseThickness: 2,
            pulseAmount: 3,
            frequency: 2,
            duration: pulseCount * 0.5,
            (e, thickness) => UpdateEdgeThickness(e, thickness),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Starts a continuous flow animation on an edge (animated dashed line).
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="speed">Speed of the flow animation (default: 50).</param>
    /// <param name="reverse">If true, flow goes from target to source. If false (default), flow goes from source to target.</param>
    /// <returns>The animation instance (can be used to stop it later).</returns>
    public EdgeFlowAnimation StartEdgeFlowAnimation(Edge edge, double speed = 50, bool reverse = false)
    {
        var animation = new EdgeFlowAnimation(
            edge,
            speed,
            reverse,
            (e, offset) => UpdateEdgeDashOffset(e, offset));
        _animationManager.Start(animation);
        return animation;
    }

    /// <summary>
    /// Stops an edge flow animation.
    /// </summary>
    /// <param name="animation">The animation to stop.</param>
    public void StopEdgeFlowAnimation(EdgeFlowAnimation animation)
    {
        _animationManager.Stop(animation);
    }

    /// <summary>
    /// Animates an edge's color.
    /// </summary>
    /// <param name="edge">The edge to animate.</param>
    /// <param name="targetColor">Target color.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateEdgeColor(Edge edge, Color targetColor, double duration = 0.3, Action? onComplete = null)
    {
        // Get the current color from the visible path
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        var currentColor = visiblePath?.Stroke is SolidColorBrush brush 
            ? brush.Color 
            : (_theme?.EdgeStroke is SolidColorBrush themeBrush ? themeBrush.Color : Colors.Gray);
        
        var animation = new EdgeColorAnimation(
            edge,
            currentColor,
            targetColor,
            duration,
            onUpdate: (e, color) => UpdateEdgeColor(e, color),
            onComplete: onComplete);
        _animationManager.Start(animation);
    }

    #endregion

    #region Node Animations

    /// <summary>
    /// Smoothly animates a node appearing with fade and scale effect.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.3).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateNodeAppear(Node node, double duration = 0.3, Action? onComplete = null)
    {
        var animation = NodeAppearAnimation.Appear(
            node,
            duration,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates a node disappearing with fade and scale effect.
    /// </summary>
    /// <param name="node">The node to animate.</param>
    /// <param name="duration">Animation duration in seconds (default: 0.2).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateNodeDisappear(Node node, double duration = 0.2, Action? onComplete = null)
    {
        var animation = NodeAppearAnimation.Disappear(
            node,
            duration,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates multiple nodes appearing with staggered effect.
    /// </summary>
    /// <param name="nodes">The nodes to animate.</param>
    /// <param name="duration">Animation duration per node in seconds (default: 0.3).</param>
    /// <param name="stagger">Delay between each node's animation start (default: 0.05).</param>
    /// <param name="onComplete">Optional callback when all animations complete.</param>
    public void AnimateNodesAppear(IEnumerable<Node> nodes, double duration = 0.3, double stagger = 0.05, Action? onComplete = null)
    {
        var animation = new MultiNodeAppearAnimation(
            nodes,
            appearing: true,
            duration,
            stagger,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates multiple nodes disappearing with staggered effect.
    /// </summary>
    /// <param name="nodes">The nodes to animate.</param>
    /// <param name="duration">Animation duration per node in seconds (default: 0.2).</param>
    /// <param name="stagger">Delay between each node's animation start (default: 0.03).</param>
    /// <param name="onComplete">Optional callback when all animations complete.</param>
    public void AnimateNodesDisappear(IEnumerable<Node> nodes, double duration = 0.2, double stagger = 0.03, Action? onComplete = null)
    {
        var animation = new MultiNodeAppearAnimation(
            nodes,
            appearing: false,
            duration,
            stagger,
            (n, opacity, scale) => UpdateNodeAppearance(n, opacity, scale),
            onComplete);
        _animationManager.Start(animation);
    }

    /// <summary>
    /// Animates a brief selection pulse on a node.
    /// </summary>
    /// <param name="node">The node to pulse.</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateSelectionPulse(Node node, Action? onComplete = null)
    {
        var animation = new SelectionPulseAnimation(
            0.4,
            intensity => UpdateNodeSelectionPulse(node, intensity),
            onComplete);
        _animationManager.Start(animation);
    }

    #endregion

    #region Group Animations

    /// <summary>
    /// Animates a group collapsing with sequenced transitions:
    /// 1. Children nodes and edges fade out together
    /// 2. Group shrinks
    /// </summary>
    /// <param name="groupId">The ID of the group to collapse.</param>
    /// <param name="duration">Total animation duration in seconds (default: 0.5).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateGroupCollapse(string groupId, double duration = 0.5, Action? onComplete = null)
    {
        if (Graph == null || _mainCanvas == null || _theme == null) return;

        var group = Graph.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null || group.IsCollapsed) return;

        var expandedWidth = group.Width ?? 200;
        var expandedHeight = group.Height ?? 100;
        var collapsedWidth = 150.0;
        var collapsedHeight = 40.0;

        // Get children and connected/boundary edges
        var children = Graph.GetGroupChildren(groupId).ToList();
        var connectedEdges = GetEdgesForGroupFade(Graph, groupId);

        // Hide resize handles during animation
        _graphRenderer.RemoveResizeHandles(_mainCanvas, groupId);

        // Phase durations (content fade -> shrink)
        var contentFadeDuration = duration * 0.5;
        var shrinkDuration = duration * 0.5;

        // PHASE 1: Fade out edges AND nodes together (in parallel)
        var contentFadeAnimation = new GenericAnimation(
            contentFadeDuration,
            t =>
            {
                var opacity = 1.0 - Easing.EaseInCubic(t);

                // Fade out edges (visible + hit area + markers)
                foreach (var edge in connectedEdges)
                {
                    UpdateEdgeOpacity(edge, opacity);
                }

                // Fade out nodes and ports
                foreach (var child in children)
                {
                    var childVisual = _graphRenderer.GetNodeVisual(child.Id);
                    if (childVisual != null) childVisual.Opacity = opacity;
                    foreach (var port in child.Inputs.Concat(child.Outputs))
                    {
                        var portVisual = _graphRenderer.GetPortVisual(child.Id, port.Id);
                        if (portVisual != null) portVisual.Opacity = opacity;
                    }
                }
            },
            onComplete: () =>
            {
                // PHASE 2: Shrink group
                var shrinkAnimation = new GenericAnimation(
                    shrinkDuration,
                    t =>
                    {
                        var easedT = Easing.EaseInOutCubic(t);
                        group.Width = expandedWidth + (collapsedWidth - expandedWidth) * easedT;
                        group.Height = expandedHeight + (collapsedHeight - expandedHeight) * easedT;
                        _graphRenderer.UpdateNodeSize(group, _theme);
                        _graphRenderer.UpdateNodePosition(group);

                        // This re-renders edges; ensure overrides are re-applied in RenderEdges()
                        RenderEdges();
                    },
                    onComplete: () =>
                    {
                        // Final: Set collapsed state (triggers re-render)
                        _groupManager.SetCollapsed(groupId, true);

                        ClearEdgeOpacityOverrides(connectedEdges);
                        onComplete?.Invoke();
                    });
                _animationManager.Start(shrinkAnimation);
            });
        _animationManager.Start(contentFadeAnimation);
    }

    /// <summary>
    /// Animates a group expanding with sequenced transitions:
    /// 1. Group grows
    /// 2. Children nodes and edges fade in together
    /// </summary>
    /// <param name="groupId">The ID of the group to expand.</param>
    /// <param name="duration">Total animation duration in seconds (default: 0.5).</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateGroupExpand(string groupId, double duration = 0.5, Action? onComplete = null)
    {
        if (Graph == null || _mainCanvas == null || _theme == null) return;

        var group = Graph.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null || !group.IsCollapsed) return;

        var collapsedWidth = group.Width ?? 150;
        var collapsedHeight = group.Height ?? 40;

        // Hide resize handles during animation
        _graphRenderer.RemoveResizeHandles(_mainCanvas, groupId);

        // Get children info and calculate expanded size
        var children = Graph.GetGroupChildren(groupId).ToList();
        if (children.Count == 0)
        {
            _groupManager.SetCollapsed(groupId, false);
            onComplete?.Invoke();
            return;
        }

        var connectedEdges = GetEdgesForGroupFade(Graph, groupId);

        // Calculate expanded size from children
        var padding = 20.0;
        var headerHeight = 30.0;
        var minX = children.Min(n => n.Position.X);
        var minY = children.Min(n => n.Position.Y);
        var maxX = children.Max(n => n.Position.X + (n.Width ?? Settings.NodeWidth));
        var maxY = children.Max(n => n.Position.Y + (n.Height ?? Settings.NodeHeight));
        var expandedWidth = maxX - minX + padding * 2;
        var expandedHeight = maxY - minY + padding * 2 + headerHeight;

        // Set IsCollapsed = false directly (avoid triggering re-render event)
        group.IsCollapsed = false;

        // Manually render children at opacity 0
        foreach (var child in children)
        {
            var control = _graphRenderer.RenderNode(_mainCanvas, child, _theme, null);
            control.Opacity = 0;
            control.PointerPressed += OnNodePointerPressed;
            control.PointerMoved += OnNodePointerMoved;
            control.PointerReleased += OnNodePointerReleased;

            foreach (var port in child.Inputs.Concat(child.Outputs))
            {
                var portVisual = _graphRenderer.GetPortVisual(child.Id, port.Id);
                if (portVisual != null)
                {
                    portVisual.Opacity = 0;
                    portVisual.PointerPressed += OnPortPointerPressed;
                    portVisual.PointerEntered += OnPortPointerEntered;
                    portVisual.PointerExited += OnPortPointerExited;
                }
            }
        }

        // Re-render edges then set them to opacity 0 (and record override)
        RenderEdges();
        foreach (var edge in connectedEdges)
        {
            UpdateEdgeOpacity(edge, 0);
        }

        // Phase durations (expand -> content fade)
        var expandDuration = duration * 0.5;
        var contentFadeDuration = duration * 0.5;

        // PHASE 1: Expand group
        var expandAnimation = new GenericAnimation(
            expandDuration,
            t =>
            {
                var easedT = Easing.EaseOutCubic(t);
                group.Width = collapsedWidth + (expandedWidth - collapsedWidth) * easedT;
                group.Height = collapsedHeight + (expandedHeight - collapsedHeight) * easedT;
                _graphRenderer.UpdateNodeSize(group, _theme);
                _graphRenderer.UpdateNodePosition(group);
            },
            onComplete: () =>
            {
                // PHASE 2: Fade in nodes AND edges together (in parallel)
                var contentFadeAnimation = new GenericAnimation(
                    contentFadeDuration,
                    t =>
                    {
                        var opacity = Easing.EaseOutCubic(t);

                        // Fade in nodes and ports
                        foreach (var child in children)
                        {
                            var childVisual = _graphRenderer.GetNodeVisual(child.Id);
                            if (childVisual != null) childVisual.Opacity = opacity;
                            foreach (var port in child.Inputs.Concat(child.Outputs))
                            {
                                var portVisual = _graphRenderer.GetPortVisual(child.Id, port.Id);
                                if (portVisual != null) portVisual.Opacity = opacity;
                            }
                        }

                        // Fade in edges and markers
                        foreach (var edge in connectedEdges)
                        {
                            UpdateEdgeOpacity(edge, opacity);
                        }
                    },
                    onComplete: () =>
                    {
                        ClearEdgeOpacityOverrides(connectedEdges);

                        // Restore resize handles if group is still selected
                        if (group.IsSelected && _mainCanvas != null)
                        {
                            UpdateResizeHandlesForNode(group);
                        }
                        onComplete?.Invoke();
                    });
                _animationManager.Start(contentFadeAnimation);
            });
        _animationManager.Start(expandAnimation);
    }

    #endregion

    #region Animation Helpers

    /// <summary>
    /// Updates an edge's opacity and keeps all associated visuals in sync.
    /// </summary>
    private void UpdateEdgeOpacity(Edge edge, double opacity)
    {
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            visiblePath.Opacity = opacity;
        }

        var hitArea = _graphRenderer.GetEdgeVisual(edge.Id);
        if (hitArea != null)
        {
            hitArea.Opacity = opacity;
        }

        var markers = _graphRenderer.GetEdgeMarkers(edge.Id);
        if (markers != null)
        {
            foreach (var marker in markers)
            {
                marker.Opacity = opacity;
            }
        }

        var label = _graphRenderer.GetEdgeLabel(edge.Id);
        if (label != null)
        {
            label.Opacity = opacity;
        }
    }

    private void UpdateEdgeThickness(Edge edge, double thickness)
    {
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            visiblePath.StrokeThickness = thickness * _viewport.Zoom;
        }
    }

    private void UpdateEdgeDashOffset(Edge edge, double offset)
    {
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            if (visiblePath.StrokeDashArray == null || visiblePath.StrokeDashArray.Count == 0)
            {
                visiblePath.StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 5, 5 };
            }
            visiblePath.StrokeDashOffset = offset;
        }
    }

    private void UpdateEdgeColor(Edge edge, Color color)
    {
        var visiblePath = _graphRenderer.GetEdgeVisiblePath(edge.Id);
        if (visiblePath != null)
        {
            visiblePath.Stroke = new SolidColorBrush(color);
        }
    }

    private void ApplyEdgeOpacityOverrides()
    {
        if (Graph == null) return;

        // Prune missing edges
        var existing = Graph.Edges.Select(e => e.Id).ToHashSet();
        foreach (var edgeId in _edgeOpacityOverrides.Keys.Where(id => !existing.Contains(id)).ToList())
        {
            _edgeOpacityOverrides.Remove(edgeId);
        }

        foreach (var (edgeId, opacity) in _edgeOpacityOverrides)
        {
            var edge = Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge == null) continue;
            UpdateEdgeOpacity(edge, opacity);
        }
    }

    private void ClearEdgeOpacityOverrides(IEnumerable<Edge> edges)
    {
        foreach (var e in edges)
        {
            _edgeOpacityOverrides.Remove(e.Id);
        }
    }

    private List<Edge> GetEdgesForGroupFade(Graph graph, string groupId)
    {
        var children = graph.GetGroupChildren(groupId).ToList();
        var childIds = children.Select(c => c.Id).ToHashSet();

        return graph.Edges
            .Where(e => childIds.Contains(e.Source) || childIds.Contains(e.Target))
            .ToList();
    }

    private void UpdateNodeAppearance(Node node, double opacity, double scale)
    {
        var nodeVisual = _graphRenderer.GetNodeVisual(node.Id);
        if (nodeVisual != null)
        {
            nodeVisual.Opacity = opacity;
            nodeVisual.RenderTransform = new ScaleTransform(scale, scale);
            nodeVisual.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }

        foreach (var port in node.Inputs.Concat(node.Outputs))
        {
            var portVisual = _graphRenderer.GetPortVisual(node.Id, port.Id);
            if (portVisual != null)
            {
                portVisual.Opacity = opacity;
            }
        }
    }

    private void UpdateNodeSelectionPulse(Node node, double intensity)
    {
        var nodeVisual = _graphRenderer.GetNodeVisual(node.Id);
        if (nodeVisual is Border border && _theme != null)
        {
            var baseThickness = node.IsSelected ? 3 : 2;
            var pulseThickness = baseThickness + intensity * 2;
            border.BorderThickness = new Thickness(pulseThickness);
        }
    }

    #endregion
}
