using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Aether.WinUI.Services;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

public partial class BackendManager : ObservableObject
{
    private Process? _backendProcess;
    
    // DEV TOGGLE: Set to true if you are running the backend manually (dotnet run)
    private readonly bool _useExternalBackend = true; 

    [ObservableProperty] private ConnectionState connectionState = ConnectionState.Disconnected;
    [ObservableProperty] private string statusMessage = "";

    private readonly GrpcClientService _grpcClient;
    private readonly ILogger<BackendManager> _logger;

    public BackendManager(GrpcClientService grpcClient, ILogger<BackendManager> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
        _logger.LogDebug("BackendManager initialized");
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("BackendManager.StartAsync invoked");
        if (_useExternalBackend)
        {
            StatusMessage = "Dev Mode: Using external backend...";
            _logger.LogInformation("Using external backend (dev mode)");
            await StartHealthProbing();
            return;
        }

        StatusMessage = "Starting backend...";
        ConnectionState = ConnectionState.Connecting;

        try
        {
            // Kill any stale instances
            foreach (var proc in Process.GetProcessesByName("AetherBackend"))
            {
                try
                {
                    proc.Kill();
                    _logger.LogInformation("Killed stale backend process: {Pid}", proc.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill stale backend process: {Pid}", proc.Id);
                }
            }

            // Path to backend executable 
            // In a real deployed app, this would be in the same folder or a specific subfolder
            var backendPath = Path.Combine(AppContext.BaseDirectory, "AetherBackend.exe");
            
            if (!File.Exists(backendPath))
            {
                // Fallback for development if not copied to output
                // Try to find relative to project
                var projectDir = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
                if (projectDir != null)
                {
                     var devPath = Path.Combine(projectDir, "Aether.Backend", "bin", "Debug", "net10.0", "Aether.Backend.exe");
                     if (File.Exists(devPath)) backendPath = devPath;
                }
            }

            if (!File.Exists(backendPath))
            {
                StatusMessage = "Backend executable not found!";
                ConnectionState = ConnectionState.Error;
                _logger.LogError("Backend executable not found at {Path}", backendPath);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = backendPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(backendPath)
            };

            _backendProcess = new Process { StartInfo = startInfo };
            _backendProcess.Start();
            _logger.LogInformation("Backend process started: {Pid}", _backendProcess.Id);
            
            StatusMessage = $"Backend started (PID: {_backendProcess.Id})";
            
            await StartHealthProbing();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start backend: {ex.Message}";
            ConnectionState = ConnectionState.Error;
            _logger.LogError(ex, "Failed to start backend");
        }
    }

    private async Task StartHealthProbing()
    {
        _logger.LogDebug("StartHealthProbing started");
        ConnectionState = ConnectionState.Connecting;
        var attempts = 0;
        
        while (attempts < 30)
        {
            try
            {
                var response = await _grpcClient.Client.PingAsync(new Aether.Protos.Empty());
                if (response.Healthy)
                {
                    ConnectionState = ConnectionState.Connected;
                    StatusMessage = "Connected";
                    _logger.LogInformation("Backend connected");
                    return;
                }
            }
            catch (Exception ex)
            {
                // Ignore errors while connecting
                _logger.LogTrace(ex, "Health probe failed attempt {Attempt}", attempts);
            }

            attempts++;
            await Task.Delay(500);
        }

        ConnectionState = ConnectionState.Error;
        StatusMessage = "Failed to connect to backend";
        _logger.LogError("Failed to connect to backend after {Attempts} attempts", attempts);
    }

    public async Task RetryConnectionAsync()
    {
        _logger.LogInformation("RetryConnectionAsync invoked");
        StatusMessage = "Retrying connection...";
        await StartHealthProbing();
    }

    public void Stop()
    {
        _logger.LogInformation("BackendManager.Stop invoked");
        if (_backendProcess != null && !_backendProcess.HasExited)
        {
            try
            {
                _backendProcess.Kill();
                _logger.LogInformation("Backend process killed: {Pid}", _backendProcess.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill backend process: {Pid}", _backendProcess.Id);
            }
            _backendProcess = null;
        }
    }
}
