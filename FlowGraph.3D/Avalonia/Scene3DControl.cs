using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using FlowGraph.ThreeD.Abstractions;
using FlowGraph.ThreeD.Scenes;
using Silk.NET.OpenGL;

namespace FlowGraph.ThreeD.Avalonia;

/// <summary>
/// An Avalonia control that renders a 3D scene using OpenGL.
/// </summary>
public class Scene3DControl : OpenGlControlBase
{
  /// <summary>
  /// Defines the Parameters property.
  /// </summary>
  public static readonly StyledProperty<SceneParameters> ParametersProperty =
      AvaloniaProperty.Register<Scene3DControl, SceneParameters>(
          nameof(Parameters),
          SceneParameters.Default);

  /// <summary>
  /// Gets or sets the scene parameters.
  /// </summary>
  public SceneParameters Parameters
  {
    get => GetValue(ParametersProperty);
    set => SetValue(ParametersProperty, value);
  }

  private GL? _gl;
  private IScene3D? _scene;
  private readonly Stopwatch _stopwatch = new();
  private double _lastFrameTime;
  private DispatcherTimer? _animationTimer;

  /// <summary>
  /// Initializes a new instance of Scene3DControl.
  /// </summary>
  public Scene3DControl()
  {
    // Request continuous rendering for animations
    _stopwatch.Start();
  }

  /// <inheritdoc />
  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);

    if (change.Property == ParametersProperty)
    {
      // Start or stop animation timer based on AutoRotate
      var newParams = change.GetNewValue<SceneParameters>();
      UpdateAnimationTimer(newParams?.AutoRotate ?? false);
      RequestNextFrameRendering();
    }
    else if (change.Property == BoundsProperty)
    {
      var newBounds = change.GetNewValue<Rect>();
      _scene?.Resize((int)newBounds.Width, (int)newBounds.Height);
      RequestNextFrameRendering();
    }
  }

  private void UpdateAnimationTimer(bool autoRotate)
  {
    if (autoRotate)
    {
      if (_animationTimer == null)
      {
        _animationTimer = new DispatcherTimer
        {
          Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _animationTimer.Tick += (_, _) => RequestNextFrameRendering();
      }
      _animationTimer.Start();
    }
    else
    {
      _animationTimer?.Stop();
    }
  }

  /// <inheritdoc />
  protected override void OnOpenGlInit(GlInterface gl)
  {
    base.OnOpenGlInit(gl);

    try
    {
      Console.WriteLine("Scene3DControl: Starting OpenGL initialization...");

      // Create Silk.NET GL context from Avalonia's context
      _gl = GL.GetApi(gl.GetProcAddress);
      Console.WriteLine($"Scene3DControl: GL context created, version: {_gl.GetStringS(StringName.Version)}");

      // Create and initialize the scene
      _scene = new ShapeScene();
      _scene.Initialize(_gl);
      _scene.InvalidateRequested += (_, _) => RequestNextFrameRendering();

      // Initialize with current size
      var bounds = Bounds;
      _scene.Resize((int)bounds.Width, (int)bounds.Height);

      // Start animation if needed
      UpdateAnimationTimer(Parameters.AutoRotate);

      Console.WriteLine("Scene3DControl: OpenGL initialization successful");
      Debug.WriteLine("Scene3DControl: OpenGL initialization successful");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Scene3DControl: OpenGL initialization failed: {ex.Message}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      Debug.WriteLine($"Scene3DControl: OpenGL initialization failed: {ex.Message}");
      Debug.WriteLine($"Stack trace: {ex.StackTrace}");
      _scene = null;
      _gl = null;
    }
  }

  /// <inheritdoc />
  protected override void OnOpenGlDeinit(GlInterface gl)
  {
    _animationTimer?.Stop();
    _animationTimer = null;

    _scene?.Dispose();
    _scene = null;
    _gl?.Dispose();
    _gl = null;

    base.OnOpenGlDeinit(gl);
  }

  /// <inheritdoc />
  protected override void OnOpenGlRender(GlInterface gl, int fb)
  {
    if (_gl == null || _scene == null)
    {
      // If scene not initialized, clear with a visible color for debugging
      var tempGl = GL.GetApi(gl.GetProcAddress);
      tempGl.ClearColor(0.3f, 0.1f, 0.1f, 1.0f); // Dark red to show something is wrong
      tempGl.Clear(ClearBufferMask.ColorBufferBit);
      return;
    }

    // Calculate delta time
    var currentTime = _stopwatch.Elapsed.TotalSeconds;
    var deltaTime = currentTime - _lastFrameTime;
    _lastFrameTime = currentTime;

    // Clamp delta time to avoid large jumps
    deltaTime = Math.Min(deltaTime, 0.1);

    // Update and render scene
    _scene.Update(Parameters, deltaTime);
    _scene.Render();
  }
}
