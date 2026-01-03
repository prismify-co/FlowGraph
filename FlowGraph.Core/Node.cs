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

    public object? Data { get; set; }
    public List<Port> Inputs { get; set; } = [];
    public List<Port> Outputs { get; set; } = [];
}
