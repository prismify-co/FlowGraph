using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowGraph.Core;

/// <summary>
/// Represents a connection between two nodes.
/// </summary>
public class Edge : INotifyPropertyChanged
{
    private bool _isSelected;
    private List<Point>? _waypoints;

    /// <summary>
    /// Unique identifier for the edge.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The ID of the source node.
    /// </summary>
    public required string Source { get; set; }
    
    /// <summary>
    /// The ID of the target node.
    /// </summary>
    public required string Target { get; set; }
    
    /// <summary>
    /// The ID of the source port on the source node.
    /// </summary>
    public required string SourcePort { get; set; }
    
    /// <summary>
    /// The ID of the target port on the target node.
    /// </summary>
    public required string TargetPort { get; set; }
    
    /// <summary>
    /// The visual type of the edge (bezier, straight, step, etc.).
    /// </summary>
    public EdgeType Type { get; set; } = EdgeType.Bezier;
    
    /// <summary>
    /// The marker to display at the start of the edge.
    /// </summary>
    public EdgeMarker MarkerStart { get; set; } = EdgeMarker.None;
    
    /// <summary>
    /// The marker to display at the end of the edge.
    /// </summary>
    public EdgeMarker MarkerEnd { get; set; } = EdgeMarker.Arrow;
    
    /// <summary>
    /// Optional label to display on the edge.
    /// </summary>
    public string? Label { get; set; }
    
    /// <summary>
    /// Whether this edge is currently selected.
    /// </summary>
    public bool IsSelected 
    { 
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>
    /// Optional waypoints for custom edge routing.
    /// When set, the edge will pass through these intermediate points.
    /// Does not include the start and end points (port positions).
    /// </summary>
    public List<Point>? Waypoints
    {
        get => _waypoints;
        set => SetField(ref _waypoints, value);
    }

    /// <summary>
    /// Whether this edge should use automatic routing to avoid obstacles.
    /// </summary>
    public bool AutoRoute { get; set; } = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// Defines the visual style of an edge path.
/// </summary>
public enum EdgeType
{
    /// <summary>
    /// A smooth bezier curve (default).
    /// </summary>
    Bezier,
    
    /// <summary>
    /// A straight line between points.
    /// </summary>
    Straight,
    
    /// <summary>
    /// A path with right-angle turns.
    /// </summary>
    Step,
    
    /// <summary>
    /// A path with rounded right-angle turns.
    /// </summary>
    SmoothStep
}

/// <summary>
/// Defines the marker (arrow) style at edge endpoints.
/// </summary>
public enum EdgeMarker
{
    /// <summary>
    /// No marker.
    /// </summary>
    None,
    
    /// <summary>
    /// An open arrow (lines only).
    /// </summary>
    Arrow,
    
    /// <summary>
    /// A filled/closed arrow.
    /// </summary>
    ArrowClosed
}
