# Custom Port Renderers

FlowGraph allows you to create custom port appearances by implementing the `IPortRenderer` interface. This enables you to create different port shapes, styles, and visual states for different port types.

## IPortRenderer Interface

```csharp
public interface IPortRenderer
{
    /// <summary>
    /// Creates the visual control for a port.
    /// </summary>
    Control CreatePortVisual(PortRenderContext context);
    
    /// <summary>
    /// Updates the visual state of a port (hover, connected, dragging).
    /// </summary>
    void UpdateState(Control visual, PortVisualState state, PortRenderContext context);
    
    /// <summary>
    /// Gets the size of the port visual.
    /// </summary>
    Size GetSize(PortRenderContext context);
}
```

## PortRenderContext

The `PortRenderContext` provides all the information needed to render a port:

| Property | Type | Description |
|----------|------|-------------|
| `Port` | `Port` | The port being rendered |
| `Node` | `Node` | The parent node |
| `IsOutput` | `bool` | Whether this is an output port |
| `Theme` | `ThemeResources` | Theme colors and brushes |
| `PortIndex` | `int` | Index of the port (0-based) |
| `TotalPorts` | `int` | Total ports on this side of the node |

## PortVisualState

The `PortVisualState` enum indicates the current state of the port:

| Value | Description |
|-------|-------------|
| `Normal` | Default state |
| `Hover` | Mouse is hovering over the port |
| `Connected` | Port has one or more connections |
| `Dragging` | Connection is being dragged from this port |

## Basic Custom Renderer

Here's a simple example that creates square ports:

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering.PortRenderers;

public class SquarePortRenderer : IPortRenderer
{
    private readonly double _size;
    
    public SquarePortRenderer(double size = 12)
    {
        _size = size;
    }
    
    public Control CreatePortVisual(PortRenderContext context)
    {
        return new Rectangle
        {
            Width = _size,
            Height = _size,
            Fill = context.Theme.PortBackground,
            Stroke = context.Theme.PortBorder,
            StrokeThickness = 2
        };
    }
    
    public void UpdateState(Control visual, PortVisualState state, PortRenderContext context)
    {
        if (visual is Rectangle rect)
        {
            rect.Fill = state switch
            {
                PortVisualState.Hover => context.Theme.PortHoverBackground,
                PortVisualState.Connected => context.Theme.PortConnectedBackground,
                PortVisualState.Dragging => context.Theme.PortHoverBackground,
                _ => context.Theme.PortBackground
            };
        }
    }
    
    public Size GetSize(PortRenderContext context) => new Size(_size, _size);
}
```

## Diamond Port Renderer

Create a diamond-shaped port using a rotated square:

```csharp
public class DiamondPortRenderer : IPortRenderer
{
    private readonly double _size;
    
    public DiamondPortRenderer(double size = 12)
    {
        _size = size;
    }
    
    public Control CreatePortVisual(PortRenderContext context)
    {
        return new Rectangle
        {
            Width = _size,
            Height = _size,
            Fill = context.Theme.PortBackground,
            Stroke = context.Theme.PortBorder,
            StrokeThickness = 2,
            RenderTransform = new RotateTransform(45),
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        };
    }
    
    public void UpdateState(Control visual, PortVisualState state, PortRenderContext context)
    {
        if (visual is Rectangle rect)
        {
            rect.Fill = state == PortVisualState.Hover 
                ? context.Theme.PortHoverBackground 
                : context.Theme.PortBackground;
        }
    }
    
    public Size GetSize(PortRenderContext context) => new Size(_size, _size);
}
```

## Registering Port Renderers

Register your custom port renderers with the `PortRendererRegistry`:

```csharp
// Access the registry via GraphRenderer
var portRenderers = canvas.GraphRenderer.PortRenderers;

// Register renderers for specific port types
portRenderers.Register("data", new SquarePortRenderer());
portRenderers.Register("flow", new DiamondPortRenderer(14));

// Or set a custom default renderer for all unregistered types
portRenderers.SetDefaultRenderer(new SquarePortRenderer(10));
```

## Using Port Types

Set the `Type` property on your ports to determine which renderer is used:

```csharp
var node = new Node("node1", "My Node", 100, 100)
{
    Inputs = new[]
    {
        new Port("in1", "Data In") { Type = "data" },
        new Port("in2", "Flow In") { Type = "flow" }
    },
    Outputs = new[]
    {
        new Port("out1", "Data Out") { Type = "data" },
        new Port("out2", "Flow Out") { Type = "flow" }
    }
};
```

## DefaultPortRenderer

The `DefaultPortRenderer` creates standard circular ports (ellipses) and is used for any port type that doesn't have a registered renderer:

```csharp
public class DefaultPortRenderer : IPortRenderer
{
    public virtual Control CreatePortVisual(PortRenderContext context)
    {
        return new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = context.Theme.PortBackground,
            Stroke = context.Theme.PortBorder,
            StrokeThickness = 2
        };
    }
    // ...
}
```

You can extend `DefaultPortRenderer` to customize the standard circular ports:

```csharp
public class LargePortRenderer : DefaultPortRenderer
{
    public override Control CreatePortVisual(PortRenderContext context)
    {
        var ellipse = (Ellipse)base.CreatePortVisual(context);
        ellipse.Width = 16;
        ellipse.Height = 16;
        return ellipse;
    }
    
    public override Size GetSize(PortRenderContext context) => new Size(16, 16);
}
```

## Theme Resources

The `ThemeResources` class provides standard colors for ports:

| Property | Description |
|----------|-------------|
| `PortBackground` | Default port fill color |
| `PortBorder` | Default port border color |
| `PortHoverBackground` | Fill color when hovering |
| `PortHoverBorder` | Border color when hovering |
| `PortConnectedBackground` | Fill color when connected |

## Best Practices

1. **Keep ports small** - Standard size is 10-14 pixels
2. **Use Shape-based visuals** - Enables automatic state updates via Fill/Stroke
3. **Implement all states** - Handle Normal, Hover, Connected, and Dragging
4. **Use theme colors** - Respect the user's theme for consistency
5. **Return accurate sizes** - `GetSize()` affects port positioning
