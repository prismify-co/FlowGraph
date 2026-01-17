namespace FlowGraph.Core.Tests;

using FlowGraph.Core.Events;

public class NodeTests
{
    [Fact]
    public void Node_ShouldHaveUniqueId()
    {
        var node1 = new Node { Type = "Test" };
        var node2 = new Node { Type = "Test" };

        Assert.NotEqual(node1.Id, node2.Id);
    }

    [Fact]
    public void Node_ShouldHaveDefaultPosition()
    {
        var node = new Node { Type = "Test" };

        Assert.Equal(0, node.Position.X);
        Assert.Equal(0, node.Position.Y);
    }

    [Fact]
    public void Node_ShouldHaveEmptyPortCollections()
    {
        var node = new Node { Type = "Test" };

        Assert.Empty(node.Inputs);
        Assert.Empty(node.Outputs);
    }

    [Fact]
    public void Node_Position_ShouldRaisePropertyChanged()
    {
        var node = new Node { Type = "Test" };
        var propertyName = string.Empty;

        node.PropertyChanged += (s, e) => propertyName = e.PropertyName;
        node.Position = new Point(100, 200);

        Assert.Equal(nameof(Node.Position), propertyName);
    }

    [Fact]
    public void Node_IsSelected_ShouldRaisePropertyChanged()
    {
        var node = new Node { Type = "Test" };
        var propertyName = string.Empty;

        node.PropertyChanged += (s, e) => propertyName = e.PropertyName;
        node.IsSelected = true;

        Assert.Equal(nameof(Node.IsSelected), propertyName);
    }

    [Fact]
    public void Node_IsDragging_ShouldRaisePropertyChanged()
    {
        var node = new Node { Type = "Test" };
        var propertyName = string.Empty;

        node.PropertyChanged += (s, e) => propertyName = e.PropertyName;
        node.IsDragging = true;

        Assert.Equal(nameof(Node.IsDragging), propertyName);
    }

    [Fact]
    public void Node_ShouldNotRaisePropertyChanged_WhenSameValue()
    {
        var node = new Node { Type = "Test" };
        node.Position = new Point(100, 100);

        var raised = false;
        node.PropertyChanged += (s, e) => raised = true;
        node.Position = new Point(100, 100);

        Assert.False(raised);
    }

    #region PositionChanged Event Tests

    [Fact]
    public void Node_Position_ShouldRaisePositionChanged()
    {
        // Note: Setting Position triggers two PropertyChanged events (X then Y),
        // so we capture the last event which has both coordinates updated.
        var node = new Node { Type = "Test" };
        PositionChangedEventArgs? lastArgs = null;
        var eventCount = 0;

        node.PositionChanged += (s, e) => { lastArgs = e; eventCount++; };
        node.Position = new Point(100, 200);

        Assert.NotNull(lastArgs);
        // After both X and Y are set, the final position is (100, 200)
        Assert.Equal(100, lastArgs.NewPosition.X);
        Assert.Equal(200, lastArgs.NewPosition.Y);
        // Two events fired (one for X, one for Y)
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void Node_StateX_ShouldRaisePositionChanged()
    {
        var node = new Node { Type = "Test" };
        PositionChangedEventArgs? args = null;

        node.PositionChanged += (s, e) => args = e;
        node.State.X = 50;

        Assert.NotNull(args);
        Assert.Equal(50, args.NewPosition.X);
        Assert.Equal(0, args.NewPosition.Y);
    }

    [Fact]
    public void Node_PositionChanged_ShouldNotRaise_WhenSameValue()
    {
        var node = new Node { Type = "Test" };
        node.Position = new Point(100, 100);

        var raised = false;
        node.PositionChanged += (s, e) => raised = true;
        node.Position = new Point(100, 100);

        Assert.False(raised);
    }

    #endregion

    #region BoundsChanged Event Tests

    [Fact]
    public void Node_Position_ShouldRaiseBoundsChanged()
    {
        var node = new Node { Type = "Test" };
        BoundsChangedEventArgs? args = null;

        node.BoundsChanged += (s, e) => args = e;
        node.Position = new Point(100, 200);

        Assert.NotNull(args);
        Assert.Equal(100, args.NewPosition.X);
        Assert.Equal(200, args.NewPosition.Y);
        Assert.True(args.PositionOnly);
    }

    [Fact]
    public void Node_Size_ShouldRaiseBoundsChanged()
    {
        var node = new Node { Type = "Test" };
        BoundsChangedEventArgs? args = null;

        node.BoundsChanged += (s, e) => args = e;
        node.State.Width = 200;

        Assert.NotNull(args);
        Assert.Null(args.OldWidth);
        Assert.Equal(200, args.NewWidth);
        Assert.False(args.PositionOnly);
    }

    [Fact]
    public void Node_BoundsChanged_IncludesAllBoundsInfo()
    {
        var node = new Node { Type = "Test" };
        node.Position = new Point(10, 20);
        node.State.Width = 100;
        node.State.Height = 50;

        BoundsChangedEventArgs? args = null;
        node.BoundsChanged += (s, e) => args = e;
        node.State.Width = 150;

        Assert.NotNull(args);
        Assert.Equal(10, args.OldPosition.X);
        Assert.Equal(20, args.OldPosition.Y);
        Assert.Equal(100, args.OldWidth);
        Assert.Equal(150, args.NewWidth);
        Assert.Equal(50, args.OldHeight);
        Assert.Equal(50, args.NewHeight);
    }

    #endregion
}
