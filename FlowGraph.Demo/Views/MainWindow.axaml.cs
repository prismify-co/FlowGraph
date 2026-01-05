using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Animation;
using FlowGraph.Avalonia.Controls;
using FlowGraph.Core;
using FlowGraph.Demo.Helpers;
using CorePoint = FlowGraph.Core.Point;

namespace FlowGraph.Demo.Views;

public partial class MainWindow : Window
{
    private EdgeFlowAnimation? _activeFlowAnimation;
    private FlowDirection _flowDirection = FlowDirection.Off;
    private AnimationDebugger? _debugger;
    private bool _debugModeEnabled = false;
    
    private enum FlowDirection { Off, Forward, Reverse }

    public MainWindow()
    {
        InitializeComponent();
        
        // Disable built-in grid and background since we're using FlowBackground
        FlowCanvas.Settings.ShowGrid = false;
        FlowCanvas.Settings.ShowBackground = false;
        
        // Set the target canvas for FlowBackground (must be done after InitializeComponent)
        FlowBackground.TargetCanvas = FlowCanvas;
        
        // Subscribe to label edit requests (double-click or context menu rename)
        FlowCanvas.NodeLabelEditRequested += OnNodeLabelEditRequested;
        
        // Initialize the animation debugger after the window is loaded
        this.Loaded += (_, _) =>
        {
            _debugger = new AnimationDebugger(FlowCanvas);
            _debugger.IsEnabled = false; // Disabled by default
        };
    }

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
        var selectedNodes = FlowCanvas.Graph?.Nodes.Where(n => n.IsSelected && !n.IsGroup).ToList();
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
        var selectedNode = FlowCanvas.Graph?.Nodes.FirstOrDefault(n => n.IsSelected && !n.IsGroup);
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
        var selectedNodes = FlowCanvas.Graph?.Nodes.Where(n => n.IsSelected && !n.IsGroup).ToList();
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
        var selectedNodes = FlowCanvas.Graph?.Nodes.Where(n => n.IsSelected && !n.IsGroup).ToList();
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

        _flowDirection = _flowDirection switch
        {
            FlowDirection.Off => FlowDirection.Forward,
            FlowDirection.Forward => FlowDirection.Reverse,
            FlowDirection.Reverse => FlowDirection.Off,
            _ => FlowDirection.Off
        };

        if (_flowDirection == FlowDirection.Off)
        {
            FlowCanvas.RefreshEdges();
            UpdateFlowButton();
            SetStatus("Flow stopped");
        }
        else
        {
            bool reverse = _flowDirection == FlowDirection.Reverse;
            _activeFlowAnimation = FlowCanvas.StartEdgeFlowAnimation(edge, speed: 50, reverse: reverse);
            UpdateFlowButton();
            SetStatus(reverse ? "Flow: Reverse" : "Flow: Forward");
        }
    }

    private void UpdateFlowButton()
    {
        FlowText.Text = _flowDirection switch
        {
            FlowDirection.Forward => ">",
            FlowDirection.Reverse => "<",
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
        var selected = FlowCanvas.Graph?.Edges.FirstOrDefault(e => e.IsSelected);
        return selected ?? FlowCanvas.Graph?.Edges.FirstOrDefault();
    }

    #endregion

    #region Group Animations

    private void OnGroupCollapseClick(object? sender, RoutedEventArgs e)
    {
        var selectedGroup = FlowCanvas.Graph?.Nodes.FirstOrDefault(n => n.IsSelected && n.IsGroup && !n.IsCollapsed);
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
        var selectedGroup = FlowCanvas.Graph?.Nodes.FirstOrDefault(n => n.IsSelected && n.IsGroup && n.IsCollapsed);
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

        var group = graph.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null || group.IsCollapsed) return;

        // Get children and connected edges for logging
        var children = graph.GetGroupChildren(groupId).ToList();
        var childIds = children.Select(c => c.Id).ToHashSet();
        var connectedEdges = graph.Edges
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
        _flowDirection = FlowDirection.Off;
        UpdateFlowButton();
        FlowCanvas.Refresh();
        SetStatus("Stopped");
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
        var selectedNode = FlowCanvas.Graph?.Nodes.FirstOrDefault(n => n.IsSelected);
        if (selectedNode != null)
        {
            FlowCanvas.CenterOnNodeAnimated(selectedNode, duration: 0.3);
            SetStatus("Centered on selection");
        }
    }

    #endregion
}