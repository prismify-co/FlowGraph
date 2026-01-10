using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using FlowGraph.Core;
using CorePoint = FlowGraph.Core.Point;

namespace FlowGraph.Avalonia.Controls;

/// <summary>
/// Provides context menu functionality for FlowCanvas.
/// </summary>
public class FlowCanvasContextMenu
{
    private readonly FlowCanvas _canvas;
    private ContextMenu? _nodeContextMenu;
    private ContextMenu? _edgeContextMenu;
    private ContextMenu? _canvasContextMenu;
    private ContextMenu? _groupContextMenu;
    private CorePoint _contextMenuPosition;
    private MenuItem? _addToGroupMenuItem;

    /// <summary>
    /// Event raised when context menu is about to be shown.
    /// Allows customization of menu items.
    /// </summary>
    public event EventHandler<ContextMenuEventArgs>? ContextMenuOpening;

    public FlowCanvasContextMenu(FlowCanvas canvas)
    {
        _canvas = canvas;
        InitializeMenus();
    }

    private void InitializeMenus()
    {
        // Create the "Add to Group" submenu item
        _addToGroupMenuItem = new MenuItem
        {
            Header = "Add to Group"
        };

        // Node context menu
        _nodeContextMenu = new ContextMenu
        {
            ItemsSource = new List<object>
            {
                CreateMenuItem("Rename", "F2", OnRenameNode),
                new Separator(),
                CreateMenuItem("Cut", "Ctrl+X", OnCut),
                CreateMenuItem("Copy", "Ctrl+C", OnCopy),
                CreateMenuItem("Duplicate", "Ctrl+D", OnDuplicate),
                new Separator(),
                CreateMenuItem("Delete", "Delete", OnDelete),
                new Separator(),
                CreateMenuItem("Group Selected", "Ctrl+G", OnGroupSelected, isEnabled: () => CanGroup()),
                _addToGroupMenuItem
            }
        };

        // Update "Add to Group" submenu when node context menu opens
        _nodeContextMenu.Opening += (s, e) => UpdateAddToGroupSubmenu();

        // Group context menu
        _groupContextMenu = new ContextMenu
        {
            ItemsSource = new List<object>
            {
                CreateMenuItem("Rename", "F2", OnRenameNode),
                new Separator(),
                CreateMenuItem("Expand", null, OnExpandGroup, isEnabled: () => IsGroupCollapsed()),
                CreateMenuItem("Collapse", null, OnCollapseGroup, isEnabled: () => !IsGroupCollapsed()),
                new Separator(),
                CreateMenuItem("Ungroup", "Ctrl+Shift+G", OnUngroup),
                CreateMenuItem("Auto-Resize", null, OnAutoResizeGroup),
                new Separator(),
                CreateMenuItem("Cut", "Ctrl+X", OnCut),
                CreateMenuItem("Copy", "Ctrl+C", OnCopy),
                CreateMenuItem("Delete", "Delete", OnDelete)
            }
        };

        // Edge context menu
        _edgeContextMenu = new ContextMenu
        {
            ItemsSource = new List<object>
            {
                CreateMenuItem("Delete Edge", "Delete", OnDeleteEdge),
                new Separator(),
                CreateSubMenu("Edge Type", new List<MenuItem>
                {
                    CreateMenuItem("Bezier", null, () => SetEdgeType(EdgeType.Bezier)),
                    CreateMenuItem("Straight", null, () => SetEdgeType(EdgeType.Straight)),
                    CreateMenuItem("Step", null, () => SetEdgeType(EdgeType.Step)),
                    CreateMenuItem("Smooth Step", null, () => SetEdgeType(EdgeType.SmoothStep))
                }),
                CreateSubMenu("Arrow", new List<MenuItem>
                {
                    CreateMenuItem("None", null, () => SetEdgeMarker(EdgeMarker.None)),
                    CreateMenuItem("Arrow", null, () => SetEdgeMarker(EdgeMarker.Arrow)),
                    CreateMenuItem("Arrow (Filled)", null, () => SetEdgeMarker(EdgeMarker.ArrowClosed))
                })
            }
        };

        // Canvas (empty area) context menu
        _canvasContextMenu = new ContextMenu
        {
            ItemsSource = new List<object>
            {
                CreateSubMenu("Add Node", new List<MenuItem>
                {
                    CreateMenuItem("Input Node", null, () => AddNode("input")),
                    CreateMenuItem("Process Node", null, () => AddNode("process")),
                    CreateMenuItem("Output Node", null, () => AddNode("output")),
                    CreateMenuItem("Default Node", null, () => AddNode("default"))
                }),
                new Separator(),
                CreateMenuItem("Paste", "Ctrl+V", OnPaste, isEnabled: () => CanPaste()),
                new Separator(),
                CreateMenuItem("Select All", "Ctrl+A", OnSelectAll),
                new Separator(),
                CreateMenuItem("Fit to View", null, OnFitToView),
                CreateMenuItem("Reset Zoom", "Ctrl+0", OnResetZoom),
                new Separator(),
                CreateMenuItem("Collapse All Groups", null, OnCollapseAllGroups),
                CreateMenuItem("Expand All Groups", null, OnExpandAllGroups)
            }
        };
    }

