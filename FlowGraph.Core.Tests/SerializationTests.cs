using FlowGraph.Core;
using FlowGraph.Core.Models;
using FlowGraph.Core.Serialization;
using System.Text.Json;

namespace FlowGraph.Core.Tests;

public class SerializationTests
{
    #region Basic Serialization Tests

    [Fact]
    public void Serialize_EmptyGraph_ReturnsValidJson()
    {
        var graph = new Graph();

        var json = graph.ToJson();

        Assert.NotEmpty(json);
        Assert.Contains("nodes", json);
        Assert.Contains("edges", json);
    }

    [Fact]
    public void Serialize_GraphWithNodes_IncludesAllNodes()
    {
        var graph = CreateTestGraph();

        var json = graph.ToJson();

        Assert.Contains("node1", json);
        Assert.Contains("node2", json);
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsGraph()
    {
        var originalGraph = CreateTestGraph();
        var json = originalGraph.ToJson();

        var graph = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(graph);
        Assert.Equal(originalGraph.Elements.Nodes.Count(), graph.Elements.Nodes.Count());
        Assert.Equal(originalGraph.Elements.Edges.Count(), graph.Elements.Edges.Count());
    }

    [Fact]
    public void Deserialize_InvalidJson_ReturnsNull()
    {
        var json = "{ invalid json }";

        var graph = GraphSerializationExtensions.LoadFromJson(json);

        Assert.Null(graph);
    }

    #endregion

    #region Node Serialization Tests

    [Fact]
    public void Serialize_NodeProperties_AllPropertiesIncluded()
    {
        var graph = new Graph();
        graph.AddNode(TestHelpers.CreateNode("test-node", type: "custom", label: "Test Node",
            x: 100, y: 200, width: 180, height: 90, data: "custom data"));

        var json = graph.ToJson();

        Assert.Contains("test-node", json);
        Assert.Contains("custom", json);
        Assert.Contains("Test Node", json);
        Assert.Contains("100", json);
        Assert.Contains("200", json);
    }

    [Fact]
    public void Serialize_NodePorts_PortsIncluded()
    {
        var graph = new Graph();
        graph.AddNode(TestHelpers.CreateNode("node1",
            inputs: [new Port { Id = "in1", Type = "data", Label = "Input" }],
            outputs: [new Port { Id = "out1", Type = "data", Label = "Output" }]));

        var json = graph.ToJson();

        Assert.Contains("in1", json);
        Assert.Contains("out1", json);
        Assert.Contains("Input", json);
        Assert.Contains("Output", json);
    }

    [Fact]
    public void RoundTrip_NodeWithPorts_PortsPreserved()
    {
        var graph = new Graph();
        graph.AddNode(TestHelpers.CreateNode("node1",
            inputs: [new Port { Id = "in1", Type = "data", Label = "Input" }],
            outputs: [
                new Port { Id = "out1", Type = "data", Label = "Output 1" },
                new Port { Id = "out2", Type = "control", Label = "Output 2" }
            ]));

        var json = graph.ToJson();
        var restored = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(restored);
        var node = restored.Elements.Nodes.First();
        Assert.Single(node.Inputs);
        Assert.Equal(2, node.Outputs.Count);
        Assert.Equal("in1", node.Inputs[0].Id);
        Assert.Equal("out1", node.Outputs[0].Id);
    }

    #endregion

    #region Edge Serialization Tests

    [Fact]
    public void Serialize_EdgeProperties_AllPropertiesIncluded()
    {
        var graph = CreateTestGraph();

        var json = graph.ToJson();

        Assert.Contains("source", json);
        Assert.Contains("target", json);
        Assert.Contains("sourcePort", json);
        Assert.Contains("targetPort", json);
    }

    [Fact]
    public void RoundTrip_EdgeWithLabel_LabelPreserved()
    {
        var graph = CreateTestGraph();
        graph.Elements.Edges.First().Label = "Test Label";

        var json = graph.ToJson();
        var restored = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(restored);
        Assert.Equal("Test Label", restored.Elements.Edges.First().Label);
    }

    [Fact]
    public void RoundTrip_EdgeType_TypePreserved()
    {
        var graph = CreateTestGraph();
        graph.Elements.Edges.First().Type = EdgeType.SmoothStep;

        var json = graph.ToJson();
        var restored = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(EdgeType.SmoothStep, restored.Elements.Edges.First().Type);
    }

    [Fact]
    public void RoundTrip_EdgeMarkers_MarkersPreserved()
    {
        var graph = CreateTestGraph();
        var edge = graph.Elements.Edges.First();
        edge.MarkerStart = EdgeMarker.Arrow;
        edge.MarkerEnd = EdgeMarker.ArrowClosed;

        var json = graph.ToJson();
        var restored = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(EdgeMarker.Arrow, restored.Elements.Edges.First().MarkerStart);
        Assert.Equal(EdgeMarker.ArrowClosed, restored.Elements.Edges.First().MarkerEnd);
    }

    [Fact]
    public void RoundTrip_EdgeWaypoints_WaypointsPreserved()
    {
        var graph = CreateTestGraph();
        graph.Elements.Edges.First().Waypoints = [
            new Point(200, 100),
            new Point(200, 200),
            new Point(300, 200)
        ];

        var json = graph.ToJson();
        var restored = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(restored);
        var waypoints = restored.Elements.Edges.First().Waypoints;
        Assert.NotNull(waypoints);
        Assert.Equal(3, waypoints.Count);
        Assert.Equal(200, waypoints[0].X);
    }

    #endregion

    #region Group Serialization Tests

    [Fact]
    public void RoundTrip_GroupNode_GroupPropertiesPreserved()
    {
        var graph = new Graph();
        var groupNode = TestHelpers.CreateNode("group1", type: "group", isGroup: true, label: "My Group",
            x: 50, y: 50, width: 400, height: 300);
        groupNode.IsCollapsed = true;
        graph.AddNode(groupNode);

        var json = graph.ToJson();
        var restored = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(restored);
        var group = restored.Elements.Nodes.First();
        Assert.True(group.IsGroup);
        Assert.True(group.IsCollapsed);
        Assert.Equal("My Group", group.Label);
        Assert.Equal(400, group.Width);
        Assert.Equal(300, group.Height);
    }

    [Fact]
    public void RoundTrip_NestedGroups_ParentIdPreserved()
    {
        var graph = new Graph();
        graph.AddNode(TestHelpers.CreateNode("group1", isGroup: true));
        graph.AddNode(TestHelpers.CreateNode("node1", parentGroupId: "group1"));

        var json = graph.ToJson();
        var restored = GraphSerializationExtensions.LoadFromJson(json);

        Assert.NotNull(restored);
        var childNode = restored.Elements.Nodes.First(n => n.Id == "node1");
        Assert.Equal("group1", childNode.ParentGroupId);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_Graph_CreatesDeepCopy()
    {
        var original = CreateTestGraph();

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.Elements.Nodes.Count(), clone.Elements.Nodes.Count());
        Assert.Equal(original.Elements.Edges.Count(), clone.Elements.Edges.Count());
    }

    [Fact]
    public void Clone_ModifyClone_OriginalUnchanged()
    {
        var original = CreateTestGraph();
        var originalNodeCount = original.Elements.Nodes.Count();

        var clone = original.Clone();
        clone.AddNode(TestHelpers.CreateNode("new-node"));

        Assert.Equal(originalNodeCount, original.Elements.Nodes.Count());
        Assert.Equal(originalNodeCount + 1, clone.Elements.Nodes.Count());
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void TryValidate_ValidJson_ReturnsTrue()
    {
        var graph = CreateTestGraph();
        var json = graph.ToJson();

        var isValid = GraphSerializer.TryValidate(json, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_InvalidJson_ReturnsFalse()
    {
        var json = "{ invalid }";

        var isValid = GraphSerializer.TryValidate(json, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    #endregion

    #region File Operations Tests

    [Fact]
    public void Save_AndLoad_RoundTrips()
    {
        var original = CreateTestGraph();
        var tempFile = Path.GetTempFileName();

        try
        {
            original.Save(tempFile);
            var loaded = GraphSerializationExtensions.LoadFromFile(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal(original.Elements.Nodes.Count(), loaded.Elements.Nodes.Count());
            Assert.Equal(original.Elements.Edges.Count(), loaded.Elements.Edges.Count());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAsync_AndLoadAsync_RoundTrips()
    {
        var original = CreateTestGraph();
        var tempFile = Path.GetTempFileName();

        try
        {
            await original.SaveAsync(tempFile);
            var loaded = await GraphSerializationExtensions.LoadFromFileAsync(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal(original.Elements.Nodes.Count(), loaded.Elements.Nodes.Count());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region JSON Format Tests

    [Fact]
    public void ToJson_CamelCaseNaming_Used()
    {
        var graph = CreateTestGraph();

        var json = graph.ToJson();

        // Should use camelCase, not PascalCase
        Assert.Contains("sourcePort", json);
        Assert.DoesNotContain("SourcePort", json);
    }

    [Fact]
    public void ToJson_EnumsAsStrings_Used()
    {
        var graph = CreateTestGraph();
        graph.Elements.Edges.First().Type = EdgeType.SmoothStep;

        var json = graph.ToJson();

        // Should serialize enum as camelCase string
        Assert.Contains("smoothStep", json);
    }

    [Fact]
    public void ToJson_Indented_FormattedNicely()
    {
        var graph = CreateTestGraph();

        var json = graph.ToJson(indented: true);

        Assert.Contains("\n", json);
        Assert.Contains("  ", json); // Indentation
    }

    [Fact]
    public void ToJson_NotIndented_Compact()
    {
        var graph = CreateTestGraph();

        var json = graph.ToJson(indented: false);

        Assert.DoesNotContain("\n  ", json);
    }

    #endregion

    #region Helper Methods

    private static Graph CreateTestGraph()
    {
        var graph = new Graph();

        graph.AddNode(TestHelpers.CreateNode("node1", type: "input", x: 100, y: 100,
            outputs: [new Port { Id = "out", Type = "data" }]));

        graph.AddNode(TestHelpers.CreateNode("node2", type: "output", x: 400, y: 100,
            inputs: [new Port { Id = "in", Type = "data" }]));

        graph.AddEdge(TestHelpers.CreateEdge("edge1", "node1", "node2", "out", "in"));

        return graph;
    }

    #endregion
}
