using Avalonia.Controls;
using Avalonia.Input;
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
        // Node context menu
        _nodeContextMenu = new ContextMenu
        {
            ItemsSource = new List<object>
            {
                CreateMenuItem("Cut", "Ctrl+X", OnCut),
                CreateMenuItem("Copy", "Ctrl+C", OnCopy),
                CreateMenuItem("Duplicate", "Ctrl+D", OnDuplicate),
                new Separator(),
                CreateMenuItem("Delete", "Delete", OnDelete),
                new Separator(),
                CreateMenuItem("Group Selected", "Ctrl+G", OnGroupSelected, isEnabled: () => CanGroup()),
                CreateMenuItem("Add to Group...", null, OnAddToGroup, isEnabled: () => CanAddToGroup())
            }
        };

        // Group context menu
        _groupContextMenu = new ContextMenu
        {
            ItemsSource = new List<object>
            {
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
                menu = _groupContextMenu;
            else
                menu = _nodeContextMenu;
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
            menu.Open(target);
        }
    }

    /// <summary>
    /// Shows the canvas context menu at the specified position.
    /// </summary>
    public void ShowCanvasMenu(Control target, CorePoint canvasPosition)
    {
        _contextMenuPosition = canvasPosition;
        _canvasContextMenu?.Open(target);
    }

    #region Menu Action Handlers

    private void OnCut() => _canvas.Cut();
    private void OnCopy() => _canvas.Copy();
    private void OnPaste() => _canvas.Paste();
    private void OnDuplicate() => _canvas.Duplicate();
    private void OnDelete() => _canvas.Selection.DeleteSelected();
    private void OnSelectAll() => _canvas.Selection.SelectAll();
    private void OnFitToView() => _canvas.FitToView();
    private void OnResetZoom() => _canvas.ResetZoom();

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

    private void OnAddToGroup()
    {
        // This would ideally show a submenu or dialog to select a group
        // For now, we'll just provide the infrastructure
    }

    private void OnDeleteEdge()
    {
        _canvas.Selection.DeleteSelected();
    }

    private void SetEdgeType(EdgeType type)
    {
        var graph = _canvas.Graph;
        if (graph == null) return;

        foreach (var edge in graph.Edges.Where(e => e.IsSelected))
        {
            edge.Type = type;
        }
        
        // Force re-render
        // The graph renderer should pick up the change
    }

    private void SetEdgeMarker(EdgeMarker marker)
    {
        var graph = _canvas.Graph;
        if (graph == null) return;

        foreach (var edge in graph.Edges.Where(e => e.IsSelected))
        {
            edge.MarkerEnd = marker;
        }
    }

    #endregion

    #region State Helpers

    private bool CanGroup()
    {
        var graph = _canvas.Graph;
        if (graph == null) return false;
        return graph.Nodes.Count(n => n.IsSelected && !n.IsGroup) >= 2;
    }

    private bool CanAddToGroup()
    {
        var graph = _canvas.Graph;
        if (graph == null) return false;
        
        var hasSelectedNodes = graph.Nodes.Any(n => n.IsSelected && !n.IsGroup);
        var hasGroups = graph.Nodes.Any(n => n.IsGroup);
        return hasSelectedNodes && hasGroups;
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
        return _canvas.Graph?.Nodes.FirstOrDefault(n => n.IsSelected && n.IsGroup);
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
