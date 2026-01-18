using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Core;
using FlowGraph.Core.Models;
using FlowGraph.Demo.Theme;
using System.Collections.Immutable;
using System.Diagnostics;
using static FlowGraph.Demo.Theme.DesignTokens;
using CorePoint = FlowGraph.Core.Point;

namespace FlowGraph.Demo.Pages;

/// <summary>
/// Demonstrates stress testing with large node counts.
/// </summary>
public class PerformanceDemoPage : IDemoPage
{
    public string Title => "Performance Test";
    public string Description => "Large graph stress testing";

    private FlowCanvas? _canvas;
    private FlowBackground? _background;
    private Graph? _graph;
    private TextBlock? _statusText;
    private string? _lastRenderDebug;

    public Control CreateContent()
    {
        _graph = new Graph();

        var mainPanel = new DockPanel();

        // Header
        var header = CreateSectionHeader(
            "Performance Stress Test",
            "Test FlowGraph rendering performance with varying node counts. " +
            "Direct rendering mode is automatically enabled for graphs ≥500 nodes, " +
            "bypassing the visual tree for GPU-accelerated performance."
        );
        DockPanel.SetDock(header, Dock.Top);
        mainPanel.Children.Add(header);

        // Toolbar
        var toolbar = CreateToolbar();
        DockPanel.SetDock(toolbar, Dock.Top);
        mainPanel.Children.Add(toolbar);

        // Status bar
        var statusBar = CreateStatusBar(out _statusText);
        DockPanel.SetDock(statusBar, Dock.Bottom);
        mainPanel.Children.Add(statusBar);

        // Canvas with background and controls
        var canvasPanel = new Panel();

        _background = new FlowBackground
        {
            Variant = BackgroundVariant.Dots,
            Gap = 20,
            Size = 2,
            Color = Color.Parse("#40808080")
        };
        canvasPanel.Children.Add(_background);

        _canvas = new FlowCanvas
        {
            Graph = _graph,
            Settings = new FlowCanvasSettings
            {
                ShowGrid = false,
                ShowBackground = false,
                SnapToGrid = true,
                PanOnDrag = true
            }
        };

        // Subscribe to debug output
        _canvas.DebugOutput += msg => Dispatcher.UIThread.Post(() =>
        {
            Debug.WriteLine(msg);
            if (msg.Contains("[RenderGraph]"))
            {
                _lastRenderDebug = msg;
            }
        });

        _background.TargetCanvas = _canvas;
        canvasPanel.Children.Add(_canvas);

        // Add FlowControls panel (positioned bottom-left)
        var flowControls = new FlowControls
        {
            TargetCanvas = _canvas,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16)
        };
        canvasPanel.Children.Add(flowControls);

