# Theming

FlowGraph supports full theming through Avalonia's resource system. All colors can be customized by defining resources in your application.

## Theme Resources

Define resources in your `App.axaml`:

```xml
<Application.Resources>
    <!-- Canvas -->
    <SolidColorBrush x:Key="FlowCanvasBackground" Color="#1E1E1E"/>
    <SolidColorBrush x:Key="FlowCanvasGridColor" Color="#333333"/>
    
    <!-- Nodes -->
    <SolidColorBrush x:Key="FlowCanvasNodeBackground" Color="#2D2D30"/>
    <SolidColorBrush x:Key="FlowCanvasNodeBorder" Color="#4682B4"/>
    <SolidColorBrush x:Key="FlowCanvasNodeSelectedBorder" Color="#FF6B00"/>
    <SolidColorBrush x:Key="FlowCanvasNodeText" Color="#FFFFFF"/>
    
    <!-- Input Nodes -->
    <SolidColorBrush x:Key="FlowCanvasInputNodeBackground" Color="#1B5E20"/>
    <SolidColorBrush x:Key="FlowCanvasInputNodeBorder" Color="#4CAF50"/>
    <SolidColorBrush x:Key="FlowCanvasInputNodeText" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="FlowCanvasInputNodeIcon" Color="#4CAF50"/>
    
    <!-- Output Nodes -->
    <SolidColorBrush x:Key="FlowCanvasOutputNodeBackground" Color="#B71C1C"/>
    <SolidColorBrush x:Key="FlowCanvasOutputNodeBorder" Color="#EF5350"/>
    <SolidColorBrush x:Key="FlowCanvasOutputNodeText" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="FlowCanvasOutputNodeIcon" Color="#EF5350"/>
    
    <!-- Edges -->
    <SolidColorBrush x:Key="FlowCanvasEdgeStroke" Color="#808080"/>
    <SolidColorBrush x:Key="FlowCanvasEdgeSelectedStroke" Color="#FF6B00"/>
    
    <!-- Ports -->
    <SolidColorBrush x:Key="FlowCanvasPortBackground" Color="#4682B4"/>
    <SolidColorBrush x:Key="FlowCanvasPortBorder" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="FlowCanvasPortHover" Color="#FF6B00"/>
    <SolidColorBrush x:Key="FlowCanvasPortValidConnection" Color="#22C55E"/>
    <SolidColorBrush x:Key="FlowCanvasPortInvalidConnection" Color="#EF4444"/>
    
    <!-- Groups -->
    <SolidColorBrush x:Key="FlowCanvasGroupBackground" Color="#18FFFFFF"/>
    <SolidColorBrush x:Key="FlowCanvasGroupBorder" Color="#606060"/>
    <SolidColorBrush x:Key="FlowCanvasGroupLabelText" Color="#B0B0B0"/>
    
    <!-- Selection -->
    <SolidColorBrush x:Key="FlowCanvasSelectionBoxFill" Color="#204682B4"/>
    <SolidColorBrush x:Key="FlowCanvasSelectionBoxStroke" Color="#4682B4"/>
    
    <!-- Minimap -->
    <SolidColorBrush x:Key="FlowCanvasMinimapBackground" Color="#252526"/>
    <SolidColorBrush x:Key="FlowCanvasMinimapViewportFill" Color="#304682B4"/>
    <SolidColorBrush x:Key="FlowCanvasMinimapViewportStroke" Color="#4682B4"/>
</Application.Resources>
```

## Light and Dark Themes

FlowGraph automatically detects light and dark themes via Avalonia's `ActualThemeVariant`. Default colors adjust based on the detected theme.

To force a specific theme:

```csharp
Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
```

## Accessing Theme Resources

In custom renderers, use the `ThemeResources` class:

```csharp
public override Control CreateNodeVisual(Node node, NodeRenderContext context)
{
    var background = context.Theme.NodeBackground;
    var border = context.Theme.NodeBorder;
    // ...
}
```

## Available Resource Keys

| Category | Keys |
|----------|------|
| Canvas | `FlowCanvasBackground`, `FlowCanvasGridColor` |
| Node | `FlowCanvasNodeBackground`, `FlowCanvasNodeBorder`, `FlowCanvasNodeSelectedBorder`, `FlowCanvasNodeText` |
| Input Node | `FlowCanvasInputNodeBackground`, `FlowCanvasInputNodeBorder`, `FlowCanvasInputNodeText`, `FlowCanvasInputNodeIcon` |
| Output Node | `FlowCanvasOutputNodeBackground`, `FlowCanvasOutputNodeBorder`, `FlowCanvasOutputNodeText`, `FlowCanvasOutputNodeIcon` |
| Edge | `FlowCanvasEdgeStroke`, `FlowCanvasEdgeSelectedStroke` |
| Port | `FlowCanvasPortBackground`, `FlowCanvasPortBorder`, `FlowCanvasPortHover`, `FlowCanvasPortValidConnection`, `FlowCanvasPortInvalidConnection` |
| Group | `FlowCanvasGroupBackground`, `FlowCanvasGroupBorder`, `FlowCanvasGroupLabelText` |
| Selection | `FlowCanvasSelectionBoxFill`, `FlowCanvasSelectionBoxStroke` |
| Minimap | `FlowCanvasMinimapBackground`, `FlowCanvasMinimapViewportFill`, `FlowCanvasMinimapViewportStroke` |
