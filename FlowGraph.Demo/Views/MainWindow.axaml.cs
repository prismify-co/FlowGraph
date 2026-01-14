using Avalonia.Controls;
using Avalonia.Interactivity;
using FlowGraph.Demo.Pages;

namespace FlowGraph.Demo.Views;

public partial class MainWindow : Window
{
    private readonly IDemoPage[] _pages;
    private IDemoPage? _currentPage;
    private Button? _selectedNavButton;

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
        this.Loaded += (_, _) => NavigateTo(_pages[0], NavInteractive);
    }

    private void NavigateTo(IDemoPage page, Button navButton)
    {
        // Notify current page it's being navigated away from
        _currentPage?.OnNavigatingFrom();

        // Update nav button styles
        if (_selectedNavButton != null)
        {
            _selectedNavButton.Classes.Remove("selected");
        }
        navButton.Classes.Add("selected");
        _selectedNavButton = navButton;

        // Set new content
        _currentPage = page;
        PageContent.Content = page.CreateContent();
    }

    private void OnNavInteractiveClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(_pages[0], NavInteractive);
    }

    private void OnNavShapesClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(_pages[1], NavShapes);
    }

    private void OnNavPerformanceClick(object? sender, RoutedEventArgs e)
    {
        NavigateTo(_pages[2], NavPerformance);
    }
}
