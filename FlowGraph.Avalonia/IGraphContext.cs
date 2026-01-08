using FlowGraph.Core;

namespace FlowGraph.Avalonia;

/// <summary>
/// Provides access to the current graph context.
/// Implemented by FlowCanvas to provide graph access to manager classes.
/// </summary>
public interface IGraphContext
{
  /// <summary>
  /// Gets the current graph, or null if no graph is loaded.
  /// </summary>
  Graph? Graph { get; }
}

/// <summary>
/// Provides access to the current graph and settings context.
/// Extended interface for managers that also need settings access.
/// </summary>
public interface IFlowCanvasContext : IGraphContext
{
  /// <summary>
  /// Gets the current canvas settings.
  /// </summary>
  FlowCanvasSettings Settings { get; }
}

/// <summary>
/// Adapter class that wraps a Func&lt;Graph?&gt; to implement IGraphContext.
/// Used for backwards compatibility with existing code.
/// </summary>
internal sealed class FuncGraphContext : IGraphContext
{
  private readonly Func<Graph?> _getGraph;

  public FuncGraphContext(Func<Graph?> getGraph)
  {
    _getGraph = getGraph ?? throw new ArgumentNullException(nameof(getGraph));
  }

  public Graph? Graph => _getGraph();
}

/// <summary>
/// Adapter class that wraps Func delegates to implement IFlowCanvasContext.
/// Used for backwards compatibility with existing code.
/// </summary>
internal sealed class FuncFlowCanvasContext : IFlowCanvasContext
{
  private readonly Func<Graph?> _getGraph;
  private readonly Func<FlowCanvasSettings> _getSettings;

  public FuncFlowCanvasContext(Func<Graph?> getGraph, Func<FlowCanvasSettings> getSettings)
  {
    _getGraph = getGraph ?? throw new ArgumentNullException(nameof(getGraph));
    _getSettings = getSettings ?? throw new ArgumentNullException(nameof(getSettings));
  }

  public Graph? Graph => _getGraph();
  public FlowCanvasSettings Settings => _getSettings();
}
