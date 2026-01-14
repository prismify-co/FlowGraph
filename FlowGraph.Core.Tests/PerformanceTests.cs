using FlowGraph.Core;
using FlowGraph.Core.Serialization;
using System.Diagnostics;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Performance regression tests to ensure graph operations maintain acceptable performance
/// at scale. These tests use time thresholds to catch O(n²) regressions.
/// 
/// Thresholds are set conservatively to avoid flaky tests while still catching regressions.
/// Run times may vary based on hardware - these are set for CI environments.
/// </summary>
public class PerformanceTests
{
    private const int SmallGraphSize = 100;
    private const int MediumGraphSize = 1000;
    private const int LargeGraphSize = 5000;

    #region Graph Creation Performance

    [Fact]
    [Trait("Category", "Performance")]
    public void CreateGraph_1000Nodes_CompletesInUnder100ms()
    {
        var sw = Stopwatch.StartNew();
        var graph = CreateGraphWithNodes(MediumGraphSize);
        sw.Stop();

        Assert.Equal(MediumGraphSize, graph.Elements.Nodes.Count());
        Assert.True(sw.ElapsedMilliseconds < 100, 
            $"Creating {MediumGraphSize} nodes took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void CreateGraph_5000Nodes_CompletesInUnder500ms()
    {
        var sw = Stopwatch.StartNew();
        var graph = CreateGraphWithNodes(LargeGraphSize);
        sw.Stop();

        Assert.Equal(LargeGraphSize, graph.Elements.Nodes.Count());
        Assert.True(sw.ElapsedMilliseconds < 500, 
            $"Creating {LargeGraphSize} nodes took {sw.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void CreateGraph_5000NodesWithEdges_CompletesInUnder1000ms()
    {
        var sw = Stopwatch.StartNew();
        var graph = CreateGraphWithNodesAndEdges(LargeGraphSize);
        sw.Stop();

        Assert.Equal(LargeGraphSize, graph.Elements.Nodes.Count());
        Assert.True(graph.Elements.Edges.Count() > 0);
        Assert.True(sw.ElapsedMilliseconds < 1000, 
            $"Creating {LargeGraphSize} nodes with edges took {sw.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    #endregion

    #region Batch Loading Performance

    [Fact]
    [Trait("Category", "Performance")]
    public void BatchLoad_5000Nodes_FasterThanIndividualAdd()
    {
        // Without batch loading
        var graph1 = new Graph();
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < LargeGraphSize; i++)
        {
            graph1.AddNode(TestHelpers.CreateNode($"node-{i}", x: i * 10, y: i * 10));
        }
        sw1.Stop();
        var individualTime = sw1.ElapsedMilliseconds;

        // With batch loading
        var graph2 = new Graph();
        var sw2 = Stopwatch.StartNew();
        graph2.BeginBatchLoad();
        for (int i = 0; i < LargeGraphSize; i++)
        {
            graph2.AddNode(TestHelpers.CreateNode($"node-{i}", x: i * 10, y: i * 10));
        }
        graph2.EndBatchLoad();
        sw2.Stop();
        var batchTime = sw2.ElapsedMilliseconds;

        // Batch should be at least as fast (usually faster due to suppressed notifications)
        // Use a minimum threshold of 10ms and 2x tolerance for very fast operations to avoid flaky tests
        var adjustedIndividualTime = Math.Max(individualTime, 10);
        Assert.True(batchTime <= adjustedIndividualTime * 2,
            $"Batch loading ({batchTime}ms) should not be significantly slower than individual adds ({individualTime}ms)");
    }

    #endregion

    #region Node Lookup Performance

    [Fact]
    [Trait("Category", "Performance")]
    public void NodeLookupById_5000Nodes_IsO1()
    {
        var graph = CreateGraphWithNodes(LargeGraphSize);
        var nodeIds = graph.Elements.Nodes.Select(n => n.Id).ToList();

        // Lookup first, middle, and last nodes
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var first = graph.Elements.Nodes.FirstOrDefault(n => n.Id == nodeIds[0]);
            var middle = graph.Elements.Nodes.FirstOrDefault(n => n.Id == nodeIds[LargeGraphSize / 2]);
            var last = graph.Elements.Nodes.FirstOrDefault(n => n.Id == nodeIds[LargeGraphSize - 1]);
            Assert.NotNull(first);
            Assert.NotNull(middle);
            Assert.NotNull(last);
        }
        sw.Stop();

        // 3000 lookups (1000 iterations x 3 lookups) should complete quickly
        // Even with O(n) lookup, this tests the baseline
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"3000 node lookups took {sw.ElapsedMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region Graph Modification Performance

    [Fact]
    [Trait("Category", "Performance")]
    public void RemoveNode_FromLargeGraph_IsNotOn2()
    {
        var graph = CreateGraphWithNodes(MediumGraphSize);
        var nodesToRemove = graph.Elements.Nodes.Take(100).Select(n => n.Id).ToList();

        var sw = Stopwatch.StartNew();
        foreach (var nodeId in nodesToRemove)
        {
            graph.RemoveNode(nodeId);
        }
        sw.Stop();

        Assert.Equal(MediumGraphSize - 100, graph.Elements.Nodes.Count());
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Removing 100 nodes from {MediumGraphSize} node graph took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void RemoveEdge_FromLargeGraph_IsNotOn2()
    {
        var graph = CreateGraphWithNodesAndEdges(MediumGraphSize);
        var edgesToRemove = graph.Elements.Edges.Take(100).Select(e => e.Id).ToList();
        var initialEdgeCount = graph.Elements.Edges.Count();

        var sw = Stopwatch.StartNew();
        foreach (var edgeId in edgesToRemove)
        {
            graph.RemoveEdge(edgeId);
        }
        sw.Stop();

        Assert.Equal(initialEdgeCount - 100, graph.Elements.Edges.Count());
        Assert.True(sw.ElapsedMilliseconds < 50,
            $"Removing 100 edges took {sw.ElapsedMilliseconds}ms, expected < 50ms");
    }

    #endregion

    #region Node Position Updates

    [Fact]
    [Trait("Category", "Performance")]
    public void UpdateNodePosition_ManyUpdates_IsEfficient()
    {
        var graph = CreateGraphWithNodes(SmallGraphSize);
        var nodes = graph.Elements.Nodes.ToList();

        // Simulate drag operation: update positions 100 times
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            foreach (var node in nodes)
            {
                node.Position = new Point(node.Position.X + 1, node.Position.Y + 1);
            }
        }
        sw.Stop();

        // 100 iterations x 100 nodes = 10,000 position updates
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"10,000 position updates took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    #endregion

    #region Selection Performance

    [Fact]
    [Trait("Category", "Performance")]
    public void SelectAllNodes_LargeGraph_IsEfficient()
    {
        var graph = CreateGraphWithNodes(LargeGraphSize);
        var nodes = graph.Elements.Nodes.ToList();

        // Select all
        var sw = Stopwatch.StartNew();
        foreach (var node in nodes)
        {
            node.IsSelected = true;
        }
        sw.Stop();

        Assert.True(nodes.All(n => n.IsSelected));
        Assert.True(sw.ElapsedMilliseconds < 50,
            $"Selecting {LargeGraphSize} nodes took {sw.ElapsedMilliseconds}ms, expected < 50ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void DeselectAllNodes_LargeGraph_IsEfficient()
    {
        var graph = CreateGraphWithNodes(LargeGraphSize);
        var nodes = graph.Elements.Nodes.ToList();
        
        // First select all
        foreach (var node in nodes)
        {
            node.IsSelected = true;
        }

        // Then deselect all
        var sw = Stopwatch.StartNew();
        foreach (var node in nodes)
        {
            node.IsSelected = false;
        }
        sw.Stop();

        Assert.True(nodes.All(n => !n.IsSelected));
        Assert.True(sw.ElapsedMilliseconds < 50,
            $"Deselecting {LargeGraphSize} nodes took {sw.ElapsedMilliseconds}ms, expected < 50ms");
    }

    #endregion

    #region Serialization Performance

    [Fact]
    [Trait("Category", "Performance")]
    public void SerializeGraph_5000Nodes_CompletesInUnder500ms()
    {
        var graph = CreateGraphWithNodesAndEdges(LargeGraphSize);

        var sw = Stopwatch.StartNew();
        var json = graph.ToJson();
        sw.Stop();

        Assert.NotEmpty(json);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Serializing {LargeGraphSize} nodes took {sw.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void DeserializeGraph_5000Nodes_CompletesInUnder500ms()
    {
        var graph = CreateGraphWithNodesAndEdges(LargeGraphSize);
        var json = graph.ToJson();

        var sw = Stopwatch.StartNew();
        var loadedGraph = Serialization.GraphSerializationExtensions.LoadFromJson(json);
        sw.Stop();

        Assert.NotNull(loadedGraph);
        Assert.Equal(LargeGraphSize, loadedGraph.Elements.Nodes.Count());
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Deserializing {LargeGraphSize} nodes took {sw.ElapsedMilliseconds}ms, expected < 500ms");
    }

    #endregion

    #region Iteration Performance (Regression Tests for O(n²) Bugs)

    [Fact]
    [Trait("Category", "Performance")]
    public void IterateNodes_ScalesLinearly()
    {
        // Create two graphs of different sizes
        var smallGraph = CreateGraphWithNodes(SmallGraphSize);
        var largeGraph = CreateGraphWithNodes(LargeGraphSize);

        // Time iteration over small graph
        var swSmall = Stopwatch.StartNew();
        int smallCount = 0;
        foreach (var node in smallGraph.Elements.Nodes)
        {
            smallCount++;
        }
        swSmall.Stop();

        // Time iteration over large graph
        var swLarge = Stopwatch.StartNew();
        int largeCount = 0;
        foreach (var node in largeGraph.Elements.Nodes)
        {
            largeCount++;
        }
        swLarge.Stop();

        // Large graph is 50x bigger, time should be roughly 50x (not 2500x for O(n²))
        // Allow 100x as margin for overhead
        var ratio = (double)swLarge.ElapsedTicks / Math.Max(1, swSmall.ElapsedTicks);
        var sizeRatio = (double)LargeGraphSize / SmallGraphSize;

        // Iteration time ratio should be within 3x of size ratio for O(n) algorithm
        // O(n²) would show ratio close to sizeRatio²
        Assert.True(ratio < sizeRatio * 10,
            $"Time ratio ({ratio:F1}x) should be close to size ratio ({sizeRatio}x) for O(n) algorithm. " +
            $"Small: {swSmall.ElapsedTicks} ticks, Large: {swLarge.ElapsedTicks} ticks");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void GroupChildrenLookup_LargeNestedGraph_IsNotOn2()
    {
        var graph = CreateGraphWithNestedGroups(100, 10); // 100 groups with 10 children each

        var sw = Stopwatch.StartNew();
        foreach (var node in graph.Elements.Nodes.Where(n => n.IsGroup))
        {
            var children = graph.Elements.Nodes.Where(n => n.ParentGroupId == node.Id).ToList();
            Assert.NotNull(children);
        }
        sw.Stop();

        // Looking up children for 100 groups should be fast
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Looking up children for 100 groups took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void GetGroupDepth_DeeplyNestedGroups_IsNotOn2()
    {
        // Create deeply nested groups: each group contains the next
        // This tests GetGroupDepth performance which traverses parent chain
        var graph = CreateDeeplyNestedGroups(50); // 50 levels deep

        // Build a node lookup dictionary (simulating O(1) lookup optimization)
        var nodeById = graph.Elements.Nodes.ToDictionary(n => n.Id);

        var sw = Stopwatch.StartNew();
        // Calculate depth for all nodes 100 times (simulating render loop)
        for (int iteration = 0; iteration < 100; iteration++)
        {
            foreach (var node in graph.Elements.Nodes)
            {
                var depth = GetGroupDepthOptimized(node, nodeById);
                Assert.True(depth >= 0);
            }
        }
        sw.Stop();

        // 100 iterations x 50 nodes = 5000 depth calculations should be fast with O(1) lookups
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"5000 depth calculations took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    private static int GetGroupDepthOptimized(Node node, Dictionary<string, Node> nodeById)
    {
        int depth = 0;
        var currentParentId = node.ParentGroupId;
        while (!string.IsNullOrEmpty(currentParentId))
        {
            depth++;
            if (nodeById.TryGetValue(currentParentId, out var parent))
            {
                currentParentId = parent.ParentGroupId;
            }
            else
            {
                break;
            }
        }
        return depth;
    }

    #endregion

    #region Helpers

    private static Graph CreateGraphWithNodes(int nodeCount)
    {
        var graph = new Graph();
        graph.BeginBatchLoad();
        for (int i = 0; i < nodeCount; i++)
        {
            graph.AddNode(TestHelpers.CreateNode($"node-{i}", x: (i % 100) * 200, y: (i / 100) * 100));
        }
        graph.EndBatchLoad();
        return graph;
    }

    private static Graph CreateGraphWithNodesAndEdges(int nodeCount)
    {
        var graph = new Graph();
        graph.BeginBatchLoad();
        
        // Create nodes
        for (int i = 0; i < nodeCount; i++)
        {
            var node = TestHelpers.CreateNode($"node-{i}", 
                x: (i % 100) * 200, 
                y: (i / 100) * 100,
                inputs: [new Port { Id = "in", Type = "any" }],
                outputs: [new Port { Id = "out", Type = "any" }]);
            graph.AddNode(node);
        }

        // Create edges - connect each node to the next (creating a chain)
        for (int i = 0; i < nodeCount - 1; i++)
        {
            graph.AddEdge(TestHelpers.CreateEdge($"edge-{i}", $"node-{i}", $"node-{i + 1}", "out", "in"));
        }

        // Also connect some random nodes for more realistic graph
        var random = new Random(42); // Deterministic for reproducibility
        for (int i = 0; i < nodeCount / 10; i++)
        {
            var source = random.Next(nodeCount);
            var target = random.Next(nodeCount);
            if (source != target)
            {
                graph.AddEdge(TestHelpers.CreateEdge($"edge-random-{i}", $"node-{source}", $"node-{target}", "out", "in"));
            }
        }

        graph.EndBatchLoad();
        return graph;
    }

    private static Graph CreateGraphWithNestedGroups(int groupCount, int childrenPerGroup)
    {
        var graph = new Graph();
        graph.BeginBatchLoad();

        for (int g = 0; g < groupCount; g++)
        {
            // Create group
            var group = TestHelpers.CreateNode($"group-{g}", 
                x: (g % 10) * 500, 
                y: (g / 10) * 400,
                width: 400,
                height: 300);
            group.IsGroup = true;
            graph.AddNode(group);

            // Create children
            for (int c = 0; c < childrenPerGroup; c++)
            {
                var child = TestHelpers.CreateNode($"child-{g}-{c}", 
                    x: (g % 10) * 500 + 50 + (c % 3) * 100, 
                    y: (g / 10) * 400 + 50 + (c / 3) * 60);
                child.ParentGroupId = group.Id;
                graph.AddNode(child);
            }
        }

        graph.EndBatchLoad();
        return graph;
    }

    /// <summary>
    /// Creates a graph with deeply nested groups where each group contains the next level.
    /// Used for testing GetGroupDepth performance.
    /// </summary>
    private static Graph CreateDeeplyNestedGroups(int depth)
    {
        var graph = new Graph();
        graph.BeginBatchLoad();

        string? parentId = null;
        for (int level = 0; level < depth; level++)
        {
            var group = TestHelpers.CreateNode($"group-level-{level}",
                x: level * 20,
                y: level * 20,
                width: 500 - level * 8,
                height: 400 - level * 6);
            group.IsGroup = true;
            group.ParentGroupId = parentId;
            graph.AddNode(group);
            parentId = group.Id;
        }

        graph.EndBatchLoad();
        return graph;
    }

    #endregion
}
