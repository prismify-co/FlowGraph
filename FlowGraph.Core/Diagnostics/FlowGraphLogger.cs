using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FlowGraph.Core.Diagnostics;

/// <summary>
/// Central static logger for FlowGraph diagnostics.
/// Thread-safe and designed for high-performance logging with minimal overhead when disabled.
/// </summary>
public static class FlowGraphLogger
{
  private static readonly ConcurrentDictionary<string, CorrelationScope> _activeScopes = new();
  private static ILogSink _sink = new DebugLogSink();
  private static LogLevel _minimumLevel = LogLevel.Warning;
  private static LogCategory _enabledCategories = LogCategory.All;
  private static bool _isEnabled = false;
  private static readonly object _configLock = new();

  /// <summary>
  /// Gets or sets whether logging is enabled globally.
  /// When disabled, all logging calls are no-ops with minimal overhead.
  /// </summary>
  public static bool IsEnabled
  {
    get => _isEnabled;
    set => _isEnabled = value;
  }

  /// <summary>
  /// Gets or sets the minimum log level. Messages below this level are ignored.
  /// </summary>
  public static LogLevel MinimumLevel
  {
    get => _minimumLevel;
    set => _minimumLevel = value;
  }

  /// <summary>
  /// Gets or sets the enabled categories. Only messages matching these categories are logged.
  /// </summary>
  public static LogCategory EnabledCategories
  {
    get => _enabledCategories;
    set => _enabledCategories = value;
  }

  /// <summary>
  /// Gets the current log sink.
  /// </summary>
  public static ILogSink Sink => _sink;

  /// <summary>
  /// Configures the logger with the specified settings.
  /// </summary>
  public static void Configure(Action<LoggerConfiguration> configure)
  {
    var config = new LoggerConfiguration();
    configure(config);

    lock (_configLock)
    {
      _sink = config.BuildSink();
      _minimumLevel = config.MinimumLevel;
      _enabledCategories = config.EnabledCategories;
      _isEnabled = config.IsEnabled;
    }
  }

  /// <summary>
  /// Sets the log sink directly.
  /// </summary>
  public static void SetSink(ILogSink sink)
  {
    lock (_configLock)
    {
      _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }
  }

  /// <summary>
  /// Enables all logging at the specified level.
  /// </summary>
  public static void EnableAll(LogLevel minimumLevel = LogLevel.Debug)
  {
    _minimumLevel = minimumLevel;
    _enabledCategories = LogCategory.All;
    _isEnabled = true;
  }

  /// <summary>
  /// Disables all logging.
  /// </summary>
  public static void DisableAll()
  {
    _isEnabled = false;
  }

  /// <summary>
  /// Checks if a message at the given level and category would be logged.
  /// Use this to avoid expensive message formatting when logging is disabled.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsLevelEnabled(LogLevel level, LogCategory category)
  {
    return _isEnabled && level >= _minimumLevel && (_enabledCategories & category) != 0;
  }

  #region Core Logging Methods

  /// <summary>
  /// Logs a message at the specified level and category.
  /// </summary>
  public static void Log(
      LogLevel level,
      LogCategory category,
      string message,
      string? source = null,
      Exception? exception = null,
      Dictionary<string, object?>? properties = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    if (!IsLevelEnabled(level, category)) return;

    var entry = new LogEntry
    {
      Level = level,
      Category = category,
      Message = message,
      Source = source ?? FormatSource(memberName, filePath, lineNumber),
      Exception = exception,
      Properties = properties ?? new Dictionary<string, object?>(),
      CorrelationId = CorrelationScope.CurrentId
    };

    WriteEntry(entry);
  }

  /// <summary>
  /// Logs a message with structured properties.
  /// </summary>
  public static void Log(
      LogLevel level,
      LogCategory category,
      string message,
      object properties,
      string? source = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    if (!IsLevelEnabled(level, category)) return;

    var entry = new LogEntry
    {
      Level = level,
      Category = category,
      Message = message,
      Source = source ?? FormatSource(memberName, filePath, lineNumber),
      Properties = ObjectToProperties(properties),
      CorrelationId = CorrelationScope.CurrentId
    };

    WriteEntry(entry);
  }

  #endregion

  #region Level-Specific Methods

  /// <summary>
  /// Logs a trace message for fine-grained debugging.
  /// </summary>
  [Conditional("DEBUG")]
  public static void Trace(
      LogCategory category,
      string message,
      string? source = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    Log(LogLevel.Trace, category, message, source, null, null, memberName, filePath, lineNumber);
  }

  /// <summary>
  /// Logs a debug message.
  /// </summary>
  public static void Debug(
      LogCategory category,
      string message,
      string? source = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    Log(LogLevel.Debug, category, message, source, null, null, memberName, filePath, lineNumber);
  }