        // Add FlowMinimap (positioned bottom-right)
        var minimap = new FlowMinimap
        {
            TargetCanvas = _canvas,
            Width = 150,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16)
        };
        canvasPanel.Children.Add(minimap);

        var canvasContainer = CreateCanvasContainer(canvasPanel);
        mainPanel.Children.Add(canvasContainer);

        SetStatus("Click a button to generate nodes");

        return mainPanel;
    }

    private WrapPanel CreateToolbar()
    {
        var panel = new WrapPanel
        {
            Margin = new Thickness(0, SpaceSm)
        };

        panel.Children.Add(CreateButton("100 Nodes", () => GenerateStressTestGraph(100)));
        panel.Children.Add(CreateButton("500 Nodes", () => GenerateStressTestGraph(500)));
        panel.Children.Add(CreateButton("1K Nodes", () => GenerateStressTestGraph(1000)));
        panel.Children.Add(CreateButton("5K Nodes", () => GenerateStressTestGraph(5000)));
        panel.Children.Add(CreateButton("Clear", ClearGraph));

        return panel;
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.Text = message;
    }

    private void ClearGraph()
    {
        if (_graph == null || _canvas == null) return;

        // Disable direct rendering before clearing
        _canvas.DisableDirectRendering();

        // Get snapshot and count
        var allElements = _graph.Elements.ToList();
        var count = allElements.Count;

        // Remove all elements using batch API (single notification)
        _graph.RemoveElements(allElements);

        _canvas.Refresh();
        SetStatus($"Graph cleared - removed {count} elements");
    }

    private async void GenerateStressTestGraph(int nodeCount)
    {
        if (_graph == null || _canvas == null) return;

        SetStatus($"Generating {nodeCount} nodes...");

        // Enable debug output for performance analysis
        _canvas.DebugRenderingPerformance = true;
        _lastRenderDebug = null;

        // Always enable direct rendering for stress tests (fixes panning and viewport culling)
        // Visual tree mode has issues with empty canvas hit testing and performance
        _canvas.EnableDirectRendering();

        // Clear all existing elements using bulk removal (single notification instead of N notifications)
        var existingElements = _graph.Elements.ToList();
        if (existingElements.Count > 0)
        {
            _graph.RemoveElements(existingElements);
        }

        var sw = Stopwatch.StartNew();
        var random = new Random(42); // Fixed seed for reproducibility

        // Calculate grid layout
        var cols = (int)Math.Ceiling(Math.Sqrt(nodeCount * 2)); // Wider than tall
        var spacingX = 200.0;
        var spacingY = 120.0;

        var nodeTypes = new[] { "input", "Process", "output", "default" };

        // Generate nodes into a temporary list first (avoids ObservableCollection notifications)
        var nodesList = new List<Node>(nodeCount);
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        for (int i = 0; i < nodeCount; i++)
        {
            var col = i % cols;
            var row = i / cols;

            // Add some randomness to positions
            var offsetX = random.Next(-20, 21);
            var offsetY = random.Next(-20, 21);

            var nodeType = nodeTypes[random.Next(nodeTypes.Length)];
            var inputs = nodeType != "input"
                ? ImmutableList.Create(new PortDefinition { Id = "in", Type = "data" })
                : ImmutableList<PortDefinition>.Empty;
            var outputs = nodeType != "output"
                ? ImmutableList.Create(new PortDefinition { Id = "out", Type = "data" })
                : ImmutableList<PortDefinition>.Empty;

            var definition = new NodeDefinition
            {
                Id = $"node-{i}",
                Type = nodeType,
                Label = $"N{i}",
                Inputs = inputs,
                Outputs = outputs
            };

            var x = col * spacingX + offsetX;
            var y = row * spacingY + offsetY;

            // Track bounds during generation
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x + _canvas.Settings.NodeWidth);
            maxY = Math.Max(maxY, y + _canvas.Settings.NodeHeight);

            var state = new NodeState { X = x, Y = y };
            var node = new Node(definition, state);

            nodesList.Add(node);
        }

        var dataGenTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Generate edges using spatial grid for O(1) neighbor lookup instead of O(n) per node
        var edgesList = new List<Edge>();

        // Build spatial grid: each cell contains nodes in that grid position
        var gridCellSize = Math.Max(spacingX, spacingY);
        var grid = new Dictionary<(int, int), List<Node>>();

        foreach (var node in nodesList.Where(n => n.Inputs.Count > 0))
        {
            var cellX = (int)(node.Position.X / gridCellSize);
            var cellY = (int)(node.Position.Y / gridCellSize);
            var key = (cellX, cellY);

            if (!grid.ContainsKey(key))
                grid[key] = new List<Node>();
            grid[key].Add(node);
        }

        // For each node with outputs, query neighboring grid cells
        foreach (var node in nodesList.Where(n => n.Outputs.Count > 0))
        {
            var cellX = (int)(node.Position.X / gridCellSize);
            var cellY = (int)(node.Position.Y / gridCellSize);

            // Check current cell and 8 neighboring cells
            var candidates = new List<Node>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var key = (cellX + dx, cellY + dy);
                    if (grid.TryGetValue(key, out var cellNodes))
                        candidates.AddRange(cellNodes);
                }
            }

            // Filter to nearby nodes (within 1.5x spacing) and pick 1-2 random targets
            var nearbyNodes = candidates
                .Where(n => n.Id != node.Id)
                .Where(n => Math.Abs(n.Position.X - node.Position.X) < spacingX * 1.5 &&
                           Math.Abs(n.Position.Y - node.Position.Y) < spacingY * 1.5)
                .OrderBy(n => random.Next()) // Random instead of distance sort (faster)
                .Take(random.Next(1, 3))
                .ToList();

            foreach (var target in nearbyNodes)
            {
                edgesList.Add(new Edge
                {
                    Source = node.Id,
                    Target = target.Id,
                    SourcePort = "out",
                    TargetPort = "in",
                    Type = EdgeType.Bezier
                });
            }
        }

        // Post-process: Connect orphan nodes to ensure full graph coverage
        var connectedAsSource = new HashSet<string>(edgesList.Select(e => e.Source));
        var connectedAsTarget = new HashSet<string>(edgesList.Select(e => e.Target));

        // Find orphan nodes (nodes with no incoming AND no outgoing edges)
        var orphanNodes = nodesList
            .Where(n => !connectedAsSource.Contains(n.Id) && !connectedAsTarget.Contains(n.Id))
            .ToList();

        foreach (var orphan in orphanNodes)
        {
            // Find nearest compatible node to connect to
            Node? bestTarget = null;
            double bestDist = double.MaxValue;

            if (orphan.Outputs.Count > 0)
            {
                // Orphan has outputs - find nearest node with inputs
                foreach (var candidate in nodesList.Where(n => n.Id != orphan.Id && n.Inputs.Count > 0))
                {
                    var dist = Math.Abs(candidate.Position.X - orphan.Position.X) +
                               Math.Abs(candidate.Position.Y - orphan.Position.Y);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestTarget = candidate;
                    }
                }

                if (bestTarget != null)
                {
                    edgesList.Add(new Edge
                    {
                        Source = orphan.Id,
                        Target = bestTarget.Id,
                        SourcePort = "out",
                        TargetPort = "in",
                        Type = EdgeType.Bezier
                    });
                }
            }
            else if (orphan.Inputs.Count > 0)
            {
                // Orphan has inputs only - find nearest node with outputs
                foreach (var candidate in nodesList.Where(n => n.Id != orphan.Id && n.Outputs.Count > 0))
                {
                    var dist = Math.Abs(candidate.Position.X - orphan.Position.X) +
                               Math.Abs(candidate.Position.Y - orphan.Position.Y);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestTarget = candidate;
                    }
                }

                if (bestTarget != null)
                {
                    edgesList.Add(new Edge
                    {
                        Source = bestTarget.Id,
                        Target = orphan.Id,
                        SourcePort = "out",
                        TargetPort = "in",
                        Type = EdgeType.Bezier
                    });
                }
            }
            // Note: Nodes with neither inputs nor outputs remain orphans (by design)
        }

        var edgeGenTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Use bulk add methods to avoid triggering UI updates for each item
        _graph.AddNodes(nodesList);
        _graph.AddEdges(edgesList);

        var collectionAddTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Debug: Log graph layout stats
        Debug.WriteLine($"[StressTest] Generated graph: {nodeCount} nodes, {edgesList.Count} edges");
        Debug.WriteLine($"[StressTest] Canvas bounds: X=[{minX:F0} to {maxX:F0}] Y=[{minY:F0} to {maxY:F0}] = {maxX - minX:F0}x{maxY - minY:F0}");
        Debug.WriteLine($"[StressTest] View size: {_canvas.Bounds.Width:F0}x{_canvas.Bounds.Height:F0}");

        // Fit to view using pre-calculated bounds (no need to iterate nodes again)
        _canvas.FitToView();

        var fitTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Now render - this should be fast with direct rendering
        SetStatus($"Rendering {nodeCount}n/{edgesList.Count}e...");
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        _canvas.Refresh();

        var renderTime = sw.ElapsedMilliseconds;
        sw.Stop();

        // Disable debug output after test
        _canvas.DebugRenderingPerformance = false;

        var total = dataGenTime + edgeGenTime + collectionAddTime + fitTime + renderTime;

        // Show detailed timing in status
        var status = $"{nodeCount}n/{edgesList.Count}e - D:{dataGenTime}ms E:{edgeGenTime}ms C:{collectionAddTime}ms F:{fitTime}ms R:{renderTime}ms = {total}ms";
        SetStatus(status);

        // Show in Output window (View > Output in VS, select "Debug" from dropdown)
        Debug.WriteLine($"\n=== STRESS TEST RESULTS ===");
        Debug.WriteLine($"Nodes: {nodeCount}, Edges: {edgesList.Count}, DirectRendering: True");
        Debug.WriteLine($"Data Gen:       {dataGenTime}ms");
        Debug.WriteLine($"Edge Gen:       {edgeGenTime}ms");
        Debug.WriteLine($"Collection Add: {collectionAddTime}ms");
        Debug.WriteLine($"Fit To View:    {fitTime}ms");
        Debug.WriteLine($"Render:         {renderTime}ms");
        Debug.WriteLine($"TOTAL:          {total}ms");
        if (_lastRenderDebug != null)
        {
            Debug.WriteLine($"Details:        {_lastRenderDebug}");
        }
        Debug.WriteLine("===========================\n");
    }
}
