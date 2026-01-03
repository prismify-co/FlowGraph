namespace FlowGraph.Core.Tests;

public class PortTests
{
    [Fact]
    public void Port_ShouldStoreIdAndType()
    {
        var port = new Port
        {
            Id = "port-1",
            Type = "data"
        };

        Assert.Equal("port-1", port.Id);
        Assert.Equal("data", port.Type);
    }

    [Fact]
    public void Port_Label_ShouldBeOptional()
    {
        var portWithoutLabel = new Port
        {
            Id = "port-1",
            Type = "data"
        };

        var portWithLabel = new Port
        {
            Id = "port-2",
            Type = "data",
            Label = "Input Data"
        };

        Assert.Null(portWithoutLabel.Label);
        Assert.Equal("Input Data", portWithLabel.Label);
    }

    [Fact]
    public void Port_ShouldBeImmutableRecord()
    {
        var port = new Port
        {
            Id = "port-1",
            Type = "data",
            Label = "Test"
        };

        // Records support with-expressions for creating modified copies
        var modifiedPort = port with { Label = "Modified" };

        Assert.Equal("Test", port.Label);
        Assert.Equal("Modified", modifiedPort.Label);
        Assert.Equal(port.Id, modifiedPort.Id);
    }

    [Fact]
    public void Port_Equality_ShouldBeByValue()
    {
        var port1 = new Port { Id = "port-1", Type = "data" };
        var port2 = new Port { Id = "port-1", Type = "data" };
        var port3 = new Port { Id = "port-2", Type = "data" };

        Assert.Equal(port1, port2);
        Assert.NotEqual(port1, port3);
    }
}