  /// <summary>
  /// Logs an informational message.
  /// </summary>
  public static void Info(
      LogCategory category,
      string message,
      string? source = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    Log(LogLevel.Information, category, message, source, null, null, memberName, filePath, lineNumber);
  }

  /// <summary>
  /// Logs a warning message.
  /// </summary>
  public static void Warn(
      LogCategory category,
      string message,
      string? source = null,
      Exception? exception = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    Log(LogLevel.Warning, category, message, source, exception, null, memberName, filePath, lineNumber);
  }

  /// <summary>
  /// Logs an error message.
  /// </summary>
  public static void Error(
      LogCategory category,
      string message,
      string? source = null,
      Exception? exception = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    Log(LogLevel.Error, category, message, source, exception, null, memberName, filePath, lineNumber);
  }

  #endregion

  #region Scoped Logging

  /// <summary>
  /// Creates a timing scope that logs entry and exit with elapsed time.
  /// </summary>
  public static IDisposable TimeScope(
      LogCategory category,
      string operationName,
      string? source = null,
      [CallerMemberName] string? memberName = null,
      [CallerFilePath] string? filePath = null,
      [CallerLineNumber] int lineNumber = 0)
  {
    return new TimingScope(category, operationName, source ?? FormatSource(memberName, filePath, lineNumber));
  }

  /// <summary>
  /// Creates a correlation scope for tracking related log entries.
  /// </summary>
  public static IDisposable BeginScope(string name)
  {
    return new CorrelationScope(name);
  }

  #endregion

  #region Category-Specific Convenience Methods

  /// <summary>
  /// Logs a rendering-related message.
  /// </summary>
  public static void Rendering(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.Rendering)) return;
    if (properties != null)
      Log(level, LogCategory.Rendering, message, properties, source);
    else
      Log(level, LogCategory.Rendering, message, source);
  }

  /// <summary>
  /// Logs a node-related message.
  /// </summary>
  public static void Nodes(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.Nodes)) return;
    if (properties != null)
      Log(level, LogCategory.Nodes, message, properties, source);
    else
      Log(level, LogCategory.Nodes, message, source);
  }

  /// <summary>
  /// Logs an edge-related message.
  /// </summary>
  public static void Edges(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.Edges)) return;
    if (properties != null)
      Log(level, LogCategory.Edges, message, properties, source);
    else
      Log(level, LogCategory.Edges, message, source);
  }

  /// <summary>
  /// Logs an input-related message.
  /// </summary>
  public static void Input(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.Input)) return;
    if (properties != null)
      Log(level, LogCategory.Input, message, properties, source);
    else
      Log(level, LogCategory.Input, message, source);
  }

  /// <summary>
  /// Logs a viewport-related message.
  /// </summary>
  public static void Viewport(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.Viewport)) return;
    if (properties != null)
      Log(level, LogCategory.Viewport, message, properties, source);
    else
      Log(level, LogCategory.Viewport, message, source);
  }

  /// <summary>
  /// Logs a custom renderer message.
  /// </summary>
  public static void CustomRenderer(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.CustomRenderers)) return;
    if (properties != null)
      Log(level, LogCategory.CustomRenderers, message, properties, source);
    else
      Log(level, LogCategory.CustomRenderers, message, source);
  }

  /// <summary>
  /// Logs a background renderer message.
  /// </summary>
  public static void BackgroundRenderer(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.BackgroundRenderers)) return;
    if (properties != null)
      Log(level, LogCategory.BackgroundRenderers, message, properties, source);
    else
      Log(level, LogCategory.BackgroundRenderers, message, source);
  }

  /// <summary>
  /// Logs a layout-related message.
  /// </summary>
  public static void Layout(LogLevel level, string message, string? source = null, object? properties = null)
  {
    if (!IsLevelEnabled(level, LogCategory.Layout)) return;
    if (properties != null)
      Log(level, LogCategory.Layout, message, properties, source);
    else
      Log(level, LogCategory.Layout, message, source);
  }

  #endregion

  #region Helpers

  private static void WriteEntry(LogEntry entry)
  {
    try
    {
      _sink.Write(entry);
    }
    catch
    {
      // Never let logging failure crash the application
    }
  }

  private static string FormatSource(string? memberName, string? filePath, int lineNumber)
  {
    if (string.IsNullOrEmpty(filePath))
      return memberName ?? "Unknown";

    var fileName = Path.GetFileNameWithoutExtension(filePath);
    return $"{fileName}.{memberName}:{lineNumber}";
  }

  private static Dictionary<string, object?> ObjectToProperties(object obj)
  {
    var props = new Dictionary<string, object?>();
    foreach (var prop in obj.GetType().GetProperties())
    {
      try
      {
        props[prop.Name] = prop.GetValue(obj);
      }
      catch
      {
        props[prop.Name] = "<error>";
      }
    }
    return props;
  }

  #endregion

  #region Nested Types

  /// <summary>
  /// Scope for timing operations.
  /// </summary>
  private sealed class TimingScope : IDisposable
  {
    private readonly LogCategory _category;
    private readonly string _operationName;
    private readonly string _source;
    private readonly Stopwatch _stopwatch;

    public TimingScope(LogCategory category, string operationName, string source)
    {
      _category = category;
      _operationName = operationName;
      _source = source;
      _stopwatch = Stopwatch.StartNew();

      if (IsLevelEnabled(LogLevel.Debug, category))
      {
        var entry = new LogEntry
        {
          Level = LogLevel.Debug,
          Category = category,
          Message = $"BEGIN: {operationName}",
          Source = source,
          CorrelationId = CorrelationScope.CurrentId
        };
        WriteEntry(entry);
      }
    }

    public void Dispose()
    {
      _stopwatch.Stop();

      if (IsLevelEnabled(LogLevel.Debug, _category))
      {
        var entry = new LogEntry
        {
          Level = LogLevel.Debug,
          Category = _category,
          Message = $"END: {_operationName}",
          Source = _source,
          Elapsed = _stopwatch.Elapsed,
          CorrelationId = CorrelationScope.CurrentId
        };
        WriteEntry(entry);
      }
    }
  }

  /// <summary>
  /// Scope for correlating related log entries.
  /// </summary>
  public sealed class CorrelationScope : IDisposable
  {
    private static readonly AsyncLocal<string?> _currentId = new();
    private readonly string? _previousId;
    private readonly string _scopeId;

    public CorrelationScope(string name)
    {
      _previousId = _currentId.Value;
      _scopeId = $"{name}_{Guid.NewGuid():N}".Substring(0, Math.Min(name.Length + 9, 32));
      _currentId.Value = _scopeId;
    }

    public static string? CurrentId => _currentId.Value;

    public void Dispose()
    {
      _currentId.Value = _previousId;
    }
  }

  #endregion
}

