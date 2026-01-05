using FlowGraph.Core;

namespace FlowGraph.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Graph MyGraph { get; }

    // Default dimensions (should match FlowCanvasSettings)
    private const double DefaultNodeWidth = 150;
    private const double DefaultNodeHeight = 80;
    private const double GroupPadding = 20;
    private const double GroupHeaderHeight = 30;

    public MainWindowViewModel()
    {
        MyGraph = new Graph();

        // ============================================
        // GROUP 1: Data Pipeline (with external ports)
        // ============================================
        
        // Define child nodes first to calculate group bounds
        var inputNode = new Node
        {
            Type = "input",
            Data = "Data Source",
            Position = new Core.Point(100, 100),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var processNode = new Node
        {
            Type = "Process",
            Position = new Core.Point(350, 130),
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }],
            Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
        };

        var outputNode = new Node
        {
            Type = "output",
            Data = "Result",
            Position = new Core.Point(600, 100),
            Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }]
        };

        // Calculate group1 bounds
        var group1Bounds = CalculateGroupBounds([inputNode, processNode, outputNode]);
        var group1 = new Node
        {
            Id = "group1",
            Type = "group",
            IsGroup = true,
            Label = "Data Pipeline",
            Position = group1Bounds.position,
            Width = group1Bounds.width,
            Height = group1Bounds.height,
            // Groups can have ports for external connections!
            Inputs = [new Port { Id = "group-in", Type = "data", Label = "Pipeline Input" }],
            Outputs = [new Port { Id = "group-out", Type = "data", Label = "Pipeline Output" }]
        };

        // Set parent after creating group
        inputNode.ParentGroupId = "group1";
        processNode.ParentGroupId = "group1";
        outputNode.ParentGroupId = "group1";

        // ============================================
        // External nodes that connect TO the group
        // ============================================
        var externalInput = new Node
        {
            Type = "input",
            Data = "External Source",
            Position = new Core.Point(-150, 130),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var externalOutput = new Node
        {
            Type = "output",
            Data = "External Sink",
            Position = new Core.Point(900, 130),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        // ============================================
        // UNGROUPED: Edge type demonstrations
        // ============================================
        var straightStart = new Node
        {
            Type = "input",
            Data = "Start",
            Position = new Core.Point(100, 350),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var straightEnd = new Node
        {
            Type = "output",
            Data = "End",
            Position = new Core.Point(350, 410),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        // ============================================
        // GROUP 2: Step Processing (with ports)
        // ============================================
        var stepStart = new Node
        {
            Type = "Step",
            Position = new Core.Point(100, 570),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var stepEnd = new Node
        {
            Type = "Step End",
            Position = new Core.Point(350, 620),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        // Calculate group2 bounds
        var group2Bounds = CalculateGroupBounds([stepStart, stepEnd]);
        var group2 = new Node
        {
            Id = "group2",
            Type = "group",
            IsGroup = true,
            Label = "Step Processing",
            Position = group2Bounds.position,
            Width = group2Bounds.width,
            Height = group2Bounds.height,
            // This group also has ports
            Inputs = [new Port { Id = "step-in", Type = "data", Label = "In" }],
            Outputs = [new Port { Id = "step-out", Type = "data", Label = "Out" }]
        };

        // Set parent after creating group
        stepStart.ParentGroupId = "group2";
        stepEnd.ParentGroupId = "group2";

        // Ungrouped nodes
        var smoothStepStart = new Node
        {
            Type = "SmoothStep",
            Position = new Core.Point(600, 570),
            Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
        };

        var smoothStepEnd = new Node
        {
            Type = "output",
            Data = "Final",
            Position = new Core.Point(850, 620),
            Inputs = [new Port { Id = "in", Type = "data", Label = "In" }]
        };

        // Add groups first (they render behind children)
        MyGraph.AddNode(group1);
        MyGraph.AddNode(group2);

        // Add external nodes
        MyGraph.AddNode(externalInput);
        MyGraph.AddNode(externalOutput);

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

        // ============================================
        // EDGES: Internal to group1
        // ============================================
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

        // ============================================
        // EDGES: External connections to group1
        // ============================================
        // External node -> Group input port
        MyGraph.AddEdge(new Edge
        {
            Source = externalInput.Id,
            Target = group1.Id,
            SourcePort = "out",
            TargetPort = "group-in",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.Arrow,
            Label = "To Group"
        });

        // Group output port -> External node
        MyGraph.AddEdge(new Edge
        {
            Source = group1.Id,
            Target = externalOutput.Id,
            SourcePort = "group-out",
            TargetPort = "in",
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.Arrow,
            Label = "From Group"
        });

        // ============================================
        // EDGES: Ungrouped demonstrations
        // ============================================
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

        // ============================================
        // EDGE: Connect group2 output to smoothStepStart
        // This demonstrates group-to-node connectivity
        // ============================================
        // Note: smoothStepStart only has outputs, so we need a different target
        // Let's create an edge from group2 to the external area
        MyGraph.AddEdge(new Edge
        {
            Source = group2.Id,
            Target = externalOutput.Id,
            SourcePort = "step-out",
            TargetPort = "in", // Connect to externalOutput's only port
            Type = EdgeType.Bezier,
            MarkerEnd = EdgeMarker.Arrow,
            Label = "Group→External"
        });
    }

    private static (Core.Point position, double width, double height) CalculateGroupBounds(Node[] nodes)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in nodes)
        {
            var nodeWidth = node.Width ?? DefaultNodeWidth;
            var nodeHeight = node.Height ?? DefaultNodeHeight;

            minX = Math.Min(minX, node.Position.X);
            minY = Math.Min(minY, node.Position.Y);
            maxX = Math.Max(maxX, node.Position.X + nodeWidth);
            maxY = Math.Max(maxY, node.Position.Y + nodeHeight);
        }

        var position = new Core.Point(minX - GroupPadding, minY - GroupPadding - GroupHeaderHeight);
        var width = maxX - minX + GroupPadding * 2;
        var height = maxY - minY + GroupPadding * 2 + GroupHeaderHeight;

        return (position, width, height);
    }
}

