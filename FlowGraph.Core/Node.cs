using System.Collections.Immutable;
using System.ComponentModel;
using FlowGraph.Core.Elements;
using FlowGraph.Core.Models;

namespace FlowGraph.Core;

/// <summary>
/// Represents a node in the flow graph.
/// Uses a Definition (immutable) + State (mutable) composition pattern.
/// Implements <see cref="ICanvasElement"/> for unified element handling.
/// </summary>
/// <remarks>
/// <para>
/// The node's structural properties (Id, Type, Label, Ports, capability flags) are stored
/// in the <see cref="Definition"/> record. Runtime state (position, size, selection) is
/// stored in the <see cref="State"/> object.
/// </para>
/// <para>
/// For backward compatibility, pass-through properties are provided that delegate
/// to either Definition or State as appropriate.
/// </para>
/// </remarks>
public class Node : ICanvasElement
{
    private NodeDefinition _definition;
    private INodeState _state;

    // Mutable port lists for backward compatibility
    private List<Port> _inputs;
    private List<Port> _outputs;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Creates a node with the specified definition and optional state.
    /// </summary>
    /// <param name="definition">The immutable node definition.</param>
    /// <param name="state">The mutable node state. If null, a new NodeState is created.</param>
    public Node(NodeDefinition definition, INodeState? state = null)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _state = state ?? new NodeState();
        _inputs = definition.Inputs.Select(p => p.ToPort()).ToList();
        _outputs = definition.Outputs.Select(p => p.ToPort()).ToList();
        SubscribeToState();
    }

    /// <summary>
    /// Creates a node with default definition. For backward compatibility.
    /// </summary>
    public Node()
    {
        _definition = new NodeDefinition { Id = Guid.NewGuid().ToString() };
        _state = new NodeState();
        _inputs = [];
        _outputs = [];
        SubscribeToState();
    }

    #region Definition + State

    /// <summary>
    /// The immutable definition of this node (identity, type, ports, capabilities).
    /// Use <c>with</c> expressions to modify: <c>node.Definition = node.Definition with { Label = "x" };</c>
    /// </summary>
    public NodeDefinition Definition
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
            if (old.Type != value.Type) OnPropertyChanged(nameof(Type));
            if (old.Label != value.Label) OnPropertyChanged(nameof(Label));
            if (old.ParentGroupId != value.ParentGroupId) OnPropertyChanged(nameof(ParentGroupId));
            if (old.IsGroup != value.IsGroup) OnPropertyChanged(nameof(IsGroup));
            if (old.IsSelectable != value.IsSelectable) OnPropertyChanged(nameof(IsSelectable));
            if (old.IsDraggable != value.IsDraggable) OnPropertyChanged(nameof(IsDraggable));
            if (old.IsDeletable != value.IsDeletable) OnPropertyChanged(nameof(IsDeletable));
            if (old.IsConnectable != value.IsConnectable) OnPropertyChanged(nameof(IsConnectable));
            if (old.IsResizable != value.IsResizable) OnPropertyChanged(nameof(IsResizable));
            if (old.Data != value.Data) OnPropertyChanged(nameof(Data));
            if (!old.Inputs.SequenceEqual(value.Inputs))
            {
                _inputs = value.Inputs.Select(p => p.ToPort()).ToList();
                OnPropertyChanged(nameof(Inputs));
            }
            if (!old.Outputs.SequenceEqual(value.Outputs))
            {
                _outputs = value.Outputs.Select(p => p.ToPort()).ToList();
                OnPropertyChanged(nameof(Outputs));
            }
        }
    }

    /// <summary>
    /// The mutable runtime state of this node (position, size, selection).
    /// </summary>
    public INodeState State
    {
        get => _state;
        set
        {
            if (ReferenceEquals(_state, value)) return;
            UnsubscribeFromState();
            _state = value ?? throw new ArgumentNullException(nameof(value));
            SubscribeToState();
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsDragging));
            OnPropertyChanged(nameof(IsCollapsed));
        }
    }

    #endregion

    #region Pass-through Properties (Definition - Identity)

    /// <summary>
    /// Unique identifier for the node. Immutable - to change, replace the Definition.
    /// </summary>
    public string Id => Definition.Id;

    /// <summary>
    /// The type/category of the node (e.g., "process", "decision", "input").
    /// </summary>
    public string Type
    {
        get => Definition.Type;
        set => Definition = Definition with { Type = value };
    }

    /// <summary>
    /// Optional display label for the node.
    /// </summary>
    public string? Label
    {
        get => Definition.Label;
        set => Definition = Definition with { Label = value };
    }

    /// <summary>
    /// The ID of the parent group node, if this node is part of a group.
    /// </summary>
    public string? ParentGroupId
    {
        get => Definition.ParentGroupId;
        set => Definition = Definition with { ParentGroupId = value };
    }

    /// <summary>
    /// Whether this node is a group container.
    /// </summary>
    public bool IsGroup
    {
        get => Definition.IsGroup;
        set => Definition = Definition with { IsGroup = value };
    }

    /// <summary>
    /// Custom user data associated with the node.
    /// </summary>
    public object? Data
    {
        get => Definition.Data;
        set => Definition = Definition with { Data = value };
    }

    #endregion

    #region Pass-through Properties (Definition - Capabilities)

    /// <summary>
    /// Whether this node can be selected. Default is true.
    /// </summary>
    public bool IsSelectable
    {
        get => Definition.IsSelectable;
        set => Definition = Definition with { IsSelectable = value };
    }

    /// <summary>
    /// Whether this node can be dragged. Default is true.
    /// </summary>
    public bool IsDraggable
    {
        get => Definition.IsDraggable;
        set => Definition = Definition with { IsDraggable = value };
    }

    /// <summary>
    /// Whether this node can be deleted. Default is true.
    /// </summary>
    public bool IsDeletable
    {
        get => Definition.IsDeletable;
        set => Definition = Definition with { IsDeletable = value };
    }

    /// <summary>
    /// Whether this node can have new connections. Default is true.
    /// </summary>
    public bool IsConnectable
    {
        get => Definition.IsConnectable;
        set => Definition = Definition with { IsConnectable = value };
    }

    /// <summary>
    /// Whether this node can be resized. Default is true.
    /// </summary>
    public bool IsResizable
    {
        get => Definition.IsResizable;
        set => Definition = Definition with { IsResizable = value };
    }

    #endregion

    #region Pass-through Properties (Definition - Ports)

    /// <summary>
    /// Input ports for this node.
    /// For full immutability, use Definition.Inputs directly.
    /// </summary>
    public List<Port> Inputs
    {
        get => _inputs;
        set
        {
            _inputs = value ?? [];
            Definition = Definition with
            {
                Inputs = value?.Select(PortDefinition.FromPort).ToImmutableList() ?? []
            };
        }
    }

    /// <summary>
    /// Output ports for this node.
    /// For full immutability, use Definition.Outputs directly.
    /// </summary>
    public List<Port> Outputs
    {
        get => _outputs;
        set
        {
            _outputs = value ?? [];
            Definition = Definition with
            {
                Outputs = value?.Select(PortDefinition.FromPort).ToImmutableList() ?? []
            };
        }
    }

    #endregion

    #region Pass-through Properties (State - Position)

    /// <summary>
    /// The position of the node in canvas space.
    /// </summary>
    public Point Position
    {
        get => new(State.X, State.Y);
        set
        {
            State.X = value.X;
            State.Y = value.Y;
        }
    }

    #endregion

    #region Pass-through Properties (State - Size)

    /// <summary>
    /// The width of the node. Null means auto-sized.
    /// </summary>
    public double? Width
    {
        get => State.Width;
        set => State.Width = value;
    }

    /// <summary>
    /// The height of the node. Null means auto-sized.
    /// </summary>
    public double? Height
    {
        get => State.Height;
        set => State.Height = value;
    }

    #endregion

    #region Pass-through Properties (State - UI State)

    /// <summary>
    /// Whether this node is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => State.IsSelected;
        set => State.IsSelected = value;
    }

    /// <summary>
    /// Whether this node is currently being dragged.
    /// </summary>
    public bool IsDragging
    {
        get => State.IsDragging;
        set => State.IsDragging = value;
    }

    /// <summary>
    /// Whether the group is collapsed (only applicable when IsGroup is true).
    /// </summary>
    public bool IsCollapsed
    {
        get => State.IsCollapsed;
        set => State.IsCollapsed = value;
    }

    #endregion

    #region ICanvasElement Implementation

    /// <summary>
    /// Gets or sets whether this node is visible in the canvas.
    /// Delegates to State.IsVisible.
    /// </summary>
    public bool IsVisible
    {
        get => State.IsVisible;
        set => State.IsVisible = value;
    }

    /// <summary>
    /// Gets or sets the Z-index for rendering order.
    /// Delegates to State.ZIndex. Default is CanvasElement.ZIndexNodes (300).
    /// </summary>
    public int ZIndex
    {
        get => State.ZIndex;
        set => State.ZIndex = value;
    }

    /// <summary>
    /// Gets the bounding rectangle of this node in canvas coordinates.
    /// </summary>
    public Rect GetBounds()
    {
        return new Rect(
            Position.X,
            Position.Y,
            Width ?? GraphDefaults.NodeWidth,
            Height ?? GraphDefaults.NodeHeight);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Moves the node by the specified delta.
    /// </summary>
    public void Move(double deltaX, double deltaY)
    {
        State.X += deltaX;
        State.Y += deltaY;
    }

    /// <summary>
    /// Sets the node's position.
    /// </summary>
    public void SetPosition(double x, double y)
    {
        State.X = x;
        State.Y = y;
    }

    /// <summary>
    /// Sets the node's size.
    /// </summary>
    public void SetSize(double? width, double? height)
    {
        State.Width = width;
        State.Height = height;
    }

    /// <summary>
    /// Creates a snapshot of the current state.
    /// </summary>
    public NodeState CreateStateSnapshot()
    {
        return new NodeState
        {
            X = State.X,
            Y = State.Y,
            Width = State.Width,
            Height = State.Height,
            IsSelected = State.IsSelected,
            IsDragging = State.IsDragging,
            IsCollapsed = State.IsCollapsed
        };
    }

    /// <summary>
    /// Restores state from a snapshot.
    /// </summary>
    public void RestoreState(INodeState snapshot)
    {
        State.X = snapshot.X;
        State.Y = snapshot.Y;
        State.Width = snapshot.Width;
        State.Height = snapshot.Height;
        State.IsSelected = snapshot.IsSelected;
        State.IsDragging = snapshot.IsDragging;
        State.IsCollapsed = snapshot.IsCollapsed;
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
        // Forward state property changes and map X/Y to Position
        OnPropertyChanged(e.PropertyName);
        if (e.PropertyName is "X" or "Y")
        {
            OnPropertyChanged(nameof(Position));
        }
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
