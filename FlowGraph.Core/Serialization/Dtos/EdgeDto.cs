using FlowGraph.Core.Models;

namespace FlowGraph.Core.Serialization.Dtos;

/// <summary>
/// Legacy data transfer object for Edge serialization (Version 1 format).
/// Maintained for backward compatibility when deserializing old files.
/// </summary>
public class EdgeDto
{
    public required string Id { get; set; }
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
    public Dictionary<string, object>? Metadata { get; set; }

    public static EdgeDto FromEdge(Edge edge)
    {
        return new EdgeDto
        {
            Id = edge.Id,
            Source = edge.Source,
            Target = edge.Target,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            Type = edge.Type,
            MarkerStart = edge.MarkerStart,
            MarkerEnd = edge.MarkerEnd,
            Label = edge.Label,
            LabelInfo = edge.Definition.LabelInfo != null
                ? LabelInfoDto.FromLabelInfo(edge.Definition.LabelInfo)
                : null,
            AutoRoute = edge.AutoRoute,
            Waypoints = edge.Waypoints?.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList()
        };
    }

    public Edge ToEdge()
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
            LabelInfo = LabelInfo?.ToLabelInfo(),
            AutoRoute = AutoRoute
        };

        var state = new EdgeState
        {
            Waypoints = Waypoints?.Select(p => new Point(p.X, p.Y)).ToList()
        };

        return new Edge(definition, state);
    }
}
