# INodeRenderer Implementation Checklist

When implementing or reviewing an `INodeRenderer`, verify the following:

## Recommended: Use ResizableVisual (Type-Safe Approach)

For composite visuals, use `ResizableVisual` to **automatically track and update children**:

```csharp
// In CreateNodeVisual - register children at creation time
public Control CreateNodeVisual(Node node, NodeRenderContext context)
{
    var grid = new Grid { Width = 200, Height = 100 };
    var background = new Rectangle { Width = 200, Height = 100 };
    var border = new Rectangle { Width = 200, Height = 100 };
    var content = new Border { Width = 180, Height = 80 };

    grid.Children.Add(background);
    grid.Children.Add(border);
    grid.Children.Add(content);

    // Register children - they'll be auto-updated on resize!
    return ResizableVisual.Create(grid)
        .WithFullSizeChildren(background, border)
        .WithInsetChild(content, new Thickness(10))  // 10px padding
        .Build();
}

// In UpdateSize - one line handles everything
public void UpdateSize(Control visual, Node node, NodeRenderContext context, double w, double h)
{
    ResizableVisual.UpdateSize(visual, w * context.Scale, h * context.Scale);
}
```

Implement `IResizableNodeRenderer` to signal you're using this pattern.

---

## Manual Approach (Legacy)

If not using `ResizableVisual`, follow these checks:

### CreateNodeVisual

- [ ] All size-dependent values use `context.Scale` multiplier
- [ ] Complex visuals tag child elements for later lookup (e.g., `Tag = "background"`)
- [ ] Container controls (Grid, Panel) have explicit Width/Height set
- [ ] Set `HasCompositeVisual => true` if visual has children needing size updates

### UpdateSize - CRITICAL

- [ ] Root container Width/Height updated with `width * context.Scale`
- [ ] **ALL child elements** have their Width/Height updated
- [ ] Background rectangles/shapes resized
- [ ] Border rectangles/shapes resized
- [ ] Content containers (Canvas, panels) resized
- [ ] For Polygon shapes, Points array recalculated

## UpdateSelection

- [ ] Border colors/thickness updated appropriately
- [ ] Any selection indicators (glow, shadow) toggled

## Common Mistakes to Avoid

1. ❌ Only updating root container, forgetting children
2. ❌ Using `Viewbox` expecting automatic scaling (works for render but not for hit testing)
3. ❌ Hardcoding sizes in CreateNodeVisual without corresponding UpdateSize logic
4. ❌ Creating scaled font sizes at creation but not updating them on resize

## Testing Requirements

- [ ] Manual test: Resize node and verify visual updates immediately (not on next pan/zoom)
- [ ] Manual test: Resize to minimum size, verify children don't overflow
- [ ] Unit test: Call UpdateSize and verify all child bounds changed

## Example Proper Implementation

```csharp
public void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
{
    var scaledWidth = width * context.Scale;
    var scaledHeight = height * context.Scale;

    if (visual is Grid grid)
    {
        // 1. Update container
        grid.Width = scaledWidth;
        grid.Height = scaledHeight;

        // 2. Update all shape children (backgrounds, borders)
        foreach (var child in grid.Children.OfType<Rectangle>())
        {
            child.Width = scaledWidth;
            child.Height = scaledHeight;
        }

        // 3. Update content containers
        var contentBorder = grid.Children.OfType<Border>().FirstOrDefault();
        if (contentBorder != null)
        {
            var padding = 10 * context.Scale;
            var contentWidth = scaledWidth - 2 * padding;
            var contentHeight = scaledHeight - 2 * padding;
            contentBorder.Width = contentWidth;
            contentBorder.Height = contentHeight;

            if (contentBorder.Child is Canvas canvas)
            {
                canvas.Width = contentWidth;
                canvas.Height = contentHeight;
            }
        }
    }
}
```
