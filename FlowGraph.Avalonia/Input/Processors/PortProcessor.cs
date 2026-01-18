using Avalonia.Input;
using FlowGraph.Avalonia.Input.States;
using FlowGraph.Core;
using FlowGraph.Core.Input;
using AvaloniaPoint = Avalonia.Point;

namespace FlowGraph.Avalonia.Input.Processors;

/// <summary>
/// Processes input for ports (connection creation/modification).
/// </summary>
/// <remarks>
/// <para>
/// Ports are small targets that need high priority over their parent nodes.
/// This processor handles:
/// <list type="bullet">
/// <item>Left click + drag: start connection from port</item>
/// <item>Left click on connected port: potentially select connected edge</item>
/// </list>
/// </para>
/// <para>
/// <b>Connection Flow:</b>
/// Click on output port → drag → release on input port = create edge
/// </para>
/// </remarks>
public class PortProcessor : InputProcessorBase
{
  public override HitTargetType HandledTypes => HitTargetType.Port;

  public override int Priority => InputProcessorPriority.Port;

  public override string Name => "PortProcessor";

  public override InputProcessorResult HandlePointerPressed(
      InputStateContext context,
      GraphHitTestResult hit,
      PointerPressedEventArgs e)
  {
    var point = e.GetCurrentPoint(context.RootPanel);
    if (!point.Properties.IsLeftButtonPressed)
      return InputProcessorResult.NotHandled;

    // Connections blocked in read-only mode
    if (context.Settings.IsReadOnly)
      return InputProcessorResult.NotHandled;

    // Get port info from hit result
    var portOwner = hit.PortOwner;
    var port = hit.Port;
    if (portOwner == null || port == null)
      return InputProcessorResult.NotHandled;

    // Theme required for connection line rendering
    if (context.Theme == null)
      return InputProcessorResult.NotHandled;

    // Check if node allows connections
    if (!portOwner.IsConnectable)
      return InputProcessorResult.HandledStay;

    var isInputPort = hit.IsInputPort;
    var isOutput = !isInputPort;

    // If strict connection direction is enabled, only allow starting from output ports
    if (context.Settings.StrictConnectionDirection && !isOutput)
      return InputProcessorResult.HandledStay;

    var canvasPos = new AvaloniaPoint(hit.CanvasPosition.X, hit.CanvasPosition.Y);

    // Get port visual if available (may be null in direct rendering mode)
    var portVisual = context.GraphRenderer?.GetPortVisual(portOwner.Id, port.Id);

    // Create connecting state
    var connectingState = new ConnectingState(
        portOwner,
        port,
        isOutput,
        canvasPos,
        portVisual,
        context.Theme);

    // Create temp line for visual feedback
    connectingState.CreateTempLine(context);

    CapturePointer(e, context.RootPanel);
    return InputProcessorResult.TransitionTo(connectingState);
  }
}
