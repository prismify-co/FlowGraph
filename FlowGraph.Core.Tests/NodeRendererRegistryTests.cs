using FlowGraph.Avalonia;
using FlowGraph.Avalonia.Rendering.NodeRenderers;
using FlowGraph.Core;
using Avalonia.Controls;

namespace FlowGraph.Core.Tests;

public class NodeRendererRegistryTests
{
    [Fact]
    public void DefaultRenderer_IsReturnedForUnknownType()
    {
        var registry = new NodeRendererRegistry();
        
        var renderer = registry.GetRenderer("unknown-type");
        
        Assert.Same(registry.DefaultRenderer, renderer);
    }

    [Fact]
    public void DefaultRenderer_IsReturnedForNullType()
    {
        var registry = new NodeRendererRegistry();
        
        var renderer = registry.GetRenderer(null!);
        
        Assert.Same(registry.DefaultRenderer, renderer);
    }

    [Fact]
    public void DefaultRenderer_IsReturnedForEmptyType()
    {
        var registry = new NodeRendererRegistry();
        
        var renderer = registry.GetRenderer("");
        
        Assert.Same(registry.DefaultRenderer, renderer);
    }

    [Fact]
    public void BuiltInRenderers_AreRegisteredByDefault()
    {
        var registry = new NodeRendererRegistry();
        
        Assert.True(registry.IsRegistered("input"));
        Assert.True(registry.IsRegistered("output"));
    }

    [Fact]
    public void GetRenderer_IsCaseInsensitive()
    {
        var registry = new NodeRendererRegistry();
        
        var renderer1 = registry.GetRenderer("input");
        var renderer2 = registry.GetRenderer("INPUT");
        var renderer3 = registry.GetRenderer("Input");
        
        Assert.Same(renderer1, renderer2);
        Assert.Same(renderer2, renderer3);
    }

    [Fact]
    public void Register_AddsNewRenderer()
    {
        var registry = new NodeRendererRegistry();
        var customRenderer = new TestNodeRenderer();
        
        registry.Register("custom", customRenderer);
        
        Assert.True(registry.IsRegistered("custom"));
        Assert.Same(customRenderer, registry.GetRenderer("custom"));
    }

    [Fact]
    public void Register_OverwritesExistingRenderer()
    {
        var registry = new NodeRendererRegistry();
        var originalRenderer = registry.GetRenderer("input");
        var customRenderer = new TestNodeRenderer();
        
        registry.Register("input", customRenderer);
        
        Assert.Same(customRenderer, registry.GetRenderer("input"));
        Assert.NotSame(originalRenderer, registry.GetRenderer("input"));
    }

    [Fact]
    public void Unregister_RemovesRenderer()
    {
        var registry = new NodeRendererRegistry();
        var customRenderer = new TestNodeRenderer();
        registry.Register("custom", customRenderer);
        
        var result = registry.Unregister("custom");
        
        Assert.True(result);
        Assert.False(registry.IsRegistered("custom"));
        Assert.Same(registry.DefaultRenderer, registry.GetRenderer("custom"));
    }

    [Fact]
    public void Unregister_ReturnsFalseForNonExistentType()
    {
        var registry = new NodeRendererRegistry();
        
        var result = registry.Unregister("non-existent");
        
        Assert.False(result);
    }

    [Fact]
    public void RegisteredTypes_ReturnsAllRegisteredTypes()
    {
        var registry = new NodeRendererRegistry();
        registry.Register("custom1", new TestNodeRenderer());
        registry.Register("custom2", new TestNodeRenderer());
        
        var types = registry.RegisteredTypes.ToList();
        
        Assert.Contains("input", types);
        Assert.Contains("output", types);
        Assert.Contains("custom1", types);
        Assert.Contains("custom2", types);
    }

    [Fact]
    public void ClearCustomRenderers_RemovesCustomButKeepsBuiltIn()
    {
        var registry = new NodeRendererRegistry();
        registry.Register("custom", new TestNodeRenderer());
        
        registry.ClearCustomRenderers();
        
        Assert.False(registry.IsRegistered("custom"));
        Assert.True(registry.IsRegistered("input"));
        Assert.True(registry.IsRegistered("output"));
    }

    [Fact]
    public void Register_ThrowsForNullRenderer()
    {
        var registry = new NodeRendererRegistry();
        
        Assert.Throws<ArgumentNullException>(() => registry.Register("test", null!));
    }

    [Fact]
    public void Register_ThrowsForNullOrEmptyType()
    {
        var registry = new NodeRendererRegistry();
        var renderer = new TestNodeRenderer();
        
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!, renderer));
        Assert.Throws<ArgumentException>(() => registry.Register("", renderer));
        Assert.Throws<ArgumentException>(() => registry.Register("  ", renderer));
    }

    // Test implementation of INodeRenderer
    private class TestNodeRenderer : INodeRenderer
    {
        public Control CreateNodeVisual(Node node, NodeRenderContext context)
        {
            return new Border { Tag = node };
        }

        public void UpdateSelection(Control visual, Node node, NodeRenderContext context)
        {
        }

        public void UpdateSize(Control visual, Node node, NodeRenderContext context, double width, double height)
        {
        }

        public double? GetWidth(Node node, FlowCanvasSettings settings) => null;
        public double? GetHeight(Node node, FlowCanvasSettings settings) => null;
        public double? GetMinWidth(Node node, FlowCanvasSettings settings) => 60;
        public double? GetMinHeight(Node node, FlowCanvasSettings settings) => 40;
    }
}
