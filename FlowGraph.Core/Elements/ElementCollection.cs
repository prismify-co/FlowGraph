using System.Collections.ObjectModel;
using System.Collections.Specialized;

// Import types from parent namespace
using FlowGraph.Core;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Core.Elements;

/// <summary>
/// An observable collection of canvas elements with typed accessors and lookup methods.
/// </summary>
/// <remarks>
/// <para>
/// This collection is the core data structure for the canvas-first architecture.
/// It holds all elements (nodes, edges, shapes, etc.) and provides efficient
/// lookup and filtering methods.
/// </para>
/// <para>
/// Supports batch operations via <see cref="AddRange"/> for efficient bulk updates.
/// </para>
/// </remarks>
public class ElementCollection : ObservableCollection<ICanvasElement>
{
  private bool _suppressNotifications;
  private readonly Dictionary<string, ICanvasElement> _idLookup = new();
  
  // PERFORMANCE: Maintain typed lists for O(1) access instead of O(n) OfType<> filtering
  private readonly List<Node> _nodeList = new();
  private readonly List<Edge> _edgeList = new();
  private readonly List<ShapeElement> _shapeList = new();
  
  // PERFORMANCE: Index edges by source/target node for O(1) edge lookup
  private readonly Dictionary<string, List<Edge>> _edgesBySourceNode = new();
  private readonly Dictionary<string, List<Edge>> _edgesByTargetNode = new();

