using global::Aether.Protos;
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

namespace Aether.WinUI.Views.Library;

public sealed partial class LibraryAddMenu : ContentDialog
{
    private readonly GrpcClientService _grpc;
    private readonly string _pluginName;

    // Store for form values from widgets
    private readonly Dictionary<string, string> _formValues = new();

    public LibraryAddMenu(string pluginName)
    {
        this.InitializeComponent();
        _pluginName = pluginName;
        Title = $"Add to Library ({pluginName})";

        // Resolve services
        _grpc = (Application.Current as App)!.Services.GetRequiredService<GrpcClientService>();

        this.Opened += LibraryAddMenu_Opened;
    }

    private async void LibraryAddMenu_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        await LoadWidgetsAsync();
    }

    private async Task LoadWidgetsAsync()
    {
        LoadingBar.Visibility = Visibility.Visible;
        ContentPanel.Children.Clear();

        try
        {
            var response = await _grpc.Client.GetWidgetsAsync(new WidgetRequest
            {
                PluginName = _pluginName,
                Location = WidgetLocation.LibraryAddMenu
            });

            if (response.Widgets.Count == 0)
            {
                ShowError("This plugin does not provide any options for manual addition.");
                return;
            }

            foreach (var widget in response.Widgets.OrderBy(w => w.SortOrder))
            {
                var renderer = new WidgetRenderer
                {
                    FormValues = _formValues, // Pass shared dictionary first
                    Widget = widget
                };

                renderer.ActionTriggered += Renderer_ActionTriggered;
                ContentPanel.Children.Add(renderer);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load options: {ex.Message}");
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
            string finalPayload = payload;

            // If action is "Submit", treat payload as JSON of form values
            if (payload == "Submit")
            {
                finalPayload = JsonSerializer.Serialize(_formValues);
            }

            var result = await _grpc.Client.TriggerPluginActionAsync(new PluginAction
            {
                PluginName = _pluginName,
                ActionId = actionId,
                PayloadJson = finalPayload
            });

            if (result.Success)
            {
                Hide(); // Close dialog on success
            }
            else
            {
                ShowError(result.Message);
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