    private void UpdateAddToGroupSubmenu()
    {
        if (_addToGroupMenuItem == null) return;

        var graph = _canvas.Graph;
        if (graph == null)
        {
            _addToGroupMenuItem.IsEnabled = false;
            _addToGroupMenuItem.ItemsSource = null;
            return;
        }

        // Get all groups that we can add to (excluding groups that are selected)
        var selectedNodeIds = graph.Elements.Nodes.Where(n => n.IsSelected).Select(n => n.Id).ToHashSet();
        var availableGroups = graph.Elements.Nodes
            .Where(n => n.IsGroup && !n.IsSelected)
            .ToList();

        // Check if there are selected non-group nodes
        var hasSelectedNonGroupNodes = graph.Elements.Nodes.Any(n => n.IsSelected && !n.IsGroup);

        if (!hasSelectedNonGroupNodes || availableGroups.Count == 0)
        {
            _addToGroupMenuItem.IsEnabled = false;
            _addToGroupMenuItem.ItemsSource = null;
            return;
        }

        _addToGroupMenuItem.IsEnabled = true;

        // Create submenu items for each available group
        var groupMenuItems = availableGroups.Select(group =>
        {
            var groupLabel = !string.IsNullOrEmpty(group.Label) ? group.Label : $"Group ({group.Id[..Math.Min(8, group.Id.Length)]})";
            var item = new MenuItem { Header = groupLabel };
            item.Click += (s, e) => AddSelectedNodesToGroup(group.Id);
            return item;
        }).ToList();

        _addToGroupMenuItem.ItemsSource = groupMenuItems;
    }

    private void AddSelectedNodesToGroup(string groupId)
    {
        _canvas.AddSelectedToGroup(groupId);
    }

    private MenuItem CreateMenuItem(string header, string? gesture, Action action, Func<bool>? isEnabled = null)
    {
        var item = new MenuItem
        {
            Header = header,
            InputGesture = gesture != null ? KeyGesture.Parse(gesture) : null
        };
        item.Click += (s, e) => action();

        if (isEnabled != null)
        {
            // Update IsEnabled when menu opens
            item.AttachedToVisualTree += (s, e) => item.IsEnabled = isEnabled();
        }

        return item;
    }

    private MenuItem CreateSubMenu(string header, List<MenuItem> items)
    {
        return new MenuItem
        {
            Header = header,
            ItemsSource = items
        };
    }

