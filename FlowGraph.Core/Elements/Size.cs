namespace FlowGraph.Core.Elements;

/// <summary>
/// Represents a size with width and height.
/// Used for viewport and element dimensions.
/// </summary>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct Size(double Width, double Height)
{
    /// <summary>
    /// Gets an empty size (0x0).
    /// </summary>
    public static Size Empty => new(0, 0);
    
    /// <summary>
    /// Gets whether the size is empty (either dimension is zero or negative).
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;
    
    /// <summary>
    /// Creates a size from width and height values.
    /// </summary>
    public static Size FromWidthHeight(double width, double height) => new(width, height);
}
