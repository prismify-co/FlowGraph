namespace FlowGraph.Avalonia.Animation;

/// <summary>
/// Represents an animation that can be updated over time.
/// </summary>
public interface IAnimation
{
    /// <summary>
    /// Gets whether the animation has completed.
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Updates the animation with the elapsed time since the last frame.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    void Update(double deltaTime);

    /// <summary>
    /// Called when the animation completes or is cancelled.
    /// </summary>
    void Cancel();
}

/// <summary>
/// Easing functions for animations.
/// </summary>
public static class Easing
{
    /// <summary>
    /// Linear interpolation (no easing).
    /// </summary>
    public static double Linear(double t) => t;

    /// <summary>
    /// Quadratic ease-in.
    /// </summary>
    public static double EaseInQuad(double t) => t * t;

    /// <summary>
    /// Quadratic ease-out.
    /// </summary>
    public static double EaseOutQuad(double t) => 1 - (1 - t) * (1 - t);

    /// <summary>
    /// Quadratic ease-in-out.
    /// </summary>
    public static double EaseInOutQuad(double t) =>
        t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;

    /// <summary>
    /// Cubic ease-in.
    /// </summary>
    public static double EaseInCubic(double t) => t * t * t;

    /// <summary>
    /// Cubic ease-out.
    /// </summary>
    public static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    /// <summary>
    /// Cubic ease-in-out.
    /// </summary>
    public static double EaseInOutCubic(double t) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    /// <summary>
    /// Exponential ease-out - smooth deceleration.
    /// </summary>
    public static double EaseOutExpo(double t) =>
        t >= 1 ? 1 : 1 - Math.Pow(2, -10 * t);

    /// <summary>
    /// Exponential ease-in-out.
    /// </summary>
    public static double EaseInOutExpo(double t) =>
        t <= 0 ? 0 :
        t >= 1 ? 1 :
        t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2 :
        (2 - Math.Pow(2, -20 * t + 10)) / 2;

    /// <summary>
    /// Back ease-out - overshoots then settles.
    /// </summary>
    public static double EaseOutBack(double t)
    {
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
    }

    /// <summary>
    /// Elastic ease-out - spring-like bounce.
    /// </summary>
    public static double EaseOutElastic(double t)
    {
        const double c4 = (2 * Math.PI) / 3;
        return t <= 0 ? 0 :
               t >= 1 ? 1 :
               Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1;
    }
}
