using Avalonia.Controls;

namespace FlowGraph.Demo.Pages;

/// <summary>
/// Interface for demo pages in the FlowGraph demo application.
/// </summary>
public interface IDemoPage
{
    /// <summary>
    /// Title shown in the navigation sidebar.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Short description of the demo.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Creates the content for this demo page.
    /// </summary>
    Control CreateContent();

    /// <summary>
    /// Called when the page is being navigated away from.
    /// Use this to clean up resources (animations, event handlers, etc.)
    /// </summary>
    void OnNavigatingFrom() { }
}
