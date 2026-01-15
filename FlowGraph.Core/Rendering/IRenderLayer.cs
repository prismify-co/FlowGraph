namespace FlowGraph.Core.Rendering;

/// <summary>
/// Defines how a rendering layer participates in viewport transformations.
/// 
/// <para>
/// This is inspired by react-diagrams' <c>LayerModel.options.transformed</c> flag,
/// which allows layers to opt in or out of viewport transforms. In FlowGraph,
/// we extend this to three modes to handle direct rendering scenarios.
/// </para>
/// </summary>
public enum LayerTransformMode
{
    /// <summary>
    /// Layer IS affected by parent viewport transforms.
    /// 
    /// <para><b>Coordinate expectations:</b></para>
    /// <list type="bullet">
    /// <item>Elements are positioned in <b>canvas coordinates</b></item>
    /// <item>Parent container applies zoom/pan via MatrixTransform</item>
    /// <item>No manual coordinate conversion needed</item>
    /// </list>
    /// 
    /// <para><b>Use for:</b> Nodes, edges, ports, resize handles, selection rectangles</para>
    /// 
    /// <para><b>Example:</b></para>
    /// <code>
    /// // Node positioned in canvas coordinates - transform handled by parent
    /// Canvas.SetLeft(nodeVisual, node.Position.X);  // ✅ Canvas coords
    /// Canvas.SetTop(nodeVisual, node.Position.Y);
    /// </code>
    /// </summary>
    Transformed,
    
    /// <summary>
    /// Layer is NOT affected by viewport transforms.
    /// 
    /// <para><b>Coordinate expectations:</b></para>
    /// <list type="bullet">
    /// <item>Elements are positioned in <b>screen coordinates</b></item>
    /// <item>Layer exists outside the transformed container</item>
    /// <item>Must manually call CanvasToScreen if referencing canvas positions</item>
    /// </list>
    /// 
    /// <para><b>Use for:</b> Fixed overlays, HUD elements, toolbars, minimap frame</para>
    /// 
    /// <para><b>Example:</b></para>
    /// <code>
    /// // Tooltip positioned in screen coordinates
    /// Canvas.SetLeft(tooltip, e.GetPosition(rootPanel).X);  // ✅ Screen coords
    /// </code>
    /// </summary>
    Untransformed,
    
    /// <summary>
    /// Layer manages its own transforms independently.
    /// 
    /// <para><b>Coordinate expectations:</b></para>
    /// <list type="bullet">
    /// <item>Layer exists <b>outside</b> any transformed container</item>
    /// <item>Layer performs its own CanvasToScreen transforms when rendering</item>
    /// <item>Used for direct DrawingContext rendering that bypasses visual tree</item>
    /// </list>
    /// 
    /// <para><b>Use for:</b> DirectGraphRenderer, custom GPU renderers, background grids</para>
    /// 
    /// <para><b>Example:</b></para>
    /// <code>
    /// // DirectGraphRenderer renders directly to DrawingContext
    /// public override void Render(DrawingContext context)
    /// {
    ///     foreach (var node in nodes)
    ///     {
    ///         // Must transform canvas coords to screen coords manually
    ///         var screenPos = viewport.CanvasToScreen(node.Position.X, node.Position.Y);
    ///         context.DrawRectangle(brush, pen, new Rect(screenPos, scaledSize));
    ///     }
    /// }
    /// </code>
    /// 
    /// <para><b>Critical:</b> SelfTransformed layers MUST be children of an 
    /// untransformed container (e.g., RootPanel, not MainCanvas). Placing them 
    /// inside a transformed container causes double-transformation bugs.</para>
    /// </summary>
    SelfTransformed
}

/// <summary>
/// Represents a rendering layer in the graph canvas.
/// Layers control Z-ordering and transform behavior for groups of elements.
/// 
/// <para>
/// <b>Inspired by:</b>
/// <list type="bullet">
/// <item>react-diagrams: Multiple layer types (LinkLayer, NodeLayer) with transform flags</item>
/// <item>Konva.js: Stage → Layer → Group → Shape hierarchy</item>
/// <item>Photoshop/Figma: Layer-based compositing model</item>
/// </list>
/// </para>
/// </summary>
public interface IRenderLayer
{
    /// <summary>
    /// Unique identifier for this layer.
    /// </summary>
    string LayerId { get; }
    
    /// <summary>
    /// Display name for debugging and UI purposes.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// The Z-order of this layer (lower = behind, higher = in front).
    /// 
    /// <para><b>Standard Z-order ranges:</b></para>
    /// <list type="bullet">
    /// <item>0-99: Background layers (grid, guides)</item>
    /// <item>100-199: Edge layers</item>
    /// <item>200-299: Node layers</item>
    /// <item>300-399: Selection/interaction layers</item>
    /// <item>400+: Overlay layers (tooltips, menus)</item>
    /// </list>
    /// </summary>
    int ZIndex { get; }
    
    /// <summary>
    /// How this layer participates in viewport transformations.
    /// </summary>
    LayerTransformMode TransformMode { get; }
    
    /// <summary>
    /// Whether the layer is currently visible.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Whether hit testing should include elements in this layer.
    /// </summary>
    bool IsHitTestVisible { get; set; }
}

/// <summary>
/// Standard layer identifiers for consistent layer management.
/// </summary>
public static class StandardLayers
{
    /// <summary>Background grid layer (Z: 0)</summary>
    public const string Grid = "grid";
    
    /// <summary>Guide/alignment lines layer (Z: 10)</summary>
    public const string Guides = "guides";
    
    /// <summary>Group backgrounds layer (Z: 50)</summary>
    public const string GroupBackgrounds = "group-backgrounds";
    
    /// <summary>Edge connections layer (Z: 100)</summary>
    public const string Edges = "edges";
    
    /// <summary>Edge labels layer (Z: 110)</summary>
    public const string EdgeLabels = "edge-labels";
    
    /// <summary>Node layer (Z: 200)</summary>
    public const string Nodes = "nodes";
    
    /// <summary>Port layer (Z: 210)</summary>
    public const string Ports = "ports";
    
    /// <summary>Selection rectangles layer (Z: 300)</summary>
    public const string Selection = "selection";
    
    /// <summary>Resize handles layer (Z: 310)</summary>
    public const string ResizeHandles = "resize-handles";
    
    /// <summary>Drag preview layer (Z: 320)</summary>
    public const string DragPreview = "drag-preview";
    
    /// <summary>Tooltip/overlay layer (Z: 400)</summary>
    public const string Overlays = "overlays";
}
