using Avalonia.Media;
using FlowGraph.Core;
using FlowGraph.Core.DataFlow;

namespace FlowGraph.Demo.Helpers;

/// <summary>
/// A processor for output nodes that have multiple inputs (color, shape, zoom).
/// This is used by the 3D output node to receive values from multiple input nodes.
/// </summary>
public class MultiInputOutputProcessor : NodeProcessor
{
  /// <summary>
  /// Creates a new multi-input output processor.
  /// </summary>
  /// <param name="node">The node.</param>
  public MultiInputOutputProcessor(Node node) : base(node)
  {
    // Register inputs matching the output3d node's input ports
    RegisterInput<Color>("color", Color.FromRgb(255, 0, 113));
    RegisterInput<string>("shape", "cube");
    RegisterInput<double>("zoom", 50.0);
  }

  /// <summary>
  /// Event raised when any input value changes.
  /// </summary>
  public event EventHandler? ValuesChanged;

  /// <inheritdoc />
  public override void Process()
  {
    // Just notify that values changed - the renderer will read InputValues
    ValuesChanged?.Invoke(this, EventArgs.Empty);
  }
}
