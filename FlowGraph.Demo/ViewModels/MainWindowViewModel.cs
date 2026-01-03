using FlowGraph.Core;

namespace FlowGraph.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Graph MyGraph { get; }

    public MainWindowViewModel()
    {
        MyGraph = new Graph();

        // Row 1: Custom node types with Bezier edges
        var inputNode = new Node
        {
            Type = "input",  // Uses InputNodeRenderer (green)
            Data = "Data Source",
            Position = new Core.Point(100, 100),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var processNode = new Node
        {
            Type = "Process",  // Uses DefaultNodeRenderer
            Position = new Core.Point(400, 150),
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }],
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var outputNode = new Node
        {
            Type = "output",  // Uses OutputNodeRenderer (orange)
            Data = "Result",
            Position = new Core.Point(700, 100),
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }]
        };

        // Row 2: Different edge types
        var straightStart = new Node
        {
            Type = "input",
            Data = "Start",
            Position = new Core.Point(100, 300),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var straightEnd = new Node
        {
            Type = "output",
            Data = "End",
            Position = new Core.Point(400, 350),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        var stepStart = new Node
        {
            Type = "Step",
            Position = new Core.Point(100, 500),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var stepEnd = new Node
        {
            Type = "Step End",
            Position = new Core.Point(400, 550),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        var smoothStepStart = new Node
        {
            Type = "SmoothStep",
            Position = new Core.Point(500, 500),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var smoothStepEnd = new Node
        {
            Type = "output",
            Data = "Final",
            Position = new Core.Point(800, 550),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        MyGraph.AddNode(inputNode);
        MyGraph.AddNode(processNode);
        MyGraph.AddNode(outputNode);
        MyGraph.AddNode(straightStart);
        MyGraph.AddNode(straightEnd);
        MyGraph.AddNode(stepStart);
        MyGraph.AddNode(stepEnd);
        MyGraph.AddNode(smoothStepStart);
        MyGraph.AddNode(smoothStepEnd);

        // Bezier edges (default) with arrow
        MyGraph.AddEdge(new Edge
        {
            Source = inputNode.Id,
            Target = processNode.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.Arrow,
            Label = "Bezier"
        });

        MyGraph.AddEdge(new Edge
        {
            Source = processNode.Id,
            Target = outputNode.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.ArrowClosed
        });

        // Straight edge
        MyGraph.AddEdge(new Edge
        {
            Source = straightStart.Id,
            Target = straightEnd.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.Straight,
            MarkerEnd = EdgeMarker.Arrow,
            Label = "Straight"
        });

        // Step edge
        MyGraph.AddEdge(new Edge
        {
            Source = stepStart.Id,
            Target = stepEnd.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.Step,
            MarkerEnd = EdgeMarker.Arrow,
            Label = "Step"
        });

        // Smooth step edge
        MyGraph.AddEdge(new Edge
        {
            Source = smoothStepStart.Id,
            Target = smoothStepEnd.Id,
            SourcePort = "out",
            TargetPort = "in",
            Type = EdgeType.SmoothStep,
            MarkerEnd = EdgeMarker.ArrowClosed,
            Label = "SmoothStep"
        });
    }
}

