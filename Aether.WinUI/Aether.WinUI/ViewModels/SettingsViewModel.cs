using Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
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

    [ObservableProperty] private string version = "1.0.0-alpha";
    [ObservableProperty] private int selectedThemeIndex = 0; // 0: System, 1: Dark, 2: Light
    [ObservableProperty] private int navigationStyleIndex = 0; // 0: Sidebar, 1: Top
    [ObservableProperty] private bool isAutoUpdateEnabled = true;
    [ObservableProperty] private bool includeBetaUpdates = false;

    [ObservableProperty] private ObservableCollection<PluginViewModel> plugins = new();

    public SettingsViewModel(GrpcClientService grpc, BackendManager backend, AppSettingsService settings)
    {
        _grpc = grpc;
        _backend = backend;
        _settings = settings;

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
        _settings.AutoUpdateEnabled = value;
    }

    partial void OnIncludeBetaUpdatesChanged(bool value)
    {
        _settings.IncludeBetaUpdates = value;
    }

    partial void OnNavigationStyleIndexChanged(int value)
    {
        _settings.UseTopNavigation = value == 1;
    }

    public async Task LoadPlugins()
    {
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
            System.Diagnostics.Debug.WriteLine($"Failed to load plugins: {ex}");
        }
    }

    [RelayCommand]
    public async Task UninstallPlugin(string pluginName)
    {
        try
        {
            await _grpc.Client.UninstallPluginAsync(new PluginName { Name = pluginName });
            await LoadPlugins();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to uninstall plugin: {ex}");
        }
    }

    [RelayCommand]
    public async Task CheckForUpdates()
    {
        try
        {
            var result = await _grpc.Client.CheckForUpdatesAsync(new CheckUpdateRequest { IncludePrerelease = IncludeBetaUpdates });

            // TODO: Show toast or dialog based on result
            System.Diagnostics.Debug.WriteLine($"Update check: {result.UpdateAvailable} - {result.Version}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex}");
        }
    }

    [RelayCommand]
    public async Task RescanLibrary()
    {
        try
        {
            var call = _grpc.Client.ScanLibrary(new ScanRequest { ForceRefresh = true });
            // Consume the stream to ensure it runs
            await foreach (var _ in call.ResponseStream.ReadAllAsync()) { }
        }
        catch (Exception) { }
    }

    [RelayCommand]
    public async Task ClearLibrary()
    {
        try
        {
            await _grpc.Client.ClearLibraryAsync(new Empty());
            await RescanLibrary(); // Refresh UI
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear library: {ex}");
        }
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
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
        catch { }
    }

    public async Task FactoryResetAsync()
    {
        try
        {
            await _grpc.Client.ResetSystemAsync(new Empty());
            _settings.ClearAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Factory reset failed: {ex}");
            throw;
        }
    }
}
