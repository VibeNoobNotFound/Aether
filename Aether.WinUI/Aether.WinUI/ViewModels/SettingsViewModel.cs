using Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Aether.WinUI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly GrpcClientService _grpc;
    private readonly BackendManager _backend;
    private readonly AppSettingsService _settings;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty] private string version = "1.0.0-alpha";
    [ObservableProperty] private int selectedThemeIndex = 0; // 0: System, 1: Dark, 2: Light
    [ObservableProperty] private int navigationStyleIndex = 0; // 0: Sidebar, 1: Top
    [ObservableProperty] private bool isAutoUpdateEnabled = true;
    [ObservableProperty] private bool includeBetaUpdates = false;

    [ObservableProperty] private ObservableCollection<PluginViewModel> plugins = new();

    public SettingsViewModel(GrpcClientService grpc, BackendManager backend, AppSettingsService settings, ILogger<SettingsViewModel> logger)
    {
        _grpc = grpc;
        _backend = backend;
        _settings = settings;
        _logger = logger;
        _logger.LogDebug("SettingsViewModel initialized");

        // Load settings
        SelectedThemeIndex = _settings.SelectedThemeIndex;
        IsAutoUpdateEnabled = _settings.AutoUpdateEnabled;
        IncludeBetaUpdates = _settings.IncludeBetaUpdates;
        NavigationStyleIndex = _settings.UseTopNavigation ? 1 : 0;

        // Load initial data
        _ = LoadPlugins();
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        _logger.LogInformation("Theme changed: {Index}", value);
        _settings.SelectedThemeIndex = value;

        // 0: System (Default), 1: Dark, 2: Light
        if (App.Current?.MainWindow?.Content is FrameworkElement root)
        {
            switch (value)
            {
                case 1: root.RequestedTheme = ElementTheme.Dark; break;
                case 2: root.RequestedTheme = ElementTheme.Light; break;
                default: root.RequestedTheme = ElementTheme.Default; break;
            }
        }
    }

    partial void OnIsAutoUpdateEnabledChanged(bool value)
    {
        _logger.LogInformation("AutoUpdateEnabled changed: {Value}", value);
        _settings.AutoUpdateEnabled = value;
    }

    partial void OnIncludeBetaUpdatesChanged(bool value)
    {
        _logger.LogInformation("IncludeBetaUpdates changed: {Value}", value);
        _settings.IncludeBetaUpdates = value;
    }

    partial void OnNavigationStyleIndexChanged(int value)
    {
        _logger.LogInformation("NavigationStyleIndex changed: {Value}", value);
        _settings.UseTopNavigation = value == 1;
    }

    public async Task LoadPlugins()
    {
        _logger.LogInformation("LoadPlugins invoked");
        try
        {
            var response = await _grpc.Client.GetPluginsAsync(new Empty());
            Plugins.Clear();
            foreach (var p in response.Plugins)
            {
                Plugins.Add(PluginViewModel.FromProto(p));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugins");
        }
    }

    [RelayCommand]
    public async Task UninstallPlugin(string pluginName)
    {
        _logger.LogInformation("UninstallPlugin invoked: {PluginName}", pluginName);
        try
        {
            await _grpc.Client.UninstallPluginAsync(new PluginName { Name = pluginName });
            await LoadPlugins();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall plugin");
        }
    }

    [RelayCommand]
    public async Task CheckForUpdates()
    {
        _logger.LogInformation("CheckForUpdates invoked");
        try
        {
            var result = await _grpc.Client.CheckForUpdatesAsync(new CheckUpdateRequest { IncludePrerelease = IncludeBetaUpdates });

            // TODO: Show toast or dialog based on result
            System.Diagnostics.Debug.WriteLine($"Update check: {result.UpdateAvailable} - {result.Version}");
            _logger.LogInformation("Update check completed: {Available} {Version}", result.UpdateAvailable, result.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update check failed");
        }
    }

    [RelayCommand]
    public async Task RescanLibrary()
    {
        _logger.LogInformation("RescanLibrary invoked");
        try
        {
            var call = _grpc.Client.ScanLibrary(new ScanRequest { ForceRefresh = true });
            // Consume the stream to ensure it runs
            await foreach (var _ in call.ResponseStream.ReadAllAsync()) { }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RescanLibrary failed");
        }
    }

    [RelayCommand]
    public async Task ClearLibrary()
    {
        _logger.LogInformation("ClearLibrary invoked");
        try
        {
            await _grpc.Client.ClearLibraryAsync(new Empty());
            await RescanLibrary(); // Refresh UI
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear library");
        }
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
        _logger.LogInformation("OpenLogsFolder invoked");
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aether", "logs");
            if (Directory.Exists(logPath))
            {
                Process.Start(new ProcessStartInfo { FileName = logPath, UseShellExecute = true });
            }
            else
            {
                // Try create it
                Directory.CreateDirectory(logPath);
                Process.Start(new ProcessStartInfo { FileName = logPath, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenLogsFolder failed");
        }
    }

    public async Task FactoryResetAsync()
    {
        _logger.LogInformation("FactoryResetAsync invoked");
        try
        {
            await _grpc.Client.ResetSystemAsync(new Empty());
            _settings.ClearAll();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Factory reset failed");
            throw;
        }
    }
}
