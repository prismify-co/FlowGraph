using Avalonia.Controls;
using Avalonia.Interactivity;
using FlowGraph.Demo.Pages;

namespace FlowGraph.Demo.Views;

public partial class MainWindow : ShadUI.Window
{
    private readonly IDemoPage[] _pages;
    private IDemoPage? _currentPage;

    public MainWindow()
    {
        InitializeComponent();

        // Create page instances
        _pages = new IDemoPage[]
        {
            new InteractiveDemoPage(),
            new ShapesDemoPage(),
            new PerformanceDemoPage()
        };

        // Show first page on load
        this.Loaded += (_, _) => NavigateTo(_pages[0]);
    }

    private void NavigateTo(IDemoPage page)
    {
        // Notify current page it's being navigated away from
        _currentPage?.OnNavigatingFrom();

        // Set new content
        _currentPage = page;
        PageContent.Content = page.CreateContent();
    }

    private void OnNavInteractiveClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(_pages[0]);
    }

    private void OnNavShapesClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(_pages[1]);
    }

    private void OnNavPerformanceClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(_pages[2]);
    }

    private void OnToggleSidebar(object? sender, RoutedEventArgs e)
    {
        MainSidebar.Expanded = !MainSidebar.Expanded;
    }
}
