# Custom Node Renderers

FlowGraph allows you to create custom node appearances by implementing the `INodeRenderer` interface or extending `DefaultNodeRenderer`.

## Basic Custom Renderer

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;

public class CustomNodeRenderer : DefaultNodeRenderer
{
    private static readonly IBrush Background = new SolidColorBrush(Color.Parse("#E3F2FD"));
    private static readonly IBrush Border = new SolidColorBrush(Color.Parse("#2196F3"));

    public override Control CreateNodeVisual(Node node, NodeRenderContext context)
    {
        var control = base.CreateNodeVisual(node, context);
        
        if (control is Border border)
        {
            border.Background = Background;
            border.BorderBrush = Border;
            border.CornerRadius = new CornerRadius(12);
        }
        
        return control;
    }

    protected override string GetDisplayText(Node node)
    {
        return node.Label ?? node.Type ?? "Node";
    }
}
```

## Registering the Renderer

```csharp
canvas.NodeRenderers.Register("custom", new CustomNodeRenderer());
```

## Using StyledNodeRendererBase

For nodes with icons, extend `StyledNodeRendererBase`:

```csharp
public class MyNodeRenderer : StyledNodeRendererBase
{
    // Use vector geometry for scalable icons
    private static readonly Geometry Icon = Geometry.Parse("M12 2L2 7l10 5 10-5-10-5z");
    
    protected override Geometry? IconGeometry => Icon;
    protected override string DefaultLabel => "My Node";
    
    protected override IBrush GetNodeBackground(ThemeResources theme) => 
        new SolidColorBrush(Color.Parse("#FFF3E0"));
    
    protected override IBrush GetNodeBorder(ThemeResources theme) => 
        new SolidColorBrush(Color.Parse("#FF9800"));
    
    protected override IBrush GetIconForeground(ThemeResources theme) => 
        new SolidColorBrush(Color.Parse("#E65100"));
    
    protected override IBrush GetTextForeground(ThemeResources theme) => 
        new SolidColorBrush(Color.Parse("#E65100"));
}
```

## Icon Sources

You can use path data from any icon library:

- [Lucide Icons](https://lucide.dev/)
- [Material Design Icons](https://materialdesignicons.com/)
- [Font Awesome](https://fontawesome.com/)

Simply copy the SVG path data and use it with `Geometry.Parse()`.
