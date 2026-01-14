using System.ComponentModel;
using FlowGraph.Core.Elements;
using FlowGraph.Core.Models;

namespace FlowGraph.Core;

/// <summary>
/// Represents a connection between two nodes.
/// Uses a Definition (immutable) + State (mutable) composition pattern.
/// Implements <see cref="ICanvasElement"/> for unified element handling.
/// </summary>
/// <remarks>
/// <para>
/// The edge's structural properties (Source, Target, ports, type, markers) are stored
/// in the <see cref="Definition"/> record. Runtime state (selection, waypoints) is
/// stored in the <see cref="State"/> object.
/// </para>
/// <para>
/// For backward compatibility, pass-through properties are provided that delegate
/// to either Definition or State as appropriate.
/// </para>
/// </remarks>
public class Edge : ICanvasElement
{
    private EdgeDefinition _definition;
    private IEdgeState _state;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Creates an edge with the specified definition and optional state.
    /// </summary>
    /// <param name="definition">The immutable edge definition.</param>
    /// <param name="state">The mutable edge state. If null, a new EdgeState is created.</param>
    public Edge(EdgeDefinition definition, IEdgeState? state = null)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _state = state ?? new EdgeState();
        SubscribeToState();
    }

    /// <summary>
    /// Creates an edge with default definition. For backward compatibility.
    /// </summary>
    public Edge()
    {
        _definition = new EdgeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Source = null!,
            Target = null!,
            SourcePort = null!,
            TargetPort = null!
        };
        _state = new EdgeState();
        SubscribeToState();
    }

    #region Definition + State

    /// <summary>
    /// The immutable definition of this edge (identity, connections, visual type).
    /// Use <c>with</c> expressions to modify: <c>edge.Definition = edge.Definition with { Label = "x" };</c>
    /// </summary>
    public EdgeDefinition Definition
    {
        get => _definition;
        set
        {
            if (ReferenceEquals(_definition, value)) return;
            var old = _definition;
            _definition = value;
            OnPropertyChanged(nameof(Definition));

            // Raise change events for any definition properties that changed
            if (old.Id != value.Id) OnPropertyChanged(nameof(Id));
            if (old.Source != value.Source) OnPropertyChanged(nameof(Source));
            if (old.Target != value.Target) OnPropertyChanged(nameof(Target));
            if (old.SourcePort != value.SourcePort) OnPropertyChanged(nameof(SourcePort));
            if (old.TargetPort != value.TargetPort) OnPropertyChanged(nameof(TargetPort));
            if (old.Type != value.Type) OnPropertyChanged(nameof(Type));
            if (old.MarkerStart != value.MarkerStart) OnPropertyChanged(nameof(MarkerStart));
            if (old.MarkerEnd != value.MarkerEnd) OnPropertyChanged(nameof(MarkerEnd));
            if (old.Label != value.Label) OnPropertyChanged(nameof(Label));
            if (old.AutoRoute != value.AutoRoute) OnPropertyChanged(nameof(AutoRoute));
        }
    }

    /// <summary>
    /// The mutable runtime state of this edge (selection, waypoints).
    /// </summary>
    public IEdgeState State
    {
        get => _state;
        set
        {
            if (ReferenceEquals(_state, value)) return;
            UnsubscribeFromState();
            _state = value ?? throw new ArgumentNullException(nameof(value));
            SubscribeToState();
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(Waypoints));
        }
    }

    #endregion

    #region Pass-through Properties (Definition)

    /// <summary>
    /// Unique identifier for the edge. Immutable - to change, replace the Definition.
    /// </summary>
    public string Id => Definition.Id;

    /// <summary>
    /// The ID of the source node. To modify, use: <c>edge.Definition = edge.Definition with { Source = "x" };</c>
    /// </summary>
    public string Source
    {
        get => Definition.Source;
        set => Definition = Definition with { Source = value };
    }

    /// <summary>
    /// The ID of the target node. To modify, use: <c>edge.Definition = edge.Definition with { Target = "x" };</c>
    /// </summary>
    public string Target
    {
        get => Definition.Target;
        set => Definition = Definition with { Target = value };
    }

    /// <summary>
    /// The ID of the source port. To modify, use: <c>edge.Definition = edge.Definition with { SourcePort = "x" };</c>
    /// </summary>
    public string SourcePort
    {
        get => Definition.SourcePort;
        set => Definition = Definition with { SourcePort = value };
    }

    /// <summary>
    /// The ID of the target port. To modify, use: <c>edge.Definition = edge.Definition with { TargetPort = "x" };</c>
    /// </summary>
    public string TargetPort
    {
        get => Definition.TargetPort;
        set => Definition = Definition with { TargetPort = value };
    }

    /// <summary>
    /// The visual type of the edge. To modify, use: <c>edge.Definition = edge.Definition with { Type = EdgeType.Straight };</c>
    /// </summary>
    public EdgeType Type
    {
        get => Definition.Type;
        set => Definition = Definition with { Type = value };
    }

    /// <summary>
    /// The marker at the start of the edge.
    /// </summary>
    public EdgeMarker MarkerStart
    {
        get => Definition.MarkerStart;
        set => Definition = Definition with { MarkerStart = value };
    }

    /// <summary>
    /// The marker at the end of the edge.
    /// </summary>
    public EdgeMarker MarkerEnd
    {
        get => Definition.MarkerEnd;
        set => Definition = Definition with { MarkerEnd = value };
    }

    /// <summary>
    /// Optional label for the edge.
    /// </summary>
    public string? Label
    {
        get => Definition.Label;
        set => Definition = Definition with { Label = value };
    }

    /// <summary>
    /// Whether automatic routing is enabled.
    /// </summary>
    public bool AutoRoute
    {
        get => Definition.AutoRoute;
        set => Definition = Definition with { AutoRoute = value };
    }

    #endregion

    #region Pass-through Properties (State)

    /// <summary>
    /// Whether this edge is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => State.IsSelected;
        set => State.IsSelected = value;
    }

    /// <summary>
    /// Optional waypoints for custom edge routing.
    /// </summary>
    public List<Point>? Waypoints
    {
        get => State.Waypoints?.ToList();
        set => State.Waypoints = value;
    }

    #endregion

    #region ICanvasElement Implementation

    /// <summary>
    /// Gets the type identifier for this edge (used by renderer registry).
    /// Note: This is the string type, not EdgeType enum. Use Definition.Type.ToString().
    /// </summary>
    string ICanvasElement.Type => Definition.Type.ToString().ToLowerInvariant();

    /// <summary>
    /// Gets or sets the position of this edge. For edges, this is the source point.
    /// </summary>
    /// <remarks>
    /// Edges don't have a fixed position like nodes - their position is derived from
    /// the connected nodes. This returns Point.Zero for compatibility.
    /// </remarks>
    Point ICanvasElement.Position
    {
        get => Point.Zero;
        set { } // Edges don't have settable position
    }

    /// <summary>
    /// Gets or sets the width of this edge. Edges don't have width.
    /// </summary>
    double? ICanvasElement.Width
    {
        get => null;
        set { } // Edges don't have width
    }

    /// <summary>
    /// Gets or sets the height of this edge. Edges don't have height.
    /// </summary>
    double? ICanvasElement.Height
    {
        get => null;
        set { } // Edges don't have height
    }

    /// <summary>
    /// Gets or sets whether this edge is visible in the canvas.
    /// Delegates to State.IsVisible.
    /// </summary>
    public bool IsVisible
    {
        get => State.IsVisible;
        set => State.IsVisible = value;
    }

    /// <summary>
    /// Gets or sets the Z-index for rendering order.
    /// Delegates to State.ZIndex. Default is CanvasElement.ZIndexEdges (200).
    /// </summary>
    public int ZIndex
    {
        get => State.ZIndex;
        set => State.ZIndex = value;
    }

    /// <summary>
    /// Gets the bounding rectangle of this edge.
    /// For edges without waypoints, returns an empty rect at origin.
    /// </summary>
    public Rect GetBounds()
    {
        // For edges, bounds are calculated from waypoints if available
        var waypoints = Waypoints;
        if (waypoints == null || waypoints.Count == 0)
            return Rect.Empty;

        var minX = waypoints.Min(p => p.X);
        var minY = waypoints.Min(p => p.Y);
        var maxX = waypoints.Max(p => p.X);
        var maxY = waypoints.Max(p => p.Y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Reconnects this edge to a different target node and port.
    /// </summary>
    public void Reconnect(string newTarget, string newTargetPort)
    {
        Definition = Definition.ReconnectTarget(newTarget, newTargetPort);
    }

    /// <summary>
    /// Reconnects this edge from a different source node and port.
    /// </summary>
    public void ReconnectSource(string newSource, string newSourcePort)
    {
        Definition = Definition.ReconnectSource(newSource, newSourcePort);
    }

    #endregion

    #region INotifyPropertyChanged

    private void SubscribeToState()
    {
        _state.PropertyChanged += OnStatePropertyChanged;
    }

    private void UnsubscribeFromState()
    {
        _state.PropertyChanged -= OnStatePropertyChanged;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward state property changes
        OnPropertyChanged(e.PropertyName);
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    protected virtual void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
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
