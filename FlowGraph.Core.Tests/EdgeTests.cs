namespace FlowGraph.Core.Tests;

public class EdgeTests
{
    [Fact]
    public void Edge_ShouldHaveUniqueId()
    {
        var edge1 = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1"
        };

        var edge2 = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1"
        };

        Assert.NotEqual(edge1.Id, edge2.Id);
    }

    [Fact]
    public void Edge_ShouldStoreSourceAndTarget()
    {
        var edge = new Edge
        {
            Source = "source-node",
            Target = "target-node",
            SourcePort = "output-port",
            TargetPort = "input-port"
        };

        Assert.Equal("source-node", edge.Source);
        Assert.Equal("target-node", edge.Target);
        Assert.Equal("output-port", edge.SourcePort);
        Assert.Equal("input-port", edge.TargetPort);
    }

    [Fact]
    public void Edge_ShouldHaveDefaultEdgeType_Bezier()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1"
        };

        Assert.Equal(EdgeType.Bezier, edge.Type);
    }

    [Fact]
    public void Edge_ShouldAllowSettingEdgeType()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1",
            Type = EdgeType.Step
        };

        Assert.Equal(EdgeType.Step, edge.Type);
    }

    [Fact]
    public void Edge_ShouldHaveDefaultMarkers()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1"
        };

        Assert.Equal(EdgeMarker.None, edge.MarkerStart);
        Assert.Equal(EdgeMarker.Arrow, edge.MarkerEnd);
    }

    [Fact]
    public void Edge_ShouldAllowSettingMarkers()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1",
            MarkerStart = EdgeMarker.Arrow,
            MarkerEnd = EdgeMarker.ArrowClosed
        };

        Assert.Equal(EdgeMarker.Arrow, edge.MarkerStart);
        Assert.Equal(EdgeMarker.ArrowClosed, edge.MarkerEnd);
    }

    [Fact]
    public void Edge_ShouldHaveNullLabelByDefault()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1"
        };

        Assert.Null(edge.Label);
    }

    [Fact]
    public void Edge_ShouldAllowSettingLabel()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1",
            Label = "Connection Label"
        };

        Assert.Equal("Connection Label", edge.Label);
    }

    [Fact]
    public void Edge_IsSelected_ShouldBeFalseByDefault()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1"
        };

        Assert.False(edge.IsSelected);
    }

    [Fact]
    public void Edge_IsSelected_ShouldBeSettable()
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1",
            IsSelected = true
        };

        Assert.True(edge.IsSelected);
    }

    [Theory]
    [InlineData(EdgeType.Bezier)]
    [InlineData(EdgeType.Straight)]
    [InlineData(EdgeType.Step)]
    [InlineData(EdgeType.SmoothStep)]
    public void EdgeType_AllValuesShouldBeValid(EdgeType edgeType)
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1",
            Type = edgeType
        };

        Assert.Equal(edgeType, edge.Type);
    }

    [Theory]
    [InlineData(EdgeMarker.None)]
    [InlineData(EdgeMarker.Arrow)]
    [InlineData(EdgeMarker.ArrowClosed)]
    public void EdgeMarker_AllValuesShouldBeValid(EdgeMarker marker)
    {
        var edge = new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1",
            MarkerEnd = marker
        };

        Assert.Equal(marker, edge.MarkerEnd);
    }
}
