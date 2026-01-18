using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering;
using FlowGraph.Core;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages inline label editing for nodes and edges.
/// Supports both visual tree mode and direct rendering mode.
/// </summary>
public class LabelEditManager
{
  private readonly Func<Graph?> _getGraph;
  private readonly Func<Canvas?> _getMainCanvas;
  private readonly Func<ViewportState> _getViewport;
  private readonly Func<ThemeResources?> _getTheme;
  private readonly Func<CanvasElementManager> _getGraphRenderer;
  private readonly Func<DirectCanvasRenderer?> _getDirectRenderer;
  private readonly Func<bool> _getIsDirectRendering;
  private readonly Action _renderEdges;

  /// <summary>
  /// Event raised when a node's label has been committed.
  /// </summary>
  public event EventHandler<NodeLabelCommittedEventArgs>? NodeLabelCommitted;

  /// <summary>
  /// Event raised when an edge's label has been committed.
  /// </summary>
  public event EventHandler<EdgeLabelCommittedEventArgs>? EdgeLabelCommitted;

  /// <summary>
  /// Creates a new label edit manager.
  /// </summary>
  public LabelEditManager(
      Func<Graph?> getGraph,
      Func<Canvas?> getMainCanvas,
      Func<ViewportState> getViewport,
      Func<ThemeResources?> getTheme,
      Func<CanvasElementManager> getGraphRenderer,
      Func<DirectCanvasRenderer?> getDirectRenderer,
      Func<bool> getIsDirectRendering,
      Action renderEdges)
  {
    _getGraph = getGraph;
    _getMainCanvas = getMainCanvas;
    _getViewport = getViewport;
    _getTheme = getTheme;
    _getGraphRenderer = getGraphRenderer;
    _getDirectRenderer = getDirectRenderer;
    _getIsDirectRendering = getIsDirectRendering;
    _renderEdges = renderEdges;
  }

  /// <summary>
  /// Begins inline editing for a node's label.
  /// </summary>
  /// <param name="node">The node to edit.</param>
  /// <returns>True if editing started successfully.</returns>
  public bool BeginEditNodeLabel(Node node)
  {
    var theme = _getTheme();
    if (theme == null) return false;

    if (_getIsDirectRendering() && _getDirectRenderer() != null && _getMainCanvas() != null)
    {
      return BeginEditNodeLabelDirect(node);
    }

    return _getGraphRenderer().BeginEditLabel(
        node,
        theme,
        newLabel => CommitNodeLabel(node, newLabel),
        () => CancelNodeLabelEdit(node));
  }

  /// <summary>
  /// Ends inline editing for a node's label.
  /// </summary>
  /// <param name="node">The node being edited.</param>
  public void EndEditNodeLabel(Node node)
  {
    var theme = _getTheme();
    if (theme == null) return;

    if (_getIsDirectRendering() && _getDirectRenderer() != null)
    {
      var directRenderer = _getDirectRenderer()!;
      directRenderer.EndEditNode();
      directRenderer.InvalidateVisual();
      return;
    }

    _getGraphRenderer().EndEditLabel(node, theme);
  }

  /// <summary>
  /// Gets whether a node is currently being edited.
  /// </summary>
  /// <param name="node">The node to check.</param>
  /// <returns>True if the node is in edit mode.</returns>
  public bool IsEditingNodeLabel(Node node)
  {
    if (_getIsDirectRendering() && _getDirectRenderer() != null)
    {
      return _getDirectRenderer()!.EditingNodeId == node.Id;
    }
    return _getGraphRenderer().IsEditingLabel(node);
  }

  /// <summary>
  /// Begins inline editing for an edge's label.
  /// </summary>
  /// <param name="edge">The edge to edit.</param>
  /// <returns>True if editing started successfully.</returns>
  public bool BeginEditEdgeLabel(Edge edge)
  {
    var theme = _getTheme();
    var mainCanvas = _getMainCanvas();
    if (theme == null || mainCanvas == null) return false;

    if (_getIsDirectRendering() && _getDirectRenderer() != null && _getGraph() != null)
    {
      return BeginEditEdgeLabelDirect(edge);
    }

    return BeginEditEdgeLabelVisualTree(edge, theme, mainCanvas);
  }