    /// <summary>
    /// Shows the appropriate context menu based on what was right-clicked.
    /// </summary>
    public void Show(Control target, PointerPressedEventArgs e, CorePoint canvasPosition)
    {
        _contextMenuPosition = canvasPosition;

        var eventArgs = new ContextMenuEventArgs(canvasPosition);
        ContextMenuOpening?.Invoke(this, eventArgs);

        if (eventArgs.Cancel)
            return;

        ContextMenu? menu = null;

        // Determine which menu to show based on target
        if (target.Tag is Node node)
        {
            if (node.IsGroup)
            {
                menu = _groupContextMenu;
            }
            else
            {
                // Update the "Add to Group" submenu before showing
                UpdateAddToGroupSubmenu();
                menu = _nodeContextMenu;
            }
        }
        else if (target.Tag is Edge)
        {
            menu = _edgeContextMenu;
        }
        else
        {
            menu = _canvasContextMenu;
        }

        if (menu != null)
        {
            // Always open on the canvas itself (not the dummy control which may not be in visual tree)
            menu.Open(_canvas);
        }
    }

    /// <summary>
    /// Shows the canvas context menu at the specified position.
    /// </summary>
    public void ShowCanvasMenu(Control target, CorePoint canvasPosition)
    {
        _contextMenuPosition = canvasPosition;
        _canvasContextMenu?.Open(_canvas);
    }

    #region Menu Action Handlers

    private void OnCut() => _canvas.Cut();
    private void OnCopy() => _canvas.Copy();
    private void OnPaste()
    {
        _canvas.Paste();
        // Schedule a deferred refresh to ensure the visual updates after the context menu fully closes
        Dispatcher.UIThread.Post(() => _canvas.Refresh(), DispatcherPriority.Render);
    }
    private void OnDuplicate()
    {
        _canvas.Duplicate();
        // Schedule a deferred refresh to ensure the visual updates after the context menu fully closes
        Dispatcher.UIThread.Post(() => _canvas.Refresh(), DispatcherPriority.Render);
    }
    private void OnDelete()
    {
        _canvas.Selection.DeleteSelected();
        // Schedule a deferred refresh to ensure the visual updates after the context menu fully closes
        Dispatcher.UIThread.Post(() => _canvas.Refresh(), DispatcherPriority.Render);
    }
    private void OnSelectAll() => _canvas.Selection.SelectAll();
    private void OnFitToView() => _canvas.FitToView();
    private void OnResetZoom() => _canvas.ResetZoom();

    private void OnRenameNode()
    {
        var selectedNode = _canvas.Graph?.Elements.Nodes.FirstOrDefault(n => n.IsSelected);
        if (selectedNode != null)
        {
            // Get screen position for the editor
            var screenPos = _canvas.Viewport.CanvasToScreen(
                new global::Avalonia.Point(selectedNode.Position.X, selectedNode.Position.Y));

            // Raise the label edit request event
            var args = new NodeLabelEditRequestedEventArgs(selectedNode, selectedNode.Label, screenPos);
            RaiseLabelEditRequested(args);
        }
    }

    /// <summary>
    /// Event raised when a node rename is requested via context menu.
    /// </summary>
    public event EventHandler<NodeLabelEditRequestedEventArgs>? NodeLabelEditRequested;

    private void RaiseLabelEditRequested(NodeLabelEditRequestedEventArgs args)
    {
        NodeLabelEditRequested?.Invoke(this, args);
    }

    private void OnGroupSelected() => _canvas.GroupSelected();
    private void OnUngroup() => _canvas.UngroupSelected();

    private void OnExpandGroup()
    {
        var selectedGroup = GetSelectedGroup();
        if (selectedGroup != null)
            _canvas.SetGroupCollapsed(selectedGroup.Id, false);
    }

    private void OnCollapseGroup()
    {
        var selectedGroup = GetSelectedGroup();
        if (selectedGroup != null)
            _canvas.SetGroupCollapsed(selectedGroup.Id, true);
    }

