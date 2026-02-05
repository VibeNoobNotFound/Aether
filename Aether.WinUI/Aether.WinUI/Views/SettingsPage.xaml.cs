using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
using Aether.WinUI.Views.Settings;
using Aether.Protos;
using Aether.WinUI.Services;
using Windows.System;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Aether.WinUI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<SettingsViewModel>();
    private GrpcClientService Grpc => (Application.Current as App)!.Services.GetRequiredService<GrpcClientService>();

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    private async void OpenMetadataSources_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new MetadataSourcesDialog();
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void AddPlugin_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".dll");
        picker.FileTypeFilter.Add(".zip");
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)!.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        try
        {
            var buffer = await Windows.Storage.FileIO.ReadBufferAsync(file);
            var data = buffer.ToArray();

            var response = await Grpc.Client.InstallPluginAsync(new PluginFile
            {
                Filename = file.Name,
                Data = Google.Protobuf.ByteString.CopyFrom(data)
            });

            if (response.Success)
            {
                await ViewModel.LoadPlugins();
            }
            else
            {
                await ShowMessageAsync("Plugin Install Failed", response.Message);
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Plugin Install Failed", ex.Message);
        }
    }

    private async void PluginList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Models.PluginViewModel plugin)
        {
            var dialog = new PluginSetupDialog(plugin.Name);
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();
        }
    }

    private async void FactoryReset_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            Title = "Factory Reset",
            Content = "This will clear your library, reset settings, and restart onboarding. This action cannot be undone.",
            PrimaryButtonText = "Reset Everything",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            await ViewModel.FactoryResetAsync();
            await ShowMessageAsync("Reset Complete", "Aether will now close. Please relaunch the app.");
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Reset Failed", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
