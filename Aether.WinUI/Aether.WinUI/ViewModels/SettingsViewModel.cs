using Aether.Protos;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Aether.WinUI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly GrpcClientService _grpc;
    private readonly BackendManager _backend;

    [ObservableProperty] private string version = "1.0.0-alpha";
    [ObservableProperty] private int selectedThemeIndex = 0; // 0: System, 1: Dark, 2: Light
    [ObservableProperty] private bool isAutoUpdateEnabled = true;
    [ObservableProperty] private bool includeBetaUpdates = false;

    public SettingsViewModel(GrpcClientService grpc, BackendManager backend)
    {
        _grpc = grpc;
        _backend = backend;

        // Initialize Theme from current state if needed
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        // 0: System (Default), 1: Dark, 2: Light
        if (Window.Current?.Content is FrameworkElement root)
        {
            switch (value)
            {
                case 1: root.RequestedTheme = ElementTheme.Dark; break;
                case 2: root.RequestedTheme = ElementTheme.Light; break;
                default: root.RequestedTheme = ElementTheme.Default; break;
            }
        }
    }

    [RelayCommand]
    public async Task CheckForUpdates()
    {
        // Todo: Implement update check logic via Backend or AutoUpdater
        await Task.Delay(1000); // Mock delay
    }

    [RelayCommand]
    public void OpenMetadataSources()
    {
        // Todo: Open dialog or navigation
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
        // Confirmation is handled in View usually, or here via Dialog service
        // For now, executing directly
        // There isn't a "ClearLibrary" RPC yet, would need to implement in Backend
        // or iterate and remove all.
        // Assuming we rely on Rescan for now.
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
        }
        catch { }
    }

    [RelayCommand]
    public void FactoryReset()
    {
        // Dangerous! 
    }
}
