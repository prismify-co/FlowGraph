using CommunityToolkit.Mvvm.ComponentModel;

namespace FlowGraph.Core;

public partial class Node : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _type = "default";

    [ObservableProperty]
    private Point _position;

    [ObservableProperty]
    private double? _width;

    [ObservableProperty]
    private double? _height;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isDragging;

    [ObservableProperty]
    private bool _isResizable = true;

    #region Capability Flags

    /// <summary>
    /// Whether this node can be selected. Default is true.
    /// When false, clicking on the node will not select it.
    /// </summary>
    [ObservableProperty]
    private bool _isSelectable = true;

    /// <summary>
    /// Whether this node can be dragged. Default is true.
    /// When false, the node cannot be moved by dragging.
    /// </summary>
    [ObservableProperty]
    private bool _isDraggable = true;

    /// <summary>
    /// Whether this node can be deleted. Default is true.
    /// When false, the node cannot be deleted via keyboard or context menu.
    /// </summary>
    [ObservableProperty]
    private bool _isDeletable = true;

    /// <summary>
    /// Whether this node can have new connections. Default is true.
    /// When false, new edges cannot be created from/to this node's ports.
    /// </summary>
    [ObservableProperty]
    private bool _isConnectable = true;

    #endregion

    /// <summary>
    /// The ID of the parent group node, if this node is part of a group.
    /// </summary>
    [ObservableProperty]
    private string? _parentGroupId;

    /// <summary>
    /// Whether this node is a group container.
    /// </summary>
    [ObservableProperty]
    private bool _isGroup;

    /// <summary>
    /// Whether the group is collapsed (only applicable when IsGroup is true).
    /// </summary>
    [ObservableProperty]
    private bool _isCollapsed;

    /// <summary>
    /// The label/title for this node (useful for groups).
    /// </summary>
    [ObservableProperty]
    private string? _label;

    public object? Data { get; set; }
    public List<Port> Inputs { get; set; } = [];
    public List<Port> Outputs { get; set; } = [];
}
