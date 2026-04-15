using Aether.Protos;
using Aether.WinUI.Controls.Renderer;
using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aether.WinUI.Views.Settings;

public sealed partial class PluginSetupDialog : ContentDialog
{
    private readonly GrpcClientService _grpc;
    private readonly string _pluginName;
    private readonly Dictionary<string, string> _formValues = new();
    private readonly ILogger<PluginSetupDialog> _logger;

    public PluginSetupDialog(string pluginName)
    {
        InitializeComponent();
        _pluginName = pluginName;
        Title = $"Setup {_pluginName}";
        _grpc = Ioc.Default.GetRequiredService<GrpcClientService>();
        _logger = Ioc.Default.GetRequiredService<ILogger<PluginSetupDialog>>();
        _logger.LogDebug("PluginSetupDialog initialized for {Plugin}", pluginName);

        Opened += PluginSetupDialog_Opened;
    }

    private async void PluginSetupDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        _logger.LogInformation("PluginSetupDialog opened");
        await LoadWidgetsAsync();
    }

    private async Task LoadWidgetsAsync()
    {
        _logger.LogInformation("LoadWidgetsAsync for plugin {Plugin}", _pluginName);
        LoadingBar.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;
        ContentPanel.Children.Clear();

        try
        {
            var response = await _grpc.Client.GetWidgetsAsync(new WidgetRequest
            {
                PluginName = _pluginName,
                Location = WidgetLocation.Settings
            });

            if (response.Widgets.Count == 0)
            {
                ShowError("This plugin does not require configuration.");
                return;
            }

            foreach (var widget in response.Widgets.OrderBy(w => w.SortOrder))
            {
                var renderer = new WidgetRenderer
                {
                    FormValues = _formValues,
                    Widget = widget
                };
                renderer.ActionTriggered += Renderer_ActionTriggered;
                ContentPanel.Children.Add(renderer);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin settings");
            ShowError($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void Renderer_ActionTriggered(string actionId, string payload)
    {
        _logger.LogInformation("Plugin action triggered: {ActionId}", actionId);
        LoadingBar.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;

        try
        {
            var finalPayload = payload;
            if (payload == "Submit" || (string.IsNullOrWhiteSpace(payload) && _formValues.Count > 0))
            {
                finalPayload = JsonSerializer.Serialize(_formValues);
            }

            var result = await _grpc.Client.TriggerPluginActionAsync(new PluginAction
            {
                PluginName = _pluginName,
                ActionId = actionId,
                PayloadJson = finalPayload
            });

            if (!result.Success)
            {
                ShowError(result.Message);
            }
            else
            {
                Hide();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin action failed: {ActionId}", actionId);
            ShowError($"Action failed: {ex.Message}");
        }
        finally
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowError(string message)
    {
        _logger.LogWarning("PluginSetupDialog error: {Message}", message);
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