    private void OnAutoResizeGroup()
    {
        var selectedGroup = GetSelectedGroup();
        if (selectedGroup != null)
            _canvas.AutoResizeGroup(selectedGroup.Id);
    }

    private void OnCollapseAllGroups() => _canvas.CollapseAllGroups();
    private void OnExpandAllGroups() => _canvas.ExpandAllGroups();

    private void AddNode(string nodeType)
    {
        var graph = _canvas.Graph;
        if (graph == null) return;

        var newNode = nodeType.ToLowerInvariant() switch
        {
            "input" => new Node
            {
                Type = "input",
                Data = "Input",
                Position = _contextMenuPosition,
                Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
            },
            "output" => new Node
            {
                Type = "output",
                Data = "Output",
                Position = _contextMenuPosition,
                Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }]
            },
            "process" => new Node
            {
                Type = "Process",
                Position = _contextMenuPosition,
                Inputs = [new Port { Id = "in", Type = "data", Label = "Input" }],
                Outputs = [new Port { Id = "out", Type = "data", Label = "Output" }]
            },
            _ => new Node
            {
                Type = "default",
                Position = _contextMenuPosition,
                Inputs = [new Port { Id = "in", Type = "data", Label = "In" }],
                Outputs = [new Port { Id = "out", Type = "data", Label = "Out" }]
            }
        };

        // Use command for undo support
        _canvas.CommandHistory.Execute(new FlowGraph.Core.Commands.AddNodeCommand(graph, newNode));

        // Select the new node
        foreach (var n in graph.Elements.Nodes)
            n.IsSelected = false;
        newNode.IsSelected = true;

        // Schedule a deferred refresh to ensure the visual updates after the context menu fully closes
        Dispatcher.UIThread.Post(() =>
        {
            _canvas.Refresh();
        }, DispatcherPriority.Render);
    }

    private void OnDeleteEdge()
    {
        _canvas.Selection.DeleteSelected();
    }

    private void SetEdgeType(EdgeType type)
    {
        var graph = _canvas.Graph;
        if (graph == null) return;

        foreach (var edge in graph.Elements.Edges.Where(e => e.IsSelected))
        {
            edge.Type = type;
        }

        // Force re-render edges
        _canvas.RefreshEdges();
    }

    private void SetEdgeMarker(EdgeMarker marker)
    {
        var graph = _canvas.Graph;
        if (graph == null) return;

        foreach (var edge in graph.Elements.Edges.Where(e => e.IsSelected))
        {
            edge.MarkerEnd = marker;
        }

        // Force re-render edges
        _canvas.RefreshEdges();
    }

    #endregion

    #region State Helpers

    private bool CanGroup()
    {
        var graph = _canvas.Graph;
        if (graph == null) return false;
        return graph.Elements.Nodes.Count(n => n.IsSelected && !n.IsGroup) >= 2;
    }

    private bool CanPaste()
    {
        // We'd need access to clipboard state
        // For now, always enable
        return true;
    }

    private bool IsGroupCollapsed()
    {
        var group = GetSelectedGroup();
        return group?.IsCollapsed ?? false;
    }

    private Node? GetSelectedGroup()
    {
        return _canvas.Graph?.Elements.Nodes.FirstOrDefault(n => n.IsSelected && n.IsGroup);
    }

    #endregion
}

/// <summary>
/// Event args for context menu opening.
/// </summary>
public class ContextMenuEventArgs : EventArgs
{
    /// <summary>
    /// The canvas position where the context menu was triggered.
    /// </summary>
    public CorePoint CanvasPosition { get; }

    /// <summary>
    /// Set to true to cancel showing the context menu.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Additional menu items to add.
    /// </summary>
    public List<Control> AdditionalItems { get; } = [];

    public ContextMenuEventArgs(CorePoint canvasPosition)
    {
        CanvasPosition = canvasPosition;
    }
}
