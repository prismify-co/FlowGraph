using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using FlowGraph.Core.Coordinates;

namespace FlowGraph.Avalonia.Rendering;

/// <summary>
/// Provides unified positioning for overlay elements that need to appear at specific
/// viewport coordinates, regardless of the parent container type.
/// 
/// <para>
/// <b>Problem this solves:</b> FlowGraph has two rendering modes with different container types:
/// <list type="bullet">
/// <item><b>Retained Mode:</b> Uses MainCanvas (Canvas) with MatrixTransform - Canvas.SetLeft/SetTop work</item>
/// <item><b>Direct Rendering:</b> Uses RootPanel (Panel) - Canvas.SetLeft/SetTop are IGNORED</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Key insight:</b> Canvas attached properties (Canvas.Left, Canvas.Top) only work when the
/// parent element is a Canvas. When attached to a Panel child, they have no effect.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Instead of this error-prone code:
/// if (isDirectMode)
/// {
///     // BUG: Won't work - RootPanel is Panel, not Canvas!
///     Canvas.SetLeft(element, x);
///     Canvas.SetTop(element, y);
/// }
/// 
/// // Use the positioner:
/// var positioner = new ViewportOverlayPositioner(isDirectMode);
/// positioner.SetPosition(element, viewportX, viewportY);
/// </code>
/// </example>
public class ViewportOverlayPositioner
{
    private readonly bool _isDirectRenderingMode;

    /// <summary>
    /// Creates a new viewport overlay positioner.
    /// </summary>
    /// <param name="isDirectRenderingMode">
    /// True when using Direct Rendering (RootPanel/Panel parent).
    /// False when using Retained Mode (MainCanvas/Canvas parent).
    /// </param>
    public ViewportOverlayPositioner(bool isDirectRenderingMode)
    {
        _isDirectRenderingMode = isDirectRenderingMode;
    }

    /// <summary>
    /// Positions an element at the specified viewport coordinates.
    /// Automatically uses the correct positioning method based on the rendering mode.
    /// </summary>
    /// <param name="element">The element to position.</param>
    /// <param name="x">X coordinate in viewport space.</param>
    /// <param name="y">Y coordinate in viewport space.</param>
    public void SetPosition(Control element, double x, double y)
    {
        if (_isDirectRenderingMode)
        {
            // Panel parent: use Margin with top-left alignment
            element.HorizontalAlignment = HorizontalAlignment.Left;
            element.VerticalAlignment = VerticalAlignment.Top;
            element.Margin = new Thickness(x, y, 0, 0);
        }
        else
        {
            // Canvas parent: use Canvas attached properties
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
        }
    }

    /// <summary>
    /// Positions an element at the specified viewport point.
    /// </summary>
    /// <param name="element">The element to position.</param>
    /// <param name="position">Position in viewport space.</param>
    public void SetPosition(Control element, ViewportPoint position)
    {
        SetPosition(element, position.X, position.Y);
    }

    /// <summary>
    /// Positions an element at the specified Avalonia point (viewport coordinates).
    /// </summary>
    /// <param name="element">The element to position.</param>
    /// <param name="position">Position in viewport space.</param>
    public void SetPosition(Control element, Point position)
    {
        SetPosition(element, position.X, position.Y);
    }

    /// <summary>
    /// Sets up an element for proper positioning in the target container.
    /// Call this when creating elements that will be positioned later.
    /// </summary>
    /// <param name="element">The element to configure.</param>
    /// <remarks>
    /// This sets HorizontalAlignment and VerticalAlignment to Left/Top for Panel parents.
    /// For Canvas parents, these properties are ignored but don't cause harm.
    /// </remarks>
    public void PrepareForPositioning(Control element)
    {
        if (_isDirectRenderingMode)
        {
            element.HorizontalAlignment = HorizontalAlignment.Left;
            element.VerticalAlignment = VerticalAlignment.Top;
        }
    }

    /// <summary>
    /// Creates a positioner based on whether a direct renderer is active.
    /// </summary>
    /// <param name="directRenderer">The direct renderer, or null if in retained mode.</param>
    /// <returns>A positioner configured for the current rendering mode.</returns>
    public static ViewportOverlayPositioner Create(DirectCanvasRenderer? directRenderer)
    {
        return new ViewportOverlayPositioner(directRenderer != null);
    }
}