  #region Private Methods - Node Label Editing

  private bool BeginEditNodeLabelDirect(Node node)
  {
    var mainCanvas = _getMainCanvas();
    var theme = _getTheme();
    var directRenderer = _getDirectRenderer();
    var viewport = _getViewport();

    if (mainCanvas == null || theme == null || directRenderer == null) return false;

    // Tell DirectRenderer to skip drawing this node's label
    directRenderer.BeginEditNode(node.Id);

    // Calculate node bounds in screen coordinates
    var model = directRenderer.Model;
    var nodeBounds = model.GetNodeBounds(node);
    var screenX = nodeBounds.X * viewport.Zoom + viewport.OffsetX;
    var screenY = nodeBounds.Y * viewport.Zoom + viewport.OffsetY;
    var screenWidth = nodeBounds.Width * viewport.Zoom;
    var screenHeight = nodeBounds.Height * viewport.Zoom;

    var scale = viewport.Zoom;
    var currentLabel = node.Label ?? node.Type ?? node.Id;

    var textBox = CreateLabelTextBox(
        currentLabel,
        fontSize: 10 * scale,
        theme: theme,
        minWidth: Math.Max(60, screenWidth * 0.8),
        tag: ("NodeLabelEditDirect", node.Id));

    // Position in center of node
    Canvas.SetLeft(textBox, screenX + (screenWidth - textBox.MinWidth) / 2);
    Canvas.SetTop(textBox, screenY + (screenHeight - textBox.FontSize * 2) / 2);

    mainCanvas.Children.Add(textBox);

    SetupTextBoxHandlers(
        textBox,
        onCommit: () => CommitNodeLabelDirect(node, textBox.Text ?? "", textBox),
        onCancel: () => CancelNodeLabelEditDirect(node, textBox));

    FocusAndSelectAll(textBox);
    return true;
  }

  private void CommitNodeLabel(Node node, string newLabel)
  {
    var trimmed = newLabel.Trim();
    node.Label = string.IsNullOrEmpty(trimmed) ? null : trimmed;
    EndEditNodeLabel(node);
    NodeLabelCommitted?.Invoke(this, new NodeLabelCommittedEventArgs(node, trimmed));
  }

  private void CancelNodeLabelEdit(Node node)
  {
    EndEditNodeLabel(node);
  }

  private void CommitNodeLabelDirect(Node node, string newLabel, TextBox textBox)
  {
    var trimmed = newLabel.Trim();
    node.Label = string.IsNullOrEmpty(trimmed) ? null : trimmed;

    _getMainCanvas()?.Children.Remove(textBox);
    _getDirectRenderer()?.EndEditNode();
    _getDirectRenderer()?.InvalidateVisual();

    NodeLabelCommitted?.Invoke(this, new NodeLabelCommittedEventArgs(node, trimmed));
  }

  private void CancelNodeLabelEditDirect(Node node, TextBox textBox)
  {
    _getMainCanvas()?.Children.Remove(textBox);
    _getDirectRenderer()?.EndEditNode();
    _getDirectRenderer()?.InvalidateVisual();
  }

  #endregion

  #region Private Methods - Edge Label Editing

