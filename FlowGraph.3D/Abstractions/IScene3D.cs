using Silk.NET.OpenGL;

namespace FlowGraph.ThreeD.Abstractions;

/// <summary>
/// Interface for a 3D scene that can be rendered.
/// Implementations handle the actual rendering logic.
/// </summary>
public interface IScene3D : IDisposable
{
  /// <summary>
  /// Initializes the scene with the OpenGL context.
  /// Must be called before Render().
  /// </summary>
  /// <param name="gl">The OpenGL context.</param>
  void Initialize(GL gl);

  /// <summary>
  /// Updates the scene state (called each frame before Render).
  /// </summary>
  /// <param name="parameters">The scene parameters.</param>
  /// <param name="deltaTime">Time since last update in seconds.</param>
  void Update(SceneParameters parameters, double deltaTime);

  /// <summary>
  /// Renders the scene to the current OpenGL context.
  /// </summary>
  void Render();

  /// <summary>
  /// Called when the viewport is resized.
  /// </summary>
  /// <param name="width">New width in pixels.</param>
  /// <param name="height">New height in pixels.</param>
  void Resize(int width, int height);

  /// <summary>
  /// Event raised when the scene needs to be re-rendered.
  /// </summary>
  event EventHandler? InvalidateRequested;
}
