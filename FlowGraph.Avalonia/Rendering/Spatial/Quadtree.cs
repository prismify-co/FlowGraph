using Avalonia;

namespace FlowGraph.Avalonia.Rendering.Spatial;

/// <summary>
/// A Quadtree spatial index for O(log N) point and range queries on 2D rectangles.
/// </summary>
/// <typeparam name="T">The type of item stored in the quadtree.</typeparam>
/// <remarks>
/// <para>
/// This implementation uses a point-region quadtree where each node subdivides
/// into 4 quadrants (NW, NE, SW, SE) when capacity is exceeded.
/// </para>
/// <para>
/// <b>Performance characteristics:</b>
/// <list type="bullet">
/// <item>Insert: O(log N) average, O(N) worst case for degenerate distributions</item>
/// <item>Query: O(log N + k) where k is number of results</item>
/// <item>Clear: O(1)</item>
/// </list>
/// </para>
/// </remarks>
public class Quadtree<T> where T : class
{
  private readonly int _maxItems;
  private readonly int _maxDepth;
  private QuadtreeNode? _root;
  private Rect _bounds;

  /// <summary>
  /// Creates a new quadtree with the specified bounds.
  /// </summary>
  /// <param name="bounds">The bounding rectangle for the entire quadtree.</param>
  /// <param name="maxItemsPerNode">Maximum items per node before subdivision (default 8).</param>
  /// <param name="maxDepth">Maximum tree depth to prevent infinite subdivision (default 10).</param>
  public Quadtree(Rect bounds, int maxItemsPerNode = 8, int maxDepth = 10)
  {
    _bounds = bounds;
    _maxItems = maxItemsPerNode;
    _maxDepth = maxDepth;
    _root = new QuadtreeNode(bounds, 0, _maxItems, _maxDepth);
  }

  /// <summary>
  /// Gets the total number of items in the quadtree.
  /// </summary>
  public int Count { get; private set; }

  /// <summary>
  /// Gets the bounds of the quadtree.
  /// </summary>
  public Rect Bounds => _bounds;

  /// <summary>
  /// Inserts an item with the specified bounds into the quadtree.
  /// </summary>
  /// <param name="item">The item to insert.</param>
  /// <param name="bounds">The bounding rectangle of the item.</param>
  /// <returns>True if inserted successfully, false if outside bounds.</returns>
  public bool Insert(T item, Rect bounds)
  {
    if (_root == null) return false;

    // Items outside the quadtree bounds are rejected
    if (!_bounds.Intersects(bounds))
      return false;

    _root.Insert(item, bounds);
    Count++;
    return true;
  }

  /// <summary>
  /// Queries for all items whose bounds contain the specified point.
  /// </summary>
  /// <param name="point">The point to query.</param>
  /// <returns>All items containing the point.</returns>
  public IEnumerable<(T item, Rect bounds)> QueryPoint(Point point)
  {
    if (_root == null)
      yield break;

    foreach (var result in _root.QueryPoint(point))
      yield return result;
  }

  /// <summary>
  /// Queries for all items whose bounds intersect the specified rectangle.
  /// </summary>
  /// <param name="queryBounds">The rectangle to query.</param>
  /// <returns>All items intersecting the rectangle.</returns>
  public IEnumerable<(T item, Rect bounds)> QueryRange(Rect queryBounds)
  {
    if (_root == null)
      yield break;

    foreach (var result in _root.QueryRange(queryBounds))
      yield return result;
  }

  /// <summary>
  /// Clears all items from the quadtree.
  /// </summary>
  public void Clear()
  {
    _root = new QuadtreeNode(_bounds, 0, _maxItems, _maxDepth);
    Count = 0;
  }

  /// <summary>
  /// Rebuilds the quadtree with new bounds, clearing existing items.
  /// </summary>
  /// <param name="newBounds">The new bounding rectangle.</param>
  public void Rebuild(Rect newBounds)
  {
    _bounds = newBounds;
    _root = new QuadtreeNode(_bounds, 0, _maxItems, _maxDepth);
    Count = 0;
  }

  /// <summary>
  /// Internal node class for the quadtree.
  /// </summary>
  private class QuadtreeNode
  {
    private readonly Rect _bounds;
    private readonly int _depth;
    private readonly int _maxItems;
    private readonly int _maxDepth;
    private List<(T item, Rect bounds)>? _items;
    private QuadtreeNode?[]? _children; // NW, NE, SW, SE

