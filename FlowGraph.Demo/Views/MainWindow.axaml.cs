using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Animation;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;
using FlowGraph.Core.Models;
using System.Collections.Immutable;
using FlowGraph.Demo.Helpers;
using FlowGraph.Demo.Renderers;
using CorePoint = FlowGraph.Core.Point;

namespace FlowGraph.Demo.Views;

public partial class MainWindow : Window
{
    private EdgeFlowAnimation? _activeFlowAnimation;
    private EdgeFlowDirection _edgeFlowDirection = EdgeFlowDirection.Off;
    private AnimationDebugger? _debugger;
    private bool _debugModeEnabled = false;

    private enum EdgeFlowDirection { Off, Forward, Reverse }

    public MainWindow()
    {
        InitializeComponent();

        // Disable built-in grid and background since we're using FlowBackground
        FlowCanvas.Settings.ShowGrid = false;
        FlowCanvas.Settings.ShowBackground = false;

        // Register custom node renderers for the React Flow-style demo
        RegisterCustomNodeRenderers();

        // Set the target canvas for FlowBackground (must be done after InitializeComponent)
        FlowBackground.TargetCanvas = FlowCanvas;

        // Subscribe to label edit requests (double-click or context menu rename)
        FlowCanvas.NodeLabelEditRequested += OnNodeLabelEditRequested;
        FlowCanvas.EdgeLabelEditRequested += OnEdgeLabelEditRequested;

        // Subscribe to debug output
        FlowCanvas.DebugOutput += msg => Dispatcher.UIThread.Post(() =>
        {
            System.Diagnostics.Debug.WriteLine(msg);
            // Append to status for visibility
            if (msg.Contains("[RenderGraph]"))
            {
                _lastRenderDebug = msg;
            }
        });

        // Initialize the animation debugger after the window is loaded
        this.Loaded += (_, _) =>
        {
            _debugger = new AnimationDebugger(FlowCanvas);
            _debugger.IsEnabled = false; // Disabled by default

            // Enable data flow for the 3D demo - use Dispatcher to ensure layout is complete
            // Use Background priority to ensure rendering has completed
            Dispatcher.UIThread.Post(() => SetupDataFlow(), DispatcherPriority.Background);
        };
    }

