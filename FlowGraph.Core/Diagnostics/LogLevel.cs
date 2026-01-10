namespace FlowGraph.Core.Diagnostics;

/// <summary>
/// Defines the severity levels for FlowGraph diagnostic logging.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Logs that contain the most detailed messages. These messages may contain sensitive application data.
    /// These messages are disabled by default and should never be enabled in a production environment.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Logs that are used for interactive investigation during development.
    /// These logs should primarily contain information useful for debugging.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Logs that track the general flow of the application.
    /// These logs should have long-term value.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Logs that highlight an abnormal or unexpected event in the application flow,
    /// but do not otherwise cause the application execution to stop.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due to a failure.
    /// These should indicate a failure in the current activity, not an application-wide failure.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Not used for writing log messages. Specifies that logging is disabled.
    /// </summary>
    None = 5
}