  private bool BeginEditEdgeLabelVisualTree(Edge edge, ThemeResources theme, Canvas mainCanvas)
  {
    var graphRenderer = _getGraphRenderer();
    var viewport = _getViewport();
    var labelVisual = graphRenderer.GetEdgeLabel(edge.Id);

    if (labelVisual == null)
    {
      // Edge has no label yet - set an initial label and re-render
      edge.Label = "Label";
      _renderEdges();
      labelVisual = graphRenderer.GetEdgeLabel(edge.Id);
      if (labelVisual == null) return false;
    }

    // Hide the label and show a TextBox in its place
    labelVisual.IsVisible = false;

    var scale = viewport.Zoom;
    var textBox = CreateLabelTextBox(
        edge.Label ?? "",
        fontSize: 12 * scale,
        theme: theme,
        minWidth: 60,
        tag: ("EdgeLabelEdit", edge.Id));

    Canvas.SetLeft(textBox, Canvas.GetLeft(labelVisual));
    Canvas.SetTop(textBox, Canvas.GetTop(labelVisual));

    mainCanvas.Children.Add(textBox);

    SetupTextBoxHandlers(
        textBox,
        onCommit: () => CommitEdgeLabel(edge, textBox.Text ?? "", textBox, labelVisual),
        onCancel: () => CancelEdgeLabelEdit(edge, textBox, labelVisual));

    FocusAndSelectAll(textBox);
    return true;
  }

  private bool BeginEditEdgeLabelDirect(Edge edge)
  {
    var mainCanvas = _getMainCanvas();
    var theme = _getTheme();
    var directRenderer = _getDirectRenderer();
    var graph = _getGraph();
    var viewport = _getViewport();

    if (mainCanvas == null || theme == null || directRenderer == null || graph == null) return false;

    // Tell DirectRenderer to skip drawing this edge's label
    directRenderer.BeginEditEdge(edge.Id);

    // Calculate edge midpoint in screen coordinates
    var model = directRenderer.Model;
    var (start, end) = model.GetEdgeEndpoints(edge, graph);
    var midpoint = model.GetEdgeMidpoint(start, end);

    var screenX = midpoint.X * viewport.Zoom + viewport.OffsetX;
    var screenY = midpoint.Y * viewport.Zoom + viewport.OffsetY;

    var scale = viewport.Zoom;
    var currentLabel = edge.Label ?? "Label";

    var textBox = CreateLabelTextBox(
        currentLabel,
        fontSize: 12 * scale,
        theme: theme,
        minWidth: 60,
        tag: ("EdgeLabelEditDirect", edge.Id));

    Canvas.SetLeft(textBox, screenX);
    Canvas.SetTop(textBox, screenY - 10 * scale);

    mainCanvas.Children.Add(textBox);

    SetupTextBoxHandlers(
        textBox,
        onCommit: () => CommitEdgeLabelDirect(edge, textBox.Text ?? "", textBox),
        onCancel: () => CancelEdgeLabelEditDirect(edge, textBox));

    FocusAndSelectAll(textBox);
    return true;
  }

  private void CommitEdgeLabel(Edge edge, string newLabel, TextBox textBox, TextBlock labelVisual)
  {
    var trimmed = newLabel.Trim();
    edge.Label = string.IsNullOrEmpty(trimmed) ? null : trimmed;

    _getMainCanvas()?.Children.Remove(textBox);
    _renderEdges();

    EdgeLabelCommitted?.Invoke(this, new EdgeLabelCommittedEventArgs(edge, trimmed));
  }

  private void CancelEdgeLabelEdit(Edge edge, TextBox textBox, TextBlock labelVisual)
  {
    _getMainCanvas()?.Children.Remove(textBox);
    labelVisual.IsVisible = true;
  }

  private void CommitEdgeLabelDirect(Edge edge, string newLabel, TextBox textBox)
  {
    var trimmed = newLabel.Trim();
    edge.Label = string.IsNullOrEmpty(trimmed) ? null : trimmed;

    _getMainCanvas()?.Children.Remove(textBox);
    _getDirectRenderer()?.EndEditEdge();
    _getDirectRenderer()?.InvalidateVisual();

    EdgeLabelCommitted?.Invoke(this, new EdgeLabelCommittedEventArgs(edge, trimmed));
  }

  private void CancelEdgeLabelEditDirect(Edge edge, TextBox textBox)
  {
    _getMainCanvas()?.Children.Remove(textBox);
    _getDirectRenderer()?.EndEditEdge();
    _getDirectRenderer()?.InvalidateVisual();
  }

  #endregion

  #region Shape Text Editing