/// <summary>
/// Fluent configuration builder for FlowGraphLogger.
/// </summary>
public sealed class LoggerConfiguration
{
  private readonly List<ILogSink> _sinks = new();

  /// <summary>
  /// Gets or sets the minimum log level.
  /// </summary>
  public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

  /// <summary>
  /// Gets or sets the enabled categories.
  /// </summary>
  public LogCategory EnabledCategories { get; set; } = LogCategory.All;

  /// <summary>
  /// Gets or sets whether logging is enabled.
  /// </summary>
  public bool IsEnabled { get; set; } = true;

  /// <summary>
  /// Sets the minimum log level.
  /// </summary>
  public LoggerConfiguration WithMinimumLevel(LogLevel level)
  {
    MinimumLevel = level;
    return this;
  }

  /// <summary>
  /// Sets the enabled categories.
  /// </summary>
  public LoggerConfiguration WithCategories(LogCategory categories)
  {
    EnabledCategories = categories;
    return this;
  }

  /// <summary>
  /// Enables logging.
  /// </summary>
  public LoggerConfiguration Enable()
  {
    IsEnabled = true;
    return this;
  }

  /// <summary>
  /// Adds a debug output sink.
  /// </summary>
  public LoggerConfiguration WriteToDebug()
  {
    _sinks.Add(new DebugLogSink());
    return this;
  }

  /// <summary>
  /// Adds a console sink.
  /// </summary>
  public LoggerConfiguration WriteToConsole(bool useColors = true)
  {
    _sinks.Add(new ConsoleLogSink(useColors));
    return this;
  }

  /// <summary>
  /// Adds a file sink.
  /// </summary>
  public LoggerConfiguration WriteToFile(string path, int maxFileSizeMB = 10, int maxFiles = 5)
  {
    _sinks.Add(new FileLogSink(path, maxFileSizeMB, maxFiles));
    return this;
  }

  /// <summary>
  /// Adds a memory sink.
  /// </summary>
  public LoggerConfiguration WriteToMemory(MemoryLogSink sink)
  {
    _sinks.Add(sink);
    return this;
  }

  /// <summary>
  /// Adds a custom sink.
  /// </summary>
  public LoggerConfiguration WriteTo(ILogSink sink)
  {
    _sinks.Add(sink);
    return this;
  }

  internal ILogSink BuildSink()
  {
    return _sinks.Count switch
    {
      0 => new DebugLogSink(),
      1 => _sinks[0],
      _ => _sinks.Aggregate(new CompositeLogSink(), (composite, sink) => composite.Add(sink))
    };
  }
}
