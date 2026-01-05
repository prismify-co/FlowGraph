using FlowGraph.Avalonia.Animation;
using FlowGraph.Core;
using Avalonia.Media;

namespace FlowGraph.Core.Tests;

public class AnimationTests
{
    #region Easing Tests

    [Fact]
    public void Easing_Linear_ReturnsInput()
    {
        Assert.Equal(0.0, Easing.Linear(0.0));
        Assert.Equal(0.5, Easing.Linear(0.5));
        Assert.Equal(1.0, Easing.Linear(1.0));
    }

    [Fact]
    public void Easing_EaseOutQuad_StartsAtZero()
    {
        Assert.Equal(0.0, Easing.EaseOutQuad(0.0));
    }

    [Fact]
    public void Easing_EaseOutQuad_EndsAtOne()
    {
        Assert.Equal(1.0, Easing.EaseOutQuad(1.0));
    }

    [Fact]
    public void Easing_EaseOutCubic_IsMonotonic()
    {
        var prev = 0.0;
        for (double t = 0.0; t <= 1.0; t += 0.1)
        {
            var current = Easing.EaseOutCubic(t);
            Assert.True(current >= prev, $"EaseOutCubic should be monotonic at t={t}");
            prev = current;
        }
    }

    [Fact]
    public void Easing_AllFunctions_StartAtZero()
    {
        Assert.Equal(0.0, Easing.Linear(0.0));
        Assert.Equal(0.0, Easing.EaseInQuad(0.0));
        Assert.Equal(0.0, Easing.EaseOutQuad(0.0));
        Assert.Equal(0.0, Easing.EaseInOutQuad(0.0));
        Assert.Equal(0.0, Easing.EaseInCubic(0.0));
        Assert.Equal(0.0, Easing.EaseOutCubic(0.0));
        Assert.Equal(0.0, Easing.EaseInOutCubic(0.0));
        Assert.Equal(0.0, Easing.EaseOutExpo(0.0), 5);
    }

    [Fact]
    public void Easing_AllFunctions_EndAtOne()
    {
        Assert.Equal(1.0, Easing.Linear(1.0));
        Assert.Equal(1.0, Easing.EaseInQuad(1.0));
        Assert.Equal(1.0, Easing.EaseOutQuad(1.0));
        Assert.Equal(1.0, Easing.EaseInOutQuad(1.0));
        Assert.Equal(1.0, Easing.EaseInCubic(1.0));
        Assert.Equal(1.0, Easing.EaseOutCubic(1.0));
        Assert.Equal(1.0, Easing.EaseInOutCubic(1.0));
        Assert.Equal(1.0, Easing.EaseOutExpo(1.0));
        Assert.Equal(1.0, Easing.EaseInOutExpo(1.0));
    }

    [Fact]
    public void Easing_EaseInOutQuad_SymmetricAtHalf()
    {
        Assert.Equal(0.5, Easing.EaseInOutQuad(0.5));
    }

    #endregion

    #region EdgeFadeAnimation Tests

    [Fact]
    public void EdgeFadeAnimation_StartsWithStartOpacity()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeFadeAnimation(edge, 0.2, 1.0);
        
