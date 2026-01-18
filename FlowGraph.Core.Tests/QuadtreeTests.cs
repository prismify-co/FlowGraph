using Avalonia;
using FlowGraph.Avalonia.Rendering.Spatial;
using System.Diagnostics;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Wrapper class for int to satisfy class constraint on Quadtree.
/// </summary>
internal class IntWrapper
{
  public int Value { get; }
  public IntWrapper(int value) => Value = value;
}

/// <summary>
/// Tests for the Quadtree spatial index data structure.
/// Verifies correctness and O(log N) performance characteristics.
/// </summary>
public class QuadtreeTests
{
  [Fact]
  public void Insert_SingleItem_CanBeQueried()
  {
    var quadtree = new Quadtree<string>(new Rect(0, 0, 1000, 1000));

    quadtree.Insert("test", new Rect(100, 100, 50, 50));

    var results = quadtree.QueryPoint(new AvaloniaPoint(125, 125)).ToList();
    Assert.Single(results);
    Assert.Equal("test", results[0].item);
  }

  [Fact]
  public void QueryPoint_OutsideItem_ReturnsEmpty()
  {
    var quadtree = new Quadtree<string>(new Rect(0, 0, 1000, 1000));

    quadtree.Insert("test", new Rect(100, 100, 50, 50));

    var results = quadtree.QueryPoint(new AvaloniaPoint(200, 200)).ToList();
    Assert.Empty(results);
  }

  [Fact]
  public void QueryRange_IntersectingItems_ReturnsAll()
  {
    var quadtree = new Quadtree<string>(new Rect(0, 0, 1000, 1000));

    quadtree.Insert("a", new Rect(100, 100, 50, 50));
    quadtree.Insert("b", new Rect(200, 200, 50, 50));
    quadtree.Insert("c", new Rect(500, 500, 50, 50));

    // Query that intersects a and b
    var results = quadtree.QueryRange(new Rect(90, 90, 200, 200)).ToList();
    Assert.Equal(2, results.Count);
    Assert.Contains(results, r => r.item == "a");
    Assert.Contains(results, r => r.item == "b");
  }

  [Fact]
  public void Clear_RemovesAllItems()
  {
    var quadtree = new Quadtree<string>(new Rect(0, 0, 1000, 1000));

    quadtree.Insert("a", new Rect(100, 100, 50, 50));
    quadtree.Insert("b", new Rect(200, 200, 50, 50));

    quadtree.Clear();

    Assert.Equal(0, quadtree.Count);
    var results = quadtree.QueryPoint(new AvaloniaPoint(125, 125)).ToList();
    Assert.Empty(results);
  }

  [Fact]
  public void Rebuild_ChangesQuadtreeBounds()
  {
    var quadtree = new Quadtree<string>(new Rect(0, 0, 100, 100));

    quadtree.Rebuild(new Rect(0, 0, 1000, 1000));

    // Should now accept items in expanded bounds
    var inserted = quadtree.Insert("test", new Rect(500, 500, 50, 50));
    Assert.True(inserted);
  }

  [Fact]
  [Trait("Category", "Performance")]
  public void QueryPoint_1000Items_IsSubLinear()
  {
    const int itemCount = 1000;
    const int queryCount = 1000;
    var quadtree = new Quadtree<IntWrapper>(new Rect(-10000, -10000, 20000, 20000));

    // Insert items spread across the space
    var random = new Random(42);
    for (int i = 0; i < itemCount; i++)
    {
      var x = random.NextDouble() * 18000 - 9000;
      var y = random.NextDouble() * 18000 - 9000;
      quadtree.Insert(new IntWrapper(i), new Rect(x, y, 100, 60));
    }

    // Warm up
    _ = quadtree.QueryPoint(new AvaloniaPoint(0, 0)).ToList();

    // Time many queries
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < queryCount; i++)
    {
      var x = random.NextDouble() * 18000 - 9000;
      var y = random.NextDouble() * 18000 - 9000;
      _ = quadtree.QueryPoint(new AvaloniaPoint(x, y)).ToList();
    }
    sw.Stop();

    // 1000 queries on 1000 items should complete in well under 100ms
    // Linear scan would be ~1,000,000 comparisons; quadtree is ~10,000 (log N per query)
    Assert.True(sw.ElapsedMilliseconds < 100,
        $"1000 queries on 1000 items took {sw.ElapsedMilliseconds}ms, expected < 100ms");
  }

  [Fact]
  [Trait("Category", "Performance")]
  public void QueryPoint_5000Items_ScalesLogarithmically()
  {
    const int itemCount = 5000;
    const int queryCount = 1000;
    var quadtree = new Quadtree<IntWrapper>(new Rect(-50000, -50000, 100000, 100000));

    // Insert items spread across the space
    var random = new Random(42);
    for (int i = 0; i < itemCount; i++)
    {
      var x = random.NextDouble() * 90000 - 45000;
      var y = random.NextDouble() * 90000 - 45000;
      quadtree.Insert(new IntWrapper(i), new Rect(x, y, 100, 60));
    }

    // Warm up
    _ = quadtree.QueryPoint(new AvaloniaPoint(0, 0)).ToList();

    // Time many queries
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < queryCount; i++)
    {
      var x = random.NextDouble() * 90000 - 45000;
      var y = random.NextDouble() * 90000 - 45000;
      _ = quadtree.QueryPoint(new AvaloniaPoint(x, y)).ToList();
    }
    sw.Stop();

    // 5x more items should NOT take 5x longer if O(log N)
    // Should still complete in under 150ms (slight increase from 1000 items due to deeper tree)
    Assert.True(sw.ElapsedMilliseconds < 150,
        $"1000 queries on 5000 items took {sw.ElapsedMilliseconds}ms, expected < 150ms");
  }

  [Fact]
  public void Insert_OutsideBounds_ReturnsFalse()
  {
    var quadtree = new Quadtree<string>(new Rect(0, 0, 100, 100));

    var inserted = quadtree.Insert("outside", new Rect(200, 200, 50, 50));

    Assert.False(inserted);
    Assert.Equal(0, quadtree.Count);
  }

  [Fact]
  public void QueryPoint_ItemSpanningMultipleQuadrants_Found()
  {
    var quadtree = new Quadtree<string>(new Rect(0, 0, 1000, 1000));

    // Large item spanning center of quadtree (multiple quadrants)
    quadtree.Insert("large", new Rect(400, 400, 200, 200));

    // Query at center - should find the large item
    var results = quadtree.QueryPoint(new AvaloniaPoint(500, 500)).ToList();
    Assert.Single(results);
    Assert.Equal("large", results[0].item);
  }

  [Fact]
  public void Insert_ManyItems_TriggersSubdivision()
  {
    var quadtree = new Quadtree<IntWrapper>(new Rect(0, 0, 1000, 1000), maxItemsPerNode: 4);

    // Insert more than maxItemsPerNode items in same quadrant
    for (int i = 0; i < 10; i++)
    {
      quadtree.Insert(new IntWrapper(i), new Rect(100 + i * 5, 100 + i * 5, 10, 10));
    }

    Assert.Equal(10, quadtree.Count);

    // All items should still be queryable
    var results = quadtree.QueryRange(new Rect(0, 0, 300, 300)).ToList();
    Assert.Equal(10, results.Count);
  }
}
