namespace FlowGraph.Core;

public record struct Point(double X, double Y)
{
    public static Point Zero => new(0, 0);

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
}
