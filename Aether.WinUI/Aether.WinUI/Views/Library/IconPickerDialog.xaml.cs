using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Aether.WinUI.Views.Library;

public sealed partial class IconPickerDialog : ContentDialog
{
    public string SelectedIconGlyph { get; private set; } = "";

    private List<IconItem> _allIcons;
    private readonly ILogger<IconPickerDialog> _logger;

    public IconPickerDialog()
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<IconPickerDialog>>();
        _logger.LogDebug("IconPickerDialog initialized");
        
        // Populate standard Fluent icons
        _allIcons = new List<IconItem>
        {
            new IconItem("Folder", "\uE8B7"),
            new IconItem("Game", "\uE7FC"),
            new IconItem("Heart", "\uEB51"), // HeartFill
            new IconItem("Star", "\uE735"), // StarFill
            new IconItem("Clock", "\uE916"),
            new IconItem("Flame", "\uE73D"), // Like a hot/trend icon
            new IconItem("Lightning", "\uE945"),
            new IconItem("Desktop", "\uE7F8"),
            new IconItem("Laptop", "\uE7F4"),
            new IconItem("Display", "\uE7F3"),
            new IconItem("Keyboard", "\uE765"),
            new IconItem("Mouse", "\uE962"),
            new IconItem("Globe", "\uE774"), // World
            new IconItem("Cloud", "\uE753"),
            new IconItem("Wifi", "\uE701"),
            new IconItem("Person", "\uE77B"),
            new IconItem("People", "\uE716"),
            new IconItem("Home", "\uE80F"),
            new IconItem("Building", "\uE821"),
            new IconItem("Cart", "\uE7BF"),
            new IconItem("Bag", "\uE825"), // Shopping bag
            new IconItem("Wand", "\uE713"), // Settings/Wand
            new IconItem("Crown", "\uE754"), // Education/Ribbon/Award ish
            new IconItem("Trophy", "\uE7C6"),
            new IconItem("Flag", "\uE7C1"),
            new IconItem("Map", "\uE81D"),
            new IconItem("Tag", "\uE8EC"),
            new IconItem("Bookmark", "\uE8A4"),
            new IconItem("Book", "\uE82D")
        };

        IconsGrid.ItemsSource = _allIcons;
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _logger.LogTrace("Icon search changed: {Query}", sender.Text);
        if (string.IsNullOrWhiteSpace(sender.Text))
        {
             IconsGrid.ItemsSource = _allIcons;
        }
        else
        {
            var query = sender.Text.ToLower();
            IconsGrid.ItemsSource = _allIcons.Where(i => i.Name.ToLower().Contains(query)).ToList();
        }
    }

    private void IconsGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        _logger.LogInformation("Icon selected");
        if (e.ClickedItem is IconItem icon)
        {
            SelectedIconGlyph = icon.Glyph;
            this.Hide(); // Close with selection
        }
    }
}

public record IconItem(string Name, string Glyph);
