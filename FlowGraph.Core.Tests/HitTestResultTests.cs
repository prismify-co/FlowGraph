using FlowGraph.Core.Input;
using FlowGraph.Core.Rendering;
using static FlowGraph.Core.Tests.TestHelpers;

namespace FlowGraph.Core.Tests;

/// <summary>
/// Tests for hit test result types and contracts.
/// </summary>
public class HitTestResultTests
{
    #region HitTestResult<T> Tests

    [Fact]
    public void HitTestResult_NoHit_ReturnsCorrectState()
    {
        var position = new Point(100, 200);
        var result = HitTestResult<Node>.NoHit(position);
        
        Assert.False(result.IsHit);
        Assert.Null(result.Element);
        Assert.Equal(100, result.CanvasPosition.X);
        Assert.Equal(200, result.CanvasPosition.Y);
        Assert.Equal(double.MaxValue, result.Distance);
    }

    [Fact]
    public void HitTestResult_WithElement_IsHitReturnsTrue()
    {
        var node = CreateNode("test-node");
        var result = new HitTestResult<Node>
        {
            Element = node,
            CanvasPosition = new Point(100, 200),
            LocalPosition = new Point(10, 20),
            Distance = 0
        };
        
        Assert.True(result.IsHit);
        Assert.Same(node, result.Element);
    }

    #endregion

    #region GraphHitTestResult Factory Tests

    [Fact]
    public void CanvasHit_CreatesCorrectResult()
    {
        var position = new Point(100, 200);
        var result = GraphHitTestResult.CanvasHit(position);
        
        Assert.Equal(HitTargetType.Canvas, result.TargetType);
        Assert.Null(result.Target);
        Assert.True(result.IsCanvasHit);
        Assert.False(result.IsHit);
        Assert.Equal(100, result.CanvasPosition.X);
        Assert.Equal(200, result.CanvasPosition.Y);
    }

    [Fact]
    public void NodeHit_CreatesCorrectResult()
    {
        var node = CreateNode("test-node");
        var position = new Point(100, 200);
        var result = GraphHitTestResult.NodeHit(node, position, distance: 5);
        
        Assert.Equal(HitTargetType.Node, result.TargetType);
        Assert.Same(node, result.Target);
        Assert.Same(node, result.Node);
        Assert.True(result.IsHit);
        Assert.False(result.IsCanvasHit);
        Assert.Equal(5, result.Distance);
    }

    [Fact]
    public void EdgeHit_CreatesCorrectResult()
    {
        var edge = CreateEdge("edge-1", "source", "target");
        var position = new Point(150, 250);
        var result = GraphHitTestResult.EdgeHit(edge, position, distance: 3);
        
        Assert.Equal(HitTargetType.Edge, result.TargetType);
        Assert.Same(edge, result.Target);
        Assert.Same(edge, result.Edge);
        Assert.True(result.IsHit);
        Assert.Equal(3, result.Distance);
    }

    [Fact]
    public void PortHit_CreatesCorrectResult()
    {
        var node = CreateNode("node-1");
        var port = new Port { Id = "port-1", Type = "default" };
        var position = new Point(100, 200);
        
        var result = GraphHitTestResult.PortHit(node, port, isInput: true, position);
        
        Assert.Equal(HitTargetType.Port, result.TargetType);
        Assert.Same(port, result.Port);
        Assert.Same(node, result.PortOwner);
        Assert.True(result.IsInputPort);
        Assert.True(result.IsHit);
    }

    [Fact]
    public void PortHit_OutputPort_IsInputPortReturnsFalse()
    {
        var node = CreateNode("node-1");
        var port = new Port { Id = "port-1", Type = "default" };
        var position = new Point(100, 200);
        
        var result = GraphHitTestResult.PortHit(node, port, isInput: false, position);
        
        Assert.False(result.IsInputPort);
    }

    [Fact]
    public void ResizeHandleHit_CreatesCorrectResult()
    {
        var node = CreateNode("node-1");
        var position = new Point(100, 200);
        
        var result = GraphHitTestResult.ResizeHandleHit(node, ResizeHandlePosition.BottomRight, position);
        
        Assert.Equal(HitTargetType.ResizeHandle, result.TargetType);
        Assert.Equal(ResizeHandlePosition.BottomRight, result.ResizeHandle);
        Assert.Same(node, result.ResizeHandleOwner);
        Assert.True(result.IsHit);
    }

    #endregion

    #region Typed Accessor Tests

    [Fact]
    public void NodeAccessor_OnNonNodeHit_ReturnsNull()
    {
        var edge = CreateEdge("edge-1", "source", "target");
        var result = GraphHitTestResult.EdgeHit(edge, new Point(0, 0));
        
        Assert.Null(result.Node);
        Assert.NotNull(result.Edge);
    }

    [Fact]
    public void EdgeAccessor_OnNonEdgeHit_ReturnsNull()
    {
        var node = CreateNode("test");
        var result = GraphHitTestResult.NodeHit(node, new Point(0, 0));
        
        Assert.Null(result.Edge);
        Assert.NotNull(result.Node);
    }

    [Fact]
    public void PortAccessor_OnNonPortHit_ReturnsNull()
    {
        var node = CreateNode("test");
        var result = GraphHitTestResult.NodeHit(node, new Point(0, 0));
        
        Assert.Null(result.Port);
        Assert.Null(result.PortOwner);
    }

    [Fact]
    public void ResizeHandleAccessor_OnNonResizeHandleHit_ReturnsNull()
    {
        var node = CreateNode("test");
        var result = GraphHitTestResult.NodeHit(node, new Point(0, 0));
        
        Assert.Null(result.ResizeHandle);
        Assert.Null(result.ResizeHandleOwner);
    }

    #endregion

    #region HitTargetType Priority Tests

    [Fact]
    public void AllResizeHandlePositions_AreValid()
    {
        var node = CreateNode("test");
        var positions = Enum.GetValues<ResizeHandlePosition>();
        
        foreach (var pos in positions)
        {
            var result = GraphHitTestResult.ResizeHandleHit(node, pos, new Point(0, 0));
            Assert.Equal(pos, result.ResizeHandle);
        }
    }

    [Fact]
    public void HitTargetType_None_IsCanvasHit()
    {
        var result = new GraphHitTestResult
        {
            TargetType = HitTargetType.None,
            CanvasPosition = new Point(100, 200)
        };
        
        Assert.True(result.IsCanvasHit);
        Assert.False(result.IsHit);
    }

    #endregion
}
