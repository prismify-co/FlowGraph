namespace FlowGraph.Core.Diagnostics;

/// <summary>
/// Interface for log sinks that receive and process log entries.
/// Implement this interface to create custom log destinations (file, console, network, etc.).
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Gets the name of this sink for identification purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Writes a log entry to this sink.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    void Write(LogEntry entry);

    /// <summary>
    /// Flushes any buffered log entries.
    /// </summary>
    void Flush();
}

/// <summary>
/// Log sink that writes to <see cref="System.Diagnostics.Debug"/>.
/// Useful for development in Visual Studio and other IDEs.
/// </summary>
public sealed class DebugLogSink : ILogSink
{
    /// <inheritdoc />
    public string Name => "Debug";

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        System.Diagnostics.Debug.WriteLine(entry.ToString());
    }

    /// <inheritdoc />
    public void Flush() { }
}

/// <summary>
/// Log sink that writes to <see cref="Console"/>.
/// Useful for console applications and terminal output.
/// </summary>
public sealed class ConsoleLogSink : ILogSink
{
    private readonly bool _useColors;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new console log sink.
    /// </summary>
    /// <param name="useColors">Whether to use console colors for different log levels.</param>
    public ConsoleLogSink(bool useColors = true)
    {
        _useColors = useColors;
    }

    /// <inheritdoc />
    public string Name => "Console";

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            if (_useColors)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = GetColor(entry.Level);
                Console.WriteLine(entry.ToString());
                Console.ForegroundColor = originalColor;
            }
            else
            {
                Console.WriteLine(entry.ToString());
            }
        }
    }

    /// <inheritdoc />
    public void Flush() { }

    private static ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.Cyan,
        LogLevel.Information => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        _ => ConsoleColor.White
    };
}

/// <summary>
/// Log sink that writes to a file with automatic rotation support.
/// </summary>
public sealed class FileLogSink : ILogSink, IDisposable
{
    private readonly string _filePath;
    private readonly long _maxFileSize;
    private readonly int _maxFiles;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private long _currentSize;
    private bool _disposed;

    /// <summary>
    /// Creates a new file log sink.
    /// </summary>
    /// <param name="filePath">Path to the log file.</param>
    /// <param name="maxFileSizeMB">Maximum file size in MB before rotation (default: 10MB).</param>
    /// <param name="maxFiles">Maximum number of rotated files to keep (default: 5).</param>
    public FileLogSink(string filePath, int maxFileSizeMB = 10, int maxFiles = 5)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _maxFileSize = maxFileSizeMB * 1024 * 1024;
        _maxFiles = maxFiles;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        OpenWriter();
    }

    /// <inheritdoc />
    public string Name => $"File({Path.GetFileName(_filePath)})";

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_writer == null) return;

            var line = entry.ToString();
            _writer.WriteLine(line);
            _currentSize += line.Length + Environment.NewLine.Length;

            if (_currentSize >= _maxFileSize)
            {
                RotateFiles();
            }
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        lock (_lock)
        {
            _writer?.Flush();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void OpenWriter()
    {
        _writer = new StreamWriter(_filePath, append: true);
        _currentSize = new FileInfo(_filePath).Length;
    }

    private void RotateFiles()
    {
        _writer?.Dispose();
        _writer = null;

        // Delete oldest file if at max
        var oldestFile = $"{_filePath}.{_maxFiles}";
        if (File.Exists(oldestFile))
        {
            File.Delete(oldestFile);
        }

        // Shift existing files
        for (int i = _maxFiles - 1; i >= 1; i--)
        {
            var source = i == 1 ? _filePath : $"{_filePath}.{i}";
            var dest = $"{_filePath}.{i + 1}";
            if (File.Exists(source))
            {
                File.Move(source, dest);
            }
        }

        // Rename current to .1
        if (File.Exists(_filePath))
        {
            File.Move(_filePath, $"{_filePath}.1");
        }

        OpenWriter();
        _currentSize = 0;
    }
}

/// <summary>
/// Log sink that stores entries in memory for retrieval.
/// Useful for displaying logs in UI or testing.
/// </summary>
public sealed class MemoryLogSink : ILogSink
{
    private readonly Queue<LogEntry> _entries = new();
    private readonly int _maxEntries;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new memory log sink.
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries to keep (default: 1000).</param>
    public MemoryLogSink(int maxEntries = 1000)
    {
        _maxEntries = maxEntries;
    }

    /// <inheritdoc />
    public string Name => "Memory";

    /// <summary>
    /// Event raised when a new log entry is added.
    /// </summary>
    public event Action<LogEntry>? EntryAdded;

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _maxEntries)
            {
                _entries.Dequeue();
            }
        }

        EntryAdded?.Invoke(entry);
    }

    /// <inheritdoc />
    public void Flush() { }

    /// <summary>
    /// Gets all stored log entries.
    /// </summary>
    public IReadOnlyList<LogEntry> GetEntries()
    {
        lock (_lock)
        {
            return _entries.ToList();
        }
    }

    /// <summary>
    /// Clears all stored log entries.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }
}

/// <summary>
/// Log sink that forwards entries to multiple other sinks.
/// </summary>
public sealed class CompositeLogSink : ILogSink
{
    private readonly List<ILogSink> _sinks = new();

    /// <inheritdoc />
    public string Name => "Composite";

    /// <summary>
    /// Adds a sink to the composite.
    /// </summary>
    public CompositeLogSink Add(ILogSink sink)
    {
        _sinks.Add(sink);
        return this;
    }

    /// <summary>
    /// Removes a sink from the composite.
    /// </summary>
    public bool Remove(ILogSink sink) => _sinks.Remove(sink);

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        foreach (var sink in _sinks)
        {
            try
            {
                sink.Write(entry);
            }
            catch
            {
                // Don't let one sink failure affect others
            }
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        foreach (var sink in _sinks)
        {
            try
            {
                sink.Flush();
            }
            catch
            {
                // Don't let one sink failure affect others
            }
        }
    }
}
