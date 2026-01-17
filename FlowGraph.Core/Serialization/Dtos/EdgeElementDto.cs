using FlowGraph.Core.Elements;
using FlowGraph.Core.Models;

namespace FlowGraph.Core.Serialization.Dtos;

/// <summary>
/// Element DTO for Edge type (Version 2 format).
/// </summary>
public class EdgeElementDto : ElementDto
{
    public required string Source { get; set; }
    public required string Target { get; set; }
    public required string SourcePort { get; set; }
    public required string TargetPort { get; set; }
    public EdgeType Type { get; set; } = EdgeType.Bezier;
    public EdgeMarker MarkerStart { get; set; } = EdgeMarker.None;
    public EdgeMarker MarkerEnd { get; set; } = EdgeMarker.Arrow;
    public string? Label { get; set; }
    public LabelInfoDto? LabelInfo { get; set; }
    public bool AutoRoute { get; set; }
    public List<PointDto>? Waypoints { get; set; }
    public bool IsVisible { get; set; } = true;
    public int ZIndex { get; set; } = CanvasElement.ZIndexEdges;
    public Dictionary<string, object>? Metadata { get; set; }

    public static EdgeElementDto FromEdgeInstance(Edge edge)
    {
        return new EdgeElementDto
        {
            Id = edge.Id,
            Kind = "edge",
            Source = edge.Source,
            Target = edge.Target,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            Type = edge.Type,
            MarkerStart = edge.MarkerStart,
            MarkerEnd = edge.MarkerEnd,
            Label = edge.Label,
            AutoRoute = edge.AutoRoute,
            Waypoints = edge.Waypoints?.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList(),
            IsVisible = edge.IsVisible,
            ZIndex = edge.ZIndex
        };
    }

    public override ICanvasElement ToElement()
    {
        var definition = new EdgeDefinition
        {
            Id = Id,
            Source = Source,
            Target = Target,
            SourcePort = SourcePort,
            TargetPort = TargetPort,
            Type = Type,
            MarkerStart = MarkerStart,
            MarkerEnd = MarkerEnd,
            Label = Label,
            AutoRoute = AutoRoute
        };

        var state = new EdgeState
        {
            Waypoints = Waypoints?.Select(p => new Point(p.X, p.Y)).ToList(),
            IsVisible = IsVisible,
            ZIndex = ZIndex
        };

        return new Edge(definition, state);
    }
}
