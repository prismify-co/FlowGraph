using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace FlowGraph.Avalonia.Controls;

/// <summary>
/// Defines the position of a FlowPanel relative to its container.
/// </summary>
public enum PanelPosition
{
    /// <summary>
    /// Top-left corner of the container.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Top-center of the container.
    /// </summary>
    TopCenter,

    /// <summary>
    /// Top-right corner of the container.
    /// </summary>
    TopRight,

    /// <summary>
    /// Center-left of the container.
    /// </summary>
    CenterLeft,

    /// <summary>
    /// Center of the container.
    /// </summary>
    Center,

    /// <summary>
    /// Center-right of the container.
    /// </summary>
    CenterRight,

    /// <summary>
    /// Bottom-left corner of the container.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Bottom-center of the container.
    /// </summary>
    BottomCenter,

    /// <summary>
    /// Bottom-right corner of the container.
    /// </summary>
    BottomRight
}

/// <summary>
/// A positioned overlay panel that can be placed at various positions within a FlowCanvas.
/// Inspired by React Flow's Panel component.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;Grid&gt;
///   &lt;local:FlowCanvas x:Name="Canvas" /&gt;
///   &lt;local:FlowPanel Position="TopRight" Margin="10"&gt;
///     &lt;!-- Your content here --&gt;
///   &lt;/local:FlowPanel&gt;
/// &lt;/Grid&gt;
/// </code>
/// </remarks>
public partial class FlowPanel : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<PanelPosition> PositionProperty =
        AvaloniaProperty.Register<FlowPanel, PanelPosition>(nameof(Position), PanelPosition.TopLeft);

    #endregion

    #region Public Properties

    /// <summary>
    /// The position of the panel within its container.
    /// </summary>
    public PanelPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    #endregion

    public FlowPanel()
    {
        InitializeComponent();
        UpdateAlignment();
    }

    private void InitializeComponent()
    {
        // Panel is positioned using alignment properties
        IsHitTestVisible = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PositionProperty)
        {
            UpdateAlignment();
        }
    }

    private void UpdateAlignment()
    {
        var (horizontal, vertical) = Position switch
        {
            PanelPosition.TopLeft => (HorizontalAlignment.Left, VerticalAlignment.Top),
            PanelPosition.TopCenter => (HorizontalAlignment.Center, VerticalAlignment.Top),
            PanelPosition.TopRight => (HorizontalAlignment.Right, VerticalAlignment.Top),
            PanelPosition.CenterLeft => (HorizontalAlignment.Left, VerticalAlignment.Center),
            PanelPosition.Center => (HorizontalAlignment.Center, VerticalAlignment.Center),
            PanelPosition.CenterRight => (HorizontalAlignment.Right, VerticalAlignment.Center),
            PanelPosition.BottomLeft => (HorizontalAlignment.Left, VerticalAlignment.Bottom),
            PanelPosition.BottomCenter => (HorizontalAlignment.Center, VerticalAlignment.Bottom),
            PanelPosition.BottomRight => (HorizontalAlignment.Right, VerticalAlignment.Bottom),
            _ => (HorizontalAlignment.Left, VerticalAlignment.Top)
        };

        HorizontalAlignment = horizontal;
        VerticalAlignment = vertical;
    }
}
