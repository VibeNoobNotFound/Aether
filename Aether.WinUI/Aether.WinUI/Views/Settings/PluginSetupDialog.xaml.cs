using Aether.Protos;
using Aether.WinUI.Controls.Renderer;
using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
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

    public PluginSetupDialog(string pluginName)
    {
        InitializeComponent();
        _pluginName = pluginName;
        Title = $"Setup {_pluginName}";
        _grpc = (Application.Current as App)!.Services.GetRequiredService<GrpcClientService>();

        Opened += PluginSetupDialog_Opened;
    }

    private async void PluginSetupDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        await LoadWidgetsAsync();
    }

    private async Task LoadWidgetsAsync()
    {
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
            ShowError($"Failed to load settings: {ex.Message}");
        }
        finally
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void Renderer_ActionTriggered(string actionId, string payload)
    {
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
            ShowError($"Action failed: {ex.Message}");
        }
        finally
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
