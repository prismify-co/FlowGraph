using System.Reflection;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;
using FlowGraph.Core.DataFlow.Processors;

namespace FlowGraph.Core.Tests;

public class DataFlowTests
{
    #region ReactivePort Tests

    [Fact]
    public void ReactivePort_InitialValue_IsSet()
    {
        var port = new ReactivePort<int>("test", 42);

        Assert.Equal(42, port.TypedValue);
        Assert.Equal("test", port.PortId);
        Assert.Equal(typeof(int), port.ValueType);
    }

    [Fact]
    public void ReactivePort_SetValue_RaisesValueChanged()
    {
        var port = new ReactivePort<string>("test", "initial");
        string? oldValue = null;
        string? newValue = null;

        port.TypedValueChanged += (s, e) =>
        {
            oldValue = e.TypedOldValue;
            newValue = e.TypedNewValue;
        };

        port.TypedValue = "changed";

        Assert.Equal("initial", oldValue);
        Assert.Equal("changed", newValue);
    }

    [Fact]
    public void ReactivePort_SetSameValue_DoesNotRaiseEvent()
    {
        var port = new ReactivePort<int>("test", 42);
        var eventCount = 0;

        port.TypedValueChanged += (s, e) => eventCount++;

        port.TypedValue = 42;

        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void ReactivePort_SetValueUntyped_Works()
    {
        var port = new ReactivePort<double>("test");

        port.SetValue(3.14);

        Assert.Equal(3.14, port.TypedValue);
    }

    [Fact]
    public void ReactivePort_NullValue_HandledCorrectly()
    {
        var port = new ReactivePort<string?>("test", "initial");

        port.TypedValue = null;

        Assert.Null(port.TypedValue);
    }

    #endregion

    #region NodeProcessor Tests

    [Fact]
    public void InputNodeProcessor_OutputsValue()
    {
        var node = new Node { Type = "input" };
        var processor = new InputNodeProcessor<int>(node, "out", 100);

        Assert.Equal(100, processor.Value);
        Assert.Single(processor.OutputValues);
        Assert.Empty(processor.InputValues);
    }

    [Fact]
    public void InputNodeProcessor_SetValue_UpdatesOutput()
    {
        var node = new Node { Type = "input" };
        var processor = new InputNodeProcessor<int>(node, "out", 0);

        processor.Value = 50;

        Assert.Equal(50, processor.OutputValues["out"].Value);
    }

    [Fact]
    public void OutputNodeProcessor_ReceivesValue()
    {
        var node = new Node { Type = "output" };
        var processor = new OutputNodeProcessor<string>(node, "in");
        string? receivedValue = null;

        processor.ValueReceived += (s, v) => receivedValue = v;
        processor.InputPort.TypedValue = "test";

        Assert.Equal("test", processor.Value);
        Assert.Equal("test", receivedValue);
    }

    [Fact]
    public void TransformProcessor_TransformsValue()
    {
        var node = new Node { Type = "transform" };
        var processor = new TransformProcessor<int, string>(
            node,
            value => $"Value: {value}");

        processor.InputValues["in"].SetValue(42);

        Assert.Equal("Value: 42", processor.OutputValue);
    }

    [Fact]
    public void CombineProcessor_CombinesTwoValues()
    {
        var node = new Node { Type = "combine" };
        var processor = new CombineProcessor<int, int, int>(
            node,
            (a, b) => a + b);

        processor.InputValues["in1"].SetValue(10);
        processor.InputValues["in2"].SetValue(20);

        Assert.Equal(30, processor.OutputValue);
    }

    [Fact]
    public void MathProcessor_Add_Works()
    {
        var node = new Node { Type = "math" };
        var processor = new MathProcessor(node, MathOperation.Add);

        processor.InputValues["a"].SetValue(5.0);
        processor.InputValues["b"].SetValue(3.0);

        Assert.Equal(8.0, processor.Result);
    }

    [Fact]
    public void MathProcessor_Divide_Works()
    {
        var node = new Node { Type = "math" };
        var processor = new MathProcessor(node, MathOperation.Divide);

        processor.InputValues["a"].SetValue(10.0);
        processor.InputValues["b"].SetValue(4.0);

        Assert.Equal(2.5, processor.Result);
    }

    [Fact]
    public void MathProcessor_DivideByZero_ReturnsZero()
    {
        var node = new Node { Type = "math" };
        var processor = new MathProcessor(node, MathOperation.Divide);

        processor.InputValues["a"].SetValue(10.0);
        processor.InputValues["b"].SetValue(0.0);

        Assert.Equal(0.0, processor.Result);
    }

    [Fact]
    public void NodeProcessor_AutoExecute_TriggersOnInputChange()
    {
        var node = new Node { Type = "transform" };
        var processCount = 0;

        var processor = new TestAutoExecuteProcessor(node, () => processCount++);

        processor.InputValues["in"].SetValue(1);
        processor.InputValues["in"].SetValue(2);
        processor.InputValues["in"].SetValue(3);

        Assert.Equal(3, processCount);
    }

    private class TestAutoExecuteProcessor : NodeProcessor
    {
        private readonly Action _onProcess;

        public TestAutoExecuteProcessor(Node node, Action onProcess) : base(node)
        {
            _onProcess = onProcess;
            RegisterInput<int>("in");
        }

        public override void Process()
        {
            _onProcess();
        }
    }

    #endregion

    #region GraphExecutor Tests

    [Fact]
    public void GraphExecutor_RegisterProcessor_Tracks()
    {
        var graph = new Graph();
        var node = new Node { Type = "input" };
        graph.AddNode(node);

        using var executor = new GraphExecutor(graph);
        var processor = new InputNodeProcessor<int>(node, "out", 0);

        executor.RegisterProcessor(processor);

        Assert.Single(executor.Processors);
        Assert.Same(processor, executor.GetProcessor(node.Id));
    }

    [Fact]
    public void GraphExecutor_UnregisterProcessor_Removes()
    {
        var graph = new Graph();
        var node = new Node { Type = "input" };
        graph.AddNode(node);

        using var executor = new GraphExecutor(graph);
        var processor = new InputNodeProcessor<int>(node, "out", 0);
        executor.RegisterProcessor(processor);

        var removed = executor.UnregisterProcessor(node.Id);

        Assert.True(removed);
        Assert.Empty(executor.Processors);
    }

    [Fact]
    public void GraphExecutor_ValuePropagation_ThroughEdges()
    {
        var graph = new Graph();
        var inputNode = new Node { Type = "input", Outputs = [new Port { Id = "out", Type = "int" }] };
        var outputNode = new Node { Type = "output", Inputs = [new Port { Id = "in", Type = "int" }] };
        graph.AddNode(inputNode);
        graph.AddNode(outputNode);
        graph.AddEdge(new Edge { Source = inputNode.Id, Target = outputNode.Id, SourcePort = "out", TargetPort = "in" });

        using var executor = new GraphExecutor(graph);
        var inputProcessor = new InputNodeProcessor<int>(inputNode, "out", 0);
        var outputProcessor = new OutputNodeProcessor<int>(outputNode, "in");

        executor.RegisterProcessor(inputProcessor);
        executor.RegisterProcessor(outputProcessor);

        // Change input value - should auto-propagate to output
        inputProcessor.Value = 100;

        Assert.Equal(100, outputProcessor.Value);
    }

    [Fact]
    public void GraphExecutor_ChainedNodes_PropagatesCorrectly()
    {
        var graph = new Graph();

        var inputNode = new Node { Type = "input", Outputs = [new Port { Id = "out", Type = "int" }] };
        var transformNode = new Node
        {
            Type = "transform",
            Inputs = [new Port { Id = "in", Type = "int" }],
            Outputs = [new Port { Id = "out", Type = "int" }]
        };
        var outputNode = new Node { Type = "output", Inputs = [new Port { Id = "in", Type = "int" }] };

        graph.AddNode(inputNode);
        graph.AddNode(transformNode);
        graph.AddNode(outputNode);
        graph.AddEdge(new Edge { Source = inputNode.Id, Target = transformNode.Id, SourcePort = "out", TargetPort = "in" });
        graph.AddEdge(new Edge { Source = transformNode.Id, Target = outputNode.Id, SourcePort = "out", TargetPort = "in" });

        using var executor = new GraphExecutor(graph);

        var inputProcessor = new InputNodeProcessor<int>(inputNode, "out", 0);
        var transformProcessor = new TransformProcessor<int, int>(transformNode, v => v * 2);
        var outputProcessor = new OutputNodeProcessor<int>(outputNode, "in");

        executor.RegisterProcessor(inputProcessor);
        executor.RegisterProcessor(transformProcessor);
        executor.RegisterProcessor(outputProcessor);

        // Set input value - should propagate through transform to output
        inputProcessor.Value = 5;

        // 5 goes to transform, transform outputs 5*2=10, 10 goes to output
        Assert.Equal(10, outputProcessor.Value);
    }

    [Fact]
    public void GraphExecutor_BatchUpdate_DefersExecution()
    {
        var graph = new Graph();
        var inputNode = new Node { Type = "input", Outputs = [new Port { Id = "out", Type = "int" }] };
        var outputNode = new Node { Type = "output", Inputs = [new Port { Id = "in", Type = "int" }] };
        graph.AddNode(inputNode);
        graph.AddNode(outputNode);
        graph.AddEdge(new Edge { Source = inputNode.Id, Target = outputNode.Id, SourcePort = "out", TargetPort = "in" });

        using var executor = new GraphExecutor(graph);
        var inputProcessor = new InputNodeProcessor<int>(inputNode, "out", 0);
        var outputProcessor = new OutputNodeProcessor<int>(outputNode, "in");
        var processCount = 0;
        outputProcessor.ValueReceived += (s, v) => processCount++;

        executor.RegisterProcessor(inputProcessor);
        executor.RegisterProcessor(outputProcessor);

        executor.BeginBatchUpdate();
        inputProcessor.Value = 10;
        inputProcessor.Value = 20;
        inputProcessor.Value = 30;

        Assert.Equal(0, processCount); // Should not have processed yet

        executor.EndBatchUpdate();

        Assert.True(processCount > 0); // Should have processed at least once
    }

    [Fact]
    public void GraphExecutor_ExecuteAll_ProcessesAllNodes()
    {
        var graph = new Graph();
        var inputNode = new Node { Type = "input", Outputs = [new Port { Id = "out", Type = "int" }] };
        var outputNode = new Node { Type = "output", Inputs = [new Port { Id = "in", Type = "int" }] };
        graph.AddNode(inputNode);
        graph.AddNode(outputNode);
        graph.AddEdge(new Edge { Source = inputNode.Id, Target = outputNode.Id, SourcePort = "out", TargetPort = "in" });

        using var executor = new GraphExecutor(graph);
        var inputProcessor = new InputNodeProcessor<int>(inputNode, "out", 42);
        var outputProcessor = new OutputNodeProcessor<int>(outputNode, "in");
        int? receivedValue = null;
        outputProcessor.ValueReceived += (s, v) => receivedValue = v;

        executor.RegisterProcessor(inputProcessor);
        executor.RegisterProcessor(outputProcessor);

        executor.PropagateFromPort(inputNode.Id, "out");

        Assert.Equal(42, receivedValue);
    }

    [Fact]
    public void GraphExecutor_RaisesEvents()
    {
        var graph = new Graph();
        var node = new Node { Type = "input" };
        graph.AddNode(node);

        using var executor = new GraphExecutor(graph);
        var processor = new InputNodeProcessor<int>(node, "out", 0);
        executor.RegisterProcessor(processor);

        var started = false;
        var completed = false;
        var processedNodeId = "";

        executor.ExecutionStarted += (s, e) => started = true;
        executor.ExecutionCompleted += (s, e) => completed = true;
        executor.NodeProcessed += (s, e) => processedNodeId = e.NodeId;

        executor.ExecuteAll();

        Assert.True(started);
        Assert.True(completed);
        Assert.Equal(node.Id, processedNodeId);
    }

    [Fact]
    public void GraphExecutor_DisposedExecutor_ClearsState()
    {
        var graph = new Graph();
        var executor = new GraphExecutor(graph);
        executor.Dispose();

        Assert.Empty(executor.Processors);
    }

    #endregion
}
