namespace FlowGraph.Core.Diagnostics;

/// <summary>
/// Represents a structured log entry with rich contextual information.
/// </summary>
public sealed class LogEntry
{
  /// <summary>
  /// Gets the timestamp when the log entry was created.
  /// </summary>
  public DateTime Timestamp { get; init; } = DateTime.UtcNow;

  /// <summary>
  /// Gets the severity level of the log entry.
  /// </summary>
  public LogLevel Level { get; init; }

  /// <summary>
  /// Gets the category of the log entry.
  /// </summary>
  public LogCategory Category { get; init; }

  /// <summary>
  /// Gets the source component or method that generated the log entry.
  /// </summary>
  public string Source { get; init; } = string.Empty;

  /// <summary>
  /// Gets the log message.
  /// </summary>
  public string Message { get; init; } = string.Empty;

  /// <summary>
  /// Gets the structured properties associated with the log entry.
  /// </summary>
  public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();

  /// <summary>
  /// Gets the exception associated with the log entry, if any.
  /// </summary>
  public Exception? Exception { get; init; }

  /// <summary>
  /// Gets the elapsed time for performance-related entries.
  /// </summary>
  public TimeSpan? Elapsed { get; init; }

  /// <summary>
  /// Gets the correlation ID for tracing related log entries.
  /// </summary>
  public string? CorrelationId { get; init; }

  /// <summary>
  /// Returns a formatted string representation of the log entry.
  /// </summary>
  public override string ToString()
  {
    var props = Properties.Count > 0
        ? $" | {string.Join(", ", Properties.Select(p => $"{p.Key}={p.Value}"))}"
        : "";
    var elapsed = Elapsed.HasValue ? $" [{Elapsed.Value.TotalMilliseconds:F2}ms]" : "";
    var ex = Exception != null ? $" | Exception: {Exception.Message}" : "";

    return $"[{Timestamp:HH:mm:ss.fff}] [{Level}] [{Category}] {Source}: {Message}{props}{elapsed}{ex}";
  }
}
