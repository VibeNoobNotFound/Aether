using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Views.Insights;

public sealed partial class InsightsDialog : ContentDialog
{
    public InsightsViewModel ViewModel { get; }
    private readonly ILogger<InsightsDialog> _logger;
    private const int SlideCount = 5;

    public InsightsDialog()
    {
        _logger = Ioc.Default.GetRequiredService<ILogger<InsightsDialog>>();
        _logger.LogDebug("InsightsDialog initialized");

        var mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        var vmLogger = Ioc.Default.GetRequiredService<ILogger<InsightsViewModel>>();
        ViewModel = new InsightsViewModel(mainViewModel, vmLogger);
        InitializeComponent();

        Loaded += InsightsDialog_Loaded;
        Slides.SelectionChanged += Slides_SelectionChanged;
    }

    private async void InsightsDialog_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("InsightsDialog loaded");
        await ViewModel.LoadAsync();
        UpdateNavigation();
    }

    private void Slides_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _logger.LogTrace("Insights slide changed: {Index}", Slides.SelectedIndex);
        UpdateNavigation();
    }

    private void UpdateNavigation()
    {
        var index = Slides.SelectedIndex;
        BackButton.IsEnabled = index > 0;
        NextButton.Content = index >= SlideCount - 1 ? "Done" : "Next";
        SlideTextBlock.Text = $"{index + 1} / {SlideCount}";
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Insights back clicked");
        if (Slides.SelectedIndex > 0)
        {
            Slides.SelectedIndex -= 1;
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Insights next clicked");
        if (Slides.SelectedIndex < SlideCount - 1)
        {
            Slides.SelectedIndex += 1;
        }
        else
        {
            Hide();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Insights close clicked");
        Hide();
    }
}