  /// <summary>
  /// Adds a range of elements without firing individual notifications.
  /// A single Reset notification is fired after all elements are added.
  /// </summary>
  /// <param name="elements">The elements to add.</param>
  public void AddRange(IEnumerable<ICanvasElement> elements)
  {
    ArgumentNullException.ThrowIfNull(elements);

    _suppressNotifications = true;
    try
    {
      foreach (var element in elements)
      {
        Items.Add(element);
        _idLookup[element.Id] = element;
        AddToTypedLists(element);
      }
    }
    finally
    {
      _suppressNotifications = false;
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
  }

  /// <summary>
  /// Removes a range of elements without firing individual notifications.
  /// A single Reset notification is fired after all elements are removed.
  /// </summary>
  /// <param name="elements">The elements to remove.</param>
  public void RemoveRange(IEnumerable<ICanvasElement> elements)
  {
    ArgumentNullException.ThrowIfNull(elements);

    _suppressNotifications = true;
    try
    {
      foreach (var element in elements)
      {
        _idLookup.Remove(element.Id);
        RemoveFromTypedLists(element);
        Items.Remove(element);
      }
    }
    finally
    {
      _suppressNotifications = false;
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
  }

  /// <summary>
  /// Clears all elements and adds the specified elements.
  /// </summary>
  /// <param name="elements">The elements to set.</param>
  public void ReplaceAll(IEnumerable<ICanvasElement> elements)
  {
    ArgumentNullException.ThrowIfNull(elements);

    _suppressNotifications = true;
    try
    {
      Items.Clear();
      _idLookup.Clear();
      _nodeList.Clear();
      _edgeList.Clear();
      _shapeList.Clear();
      _edgesBySourceNode.Clear();
      _edgesByTargetNode.Clear();
      foreach (var element in elements)
      {
        Items.Add(element);
        _idLookup[element.Id] = element;
        AddToTypedLists(element);
      }
    }
    finally
    {
      _suppressNotifications = false;
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
  }

  /// <inheritdoc />
  protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
  {
    if (!_suppressNotifications)
    {
      base.OnCollectionChanged(e);
    }
  }

  /// <inheritdoc />
  protected override void InsertItem(int index, ICanvasElement item)
  {
    _idLookup[item.Id] = item;
    AddToTypedLists(item);
    base.InsertItem(index, item);
  }

  /// <inheritdoc />
  protected override void RemoveItem(int index)
  {
    var item = Items[index];
    _idLookup.Remove(item.Id);
    RemoveFromTypedLists(item);
    base.RemoveItem(index);
  }

  /// <inheritdoc />
  protected override void SetItem(int index, ICanvasElement item)
  {
    var oldItem = Items[index];
    _idLookup.Remove(oldItem.Id);
    RemoveFromTypedLists(oldItem);
    _idLookup[item.Id] = item;
    AddToTypedLists(item);
    base.SetItem(index, item);
  }

  /// <inheritdoc />
  protected override void ClearItems()
  {
    _idLookup.Clear();
    _nodeList.Clear();
    _edgeList.Clear();
    _shapeList.Clear();
    _edgesBySourceNode.Clear();
    _edgesByTargetNode.Clear();
    base.ClearItems();
  }
  
  private void AddToTypedLists(ICanvasElement item)
  {
    if (item is Node node)
    {
      _nodeList.Add(node);
    }
    else if (item is Edge edge)
    {
      _edgeList.Add(edge);
      
      // Index by source node
      if (!_edgesBySourceNode.TryGetValue(edge.Source, out var sourceList))
      {
        sourceList = new List<Edge>();
        _edgesBySourceNode[edge.Source] = sourceList;
      }
      sourceList.Add(edge);
      
      // Index by target node
      if (!_edgesByTargetNode.TryGetValue(edge.Target, out var targetList))
      {
        targetList = new List<Edge>();
        _edgesByTargetNode[edge.Target] = targetList;
      }
      targetList.Add(edge);
    }
    else if (item is ShapeElement shape)
    {
      _shapeList.Add(shape);
    }
  }
  
  private void RemoveFromTypedLists(ICanvasElement item)
  {
    if (item is Node node)
    {
      _nodeList.Remove(node);
    }
    else if (item is Edge edge)
    {
      _edgeList.Remove(edge);
      
      // Remove from source index
      if (_edgesBySourceNode.TryGetValue(edge.Source, out var sourceList))
      {
        sourceList.Remove(edge);
        if (sourceList.Count == 0)
          _edgesBySourceNode.Remove(edge.Source);
      }
      
      // Remove from target index
      if (_edgesByTargetNode.TryGetValue(edge.Target, out var targetList))
      {
        targetList.Remove(edge);
        if (targetList.Count == 0)
          _edgesByTargetNode.Remove(edge.Target);
      }
    }
    else if (item is ShapeElement shape)
    {
      _shapeList.Remove(shape);
    }
  }

  #region Typed Accessors

  /// <summary>
  /// Gets all node elements in the collection.
  /// PERFORMANCE: Returns cached list for O(1) access instead of O(n) OfType filtering.
  /// </summary>
  public IReadOnlyList<Node> Nodes => _nodeList;
  
  /// <summary>
  /// Gets the count of nodes in the collection. O(1) operation.
  /// </summary>
  public int NodeCount => _nodeList.Count;

  /// <summary>
  /// Gets all edge elements in the collection.
  /// PERFORMANCE: Returns cached list for O(1) access instead of O(n) OfType filtering.
  /// </summary>
  public IReadOnlyList<Edge> Edges => _edgeList;
  
  /// <summary>
  /// Gets the count of edges in the collection. O(1) operation.
  /// </summary>
  public int EdgeCount => _edgeList.Count;

  /// <summary>
  /// Gets all shape elements in the collection.
  /// PERFORMANCE: Returns cached list for O(1) access instead of O(n) OfType filtering.
  /// </summary>
  public IReadOnlyList<ShapeElement> Shapes => _shapeList;
  
  /// <summary>
  /// Gets the count of shapes in the collection. O(1) operation.
  /// </summary>
  public int ShapeCount => _shapeList.Count;
  
  /// <summary>
  /// Gets all edges connected to a specific node (either as source or target).
  /// PERFORMANCE: O(1) lookup using edge index instead of O(n) iteration.
  /// </summary>
  /// <param name="nodeId">The ID of the node to find edges for.</param>
  /// <returns>Enumerable of edges connected to the node.</returns>
  public IEnumerable<Edge> GetEdgesForNode(string nodeId)
  {
    if (_edgesBySourceNode.TryGetValue(nodeId, out var sourceEdges))
    {
      foreach (var edge in sourceEdges)
        yield return edge;
    }
    if (_edgesByTargetNode.TryGetValue(nodeId, out var targetEdges))
    {
      foreach (var edge in targetEdges)
        yield return edge;
    }
  }
  
  /// <summary>
  /// Gets all edges connected to any of the specified nodes.
  /// PERFORMANCE: O(k) where k is number of connected edges, instead of O(n) iteration over all edges.
  /// </summary>
  /// <param name="nodeIds">The IDs of nodes to find edges for.</param>
  /// <returns>List of distinct edges connected to any of the specified nodes.</returns>
  public List<Edge> GetEdgesForNodes(IEnumerable<string> nodeIds)
  {
    var result = new HashSet<Edge>();
    foreach (var nodeId in nodeIds)
    {
      if (_edgesBySourceNode.TryGetValue(nodeId, out var sourceEdges))
      {
        foreach (var edge in sourceEdges)
          result.Add(edge);
      }
      if (_edgesByTargetNode.TryGetValue(nodeId, out var targetEdges))
      {
        foreach (var edge in targetEdges)
          result.Add(edge);
      }
    }
    return result.ToList();
  }

  /// <summary>
  /// Gets all elements of the specified type.
  /// </summary>
  /// <typeparam name="T">The element type to filter by.</typeparam>
  public IEnumerable<T> OfElementType<T>() where T : ICanvasElement => this.OfType<T>();

  #endregion

  #region Lookup Methods

  /// <summary>
  /// Finds an element by its ID.
  /// </summary>
  /// <param name="id">The element ID to search for.</param>
  /// <returns>The element if found; otherwise, null.</returns>
  public ICanvasElement? FindById(string id)
  {
    return _idLookup.TryGetValue(id, out var element) ? element : null;
  }

  /// <summary>
  /// Finds an element by its ID and casts to the specified type.
  /// </summary>
  /// <typeparam name="T">The expected element type.</typeparam>
  /// <param name="id">The element ID to search for.</param>
  /// <returns>The element if found and of the correct type; otherwise, null.</returns>
  public T? FindById<T>(string id) where T : class, ICanvasElement
  {
    return _idLookup.TryGetValue(id, out var element) ? element as T : null;
  }

  /// <summary>
  /// Determines whether an element with the specified ID exists.
  /// </summary>
  /// <param name="id">The element ID to check.</param>
  public bool ContainsId(string id) => _idLookup.ContainsKey(id);

  /// <summary>
  /// Gets all selected elements.
  /// </summary>
  public IEnumerable<ICanvasElement> Selected => this.Where(e => e.IsSelected);

  /// <summary>
  /// Gets all visible elements.
  /// </summary>
  public IEnumerable<ICanvasElement> Visible => this.Where(e => e.IsVisible);

  /// <summary>
  /// Gets all elements sorted by Z-index (rendering order).
  /// </summary>
  public IEnumerable<ICanvasElement> ByZIndex => this.OrderBy(e => e.ZIndex);

  /// <summary>
  /// Finds all elements within the specified bounds.
  /// </summary>
  /// <param name="bounds">The bounds to search within.</param>
  /// <param name="includePartial">If true, includes elements that partially intersect the bounds.</param>
  public IEnumerable<ICanvasElement> FindInBounds(Rect bounds, bool includePartial = false)
  {
    foreach (var element in this)
    {
      var elementBounds = element.GetBounds();
      if (includePartial)
      {
        if (elementBounds.Intersects(bounds))
          yield return element;
      }
      else
      {
        if (bounds.Contains(new Point(elementBounds.X, elementBounds.Y)) &&
            bounds.Contains(new Point(elementBounds.Right, elementBounds.Bottom)))
          yield return element;
      }
    }
  }

  #endregion
}
