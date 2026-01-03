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
}