        Assert.Equal(0.2, animation.CurrentOpacity);
    }

    [Fact]
    public void EdgeFadeAnimation_CompletesAfterDuration()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeFadeAnimation(edge, 0, 1, duration: 0.1);
        
        Assert.False(animation.IsComplete);
        
        animation.Update(0.15);
        
        Assert.True(animation.IsComplete);
        Assert.Equal(1.0, animation.CurrentOpacity);
    }

    [Fact]
    public void EdgeFadeAnimation_FadeIn_StartsAtZero()
    {
        var edge = CreateTestEdge();
        var animation = EdgeFadeAnimation.FadeIn(edge);
        
        Assert.Equal(0.0, animation.CurrentOpacity);
    }

    [Fact]
    public void EdgeFadeAnimation_FadeOut_StartsAtOne()
    {
        var edge = CreateTestEdge();
        var animation = EdgeFadeAnimation.FadeOut(edge);
        
        Assert.Equal(1.0, animation.CurrentOpacity);
    }

    [Fact]
    public void EdgeFadeAnimation_CallsOnUpdate()
    {
        var edge = CreateTestEdge();
        var updateCalled = false;
        double lastOpacity = -1;
        
        var animation = new EdgeFadeAnimation(
            edge, 0, 1, 0.1,
            onUpdate: (e, opacity) => { updateCalled = true; lastOpacity = opacity; });
        
        animation.Update(0.05);
        
        Assert.True(updateCalled);
        Assert.True(lastOpacity > 0 && lastOpacity < 1);
    }

    [Fact]
    public void EdgeFadeAnimation_CallsOnComplete()
    {
        var edge = CreateTestEdge();
        var completeCalled = false;
        
        var animation = new EdgeFadeAnimation(
            edge, 0, 1, 0.1,
            onComplete: () => completeCalled = true);
        
        animation.Update(0.15);
        
        Assert.True(completeCalled);
    }

    #endregion

    #region EdgeFlowAnimation Tests

    [Fact]
    public void EdgeFlowAnimation_IncrementsDashOffset()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeFlowAnimation(edge, speed: 100);
        
        Assert.Equal(0, animation.CurrentDashOffset);
        
        animation.Update(0.1);
        
        Assert.Equal(10, animation.CurrentDashOffset, 1);
    }

    [Fact]
    public void EdgeFlowAnimation_ReverseDecrementsDashOffset()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeFlowAnimation(edge, speed: 100, reverse: true);
        
        Assert.Equal(0, animation.CurrentDashOffset);
        
        animation.Update(0.1);
        
        Assert.Equal(-10, animation.CurrentDashOffset, 1);
    }

    [Fact]
    public void EdgeFlowAnimation_NegativeSpeedIsReverse()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeFlowAnimation(edge, speed: -100);
        
        animation.Update(0.1);
        
        Assert.Equal(-10, animation.CurrentDashOffset, 1);
    }

    [Fact]
    public void EdgeFlowAnimation_ContinuesIndefinitely()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeFlowAnimation(edge);
        
        animation.Update(1.0);
        animation.Update(1.0);
        animation.Update(1.0);
        
        Assert.False(animation.IsComplete);
    }

    [Fact]
    public void EdgeFlowAnimation_StopsAfterMaxDuration()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeFlowAnimation(edge, speed: 50, reverse: false, maxDuration: 0.5);
        
        animation.Update(0.3);
        Assert.False(animation.IsComplete);
        
        animation.Update(0.3);
        Assert.True(animation.IsComplete);
    }

    #endregion

    #region EdgePulseAnimation Tests

    [Fact]
    public void EdgePulseAnimation_StartsAtBaseThickness()
    {
        var edge = CreateTestEdge();
        var animation = new EdgePulseAnimation(edge, baseThickness: 3);
        
        Assert.Equal(3, animation.CurrentThickness);
    }

    [Fact]
    public void EdgePulseAnimation_CompletesAfterDuration()
    {
        var edge = CreateTestEdge();
        var animation = new EdgePulseAnimation(edge, duration: 0.5);
        
        Assert.False(animation.IsComplete);
        
        animation.Update(0.6);
        
        Assert.True(animation.IsComplete);
    }

    [Fact]
    public void EdgePulseAnimation_OscillatesThickness()
    {
        var edge = CreateTestEdge();
        var animation = new EdgePulseAnimation(edge, baseThickness: 2, pulseAmount: 2, frequency: 1, duration: 2);
        
        var thicknesses = new List<double>();
        for (int i = 0; i < 10; i++)
        {
            animation.Update(0.1);
            thicknesses.Add(animation.CurrentThickness);
        }
        
        var midpoint = 3.0;
        Assert.Contains(thicknesses, t => t > midpoint);
        Assert.Contains(thicknesses, t => t < midpoint);
    }

    #endregion

    #region EdgeColorAnimation Tests

    [Fact]
    public void EdgeColorAnimation_StartsWithStartColor()
    {
        var edge = CreateTestEdge();
        var startColor = Colors.Red;
        var animation = new EdgeColorAnimation(edge, startColor, Colors.Blue);
        
        Assert.Equal(startColor, animation.CurrentColor);
    }

    [Fact]
    public void EdgeColorAnimation_EndsWithEndColor()
    {
        var edge = CreateTestEdge();
        var endColor = Colors.Green;
        var animation = new EdgeColorAnimation(edge, Colors.Red, endColor, duration: 0.1);
        
        animation.Update(0.15);
        
        Assert.Equal(endColor, animation.CurrentColor);
    }

    [Fact]
    public void EdgeColorAnimation_InterpolatesColor()
    {
        var edge = CreateTestEdge();
        var animation = new EdgeColorAnimation(
            edge, 
            Color.FromRgb(0, 0, 0), 
            Color.FromRgb(100, 100, 100), 
            duration: 1.0,
            easing: Easing.Linear);
        
        animation.Update(0.5);
        
        Assert.True(animation.CurrentColor.R >= 40 && animation.CurrentColor.R <= 60);
        Assert.True(animation.CurrentColor.G >= 40 && animation.CurrentColor.G <= 60);
        Assert.True(animation.CurrentColor.B >= 40 && animation.CurrentColor.B <= 60);
    }

    #endregion

    #region MultiEdgeFadeAnimation Tests

    [Fact]
    public void MultiEdgeFadeAnimation_AnimatesAllEdges()
    {
        var edges = new[] { CreateTestEdge(), CreateTestEdge(), CreateTestEdge() };
        var updatedEdges = new List<Edge>();
        
        var animation = new MultiEdgeFadeAnimation(
            edges, 0, 1, 0.1,
            onUpdate: (edgeList, opacity) => updatedEdges.AddRange(edgeList));
        
        animation.Update(0.05);
        
        Assert.Equal(3, updatedEdges.Count);
    }

    #endregion

    #region NodeFadeAnimation Tests

    [Fact]
    public void NodeFadeAnimation_StartsWithStartOpacity()
    {
        var node = CreateTestNode();
        var animation = new NodeFadeAnimation(node, 0.3, 1.0);
        
        Assert.Equal(0.3, animation.CurrentOpacity);
    }

    [Fact]
    public void NodeFadeAnimation_FadeIn_StartsAtZero()
    {
        var node = CreateTestNode();
        var animation = NodeFadeAnimation.FadeIn(node);
        
        Assert.Equal(0.0, animation.CurrentOpacity);
    }

    [Fact]
    public void NodeFadeAnimation_FadeOut_StartsAtOne()
    {
        var node = CreateTestNode();
        var animation = NodeFadeAnimation.FadeOut(node);
        
        Assert.Equal(1.0, animation.CurrentOpacity);
    }

    [Fact]
    public void NodeFadeAnimation_CompletesAfterDuration()
    {
        var node = CreateTestNode();
        var animation = new NodeFadeAnimation(node, 0, 1, duration: 0.1);
        
        animation.Update(0.15);
        
        Assert.True(animation.IsComplete);
        Assert.Equal(1.0, animation.CurrentOpacity);
    }

    #endregion

    #region NodeScaleAnimation Tests

    [Fact]
    public void NodeScaleAnimation_StartsWithStartScale()
    {
        var node = CreateTestNode();
        var animation = new NodeScaleAnimation(node, 0.5, 1.0);
        
        Assert.Equal(0.5, animation.CurrentScale);
    }

    [Fact]
    public void NodeScaleAnimation_PopIn_StartsSmall()
    {
        var node = CreateTestNode();
        var animation = NodeScaleAnimation.PopIn(node);
        
        Assert.Equal(0.5, animation.CurrentScale);
    }

    [Fact]
    public void NodeScaleAnimation_ShrinkOut_StartsAtOne()
    {
        var node = CreateTestNode();
        var animation = NodeScaleAnimation.ShrinkOut(node);
        
        Assert.Equal(1.0, animation.CurrentScale);
    }

    [Fact]
    public void NodeScaleAnimation_CompletesWithEndScale()
    {
        var node = CreateTestNode();
        var animation = new NodeScaleAnimation(node, 0.5, 1.5, duration: 0.1);
        
        animation.Update(0.15);
        
        Assert.True(animation.IsComplete);
        Assert.Equal(1.5, animation.CurrentScale, 2);
    }

    #endregion

    #region NodeAppearAnimation Tests

    [Fact]
    public void NodeAppearAnimation_Appear_StartsHidden()
    {
        var node = CreateTestNode();
        var animation = NodeAppearAnimation.Appear(node);
        
        Assert.Equal(0, animation.CurrentOpacity);
        Assert.Equal(0.8, animation.CurrentScale);
    }

    [Fact]
    public void NodeAppearAnimation_Disappear_StartsVisible()
    {
        var node = CreateTestNode();
        var animation = NodeAppearAnimation.Disappear(node);
        
        Assert.Equal(1, animation.CurrentOpacity);
        Assert.Equal(1, animation.CurrentScale);
    }

    [Fact]
    public void NodeAppearAnimation_Appear_EndsVisible()
    {
        var node = CreateTestNode();
        double finalOpacity = 0;
        double finalScale = 0;
        
        var animation = NodeAppearAnimation.Appear(node, 0.1, (n, o, s) => { finalOpacity = o; finalScale = s; });
        animation.Update(0.15);
        
        Assert.True(animation.IsComplete);
        Assert.Equal(1, finalOpacity, 2);
        Assert.True(finalScale >= 0.99 && finalScale <= 1.1); // May overshoot slightly
    }

    #endregion

    #region MultiNodeAppearAnimation Tests

    [Fact]
    public void MultiNodeAppearAnimation_AnimatesAllNodes()
    {
        var nodes = new[] { CreateTestNode(), CreateTestNode(), CreateTestNode() };
        var updatedNodes = new HashSet<Node>();
        
        var animation = new MultiNodeAppearAnimation(
            nodes, true, 0.1, 0,
            onUpdate: (n, o, s) => updatedNodes.Add(n));
        
        animation.Update(0.05);
        
        Assert.Equal(3, updatedNodes.Count);
    }

    [Fact]
    public void MultiNodeAppearAnimation_StaggersAnimations()
    {
        var nodes = new[] { CreateTestNode(), CreateTestNode(), CreateTestNode() };
        var nodeOpacities = new Dictionary<Node, double>();
        
        var animation = new MultiNodeAppearAnimation(
            nodes, true, 0.1, 0.1,
            onUpdate: (n, o, s) => nodeOpacities[n] = o);
        
        // After 0.05s, only first node should have started
        animation.Update(0.05);
        
        Assert.True(nodeOpacities[nodes[0]] > 0);
        Assert.Equal(0, nodeOpacities[nodes[1]]);
        Assert.Equal(0, nodeOpacities[nodes[2]]);
    }

    #endregion

    #region GroupCollapseAnimation Tests

    [Fact]
    public void GroupCollapseAnimation_Collapse_StartsExpanded()
    {
        var group = CreateTestGroup();
        var animation = GroupCollapseAnimation.Collapse(group, 300, 200, 150, 50);
        
        Assert.Equal(300, animation.CurrentWidth);
        Assert.Equal(200, animation.CurrentHeight);
        Assert.Equal(1, animation.ChildrenOpacity);
    }

    [Fact]
    public void GroupCollapseAnimation_Expand_StartsCollapsed()
    {
        var group = CreateTestGroup();
        var animation = GroupCollapseAnimation.Expand(group, 300, 200, 150, 50);
        
        Assert.Equal(150, animation.CurrentWidth);
        Assert.Equal(50, animation.CurrentHeight);
        Assert.Equal(0, animation.ChildrenOpacity);
    }

    [Fact]
    public void GroupCollapseAnimation_Collapse_EndsCollapsed()
    {
        var group = CreateTestGroup();
        double finalWidth = 0, finalHeight = 0, finalOpacity = 1;
        
        var animation = GroupCollapseAnimation.Collapse(group, 300, 200, 150, 50, 0.1,
            onUpdate: (g, w, h, o) => { finalWidth = w; finalHeight = h; finalOpacity = o; });
        
        animation.Update(0.15);
        
        Assert.True(animation.IsComplete);
        Assert.Equal(150, finalWidth, 1);
        Assert.Equal(50, finalHeight, 1);
        Assert.Equal(0, finalOpacity, 1);
    }

    [Fact]
    public void GroupCollapseAnimation_ChildrenFadeOutFirst()
    {
        var group = CreateTestGroup();
        var opacities = new List<double>();
        
        var animation = GroupCollapseAnimation.Collapse(group, 300, 200, 150, 50, 0.4,
            onUpdate: (g, w, h, o) => opacities.Add(o));
        
        // Sample at multiple points
        animation.Update(0.1);
        animation.Update(0.1);
        animation.Update(0.1);
        animation.Update(0.1);
        
        // Children should fade out in first half
        Assert.True(opacities[0] < 1);
        Assert.Equal(0, opacities[1], 1);
    }

    #endregion

    #region SelectionPulseAnimation Tests

    [Fact]
    public void SelectionPulseAnimation_StartsAtZero()
    {
        var animation = new SelectionPulseAnimation();
        Assert.Equal(0, animation.CurrentIntensity);
    }

    [Fact]
    public void SelectionPulseAnimation_RisesAndFalls()
    {
        var intensities = new List<double>();
        var animation = new SelectionPulseAnimation(0.4, i => intensities.Add(i));
        
        for (int i = 0; i < 10; i++)
        {
            animation.Update(0.04);
        }
        
        // Should have risen and then fallen
        Assert.True(intensities.Max() > 0.5);
        Assert.True(intensities.Last() < intensities.Max());
    }

    [Fact]
    public void SelectionPulseAnimation_CompletesAfterDuration()
    {
        var animation = new SelectionPulseAnimation(0.2);
        
        animation.Update(0.25);
        
        Assert.True(animation.IsComplete);
    }

    #endregion

    #region BoxSelectionAnimation Tests

    [Fact]
    public void BoxSelectionAnimation_IncrementsOffset()
    {
        var animation = new BoxSelectionAnimation(100);
        
        Assert.Equal(0, animation.CurrentDashOffset);
        
        animation.Update(0.1);
        
        Assert.Equal(10, animation.CurrentDashOffset, 1);
    }

    [Fact]
    public void BoxSelectionAnimation_NeverCompletes()
    {
        var animation = new BoxSelectionAnimation();
        
        animation.Update(10.0);
        
        Assert.False(animation.IsComplete);
    }

    [Fact]
    public void BoxSelectionAnimation_WrapsOffset()
    {
        var animation = new BoxSelectionAnimation(1000);
        
        animation.Update(0.15);
        
        Assert.True(animation.CurrentDashOffset < 100);
    }

    #endregion

    #region Helper Methods

    private static Edge CreateTestEdge()
    {
        return new Edge
        {
            Source = "node1",
            Target = "node2",
            SourcePort = "out1",
            TargetPort = "in1"
        };
    }

    private static Node CreateTestNode()
    {
        return new Node
        {
            Type = "default",
            Position = new Point(100, 100)
        };
    }

    private static Node CreateTestGroup()
    {
        return new Node
        {
            Type = "group",
            IsGroup = true,
            Label = "Test Group",
            Position = new Point(50, 50),
            Width = 300,
            Height = 200
        };
    }

    #endregion
}
