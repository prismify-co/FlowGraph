using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace FlowGraph.Demo.Helpers;

/// <summary>
/// Helper class for capturing screenshots during animations and creating GIFs for debugging.
/// </summary>
public class AnimationDebugger
{
    private readonly Control _targetControl;
    private readonly string _outputDirectory;
    private int _sessionId;
    private int _frameCounter;
    private string _currentAnimationName = "";
    private DateTime _sessionStartTime;
    private readonly List<Image<Rgba32>> _frames = new();

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the frame interval - capture every Nth frame (default: 2).
    /// </summary>
    public int FrameInterval { get; set; } = 2;

    /// <summary>
    /// Gets or sets the GIF frame delay in hundredths of a second (default: 5 = 50ms = 20fps).
    /// </summary>
    public int GifFrameDelay { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to scale down the GIF (0.5 = half size, 1.0 = full size).
    /// </summary>
    public double ScaleFactor { get; set; } = 0.5;

    public AnimationDebugger(Control targetControl, string? outputDirectory = null)
    {
        _targetControl = targetControl;
        _outputDirectory = outputDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "FlowGraph_AnimationDebug");
        
        Directory.CreateDirectory(_outputDirectory);
        _sessionId = 0;
    }

    /// <summary>
    /// Starts a new animation capture session.
    /// </summary>
    /// <param name="animationName">Name of the animation being debugged.</param>
    public void StartSession(string animationName)
    {
        _sessionId++;
        _frameCounter = 0;
        _currentAnimationName = SanitizeFilename(animationName);
        _sessionStartTime = DateTime.Now;
        
        // Dispose any previous frames
        foreach (var frame in _frames)
        {
            frame.Dispose();
        }
        _frames.Clear();
        
        System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Started session {_sessionId}: {animationName}");
        System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Output directory: {_outputDirectory}");
    }

    /// <summary>
    /// Captures a frame with the given phase and progress information.
    /// </summary>
    /// <param name="phase">Current animation phase (e.g., "ContentFade", "Shrink").</param>
    /// <param name="progress">Progress value (0-1).</param>
    /// <param name="details">Additional details to include.</param>
    public void CaptureFrame(string phase, double progress, string? details = null)
    {
        if (!IsEnabled) return;

        _frameCounter++;
        
        // Skip frames based on interval (but always capture first and last)
        if (_frameCounter % FrameInterval != 0 && progress > 0.02 && progress < 0.98)
            return;

        try
        {
            var frame = CaptureFrameToImage();
            if (frame != null)
            {
                _frames.Add(frame);
                var timestamp = (DateTime.Now - _sessionStartTime).TotalMilliseconds;
                System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Captured frame {_frames.Count}: {phase} @ {progress:P0} ({timestamp:F0}ms)");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Failed to capture frame: {ex.Message}");
        }
    }

    /// <summary>
    /// Captures key frames at specific progress points (0%, 25%, 50%, 75%, 100%).
    /// </summary>
    public void CaptureKeyFrame(string phase, double progress, string? details = null)
    {
        var keyPoints = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
        var tolerance = 0.03;
        
        foreach (var keyPoint in keyPoints)
        {
            if (Math.Abs(progress - keyPoint) < tolerance)
            {
                CaptureFrame(phase, progress, details);
                return;
            }
        }
    }

    /// <summary>
    /// Ends the current session, creates a GIF, and logs summary.
    /// </summary>
    public async Task EndSessionAsync()
    {
        var duration = DateTime.Now - _sessionStartTime;
        System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Ending session {_sessionId}: {_currentAnimationName}");
        System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Captured {_frames.Count} frames in {duration.TotalMilliseconds:F0}ms");

        if (_frames.Count > 0)
        {
            var gifPath = Path.Combine(_outputDirectory, $"S{_sessionId:D2}_{_currentAnimationName}_{DateTime.Now:HHmmss}.gif");
            await CreateGifAsync(gifPath);
            System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] GIF saved to: {gifPath}");
        }
        
