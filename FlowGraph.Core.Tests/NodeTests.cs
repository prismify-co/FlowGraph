namespace FlowGraph.Core.Tests;

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
}
