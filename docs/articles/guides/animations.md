# Animations

FlowGraph provides a comprehensive animation system for smooth transitions and visual feedback.

## Viewport Animations

```csharp
// Fit all content in view
canvas.FitToViewAnimated(duration: 0.5);

// Center on a specific node
canvas.CenterOnNodeAnimated(node, duration: 0.3);

// Zoom to a specific level
canvas.ZoomToAnimated(targetZoom: 1.5, duration: 0.2);

// Pan to a position
canvas.PanToAnimated(new Point(500, 300), duration: 0.3);
```

## Node Animations

```csharp
// Move nodes to new positions
var positions = new Dictionary<string, Point>
{
    { node1.Id, new Point(100, 100) },
    { node2.Id, new Point(300, 100) }
};
canvas.AnimateNodesTo(positions, duration: 0.3);

// Appear animation (scale + fade)
canvas.AnimateNodesAppear(nodes, duration: 0.3, stagger: 0.05);

// Disappear animation
canvas.AnimateNodesDisappear(nodes, duration: 0.2);

// Selection pulse effect
canvas.AnimateSelectionPulse(node);
```

## Edge Animations

```csharp
// Pulse effect
canvas.AnimateEdgePulse(edge, pulseCount: 3);

// Fade in/out
canvas.AnimateEdgeFadeIn(edge, duration: 0.3);
canvas.AnimateEdgeFadeOut(edge, duration: 0.3);

// Color transition
canvas.AnimateEdgeColor(edge, Colors.Red, duration: 0.5);

// Continuous flow animation (data flow visualization)
var animation = canvas.StartEdgeFlowAnimation(edge, speed: 50);
// Later...
canvas.StopEdgeFlowAnimation(animation);
```

## Group Animations

```csharp
// Animated collapse/expand
canvas.AnimateGroupCollapse(groupId, duration: 0.5);
canvas.AnimateGroupExpand(groupId, duration: 0.5);
```

## Animation Settings

Configure animation behavior through settings:

```csharp
canvas.Settings.EnableAnimations = true;
canvas.Settings.DefaultAnimationDuration = 0.3;
```

## Custom Animations

For advanced use cases, access the animation manager directly:

```csharp
var animation = new GenericAnimation(
    duration: 0.5,
    easing: Easing.CubicOut,
    onUpdate: progress => 
    {
        // Update properties based on progress (0.0 to 1.0)
        node.Opacity = progress;
    },
    onComplete: () => 
    {
        // Animation finished
    }
);

canvas.AnimationManager.Start(animation);
```
