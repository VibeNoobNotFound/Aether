using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls;

public sealed partial class UpdateStatusPill : UserControl
{
    public MainViewModel ViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    private readonly ILogger<UpdateStatusPill> _logger;

    public UpdateStatusPill()
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<UpdateStatusPill>>();
        _logger.LogDebug("UpdateStatusPill initialized");
    }
}
