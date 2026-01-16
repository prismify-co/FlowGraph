using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlowGraph.Demo.Pages;

namespace FlowGraph.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDemoPage[] _pages;
    private IDemoPage? _currentPage;

    [ObservableProperty]
    private Control? _selectedPage;

    [ObservableProperty]
    private string _currentRoute = "interactive";

    [ObservableProperty]
    private bool _isDarkTheme;

    public MainWindowViewModel()
    {
        // Create page instances
        _pages = new IDemoPage[]
        {
            new InteractiveDemoPage(),
            new EdgeStylesDemoPage(),
            new ShapesDemoPage(),
            new PerformanceDemoPage()
        };

        // Check current theme
        var app = Application.Current;
        _isDarkTheme = app?.ActualThemeVariant == ThemeVariant.Dark;

        // Show first page
        NavigateTo(_pages[0], "interactive");
    }

    private void NavigateTo(IDemoPage page, string route)
    {
        // Notify current page it's being navigated away from
        _currentPage?.OnNavigatingFrom();

        // Set new content
        _currentPage = page;
        CurrentRoute = route;
        SelectedPage = page.CreateContent();
    }

    [RelayCommand]
    private void OpenInteractive()
    {
        NavigateTo(_pages[0], "interactive");
    }

    [RelayCommand]
    private void OpenEdgeStyles()
    {
        NavigateTo(_pages[1], "edge-styles");
    }

    [RelayCommand]
    private void OpenShapes()
    {
        NavigateTo(_pages[2], "shapes");
    }

    [RelayCommand]
    private void OpenPerformance()
    {
        NavigateTo(_pages[3], "performance");
    }

    [RelayCommand]
    private void SwitchTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        // Toggle between light and dark
        if (app.ActualThemeVariant == ThemeVariant.Dark)
        {
            app.RequestedThemeVariant = ThemeVariant.Light;
            IsDarkTheme = false;
        }
        else
        {
            app.RequestedThemeVariant = ThemeVariant.Dark;
            IsDarkTheme = true;
        }
    }
}

