using FlowGraph.Core;

namespace FlowGraph.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Graph MyGraph { get; }

    public MainWindowViewModel()
    {
        MyGraph = new Graph();

        // ============================================
        // GROUP 1: Data Pipeline (pre-grouped nodes)
        // ============================================
        var group1 = new Node
        {
            Id = "group1",
            Type = "group",
            IsGroup = true,
            Label = "Data Pipeline",
            Position = new Core.Point(60, 60),
            Width = 750,
            Height = 180
        };

        var inputNode = new Node
        {
            Type = "input",
            Data = "Data Source",
            Position = new Core.Point(100, 100),
            ParentGroupId = "group1",
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var processNode = new Node
        {
            Type = "Process",
            Position = new Core.Point(400, 150),
            ParentGroupId = "group1",
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }],
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var outputNode = new Node
        {
            Type = "output",
            Data = "Result",
            Position = new Core.Point(700, 100),
            ParentGroupId = "group1",
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }]
        };

        // ============================================
        // UNGROUPED: Edge type demonstrations
        // ============================================
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

        // ============================================
        // GROUP 2: Step Processing (nested example)
        // ============================================
        var group2 = new Node
        {
            Id = "group2",
            Type = "group",
            IsGroup = true,
            Label = "Step Processing",
            Position = new Core.Point(60, 440),
            Width = 550,
            Height = 200
        };

        var stepStart = new Node
        {
            Type = "Step",
            Position = new Core.Point(100, 500),
            ParentGroupId = "group2",
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var stepEnd = new Node
        {
            Type = "Step End",
            Position = new Core.Point(400, 550),
            ParentGroupId = "group2",
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        var smoothStepStart = new Node
        {
            Type = "SmoothStep",
            Position = new Core.Point(650, 500),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var smoothStepEnd = new Node
        {
            Type = "output",
            Data = "Final",
            Position = new Core.Point(950, 550),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        // Add groups first (they render behind children)
        MyGraph.AddNode(group1);
        MyGraph.AddNode(group2);

        // Add nodes
        MyGraph.AddNode(inputNode);
        MyGraph.AddNode(processNode);
        MyGraph.AddNode(outputNode);
        MyGraph.AddNode(straightStart);
        MyGraph.AddNode(straightEnd);
        MyGraph.AddNode(stepStart);
        MyGraph.AddNode(stepEnd);
        MyGraph.AddNode(smoothStepStart);
        MyGraph.AddNode(smoothStepEnd);

        // Bezier edges (default) with arrow - inside group1
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

        // Straight edge (ungrouped)
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

        // Step edge - inside group2
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

        // Smooth step edge (ungrouped)
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

