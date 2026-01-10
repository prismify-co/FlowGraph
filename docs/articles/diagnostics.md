# FlowGraph Diagnostics System

This document describes the comprehensive logging and diagnostics infrastructure in FlowGraph.

## Overview

FlowGraph includes a robust, production-quality diagnostics system designed to help:

- **Developers** debug rendering, input, and coordinate issues
- **Contributors** submit detailed bug reports with actionable context
- **Users** troubleshoot issues in their applications

## Quick Start

### Enable Basic Logging

```csharp
using FlowGraph.Core.Diagnostics;

// Enable all logging at Debug level
FlowGraphLogger.EnableAll(LogLevel.Debug);

// Or configure through FlowCanvasSettings
var settings = new FlowCanvasSettings
{
    EnableDiagnostics = true,
    DiagnosticsMinimumLevel = LogLevel.Debug,
    DiagnosticsCategories = LogCategory.All
};
settings.ApplyDiagnosticsSettings();
```

### Focused Debugging

```csharp
// Only log rendering and node issues
FlowGraphLogger.Configure(config => config
    .Enable()
    .WithMinimumLevel(LogLevel.Debug)
    .WithCategories(LogCategory.Rendering | LogCategory.Nodes | LogCategory.CustomRenderers)
    .WriteToDebug()
    .WriteToFile(@"C:\logs\flowgraph.log"));
```

## Log Levels

| Level         | Purpose                                                              |
| ------------- | -------------------------------------------------------------------- |
| `Trace`       | Very fine-grained information (position changes, every render frame) |
| `Debug`       | Useful debugging info (method entry/exit, state changes)             |
| `Information` | General operational messages                                         |
| `Warning`     | Potential issues that don't prevent operation                        |
| `Error`       | Errors that affect functionality                                     |
| `None`        | Disable all logging                                                  |

## Log Categories

Categories allow filtering logs to specific subsystems:

| Category              | Description                                      |
| --------------------- | ------------------------------------------------ |
| `Graph`               | Graph-level operations (add/remove nodes, edges) |
| `Rendering`           | Core rendering pipeline                          |
| `Nodes`               | Node creation, movement, state                   |
| `Edges`               | Edge creation, routing                           |
| `Ports`               | Port operations                                  |
| `Input`               | Mouse/keyboard input handling                    |
| `Viewport`            | Pan, zoom, viewport calculations                 |
| `Coordinates`         | Coordinate transformations                       |
| `Selection`           | Selection management                             |
| `Commands`            | Undo/redo commands                               |
| `Serialization`       | Save/load operations                             |
| `Layout`              | Layout algorithms                                |
| `Animation`           | Animation system                                 |
| `Performance`         | Performance metrics                              |
| `CustomRenderers`     | Custom node/edge renderers                       |
| `BackgroundRenderers` | Background renderers                             |
| `DataFlow`            | Data binding and flow                            |
| `Groups`              | Group operations                                 |
| `All`                 | All categories                                   |

## Log Sinks

### Built-in Sinks

```csharp
// Debug output (Visual Studio Output window)
FlowGraphLogger.Configure(config => config.WriteToDebug());

// Console output with colors
FlowGraphLogger.Configure(config => config.WriteToConsole(useColors: true));

// File with automatic rotation
FlowGraphLogger.Configure(config => config.WriteToFile(
    path: @"C:\logs\flowgraph.log",
    maxFileSizeMB: 10,
    maxFiles: 5));

// Memory sink for UI display
var memorySink = new MemoryLogSink(maxEntries: 1000);
memorySink.EntryAdded += entry => UpdateLogUI(entry);
FlowGraphLogger.Configure(config => config.WriteToMemory(memorySink));

// Multiple sinks
FlowGraphLogger.Configure(config => config
    .WriteToDebug()
    .WriteToFile(@"C:\logs\flowgraph.log"));
```

### Custom Sinks

```csharp
public class MyCustomSink : ILogSink
{
    public string Name => "Custom";

    public void Write(LogEntry entry)
    {
        // Send to your logging system
        MyLogger.Log(entry.Message);
    }

    public void Flush() { }
}

FlowGraphLogger.Configure(config => config.WriteTo(new MyCustomSink()));
```

## Using the Logger

### Direct Logging