    public QuadtreeNode(Rect bounds, int depth, int maxItems, int maxDepth)
    {
      _bounds = bounds;
      _depth = depth;
      _maxItems = maxItems;
      _maxDepth = maxDepth;
    }

    public void Insert(T item, Rect itemBounds)
    {
      // If we have children, try to insert into them
      if (_children != null)
      {
        int index = GetChildIndex(itemBounds);
        if (index != -1)
        {
          _children[index]?.Insert(item, itemBounds);
          return;
        }
        // Item spans multiple quadrants - store at this level
      }

      // Store at this level
      _items ??= new List<(T, Rect)>();
      _items.Add((item, itemBounds));

      // Subdivide if over capacity and not at max depth
      if (_items.Count > _maxItems && _depth < _maxDepth && _children == null)
      {
        Subdivide();
      }
    }

    public IEnumerable<(T item, Rect bounds)> QueryPoint(Point point)
    {
      // Check if point is in our bounds
      if (!_bounds.Contains(point))
        yield break;

      // Check items at this level
      if (_items != null)
      {
        foreach (var (item, bounds) in _items)
        {
          if (bounds.Contains(point))
            yield return (item, bounds);
        }
      }

      // Recurse into children
      if (_children != null)
      {
        for (int i = 0; i < 4; i++)
        {
          if (_children[i] != null)
          {
            foreach (var result in _children[i]!.QueryPoint(point))
              yield return result;
          }
        }
      }
    }

    public IEnumerable<(T item, Rect bounds)> QueryRange(Rect queryBounds)
    {
      // Check if query intersects our bounds
      if (!_bounds.Intersects(queryBounds))
        yield break;

      // Check items at this level
      if (_items != null)
      {
        foreach (var (item, bounds) in _items)
        {
          if (bounds.Intersects(queryBounds))
            yield return (item, bounds);
        }
      }

      // Recurse into children
      if (_children != null)
      {
        for (int i = 0; i < 4; i++)
        {
          if (_children[i] != null)
          {
            foreach (var result in _children[i]!.QueryRange(queryBounds))
              yield return result;
          }
        }
      }
    }

    private void Subdivide()
    {
      double halfW = _bounds.Width / 2;
      double halfH = _bounds.Height / 2;
      double x = _bounds.X;
      double y = _bounds.Y;

      _children = new QuadtreeNode?[4];
      _children[0] = new QuadtreeNode(new Rect(x, y, halfW, halfH), _depth + 1, _maxItems, _maxDepth);                   // NW
      _children[1] = new QuadtreeNode(new Rect(x + halfW, y, halfW, halfH), _depth + 1, _maxItems, _maxDepth);           // NE
      _children[2] = new QuadtreeNode(new Rect(x, y + halfH, halfW, halfH), _depth + 1, _maxItems, _maxDepth);           // SW
      _children[3] = new QuadtreeNode(new Rect(x + halfW, y + halfH, halfW, halfH), _depth + 1, _maxItems, _maxDepth);   // SE

      // Redistribute existing items
      var itemsToRedistribute = _items;
      _items = null;

      if (itemsToRedistribute != null)
      {
        foreach (var (item, bounds) in itemsToRedistribute)
        {
          Insert(item, bounds);
        }
      }
    }

    /// <summary>
    /// Returns the index of the child quadrant that fully contains the bounds,
    /// or -1 if the bounds spans multiple quadrants.
    /// </summary>
    private int GetChildIndex(Rect itemBounds)
    {
      double midX = _bounds.X + _bounds.Width / 2;
      double midY = _bounds.Y + _bounds.Height / 2;

      bool fitsTop = itemBounds.Bottom <= midY;
      bool fitsBottom = itemBounds.Top >= midY;
      bool fitsLeft = itemBounds.Right <= midX;
      bool fitsRight = itemBounds.Left >= midX;

      if (fitsTop && fitsLeft) return 0;  // NW
      if (fitsTop && fitsRight) return 1; // NE
      if (fitsBottom && fitsLeft) return 2; // SW
      if (fitsBottom && fitsRight) return 3; // SE

      return -1; // Spans multiple quadrants
    }
  }
}
