using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Aether.WinUI.Views.Onboarding;

public sealed partial class OnboardingDialog : ContentDialog
{
    private AppSettingsService? _settings;
    private MainViewModel? _mainViewModel;
    private ILogger<OnboardingDialog> _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<OnboardingDialog>.Instance;
    private readonly ObservableCollection<PluginViewModel> _fallbackPlugins = new();
    private bool _isLoaded;
    private const int SlideCount = 6;

    public ObservableCollection<PluginViewModel> Plugins => _mainViewModel?.Plugins ?? _fallbackPlugins;
    public string SlideText => $"{(Slides?.SelectedIndex ?? 0) + 1} / {SlideCount}";

    public OnboardingDialog()
    {
        _settings = Ioc.Default.GetService<AppSettingsService>();
        _mainViewModel = Ioc.Default.GetService<MainViewModel>();
        _logger = Ioc.Default.GetService<ILogger<OnboardingDialog>>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OnboardingDialog>.Instance;
        InitializeComponent();
        Loaded += OnboardingDialog_Loaded;
    }

    private void OnboardingDialog_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("OnboardingDialog loaded");
        _isLoaded = true;
        UpdateNavigation();
        Bindings.Update();
    }

    private void Slides_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded)
        {
            return;
        }
        _logger.LogTrace("Onboarding slide changed");
        UpdateNavigation();
    }

    private void UpdateNavigation()
    {
        BackButton.IsEnabled = Slides.SelectedIndex > 0;
        NextButton.Content = Slides.SelectedIndex >= SlideCount - 1 ? "Get Started" : "Next";
        SlideTextBlock.Text = $"{Slides.SelectedIndex + 1} / {SlideCount}";
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Onboarding back clicked");
        if (Slides.SelectedIndex > 0)
        {
            Slides.SelectedIndex -= 1;
        }
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Onboarding next clicked");
        if (Slides.SelectedIndex < SlideCount - 1)
        {
            Slides.SelectedIndex += 1;
        }
        else
        {
            await CompleteOnboardingAsync();
        }
    }

    private async void Skip_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Onboarding skipped");
        await CompleteOnboardingAsync();
    }

    private async Task CompleteOnboardingAsync()
    {
        _logger.LogInformation("Completing onboarding");
        if (_settings != null)
        {
            _settings.HasCompletedOnboarding = true;
        }
        Hide();
        if (_mainViewModel != null)
        {
            await _mainViewModel.ScanLibraryAsync();
        }
    }
}
