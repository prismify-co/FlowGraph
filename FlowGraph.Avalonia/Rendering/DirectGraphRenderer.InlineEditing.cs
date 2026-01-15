// DirectGraphRenderer.InlineEditing.cs
// Partial class containing inline editing support methods

namespace FlowGraph.Avalonia.Rendering;

public partial class DirectGraphRenderer
{
    #region Inline Editing Support

    /// <summary>
    /// Begins editing a node's label. The label will not be drawn while editing.
    /// </summary>
    /// <param name="nodeId">ID of the node to edit.</param>
    public void BeginEditNode(string nodeId)
    {
        _editingNodeId = nodeId;
        InvalidateVisual();
    }

    /// <summary>
    /// Ends editing a node's label.
    /// </summary>
    public void EndEditNode()
    {
        _editingNodeId = null;
        InvalidateVisual();
    }

    /// <summary>
    /// Begins editing an edge's label. The label will not be drawn while editing.
    /// </summary>
    /// <param name="edgeId">ID of the edge to edit.</param>
    public void BeginEditEdge(string edgeId)
    {
        _editingEdgeId = edgeId;
        InvalidateVisual();
    }

    /// <summary>
    /// Ends editing an edge's label.
    /// </summary>
    public void EndEditEdge()
    {
        _editingEdgeId = null;
        InvalidateVisual();
    }

    #endregion
}
