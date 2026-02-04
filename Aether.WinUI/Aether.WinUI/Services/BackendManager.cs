using CommunityToolkit.Mvvm.ComponentModel;
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

    public BackendManager(GrpcClientService grpcClient)
    {
        _grpcClient = grpcClient;
    }

    public async Task StartAsync()
    {
        if (_useExternalBackend)
        {
            StatusMessage = "Dev Mode: Using external backend...";
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
                try { proc.Kill(); } catch { }
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
            
            StatusMessage = $"Backend started (PID: {_backendProcess.Id})";
            
            await StartHealthProbing();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start backend: {ex.Message}";
            ConnectionState = ConnectionState.Error;
        }
    }

    private async Task StartHealthProbing()
    {
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
                    return;
                }
            }
            catch
            {
                // Ignore errors while connecting
            }

            attempts++;
            await Task.Delay(500);
        }

        ConnectionState = ConnectionState.Error;
        StatusMessage = "Failed to connect to backend";
    }

    public async Task RetryConnectionAsync()
    {
        StatusMessage = "Retrying connection...";
        await StartHealthProbing();
    }

    public void Stop()
    {
        if (_backendProcess != null && !_backendProcess.HasExited)
        {
            try { _backendProcess.Kill(); } catch { }
            _backendProcess = null;
        }
    }
}
