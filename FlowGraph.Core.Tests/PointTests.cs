namespace FlowGraph.Core.Tests;

public class PointTests
{
    [Fact]
    public void Point_Zero_ShouldBeOrigin()
    {
        var zero = Point.Zero;

        Assert.Equal(0, zero.X);
        Assert.Equal(0, zero.Y);
    }

    [Fact]
    public void Point_ShouldStoreCoordinates()
    {
        var point = new Point(10.5, 20.5);

        Assert.Equal(10.5, point.X);
        Assert.Equal(20.5, point.Y);
    }

    [Fact]
    public void Point_Addition_ShouldWork()
    {
        var a = new Point(10, 20);
        var b = new Point(5, 15);

        var result = a + b;

        Assert.Equal(15, result.X);
        Assert.Equal(35, result.Y);
    }

    [Fact]
    public void Point_Subtraction_ShouldWork()
    {
        var a = new Point(10, 20);
        var b = new Point(5, 15);

        var result = a - b;

        Assert.Equal(5, result.X);
        Assert.Equal(5, result.Y);
    }

    [Fact]
    public void Point_Equality_ShouldWork()
    {
        var a = new Point(10, 20);
        var b = new Point(10, 20);
        var c = new Point(10, 21);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Point_ShouldBeValueType()
    {
        var a = new Point(10, 20);
        var b = a;
        b = new Point(30, 40);

        // Original should be unchanged (value type semantics)
        Assert.Equal(10, a.X);
        Assert.Equal(20, a.Y);
    }
}