    private void SetupDataFlow()
    {
        if (FlowCanvas.Graph == null) return;

        // Enable data flow processing
        var executor = FlowCanvas.EnableDataFlow();

        // Create processors for input nodes
        var colorNode = FlowCanvas.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == "shape-color");
        var shapeNode = FlowCanvas.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == "shape-type");
        var zoomNode = FlowCanvas.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == "zoom-level");
        var outputNode = FlowCanvas.Graph.Elements.Nodes.FirstOrDefault(n => n.Id == "output");

        if (colorNode != null)
        {
            FlowCanvas.CreateInputProcessor<Color>(colorNode, "color", Color.FromRgb(255, 0, 113));
        }

        if (shapeNode != null)
        {
            FlowCanvas.CreateInputProcessor<string>(shapeNode, "type", "cube");
        }

        if (zoomNode != null)
        {
            FlowCanvas.CreateInputProcessor<double>(zoomNode, "zoom", 50.0);
        }

        if (outputNode != null)
        {
            var processor = new MultiInputOutputProcessor(outputNode);
            FlowCanvas.RegisterProcessor(processor);
        }

        // Execute initial graph state
        FlowCanvas.ExecuteGraph();
    }

    private void RegisterCustomNodeRenderers()
    {
        // Register the React Flow-style node renderers
        FlowCanvas.NodeRenderers.Register("colorpicker", new ColorPickerNodeRenderer());
        FlowCanvas.NodeRenderers.Register("radiobutton", new RadioButtonNodeRenderer());
        FlowCanvas.NodeRenderers.Register("zoomslider", new ZoomSliderNodeRenderer());
        FlowCanvas.NodeRenderers.Register("outputdisplay", new OutputDisplayNodeRenderer());

        // Register the 3D output renderer
        FlowCanvas.NodeRenderers.Register("output3d", new Output3DNodeRenderer());
    }

    private string? _lastRenderDebug;

    #region Node Rename

    private void OnNodeLabelEditRequested(object? sender, NodeLabelEditRequestedEventArgs e)
    {
        // Use the built-in inline editing via the renderer
        var success = FlowCanvas.BeginEditNodeLabel(e.Node);
        if (success)
        {
            e.Handled = true;
            SetStatus("Press Enter to save, Escape to cancel");
        }
        else
        {
            // Fallback message if renderer doesn't support editing
            SetStatus("This node type doesn't support label editing");
        }
    }

    #endregion

    #region Edge Rename

    private void OnEdgeLabelEditRequested(object? sender, EdgeLabelEditRequestedEventArgs e)
    {
        // Use the built-in inline editing
        var success = FlowCanvas.BeginEditEdgeLabel(e.Edge);
        if (success)
        {
            e.Handled = true;
            SetStatus("Press Enter to save, Escape to cancel");
        }
        else
        {
            SetStatus("Failed to edit edge label");
        }
    }

    #endregion

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    #region Background Variants

    private void OnBackgroundDotsClick(object? sender, RoutedEventArgs e)
    {
        FlowBackground.Variant = BackgroundVariant.Dots;
        FlowBackground.Size = 2;
        FlowBackground.LineWidth = 1;
        SetStatus("Background: Dots");
    }

    private void OnBackgroundLinesClick(object? sender, RoutedEventArgs e)
    {
        FlowBackground.Variant = BackgroundVariant.Lines;
        FlowBackground.LineWidth = 0.5;
        SetStatus("Background: Lines");
    }

    private void OnBackgroundCrossClick(object? sender, RoutedEventArgs e)
    {
        FlowBackground.Variant = BackgroundVariant.Cross;
        FlowBackground.Size = 4;
        FlowBackground.LineWidth = 1;
        SetStatus("Background: Cross");
    }

    #endregion

    #region Viewport Animations

    private void OnFitAnimatedClick(object? sender, RoutedEventArgs e)
    {
        SetStatus("Fitting to view...");
        FlowCanvas.FitToViewAnimated(duration: 0.5, easing: Easing.EaseOutCubic);
    }

    private void OnCenterAnimatedClick(object? sender, RoutedEventArgs e)
    {
        SetStatus("Centering...");
        FlowCanvas.CenterOnGraphAnimated(duration: 0.4, easing: Easing.EaseOutBack);
    }

    private void OnZoomInAnimatedClick(object? sender, RoutedEventArgs e)
    {
        var targetZoom = Math.Min(FlowCanvas.CurrentZoom + 0.3, 2.0);
        SetStatus($"Zoom {targetZoom:P0}");
        FlowCanvas.ZoomToAnimated(targetZoom, duration: 0.25);
    }

    private void OnZoomOutAnimatedClick(object? sender, RoutedEventArgs e)
    {
        var targetZoom = Math.Max(FlowCanvas.CurrentZoom - 0.3, 0.2);
        SetStatus($"Zoom {targetZoom:P0}");
        FlowCanvas.ZoomToAnimated(targetZoom, duration: 0.25);
    }

    #endregion

    #region Node Animations

    private void OnAnimateNodeClick(object? sender, RoutedEventArgs e)
    {
        var selectedNodes = FlowCanvas.Graph?.Elements.Nodes.Where(n => n.IsSelected && !n.IsGroup).ToList();
        if (selectedNodes == null || selectedNodes.Count == 0)
        {
            SetStatus("Select nodes first");
            return;
        }

        SetStatus($"Moving {selectedNodes.Count} node(s)");
        var random = new Random();
        var positions = new Dictionary<Node, CorePoint>();

        foreach (var node in selectedNodes)
        {
            positions[node] = new CorePoint(
                node.Position.X + random.Next(-50, 51),
                node.Position.Y + random.Next(-50, 51));
        }

        FlowCanvas.AnimateNodesTo(positions, duration: 0.5, easing: Easing.EaseOutElastic);
    }

    private void OnCenterOnNodeClick(object? sender, RoutedEventArgs e)
    {
        var selectedNode = FlowCanvas.Graph?.Elements.Nodes.FirstOrDefault(n => n.IsSelected && !n.IsGroup);
        if (selectedNode == null)
        {
            SetStatus("Select a node first");
            return;
        }

        SetStatus("Centering on node");
        FlowCanvas.CenterOnNodeAnimated(selectedNode, duration: 0.4, easing: Easing.EaseOutCubic);
    }

    private void OnNodePulseClick(object? sender, RoutedEventArgs e)
    {
        var selectedNodes = FlowCanvas.Graph?.Elements.Nodes.Where(n => n.IsSelected && !n.IsGroup).ToList();
        if (selectedNodes == null || selectedNodes.Count == 0)
        {
            SetStatus("Select nodes first");
            return;
        }

        SetStatus($"Pulsing {selectedNodes.Count} node(s)");
        foreach (var node in selectedNodes)
        {
            FlowCanvas.AnimateSelectionPulse(node);
        }
    }

    private void OnNodePopClick(object? sender, RoutedEventArgs e)
    {
        var selectedNodes = FlowCanvas.Graph?.Elements.Nodes.Where(n => n.IsSelected && !n.IsGroup).ToList();
        if (selectedNodes == null || selectedNodes.Count == 0)
        {
            SetStatus("Select nodes first");
            return;
        }

        SetStatus("Pop effect...");
        FlowCanvas.AnimateNodesDisappear(selectedNodes, duration: 0.15, stagger: 0.03, onComplete: () =>
        {
            FlowCanvas.AnimateNodesAppear(selectedNodes, duration: 0.3, stagger: 0.05, onComplete: () =>
            {
                Dispatcher.UIThread.Post(() => SetStatus("Ready"));
            });
        });
    }

    #endregion

    #region Edge Animations

    private void OnEdgePulseClick(object? sender, RoutedEventArgs e)
    {
        var edge = GetSelectedOrFirstEdge();
        if (edge == null)
        {
            SetStatus("No edges available");
            return;
        }

        SetStatus("Pulsing edge...");
        FlowCanvas.AnimateEdgePulse(edge, pulseCount: 3, onComplete: () =>
        {
            Dispatcher.UIThread.Post(() => SetStatus("Ready"));
        });
    }

    private void OnEdgeFadeClick(object? sender, RoutedEventArgs e)
    {
        var edge = GetSelectedOrFirstEdge();
        if (edge == null)
        {
            SetStatus("No edges available");
            return;
        }

        SetStatus("Fading edge...");
        FlowCanvas.AnimateEdgeFadeOut(edge, duration: 0.4, onComplete: () =>
        {
            FlowCanvas.AnimateEdgeFadeIn(edge, duration: 0.4, onComplete: () =>
            {
                Dispatcher.UIThread.Post(() => SetStatus("Ready"));
            });
        });
    }

    private void OnEdgeFlowClick(object? sender, RoutedEventArgs e)
    {
        var edge = GetSelectedOrFirstEdge();
        if (edge == null)
        {
            SetStatus("No edges available");
            return;
        }

        if (_activeFlowAnimation != null)
        {
            FlowCanvas.StopEdgeFlowAnimation(_activeFlowAnimation);
            _activeFlowAnimation = null;
        }

        _edgeFlowDirection = _edgeFlowDirection switch
        {
            EdgeFlowDirection.Off => EdgeFlowDirection.Forward,
            EdgeFlowDirection.Forward => EdgeFlowDirection.Reverse,
            EdgeFlowDirection.Reverse => EdgeFlowDirection.Off,
            _ => EdgeFlowDirection.Off
        };

        if (_edgeFlowDirection == EdgeFlowDirection.Off)
        {
            FlowCanvas.RefreshEdges();
            UpdateFlowButton();
            SetStatus("Flow stopped");
        }
        else
        {
            bool reverse = _edgeFlowDirection == EdgeFlowDirection.Reverse;
            _activeFlowAnimation = FlowCanvas.StartEdgeFlowAnimation(edge, speed: 50, reverse: reverse);
            UpdateFlowButton();
            SetStatus(reverse ? "Flow: Reverse" : "Flow: Forward");
        }
    }

    private void UpdateFlowButton()
    {
        FlowText.Text = _edgeFlowDirection switch
        {
            EdgeFlowDirection.Forward => ">",
            EdgeFlowDirection.Reverse => "<",
            _ => "Flow"
        };
    }

    private void OnEdgeColorClick(object? sender, RoutedEventArgs e)
    {
        var edge = GetSelectedOrFirstEdge();
        if (edge == null)
        {
            SetStatus("No edges available");
            return;
        }

        var colors = new[]
        {
            Color.FromRgb(231, 76, 60),   // Red
            Color.FromRgb(241, 196, 15),  // Yellow
            Color.FromRgb(46, 204, 113),  // Green
            Color.FromRgb(52, 152, 219),  // Blue
            Color.FromRgb(155, 89, 182),  // Purple
            Color.FromRgb(230, 126, 34),  // Orange
        };
        var random = new Random();
        var targetColor = colors[random.Next(colors.Length)];

        SetStatus("Changing color...");
        FlowCanvas.AnimateEdgeColor(edge, targetColor, duration: 0.5, onComplete: () =>
        {
            Task.Delay(600).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    FlowCanvas.AnimateEdgeColor(edge, Color.FromRgb(128, 128, 128), duration: 0.5, onComplete: () =>
                    {
                        Dispatcher.UIThread.Post(() => SetStatus("Ready"));
                    });
                });
            });
        });
    }

    private Edge? GetSelectedOrFirstEdge()
    {
        var selected = FlowCanvas.Graph?.Elements.Edges.FirstOrDefault(e => e.IsSelected);
        return selected ?? FlowCanvas.Graph?.Elements.Edges.FirstOrDefault();
    }

    #endregion

    #region Group Animations

    private void OnGroupCollapseClick(object? sender, RoutedEventArgs e)
    {
        var selectedGroup = FlowCanvas.Graph?.Elements.Nodes.FirstOrDefault(n => n.IsSelected && n.IsGroup && !n.IsCollapsed);
        if (selectedGroup == null)
        {
            SetStatus("Select an expanded group");
            return;
        }

        if (_debugModeEnabled && _debugger != null)
        {
            // Debug mode - capture screenshots during animation
            SetStatus("Collapsing group (DEBUG)...");
            _debugger.StartSession($"GroupCollapse_{selectedGroup.Label ?? selectedGroup.Id}");
            _debugger.CaptureFrame("Init", 0, "before_start");

            AnimateGroupCollapseWithDebug(selectedGroup.Id, 0.6, async () =>
            {
                _debugger.CaptureFrame("Complete", 1.0, "finished");
                await _debugger.EndSessionAsync();
                Dispatcher.UIThread.Post(() => SetStatus("Group collapsed (GIF saved)"));
            });
        }
        else
        {
            SetStatus("Collapsing group...");
            FlowCanvas.AnimateGroupCollapse(selectedGroup.Id, duration: 0.6, onComplete: () =>
            {
                Dispatcher.UIThread.Post(() => SetStatus("Group collapsed"));
            });
        }
    }

    private void OnGroupExpandClick(object? sender, RoutedEventArgs e)
    {
        var selectedGroup = FlowCanvas.Graph?.Elements.Nodes.FirstOrDefault(n => n.IsSelected && n.IsGroup && n.IsCollapsed);
        if (selectedGroup == null)
        {
            SetStatus("Select a collapsed group");
            return;
        }

        if (_debugModeEnabled && _debugger != null)
        {
            // Debug mode - capture screenshots during animation
            SetStatus("Expanding group (DEBUG)...");
            _debugger.StartSession($"GroupExpand_{selectedGroup.Label ?? selectedGroup.Id}");
            _debugger.CaptureFrame("Init", 0, "before_start");

            AnimateGroupExpandWithDebug(selectedGroup.Id, 0.6, async () =>
            {
                _debugger.CaptureFrame("Complete", 1.0, "finished");
                await _debugger.EndSessionAsync();
                Dispatcher.UIThread.Post(() => SetStatus("Group expanded (GIF saved)"));
            });
        }
        else
        {
            SetStatus("Expanding group...");
            FlowCanvas.AnimateGroupExpand(selectedGroup.Id, duration: 0.6, onComplete: () =>
            {
                Dispatcher.UIThread.Post(() => SetStatus("Group expanded"));
            });
        }
    }

    /// <summary>
    /// Animates group collapse with screenshot capture at key frames.
    /// </summary>
    private void AnimateGroupCollapseWithDebug(string groupId, double duration, Func<Task> onComplete)
    {
        var graph = FlowCanvas.Graph;
        if (graph == null) return;

        var group = graph.Elements.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null || group.IsCollapsed) return;

        // Get children and connected edges for logging
        var children = graph.GetGroupChildren(groupId).ToList();
        var childIds = children.Select(c => c.Id).ToHashSet();
        var connectedEdges = graph.Elements.Edges
            .Where(e => childIds.Contains(e.Source) || childIds.Contains(e.Target))
            .ToList();

        System.Diagnostics.Debug.WriteLine($"[GroupCollapseDebug] Children: {children.Count}, Edges: {connectedEdges.Count}");

        // Use a slower animation for better screenshot capture
        var actualDuration = duration * 2; // Double the duration for debug
        var contentFadeDuration = actualDuration * 0.5;
        var shrinkDuration = actualDuration * 0.5;

        int frameCount = 0;

        // Phase 1: Content fade with screenshots
        var contentFadeAnimation = new FlowGraph.Avalonia.Animation.GenericAnimation(
            contentFadeDuration,
            t =>
            {
                frameCount++;
                // Capture frames during animation
                if (frameCount % 3 == 0 || t < 0.05 || t > 0.95)
                {
                    _debugger?.CaptureFrame("ContentFade", t, null);
                }
            },
            onComplete: () =>
            {
                _debugger?.CaptureFrame("ContentFade", 1.0, "phase_complete");

                frameCount = 0;
                // Phase 2: Shrink with screenshots
                var shrinkAnimation = new FlowGraph.Avalonia.Animation.GenericAnimation(
                    shrinkDuration,
                    t =>
                    {
                        frameCount++;
                        if (frameCount % 3 == 0 || t < 0.05 || t > 0.95)
                        {
                            _debugger?.CaptureFrame("Shrink", t, null);
                        }
                    },
                    onComplete: () =>
                    {
                        _debugger?.CaptureFrame("Shrink", 1.0, "phase_complete");
                        _ = onComplete();
                    });
                FlowCanvas.Animations.Start(shrinkAnimation);
            });

        // Start with the actual animation (which handles the visual changes)
        FlowCanvas.AnimateGroupCollapse(groupId, actualDuration, onComplete: () => { });

        // Run our debug capture in parallel
        FlowCanvas.Animations.Start(contentFadeAnimation);
    }

    /// <summary>
    /// Animates group expand with screenshot capture at key frames.
    /// </summary>
    private void AnimateGroupExpandWithDebug(string groupId, double duration, Func<Task> onComplete)
    {
        // Use a slower animation for better screenshot capture
        var actualDuration = duration * 2; // Double the duration for debug

        int frameCount = 0;
        var expandDuration = actualDuration * 0.5;
        var contentFadeDuration = actualDuration * 0.5;

        // Phase 1: Expand with screenshots
        var expandAnimation = new FlowGraph.Avalonia.Animation.GenericAnimation(
            expandDuration,
            t =>
            {
                frameCount++;
                if (frameCount % 3 == 0 || t < 0.05 || t > 0.95)
                {
                    _debugger?.CaptureFrame("Expand", t, null);
                }
            },
            onComplete: () =>
            {
                _debugger?.CaptureFrame("Expand", 1.0, "phase_complete");

                frameCount = 0;
                // Phase 2: Content fade with screenshots
                var contentFadeAnimation = new FlowGraph.Avalonia.Animation.GenericAnimation(
                    contentFadeDuration,
                    t =>
                    {
                        frameCount++;
                        if (frameCount % 3 == 0 || t < 0.05 || t > 0.95)
                        {
                            _debugger?.CaptureFrame("ContentFade", t, null);
                        }
                    },
                    onComplete: () =>
                    {
                        _debugger?.CaptureFrame("ContentFade", 1.0, "phase_complete");
                        _ = onComplete();
                    });
                FlowCanvas.Animations.Start(contentFadeAnimation);
            });

        // Start with the actual animation
        FlowCanvas.AnimateGroupExpand(groupId, actualDuration, onComplete: () => { });

        // Run our debug capture in parallel
        FlowCanvas.Animations.Start(expandAnimation);
    }

    #endregion

    #region Debug Controls

    private void OnToggleDebugClick(object? sender, RoutedEventArgs e)
    {
        _debugModeEnabled = !_debugModeEnabled;
        if (_debugger != null)
        {
            _debugger.IsEnabled = _debugModeEnabled;
        }

        DebugText.Text = _debugModeEnabled ? "ON" : "Debug";
        SetStatus(_debugModeEnabled ? "Debug mode ON - screenshots will be captured" : "Debug mode OFF");
    }

    private void OnOpenDebugFolderClick(object? sender, RoutedEventArgs e)
    {
        _debugger?.OpenOutputDirectory();
        SetStatus("Opening debug folder...");
    }

    private void OnClearDebugClick(object? sender, RoutedEventArgs e)
    {
        _debugger?.ClearOutputDirectory();
        SetStatus("Debug screenshots cleared");
    }

    #endregion

    #region Controls

    private void OnStopAllClick(object? sender, RoutedEventArgs e)
    {
        FlowCanvas.StopAllAnimations();
        _activeFlowAnimation = null;
        _edgeFlowDirection = EdgeFlowDirection.Off;
        UpdateFlowButton();
        FlowCanvas.Refresh();
        SetStatus("Stopped");
    }

    #endregion

    #region Stress Test

    private void OnGenerate100Click(object? sender, RoutedEventArgs e) => GenerateStressTestGraph(100);
    private void OnGenerate500Click(object? sender, RoutedEventArgs e) => GenerateStressTestGraph(500);
    private void OnGenerate1000Click(object? sender, RoutedEventArgs e) => GenerateStressTestGraph(1000);
    private void OnGenerate5000Click(object? sender, RoutedEventArgs e) => GenerateStressTestGraph(5000);

    private void OnClearGraphClick(object? sender, RoutedEventArgs e)
    {
        var graph = FlowCanvas.Graph;
        if (graph == null) return;

        // Use legacy properties for Clear() - Elements returns IEnumerable
#pragma warning disable CS0618
        graph.Edges.Clear();
        graph.Nodes.Clear();
#pragma warning restore CS0618
        FlowCanvas.Refresh();
        SetStatus("Graph cleared");
    }

    private async void GenerateStressTestGraph(int nodeCount)
    {
        var graph = FlowCanvas.Graph;
        if (graph == null) return;

        SetStatus($"Generating {nodeCount} nodes...");

        // Enable debug output for performance analysis
        FlowCanvas.DebugRenderingPerformance = true;
        _lastRenderDebug = null;

        // For large graphs, enable direct rendering (GPU-accelerated, bypasses visual tree)
        if (nodeCount >= 500)
        {
            FlowCanvas.EnableDirectRendering();
        }
        else
        {
            FlowCanvas.DisableDirectRendering();
        }

        // Clear existing - Use legacy properties for Clear() - Elements returns IEnumerable
#pragma warning disable CS0618
        graph.Edges.Clear();
        graph.Nodes.Clear();
#pragma warning restore CS0618

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var random = new Random(42); // Fixed seed for reproducibility

        // Calculate grid layout
        var cols = (int)Math.Ceiling(Math.Sqrt(nodeCount * 2)); // Wider than tall
        var spacingX = 200.0;
        var spacingY = 120.0;

        var nodeTypes = new[] { "input", "Process", "output", "default" };

        // Generate nodes into a temporary list first (avoids ObservableCollection notifications)
        var nodesList = new List<Node>(nodeCount);
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

            var state = new NodeState
            {
                X = col * spacingX + offsetX,
                Y = row * spacingY + offsetY
            };

            var node = new Node(definition, state);

            nodesList.Add(node);
        }

        var dataGenTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Generate edges into a temporary list first (avoids ObservableCollection notifications)
        var edgesList = new List<Edge>();
        var nodesWithOutputs = nodesList.Where(n => n.Outputs.Count > 0).ToList();

        foreach (var node in nodesWithOutputs)
        {
            // Find nearby nodes that have inputs - only connect to immediate neighbors
            var nearbyNodes = nodesList
                .Where(n => n.Id != node.Id && n.Inputs.Count > 0)
                .Where(n => Math.Abs(n.Position.X - node.Position.X) < spacingX * 1.5 &&
                           Math.Abs(n.Position.Y - node.Position.Y) < spacingY * 1.5)
                .OrderBy(n => Math.Abs(n.Position.X - node.Position.X) + Math.Abs(n.Position.Y - node.Position.Y))
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

        var edgeGenTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Use bulk add methods to avoid triggering UI updates for each item
        graph.AddNodes(nodesList);
        graph.AddEdges(edgesList);

        var collectionAddTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Fit to view first (calculate bounds without rendering)
        FlowCanvas.FitToView();

        var fitTime = sw.ElapsedMilliseconds;
        sw.Restart();

        // Now render - this should be fast with direct rendering
        SetStatus($"Rendering {nodeCount}n/{edgesList.Count}e...");
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        FlowCanvas.Refresh();

        var renderTime = sw.ElapsedMilliseconds;
        sw.Stop();

        // Disable debug output after test
        FlowCanvas.DebugRenderingPerformance = false;

        var directMode = nodeCount >= 500 ? " [Direct]" : "";
        var total = dataGenTime + edgeGenTime + collectionAddTime + fitTime + renderTime;

        // Show detailed timing in status
        var status = $"{nodeCount}n/{edgesList.Count}e{directMode} - D:{dataGenTime}ms E:{edgeGenTime}ms C:{collectionAddTime}ms F:{fitTime}ms R:{renderTime}ms = {total}ms";
        SetStatus(status);

        // Show in Output window (View > Output in VS, select "Debug" from dropdown)
        System.Diagnostics.Debug.WriteLine($"\n=== STRESS TEST RESULTS ===");
        System.Diagnostics.Debug.WriteLine($"Nodes: {nodeCount}, Edges: {edgesList.Count}, DirectRendering: {nodeCount >= 500}");
        System.Diagnostics.Debug.WriteLine($"Data Gen:       {dataGenTime}ms");
        System.Diagnostics.Debug.WriteLine($"Edge Gen:       {edgeGenTime}ms");
        System.Diagnostics.Debug.WriteLine($"Collection Add: {collectionAddTime}ms");
        System.Diagnostics.Debug.WriteLine($"Fit To View:    {fitTime}ms");
        System.Diagnostics.Debug.WriteLine($"Render:         {renderTime}ms");
        System.Diagnostics.Debug.WriteLine($"TOTAL:          {total}ms");
        if (_lastRenderDebug != null)
        {
            System.Diagnostics.Debug.WriteLine($"Details:        {_lastRenderDebug}");
        }
        System.Diagnostics.Debug.WriteLine("===========================\n");
    }

    private static double Distance(CorePoint a, CorePoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    #endregion

    #region Node Toolbar Actions

    private void OnToolbarDeleteClick(object? sender, RoutedEventArgs e)
    {
        FlowCanvas.Selection.DeleteSelected();
        SetStatus("Deleted selected");
    }

    private void OnToolbarDuplicateClick(object? sender, RoutedEventArgs e)
    {
        FlowCanvas.Duplicate();
        SetStatus("Duplicated selected");
    }

    private void OnToolbarCenterClick(object? sender, RoutedEventArgs e)
    {
        var selectedNode = FlowCanvas.Graph?.Elements.Nodes.FirstOrDefault(n => n.IsSelected);
        if (selectedNode != null)
        {
            FlowCanvas.CenterOnNodeAnimated(selectedNode, duration: 0.3);
            SetStatus("Centered on selection");
        }
    }

    #endregion
}