using FlowGraph.Core;

namespace FlowGraph.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Graph MyGraph { get; }

    public MainWindowViewModel()
    {
        MyGraph = new Graph();

        var node1 = new Node
        {
            Type = "Input",
            Position = new Core.Point(100, 100),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var node2 = new Node
        {
            Type = "Process",
            Position = new Core.Point(400, 150),
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }],
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var node3 = new Node
        {
            Type = "Output",
            Position = new Core.Point(700, 100),
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }]
        };

        MyGraph.AddNode(node1);
        MyGraph.AddNode(node2);
        MyGraph.AddNode(node3);

        MyGraph.AddEdge(new Edge
        {
            Source = node1.Id,
            Target = node2.Id,
            SourcePort = "out",
            TargetPort = "in"
        });

        MyGraph.AddEdge(new Edge
        {
            Source = node2.Id,
            Target = node3.Id,
            SourcePort = "out",
            TargetPort = "in"
        });
    }
}

