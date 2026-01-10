namespace FlowGraph.Core.Diagnostics;

/// <summary>
/// Defines the categories of diagnostic events in FlowGraph.
/// Each category represents a distinct subsystem that can be independently configured.
/// </summary>
[Flags]
public enum LogCategory
{
    /// <summary>
    /// No categories enabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// Graph structure changes (node/edge add/remove, property changes).
    /// </summary>
    Graph = 1 << 0,

    /// <summary>
    /// Rendering pipeline events (RenderAll, RenderGraph, RenderNodes, etc.).
    /// </summary>
    Rendering = 1 << 1,

    /// <summary>
    /// Individual node rendering events.
    /// </summary>
    Nodes = 1 << 2,

    /// <summary>
    /// Edge rendering and routing events.
    /// </summary>
    Edges = 1 << 3,

    /// <summary>
    /// Port rendering and connection events.
    /// </summary>
    Ports = 1 << 4,

    /// <summary>
    /// User input events (mouse, keyboard, touch).
    /// </summary>
    Input = 1 << 5,

    /// <summary>
    /// Viewport changes (pan, zoom, scroll).
    /// </summary>
    Viewport = 1 << 6,

    /// <summary>
    /// Coordinate transformation events (Canvas ↔ Screen).
    /// </summary>
    Coordinates = 1 << 7,

    /// <summary>
    /// Selection changes and box selection.
    /// </summary>
    Selection = 1 << 8,

    /// <summary>
    /// Command execution (undo/redo stack).
    /// </summary>
    Commands = 1 << 9,

    /// <summary>
    /// Serialization (save/load) events.
    /// </summary>
    Serialization = 1 << 10,

    /// <summary>
    /// Layout algorithm events.
    /// </summary>
    Layout = 1 << 11,

    /// <summary>
    /// Animation events.
    /// </summary>
    Animation = 1 << 12,

    /// <summary>
    /// Performance timing events.
    /// </summary>
    Performance = 1 << 13,

    /// <summary>
    /// Custom renderer events.
    /// </summary>
    CustomRenderers = 1 << 14,

    /// <summary>
    /// Background renderer events.
    /// </summary>
    BackgroundRenderers = 1 << 15,

    /// <summary>
    /// Data flow and processor events.
    /// </summary>
    DataFlow = 1 << 16,

    /// <summary>
    /// Group and collapse/expand events.
    /// </summary>
    Groups = 1 << 17,

    /// <summary>
    /// All categories enabled.
    /// </summary>
    All = ~None
}
