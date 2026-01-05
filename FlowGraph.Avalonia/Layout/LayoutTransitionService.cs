using FlowGraph.Avalonia.Animation;
using FlowGraph.Core;

namespace FlowGraph.Avalonia.Layout;

/// <summary>
/// Provides a minimal API for animating layout transitions (node position changes) without coupling to layout algorithms.
/// </summary>
public sealed class LayoutTransitionService
{
    private readonly Func<Graph?> _getGraph;
    private readonly Action _refreshEdges;
    private readonly AnimationManager _animations;

    public LayoutTransitionService(Func<Graph?> getGraph, Action refreshEdges, AnimationManager animations)
    {
        _getGraph = getGraph;
        _refreshEdges = refreshEdges;
        _animations = animations;
    }

    /// <summary>
    /// Animates node positions to the provided target positions.
    /// Nodes not included in <paramref name="targetPositions"/> are not modified.
    /// </summary>
    public void AnimateTo(
        IReadOnlyDictionary<string, Core.Point> targetPositions,
        double duration = 0.35,
        Func<double, double>? easing = null,
        Action? onComplete = null)
    {
        var graph = _getGraph();
        if (graph == null || targetPositions.Count == 0) return;

        var start = graph.Nodes
            .Where(n => targetPositions.ContainsKey(n.Id))
            .ToDictionary(n => n.Id, n => n.Position);

        var animation = new LayoutAnimation(
            start,
            targetPositions,
            duration,
            onUpdate: positions =>
            {
                foreach (var (nodeId, pos) in positions)
                {
                    var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                    if (node != null)
                    {
                        node.Position = pos;
                    }
                }

                _refreshEdges();
            },
            easing: easing,
            onComplete: onComplete);

        _animations.Start(animation);
    }
}
