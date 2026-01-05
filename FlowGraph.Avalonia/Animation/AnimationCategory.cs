namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Categorizes animations by their type for state management.
/// </summary>
public enum AnimationCategory
{
    /// <summary>
    /// Viewport animations (pan, zoom, fit-to-view).
    /// </summary>
    Viewport,

    /// <summary>
    /// Node position animations.
    /// </summary>
    NodePosition,

    /// <summary>
    /// Node appearance animations (fade, scale).
    /// </summary>
    NodeAppearance,

    /// <summary>
    /// Edge animations (fade, pulse, flow).
    /// </summary>
    Edge,

    /// <summary>
    /// Group animations (collapse, expand).
    /// </summary>
    Group,

    /// <summary>
    /// Layout transition animations.
    /// </summary>
    Layout,

    /// <summary>
    /// Generic/other animations.
    /// </summary>
    Other
}

/// <summary>
/// Extended animation interface that includes category information.
/// </summary>
public interface ICategorizedAnimation : IAnimation
{
    /// <summary>
    /// Gets the category of this animation.
    /// </summary>
    AnimationCategory Category { get; }

    /// <summary>
    /// Gets the IDs of nodes affected by this animation (if applicable).
    /// </summary>
    IReadOnlyCollection<string> AffectedNodeIds { get; }
}

/// <summary>
/// Wrapper that adds category information to any animation.
/// </summary>
public class CategorizedAnimationWrapper : ICategorizedAnimation
{
    private readonly IAnimation _inner;

    public AnimationCategory Category { get; }
    public IReadOnlyCollection<string> AffectedNodeIds { get; }
    public bool IsComplete => _inner.IsComplete;

    public CategorizedAnimationWrapper(
        IAnimation animation, 
        AnimationCategory category,
        IEnumerable<string>? affectedNodeIds = null)
    {
        _inner = animation;
        Category = category;
        AffectedNodeIds = affectedNodeIds?.ToList() ?? [];
    }

    public void Update(double deltaTime) => _inner.Update(deltaTime);
    public void Cancel() => _inner.Cancel();
}