        // Clean up frames
        foreach (var frame in _frames)
        {
            frame.Dispose();
        }
        _frames.Clear();
    }

    /// <summary>
    /// Ends session synchronously (fire and forget for GIF creation).
    /// </summary>
    public void EndSession()
    {
        _ = EndSessionAsync();
    }

    private Image<Rgba32>? CaptureFrameToImage()
    {
        var bounds = _targetControl.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return null;

        var pixelSize = new PixelSize((int)bounds.Width, (int)bounds.Height);
        var dpi = new Vector(96, 96);
        
        using var bitmap = new RenderTargetBitmap(pixelSize, dpi);
        bitmap.Render(_targetControl);
        
        // Convert Avalonia bitmap to ImageSharp image
        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Position = 0;
        
        var image = ImageSharpImage.Load<Rgba32>(ms);
        
        // Scale down if needed
        if (ScaleFactor < 1.0)
        {
            var newWidth = (int)(image.Width * ScaleFactor);
            var newHeight = (int)(image.Height * ScaleFactor);
            image.Mutate(x => x.Resize(newWidth, newHeight));
        }
        
        return image;
    }

    private async Task CreateGifAsync(string outputPath)
    {
        if (_frames.Count == 0) return;

        try
        {
            await Task.Run(() =>
            {
                // Create a new GIF image
                using var gif = new Image<Rgba32>(_frames[0].Width, _frames[0].Height);
                
                // Configure GIF metadata for looping
                var gifMetadata = gif.Metadata.GetGifMetadata();
                gifMetadata.RepeatCount = 0; // 0 = loop forever
                
                // Add each frame
                for (int i = 0; i < _frames.Count; i++)
                {
                    var frame = _frames[i];
                    
                    // Clone the frame to add to GIF
                    var frameClone = frame.Clone();
                    
                    // Set frame delay
                    var frameMetadata = frameClone.Frames.RootFrame.Metadata.GetGifMetadata();
                    frameMetadata.FrameDelay = GifFrameDelay;
                    
                    if (i == 0)
                    {
                        // First frame - copy pixels to the gif base
                        gif.Frames.RootFrame.ProcessPixelRows(frameClone.Frames.RootFrame, (destAccessor, srcAccessor) =>
                        {
                            for (int y = 0; y < destAccessor.Height; y++)
                            {
                                var destRow = destAccessor.GetRowSpan(y);
                                var srcRow = srcAccessor.GetRowSpan(y);
                                srcRow.CopyTo(destRow);
                            }
                        });
                        gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = GifFrameDelay;
                    }
                    else
                    {
                        // Add subsequent frames
                        gif.Frames.AddFrame(frameClone.Frames.RootFrame);
                    }
                    
                    frameClone.Dispose();
                }
                
                // Save the GIF
                var encoder = new GifEncoder
                {
                    ColorTableMode = GifColorTableMode.Local
                };
                gif.SaveAsGif(outputPath, encoder);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Failed to create GIF: {ex.Message}");
            // Fall back to saving individual PNGs
            await SaveIndividualFramesAsync();
        }
    }

    private async Task SaveIndividualFramesAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Saving {_frames.Count} individual PNG frames as fallback...");
        
        for (int i = 0; i < _frames.Count; i++)
        {
            var filename = $"S{_sessionId:D2}_{_currentAnimationName}_{i:D3}.png";
            var filepath = Path.Combine(_outputDirectory, filename);
            await _frames[i].SaveAsPngAsync(filepath);
        }
        
        System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] PNG frames saved to: {_outputDirectory}");
    }

    private static string SanitizeFilename(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", input.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            .Replace(" ", "_")
            .Replace("=", "")
            .Replace(".", "p");
    }

    /// <summary>
    /// Opens the output directory in the file explorer.
    /// </summary>
    public void OpenOutputDirectory()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _outputDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Failed to open directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all captured files from the output directory.
    /// </summary>
    public void ClearOutputDirectory()
    {
        try
        {
            var files = Directory.GetFiles(_outputDirectory, "*.*")
                .Where(f => f.EndsWith(".png") || f.EndsWith(".gif"));
            var count = 0;
            foreach (var file in files)
            {
                File.Delete(file);
                count++;
            }
            System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Cleared {count} files from output directory");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AnimationDebugger] Failed to clear directory: {ex.Message}");
        }
    }
}
