using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FlowGraph.Core;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Controls;

/// <summary>
/// Defines the position of the NodeToolbar relative to the selected node(s).
/// </summary>
public enum NodeToolbarPosition
{
    /// <summary>
    /// Above the node(s).
    /// </summary>
    Top,

    /// <summary>
    /// Below the node(s).
    /// </summary>
    Bottom,

    /// <summary>
    /// To the left of the node(s).
    /// </summary>
    Left,

    /// <summary>
    /// To the right of the node(s).
    /// </summary>
    Right
}

/// <summary>
/// A floating toolbar that appears near selected nodes.
/// Inspired by React Flow's NodeToolbar component.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;Grid&gt;
///   &lt;local:FlowCanvas x:Name="Canvas" /&gt;
///   &lt;local:NodeToolbar TargetCanvas="{Binding #Canvas}" Position="Top" Offset="10"&gt;
///     &lt;StackPanel Orientation="Horizontal" Spacing="4"&gt;
///       &lt;Button Content="Delete" Click="OnDeleteClick" /&gt;
///       &lt;Button Content="Duplicate" Click="OnDuplicateClick" /&gt;
///     &lt;/StackPanel&gt;
///   &lt;/local:NodeToolbar&gt;
/// &lt;/Grid&gt;
/// </code>
/// </remarks>
public partial class NodeToolbar : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<FlowCanvas?> TargetCanvasProperty =
        AvaloniaProperty.Register<NodeToolbar, FlowCanvas?>(nameof(TargetCanvas));

    public static readonly StyledProperty<NodeToolbarPosition> PositionProperty =
        AvaloniaProperty.Register<NodeToolbar, NodeToolbarPosition>(nameof(Position), NodeToolbarPosition.Top);

    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<NodeToolbar, double>(nameof(Offset), 10);

    public static readonly StyledProperty<bool> IsVisibleWhenSelectedProperty =
        AvaloniaProperty.Register<NodeToolbar, bool>(nameof(IsVisibleWhenSelected), true);

    public static readonly StyledProperty<HorizontalAlignment> AlignProperty =
        AvaloniaProperty.Register<NodeToolbar, HorizontalAlignment>(nameof(Align), HorizontalAlignment.Center);

    #endregion

    #region Public Properties

    /// <summary>
    /// The FlowCanvas to track selection from.
    /// </summary>
    public FlowCanvas? TargetCanvas
    {
        get => GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }

    /// <summary>
    /// The position of the toolbar relative to the selected node(s).
    /// </summary>
    public NodeToolbarPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>
    /// The offset from the node(s) in pixels.
    /// </summary>
    public double Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    /// <summary>
    /// If true, the toolbar is visible when nodes are selected. If false, it's always hidden.
    /// </summary>
    public bool IsVisibleWhenSelected
    {
        get => GetValue(IsVisibleWhenSelectedProperty);
        set => SetValue(IsVisibleWhenSelectedProperty, value);
    }

    /// <summary>
    /// Horizontal alignment of the toolbar relative to the selection bounds.
    /// </summary>
    public HorizontalAlignment Align
    {
        get => GetValue(AlignProperty);
        set => SetValue(AlignProperty, value);
    }

    #endregion

    private Border? _container;

    public NodeToolbar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Create a container border for styling
        _container = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(240, 45, 45, 48)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 74)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            IsHitTestVisible = true
        };

        // The toolbar will wrap the Content
        IsVisible = false;
        IsHitTestVisible = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TargetCanvasProperty)
        {
            var oldCanvas = change.OldValue as FlowCanvas;
            var newCanvas = change.NewValue as FlowCanvas;

            if (oldCanvas != null)
            {
                oldCanvas.SelectionChanged -= OnSelectionChanged;
                oldCanvas.ViewportChanged -= OnViewportChanged;
            }

            if (newCanvas != null)
            {
                newCanvas.SelectionChanged += OnSelectionChanged;
                newCanvas.ViewportChanged += OnViewportChanged;
                UpdatePosition();
            }
        }
        else if (change.Property == PositionProperty ||
                 change.Property == OffsetProperty ||
                 change.Property == AlignProperty)
        {
            UpdatePosition();
        }
        else if (change.Property == IsVisibleWhenSelectedProperty)
        {
            UpdateVisibility();
        }
        else if (change.Property == ContentProperty)
        {
            // Wrap content in styled container
            WrapContent();
        }
    }

    private void WrapContent()
    {
        if (_container != null && Content != null && Content != _container)
        {
            var originalContent = Content as Control;
            if (originalContent != null)
            {
                // Clear the content first to remove the parent reference
                Content = null;
                _container.Child = originalContent;
            }
            Content = _container;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateVisibility();
        UpdatePosition();
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        UpdatePosition();
    }

    private void UpdateVisibility()
    {
        if (TargetCanvas == null)
        {
            IsVisible = false;
            return;
        }

        var selectedNodes = TargetCanvas.Selection.GetSelectedNodes().ToList();
        IsVisible = IsVisibleWhenSelected && selectedNodes.Count > 0;
    }

    private void UpdatePosition()
    {
        if (TargetCanvas == null || !IsVisible)
            return;

        var selectedNodes = TargetCanvas.Selection.GetSelectedNodes().ToList();
        if (selectedNodes.Count == 0)
            return;

        // Calculate bounding box of all selected nodes in canvas coordinates
        var viewport = TargetCanvas.Viewport;
        var settings = TargetCanvas.Settings;

        var minX = selectedNodes.Min(n => n.Position.X);
        var minY = selectedNodes.Min(n => n.Position.Y);
        var maxX = selectedNodes.Max(n => n.Position.X + (n.Width ?? settings.NodeWidth));
        var maxY = selectedNodes.Max(n => n.Position.Y + (n.Height ?? settings.NodeHeight));

        // Convert to screen coordinates
        var topLeft = viewport.CanvasToScreen(new AvaloniaPoint(minX, minY));
        var bottomRight = viewport.CanvasToScreen(new AvaloniaPoint(maxX, maxY));

        var boundsWidth = bottomRight.X - topLeft.X;
        var boundsHeight = bottomRight.Y - topLeft.Y;

        // Calculate toolbar position based on Position property
        double x, y;

        switch (Position)
        {
            case NodeToolbarPosition.Top:
                y = topLeft.Y - Offset - (DesiredSize.Height > 0 ? DesiredSize.Height : 40);
                x = CalculateHorizontalPosition(topLeft.X, boundsWidth);
                break;

            case NodeToolbarPosition.Bottom:
                y = bottomRight.Y + Offset;
                x = CalculateHorizontalPosition(topLeft.X, boundsWidth);
                break;

            case NodeToolbarPosition.Left:
                x = topLeft.X - Offset - (DesiredSize.Width > 0 ? DesiredSize.Width : 100);
                y = CalculateVerticalPosition(topLeft.Y, boundsHeight);
                break;

            case NodeToolbarPosition.Right:
                x = bottomRight.X + Offset;
                y = CalculateVerticalPosition(topLeft.Y, boundsHeight);
                break;

            default:
                x = topLeft.X;
                y = topLeft.Y - Offset - 40;
                break;
        }

        // Clamp to parent bounds
        if (Parent is Control parent)
        {
            var toolbarWidth = DesiredSize.Width > 0 ? DesiredSize.Width : 100;
            var toolbarHeight = DesiredSize.Height > 0 ? DesiredSize.Height : 40;

            x = Math.Max(0, Math.Min(x, parent.Bounds.Width - toolbarWidth));
            y = Math.Max(0, Math.Min(y, parent.Bounds.Height - toolbarHeight));
        }

        // Position the toolbar using Canvas attached properties or Margin
        Margin = new Thickness(x, y, 0, 0);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
    }

    private double CalculateHorizontalPosition(double boundsLeft, double boundsWidth)
    {
        var toolbarWidth = DesiredSize.Width > 0 ? DesiredSize.Width : 100;

        return Align switch
        {
            HorizontalAlignment.Left => boundsLeft,
            HorizontalAlignment.Right => boundsLeft + boundsWidth - toolbarWidth,
            HorizontalAlignment.Center => boundsLeft + (boundsWidth - toolbarWidth) / 2,
            _ => boundsLeft + (boundsWidth - toolbarWidth) / 2
        };
    }

    private double CalculateVerticalPosition(double boundsTop, double boundsHeight)
    {
        var toolbarHeight = DesiredSize.Height > 0 ? DesiredSize.Height : 40;
        // Center vertically by default
        return boundsTop + (boundsHeight - toolbarHeight) / 2;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdatePosition();
    }
}