  /// <summary>
  /// Event raised when a shape's text has been committed.
  /// </summary>
  public event EventHandler<ShapeTextCommittedEventArgs>? ShapeTextCommitted;

  private string? _editingShapeId;

  /// <summary>
  /// Begins inline editing for a shape's text (e.g., sticky notes).
  /// </summary>
  /// <param name="shape">The shape to edit.</param>
  /// <returns>True if editing started successfully.</returns>
  public bool BeginEditShapeText(Core.Elements.Shapes.ShapeElement shape)
  {
    var theme = _getTheme();
    var mainCanvas = _getMainCanvas();
    
    if (theme == null || mainCanvas == null) return false;
    if (shape is not Core.Elements.Shapes.CommentElement comment) return false;
    
    _editingShapeId = shape.Id;

    // Use graph coordinates directly - the MainCanvas MatrixTransform handles zoom/pan
    var shapeWidth = shape.Width ?? 200;
    var shapeHeight = shape.Height ?? 100;

    // Create a multi-line TextBox for editing
    var textBox = new TextBox
    {
      Text = comment.Text ?? "",
      FontSize = comment.FontSize,
      Foreground = Rendering.ShapeRenderers.ShapeRenderContext.CreateBrush(comment.TextColor) ?? Brushes.Black,
      Background = Rendering.ShapeRenderers.ShapeRenderContext.CreateBrush(comment.BackgroundColor) ?? Brushes.LightYellow,
      BorderThickness = new Thickness(2),
      BorderBrush = theme.NodeSelectedBorder,
      Padding = new Thickness(comment.Padding),
      Width = shapeWidth,
      Height = shapeHeight,
      AcceptsReturn = true,
      TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
      Tag = ("ShapeTextEdit", shape.Id)
    };

    // Position in graph coordinates (MainCanvas transform handles the rest)
    Canvas.SetLeft(textBox, shape.Position.X);
    Canvas.SetTop(textBox, shape.Position.Y);
    // Set high Z-index so it's on top of everything
    textBox.ZIndex = 10000;

    mainCanvas.Children.Add(textBox);

    // Hide the shape visual while editing
    var shapeVisual = _getGraphRenderer().GetShapeVisual(shape.Id);
    if (shapeVisual != null)
      shapeVisual.IsVisible = false;

    SetupShapeTextBoxHandlers(
        textBox,
        onCommit: () => CommitShapeText(comment, textBox.Text ?? "", textBox, shapeVisual),
        onCancel: () => CancelShapeTextEdit(comment, textBox, shapeVisual));

    FocusAndSelectAll(textBox);
    return true;
  }

  /// <summary>
  /// Gets whether a shape is currently being edited.
  /// </summary>
  public bool IsEditingShape(Core.Elements.Shapes.ShapeElement shape)
  {
    return _editingShapeId == shape.Id;
  }

  private void CommitShapeText(Core.Elements.Shapes.CommentElement comment, string newText, TextBox textBox, Control? shapeVisual)
  {
    comment.Text = newText;
    _editingShapeId = null;

    _getMainCanvas()?.Children.Remove(textBox);
    
    // Show and update the shape visual
    if (shapeVisual != null)
      shapeVisual.IsVisible = true;
    
    // Force shape to re-render with new text
    _getGraphRenderer().UpdateShapeVisual(comment);

    ShapeTextCommitted?.Invoke(this, new ShapeTextCommittedEventArgs(comment, newText));
  }

  private void CancelShapeTextEdit(Core.Elements.Shapes.CommentElement comment, TextBox textBox, Control? shapeVisual)
  {
    _editingShapeId = null;
    _getMainCanvas()?.Children.Remove(textBox);
    
    if (shapeVisual != null)
      shapeVisual.IsVisible = true;
  }