```csharp
// Level-specific methods
FlowGraphLogger.Debug(LogCategory.Rendering, "Starting render pass");
FlowGraphLogger.Info(LogCategory.Nodes, $"Created node: {nodeId}");
FlowGraphLogger.Warn(LogCategory.Input, "Click outside valid area");
FlowGraphLogger.Error(LogCategory.Serialization, "Failed to load graph", exception: ex);

// With structured properties
FlowGraphLogger.Log(LogLevel.Debug, LogCategory.Nodes, "Node positioned",
    new { NodeId = node.Id, X = node.X, Y = node.Y, Scale = scale });
```

### Performance-Aware Logging

```csharp
// Check before expensive message formatting
if (FlowGraphLogger.IsLevelEnabled(LogLevel.Debug, LogCategory.Rendering))
{
    var snapshot = graph.CreateDiagnosticSnapshot();
    FlowGraphLogger.Debug(LogCategory.Rendering, snapshot.ToString());
}
```

### Timing Operations

```csharp
using (FlowGraphLogger.TimeScope(LogCategory.Rendering, "Full render pass"))
{
    RenderNodes();
    RenderEdges();
}
// Logs: BEGIN: Full render pass
// Logs: END: Full render pass [elapsed: 12.34ms]
```

### Correlation Scopes

```csharp
using (FlowGraphLogger.BeginScope("DragOperation"))
{
    FlowGraphLogger.Debug(LogCategory.Input, "Started drag");
    // ... all logs in this scope share a correlation ID
    FlowGraphLogger.Debug(LogCategory.Nodes, "Moved node");
}
```

## Extension Methods

### Graph Logging

```csharp
// Log current graph state
graph.LogGraphState("after layout", "LayoutEngine");

// Get detailed snapshot
var snapshot = graph.CreateDiagnosticSnapshot();
Console.WriteLine(snapshot.ToString());
```

### Node/Edge Logging

```csharp
node.LogNodeCreated("MyRenderer");
node.LogNodeMoved(oldX, oldY, "DragHandler");
edge.LogEdgeCreated("ConnectionManager");
```

## Integration with FlowCanvasSettings

```csharp
var canvas = new FlowCanvas
{
    Settings = new FlowCanvasSettings
    {
        EnableDiagnostics = true,
        DiagnosticsMinimumLevel = LogLevel.Debug,
        DiagnosticsCategories = LogCategory.Rendering | LogCategory.Nodes,
        DiagnosticsLogFilePath = @"C:\logs\flowgraph.log"
    }
};

// Apply settings to global logger
canvas.Settings.ApplyDiagnosticsSettings();
```

## Bug Reports

When submitting bug reports, enable full diagnostics and include the log output:

```csharp
// In your startup or when issue occurs
FlowGraphLogger.Configure(config => config
    .Enable()
    .WithMinimumLevel(LogLevel.Trace)
    .WithCategories(LogCategory.All)
    .WriteToFile(@"C:\temp\flowgraph_bugreport.log"));
```

Then include the generated log file with your bug report.

## Thread Safety

The logging system is fully thread-safe:

- Multiple threads can log simultaneously
- Configuration changes are atomic
- File sinks use internal locking
- Memory sinks are synchronized

## Performance Considerations

- **Disabled logging**: Near-zero overhead (single boolean check)
- **Level/category check**: Inline check, ~1ns
- **Conditional attribute**: `Trace` level calls are removed in Release builds
- **Structured properties**: Use anonymous types for automatic serialization
- **Large outputs**: Check `IsLevelEnabled` before expensive formatting

## Files

The diagnostics system consists of:

| File                                              | Purpose                       |
| ------------------------------------------------- | ----------------------------- |
| `FlowGraph.Core/Diagnostics/LogLevel.cs`          | Log severity levels           |
| `FlowGraph.Core/Diagnostics/LogCategory.cs`       | Log categories (flags enum)   |
| `FlowGraph.Core/Diagnostics/LogEntry.cs`          | Structured log entry          |
| `FlowGraph.Core/Diagnostics/LogSinks.cs`          | Built-in sink implementations |
| `FlowGraph.Core/Diagnostics/FlowGraphLogger.cs`   | Central static logger         |
| `FlowGraph.Core/Diagnostics/LoggingExtensions.cs` | Extension methods             |
