using System.Collections.ObjectModel;
using System.Collections.Specialized;

// Import types from parent namespace
using FlowGraph.Core;

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
      foreach (var element in elements)
      {
        Items.Add(element);
        _idLookup[element.Id] = element;
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
    base.InsertItem(index, item);
  }

  /// <inheritdoc />
  protected override void RemoveItem(int index)
  {
    var item = Items[index];
    _idLookup.Remove(item.Id);
    base.RemoveItem(index);
  }

  /// <inheritdoc />
  protected override void SetItem(int index, ICanvasElement item)
  {
    var oldItem = Items[index];
    _idLookup.Remove(oldItem.Id);
    _idLookup[item.Id] = item;
    base.SetItem(index, item);
  }

  /// <inheritdoc />
  protected override void ClearItems()
  {
    _idLookup.Clear();
    base.ClearItems();
  }

  #region Typed Accessors

  /// <summary>
  /// Gets all node elements in the collection.
  /// </summary>
  public IEnumerable<Node> Nodes => this.OfType<Node>();

  /// <summary>
  /// Gets all edge elements in the collection.
  /// </summary>
  public IEnumerable<Edge> Edges => this.OfType<Edge>();

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
