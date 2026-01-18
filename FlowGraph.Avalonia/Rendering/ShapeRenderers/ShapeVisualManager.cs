using Avalonia.Controls;
using FlowGraph.Core.Diagnostics;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Avalonia.Rendering.ShapeRenderers;

/// <summary>
/// Manages the visual representation of shape elements on the canvas.
/// </summary>
/// <remarks>
/// <para>
/// ShapeVisualManager follows the same pattern as <see cref="NodeVisualManager"/>
/// and <see cref="EdgeVisualManager"/>, providing consistent management of
/// visual-to-model mappings for shape elements.
/// </para>
/// <para>
/// It handles:
/// - Creating visuals for shape elements
/// - Positioning visuals in canvas coordinates
/// - Updating visuals when properties change
/// - Removing visuals when shapes are removed
/// </para>
/// </remarks>
public class ShapeVisualManager
{
  private readonly Canvas _canvas;
  private readonly Dictionary<string, ShapeVisualEntry> _visuals = new();
  private RenderContext? _renderContext;
  private ShapeResizeHandleManager? _resizeHandleManager;
  private ThemeResources? _theme;

  /// <summary>
  /// Represents a tracked shape visual with its associated data.
  /// </summary>
  private class ShapeVisualEntry
  {
    public required Control Visual { get; init; }
    public required ShapeElement Shape { get; init; }
  }

  /// <summary>
  /// Creates a new ShapeVisualManager for the specified canvas.
  /// </summary>
  /// <param name="canvas">The canvas to manage shape visuals on.</param>
  public ShapeVisualManager(Canvas canvas)
  {
    _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
  }

  /// <summary>
  /// Sets the theme for rendering resize handles.
  /// </summary>
  public void SetTheme(ThemeResources theme)
  {
    _theme = theme;
  }

  /// <summary>
  /// Sets the render context for coordinate transformations and settings.
  /// </summary>
  /// <param name="context">The render context to use.</param>
  public void SetRenderContext(RenderContext context)
  {
    _renderContext = context;
    // Create resize handle manager with render context
    _resizeHandleManager = new ShapeResizeHandleManager(context);
  }

  /// <summary>
  /// Adds or updates a shape visual on the canvas.
  /// </summary>
  /// <param name="shape">The shape element to render.</param>
  public void AddOrUpdateShape(ShapeElement shape)
  {
    ArgumentNullException.ThrowIfNull(shape);

    if (!shape.IsVisible)
    {
      RemoveShape(shape.Id);
      return;
    }

    var shapeContext = CreateShapeContext();

    if (_visuals.TryGetValue(shape.Id, out var entry))
    {
      // Update existing visual
      var renderer = ShapeRendererRegistry.Instance.GetRenderer(shape.Type);
      renderer.UpdateVisual(entry.Visual, shape, shapeContext);
      UpdatePosition(entry.Visual, shape);
    }
    else
    {
      // Create new visual
      try
      {
        var renderer = ShapeRendererRegistry.Instance.GetRenderer(shape.Type);
        var visual = renderer.CreateShapeVisual(shape, shapeContext);

        // IMPORTANT: Set the Tag to the shape for hit testing
        // This allows FlowCanvas.Input to recognize clicks on shapes
        visual.Tag = shape;

        _visuals[shape.Id] = new ShapeVisualEntry
        {
          Visual = visual,
          Shape = shape
        };

        UpdatePosition(visual, shape);
        _canvas.Children.Add(visual);

        FlowGraphLogger.Debug(LogCategory.Rendering, $"Created shape visual for '{shape.Id}' (type: {shape.Type})", "ShapeVisualManager");
      }
      catch (Exception ex)
      {
        FlowGraphLogger.Error(LogCategory.Rendering, $"Failed to create shape visual: {ex.Message}", "ShapeVisualManager", ex);
      }
    }
  }