  private static void SetupShapeTextBoxHandlers(TextBox textBox, Action onCommit, Action onCancel)
  {
    bool finished = false;

    void Commit()
    {
      if (finished) return;
      finished = true;
      onCommit();
    }

    void Cancel()
    {
      if (finished) return;
      finished = true;
      onCancel();
    }

    textBox.KeyDown += (s, e) =>
    {
      // Escape to cancel
      if (e.Key == global::Avalonia.Input.Key.Escape)
      {
        Cancel();
        e.Handled = true;
      }
      // Ctrl+Enter to commit (since Enter adds new lines)
      else if (e.Key == global::Avalonia.Input.Key.Enter && e.KeyModifiers.HasFlag(global::Avalonia.Input.KeyModifiers.Control))
      {
        Commit();
        e.Handled = true;
      }
    };

    textBox.LostFocus += (s, e) => Commit();
  }

  #endregion

  #region Private Helpers

  private TextBox CreateLabelTextBox(
      string text,
      double fontSize,
      ThemeResources theme,
      double minWidth,
      object tag)
  {
    var viewport = _getViewport();
    var scale = viewport.Zoom;

    return new TextBox
    {
      Text = text,
      FontSize = fontSize,
      Foreground = theme.NodeText,
      Background = Brushes.White,
      BorderThickness = new Thickness(1),
      BorderBrush = theme.NodeSelectedBorder,
      Padding = new Thickness(4 * scale, 2 * scale),
      MinWidth = minWidth,
      HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
      VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
      Tag = tag
    };
  }

  private static void SetupTextBoxHandlers(TextBox textBox, Action onCommit, Action onCancel)
  {
    bool finished = false;

    void Commit()
    {
      if (finished) return;
      finished = true;
      onCommit();
    }

    void Cancel()
    {
      if (finished) return;
      finished = true;
      onCancel();
    }

    textBox.KeyDown += (s, e) =>
    {
      if (e.Key == global::Avalonia.Input.Key.Enter)
      {
        Commit();
        e.Handled = true;
      }
      else if (e.Key == global::Avalonia.Input.Key.Escape)
      {
        Cancel();
        e.Handled = true;
      }
    };

    textBox.LostFocus += (s, e) => Commit();
  }

  private static void FocusAndSelectAll(TextBox textBox)
  {
    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
    {
      textBox.Focus();
      textBox.SelectAll();
    }, global::Avalonia.Threading.DispatcherPriority.Render);
  }

  #endregion
}

/// <summary>
/// Event args for when a node's label is committed.
/// </summary>
public class NodeLabelCommittedEventArgs : EventArgs
{
  /// <summary>
  /// Creates new event args.
  /// </summary>
  public NodeLabelCommittedEventArgs(Node node, string? newLabel)
  {
    Node = node;
    NewLabel = newLabel;
  }

  /// <summary>
  /// The node that was edited.
  /// </summary>
  public Node Node { get; }

  /// <summary>
  /// The new label value (null if cleared).
  /// </summary>
  public string? NewLabel { get; }
}

/// <summary>
/// Event args for when an edge's label is committed.
/// </summary>
public class EdgeLabelCommittedEventArgs : EventArgs
{
  /// <summary>
  /// Creates new event args.
  /// </summary>
  public EdgeLabelCommittedEventArgs(Edge edge, string? newLabel)
  {
    Edge = edge;
    NewLabel = newLabel;
  }

  /// <summary>
  /// The edge that was edited.
  /// </summary>
  public Edge Edge { get; }

  /// <summary>
  /// The new label value (null if cleared).
  /// </summary>
  public string? NewLabel { get; }
}

/// <summary>
/// Event args for when a shape's text is committed.
/// </summary>
public class ShapeTextCommittedEventArgs : EventArgs
{
  /// <summary>
  /// Creates new event args.
  /// </summary>
  public ShapeTextCommittedEventArgs(Core.Elements.Shapes.ShapeElement shape, string? newText)
  {
    Shape = shape;
    NewText = newText;
  }

  /// <summary>
  /// The shape that was edited.
  /// </summary>
  public Core.Elements.Shapes.ShapeElement Shape { get; }

  /// <summary>
  /// The new text value.
  /// </summary>
  public string? NewText { get; }
}
