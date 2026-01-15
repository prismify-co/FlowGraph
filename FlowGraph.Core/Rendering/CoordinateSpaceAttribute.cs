namespace FlowGraph.Core.Rendering;

/// <summary>
/// Specifies which coordinate space a parameter, property, or return value uses.
/// </summary>
public enum CoordinateSpace
{
    /// <summary>
    /// Canvas coordinates - logical positions independent of zoom/pan.
    /// Node positions and graph geometry use this space.
    /// </summary>
    Canvas,
    
    /// <summary>
    /// Screen coordinates - pixel positions after zoom/pan transforms.
    /// Pointer events and direct rendering use this space.
    /// </summary>
    Screen,
    
    /// <summary>
    /// Local coordinates - relative to a parent element's origin.
    /// Port positions relative to their node use this space.
    /// </summary>
    Local
}

/// <summary>
/// Marks a parameter, property, or method with its expected coordinate space.
/// Use this attribute for documentation and potential static analysis tools.
/// </summary>
/// <example>
/// <code>
/// void HandleClick([CoordinateSpace(CoordinateSpace.Canvas)] Point canvasPos);
/// 
/// [CoordinateSpace(CoordinateSpace.Screen)]
/// Point PointerPosition { get; }
/// 
/// [return: CoordinateSpace(CoordinateSpace.Canvas)]
/// Point ScreenToCanvas(Point screenPoint);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | 
                AttributeTargets.ReturnValue | AttributeTargets.Method)]
public class CoordinateSpaceAttribute : Attribute
{
    /// <summary>
    /// The coordinate space this element operates in.
    /// </summary>
    public CoordinateSpace Space { get; }
    
    /// <summary>
    /// Creates a new CoordinateSpaceAttribute with the specified space.
    /// </summary>
    /// <param name="space">The coordinate space.</param>
    public CoordinateSpaceAttribute(CoordinateSpace space)
    {
        Space = space;
    }
}