  /// <summary>
  /// Updates the selection state of a shape visual.
  /// </summary>
  /// <param name="shapeId">The ID of the shape to update.</param>
  /// <param name="isSelected">Whether the shape is selected.</param>
  public void UpdateSelection(string shapeId, bool isSelected)
  {
    if (_visuals.TryGetValue(shapeId, out var entry))
    {
      entry.Shape.IsSelected = isSelected;
      var renderer = ShapeRendererRegistry.Instance.GetRenderer(entry.Shape.Type);
      renderer.UpdateSelection(entry.Visual, entry.Shape, CreateShapeContext());

      // Update resize handles for resizable shapes
      if (_resizeHandleManager != null && _theme != null)
      {
        if (isSelected && entry.Shape.IsResizable)
        {
          _resizeHandleManager.RenderResizeHandles(_canvas, entry.Shape, _theme);
        }
        else
        {
          _resizeHandleManager.RemoveResizeHandles(_canvas, shapeId);
        }
      }
    }
  }

  /// <summary>
  /// Removes a shape visual from the canvas.
  /// </summary>
  /// <param name="shapeId">The ID of the shape to remove.</param>
  public void RemoveShape(string shapeId)
  {
    if (_visuals.TryGetValue(shapeId, out var entry))
    {
      _canvas.Children.Remove(entry.Visual);
      _visuals.Remove(shapeId);
      _resizeHandleManager?.RemoveResizeHandles(_canvas, shapeId);
      FlowGraphLogger.Debug(LogCategory.Rendering, $"Removed shape visual '{shapeId}'", "ShapeVisualManager");
    }
  }

  /// <summary>
  /// Removes all shape visuals from the canvas.
  /// </summary>
  public void Clear()
  {
    foreach (var entry in _visuals.Values)
    {
      _canvas.Children.Remove(entry.Visual);
    }
    _visuals.Clear();
    _resizeHandleManager?.RemoveAllResizeHandles(_canvas);
    FlowGraphLogger.Debug(LogCategory.Rendering, "Cleared all shape visuals", "ShapeVisualManager");
  }

  /// <summary>
  /// Gets the visual for a shape by ID.
  /// </summary>
  /// <param name="shapeId">The shape ID.</param>
  /// <returns>The visual control, or null if not found.</returns>
  public Control? GetVisual(string shapeId)
  {
    return _visuals.TryGetValue(shapeId, out var entry) ? entry.Visual : null;
  }

  /// <summary>
  /// Updates the resize handle positions for all tracked shapes.
  /// Call this during shape dragging to keep resize handles in sync with shape positions.
  /// </summary>
  public void UpdateResizeHandlePositions()
  {
    _resizeHandleManager?.UpdateAllResizeHandles();
  }

  /// <summary>
  /// Updates all shape visuals (e.g., after zoom change).
  /// </summary>
  public void RefreshAll()
  {
    var shapeContext = CreateShapeContext();
    foreach (var entry in _visuals.Values)
    {
      var renderer = ShapeRendererRegistry.Instance.GetRenderer(entry.Shape.Type);
      renderer.UpdateVisual(entry.Visual, entry.Shape, shapeContext);
      UpdatePosition(entry.Visual, entry.Shape);
    }
  }

  /// <summary>
  /// Gets all tracked shape IDs.
  /// </summary>
  public IEnumerable<string> GetShapeIds() => _visuals.Keys;

  private ShapeRenderContext CreateShapeContext()
  {
    // In transform-based rendering, Scale is 1.0 - MatrixTransform handles zoom
    var settings = _renderContext?.Settings ?? FlowCanvasSettings.Default;
    return new ShapeRenderContext(settings, 1.0);
  }

  private void UpdatePosition(Control visual, ShapeElement shape)
  {
    // PHASE 1: Position in canvas coordinates directly
    // The MatrixTransform on MainCanvas handles the viewport transformation
    // Do NOT call CanvasToScreen - that would cause double transformation
    Canvas.SetLeft(visual, shape.Position.X);
    Canvas.SetTop(visual, shape.Position.Y);
    visual.ZIndex = shape.ZIndex;
  }
}
