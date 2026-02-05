using global::Aether.Protos;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using Aether.WinUI.Services;
using Aether.WinUI.AttachedProperties;
using CommunityToolkit.WinUI.Helpers;

namespace Aether.WinUI.Controls.Renderer;

public sealed partial class WidgetRenderer : UserControl
{
    public static readonly DependencyProperty WidgetProperty =
        DependencyProperty.Register(nameof(Widget), typeof(UIWidget), typeof(WidgetRenderer), new PropertyMetadata(null, OnWidgetChanged));

    public UIWidget? Widget
    {
        get => (UIWidget?)GetValue(WidgetProperty);
        set => SetValue(WidgetProperty, value);
    }

    public static readonly DependencyProperty FormValuesProperty =
        DependencyProperty.Register(nameof(FormValues), typeof(Dictionary<string, string>), typeof(WidgetRenderer), new PropertyMetadata(null));

    public Dictionary<string, string> FormValues
    {
        get => (Dictionary<string, string>)GetValue(FormValuesProperty);
        set => SetValue(FormValuesProperty, value);
    }

    // Event for actions
    public event Action<string, string>? ActionTriggered;

    public WidgetRenderer()
    {
        this.InitializeComponent();
        if (FormValues == null)
        {
            FormValues = new Dictionary<string, string>();
        }
    }

    private static void OnWidgetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WidgetRenderer renderer && e.NewValue is UIWidget widget)
        {
            renderer.RenderWidget(widget);
        }
    }

    private void RenderWidget(UIWidget widget)
    {
        var content = widget.ContentCase switch
        {
            UIWidget.ContentOneofCase.Text => RenderText(widget.Text),
            UIWidget.ContentOneofCase.Button => RenderButton(widget.Button),
            UIWidget.ContentOneofCase.TextInput => RenderTextInput(widget.TextInput),
            UIWidget.ContentOneofCase.FolderPicker => RenderFolderPicker(widget.FolderPicker),
            UIWidget.ContentOneofCase.FilePicker => RenderFilePicker(widget.FilePicker),
            UIWidget.ContentOneofCase.Toggle => RenderToggle(widget.Toggle),
            UIWidget.ContentOneofCase.Container => RenderContainer(widget.Container),
            UIWidget.ContentOneofCase.Image => RenderImage(widget.Image),
            _ => new TextBlock { Text = $"Unsupported Widget: {widget.ContentCase}", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) }
        };

        Content = ApplyWidgetStyle(content, widget);
    }

    private FrameworkElement RenderFolderPicker(FolderPickerWidget picker)
    {
        var stack = new StackPanel { Spacing = 8 };
        if (!string.IsNullOrEmpty(picker.Label))
        {
            stack.Children.Add(new TextBlock { Text = picker.Label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        }

        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 8 };

        var pathBox = new TextBox { IsReadOnly = true, PlaceholderText = "No folder selected" };
        Grid.SetColumn(pathBox, 0);

        var btn = new Button { Content = "Browse..." };
        Grid.SetColumn(btn, 1);

        btn.Click += async (s, e) =>
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            // WinUI 3 Window Handle (HWND) hack
            var window = (Application.Current as App)?.MainWindow; // Ensure App.xaml.cs exposes MainWindow
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);
            }

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                pathBox.Text = folder.Path;
                UpdateFormValue(picker.BoundFieldId, folder.Path);
            }
        };

        grid.Children.Add(pathBox);
        grid.Children.Add(btn);
        stack.Children.Add(grid);

        return stack;
    }

    private FrameworkElement RenderFilePicker(FilePickerWidget picker)
    {
        var stack = new StackPanel { Spacing = 8 };
        if (!string.IsNullOrEmpty(picker.Label))
        {
            stack.Children.Add(new TextBlock { Text = picker.Label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        }

        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 8 };

        var pathBox = new TextBox { IsReadOnly = true, PlaceholderText = "No file selected" };
        Grid.SetColumn(pathBox, 0);

        var btn = new Button { Content = "Browse..." };
        Grid.SetColumn(btn, 1);

        btn.Click += async (s, e) =>
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;

            if (!string.IsNullOrEmpty(picker.AllowedExtensions))
            {
                foreach (var ext in picker.AllowedExtensions.Split(','))
                {
                    filePicker.FileTypeFilter.Add(ext.Trim());
                }
            }
            else
            {
                filePicker.FileTypeFilter.Add("*");
            }

            // WinUI 3 Window Handle (HWND) hack
            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hWnd);
            }

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                pathBox.Text = file.Path;
                UpdateFormValue(picker.BoundFieldId, file.Path);
            }
        };

        grid.Children.Add(pathBox);
        grid.Children.Add(btn);
        stack.Children.Add(grid);

        return stack;
    }

    private FrameworkElement RenderContainer(ContainerWidget container)
    {
        var panel = new StackPanel
        {
            Orientation = container.Orientation == ContainerWidget.Types.Orientation.Horizontal
                ? Orientation.Horizontal
                : Orientation.Vertical,
            Spacing = 12
        };

        foreach (var child in container.Children)
        {
            var renderer = new WidgetRenderer
            {
                FormValues = this.FormValues, // Pass reference to same dictionary first
                Widget = child
            };
            renderer.ActionTriggered += (id, payload) => ActionTriggered?.Invoke(id, payload);
            panel.Children.Add(renderer);
        }

        // Render Actions (Submit, Cancel, etc.) defined on the container
        if (container.Actions.Count > 0)
        {
            var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            foreach (var action in container.Actions)
            {
                var btn = new Button { Content = action.Label };
                if (action.Type == "Submit")
                {
                    btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                }

                btn.Click += (s, e) => ActionTriggered?.Invoke(action.Id, action.Type); // Map ID/Type to action
                actionPanel.Children.Add(btn);

                // Special case: If this is a form container, the submit button might need to trigger validation.
                // For now, we just pass the event up.
            }
            panel.Children.Add(actionPanel);
        }

        return panel;
    }

    private FrameworkElement RenderText(TextWidget text)
    {
        var block = new TextBlock
        {
            Text = text.Text,
            TextWrapping = TextWrapping.Wrap
        };

        // Apply styles based on variant
        switch (text.Variant)
        {
            case TextWidget.Types.Variant.Headline:
                block.Style = (Style)Application.Current.Resources["TitleTextBlockStyle"];
                break;
            case TextWidget.Types.Variant.SectionHeader:
                block.Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"];
                break;
            case TextWidget.Types.Variant.Caption:
                block.Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"];
                block.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                break;
            case TextWidget.Types.Variant.Error:
                block.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                break;
            default:
                block.Style = (Style)Application.Current.Resources["BodyTextBlockStyle"];
                break;
        }

        var colorBrush = ResolveTextBrush(text.Color);
        if (colorBrush != null)
        {
            block.Foreground = colorBrush;
        }

        return block;
    }

    private FrameworkElement RenderButton(ButtonWidget button)
    {
        var iconGlyph = ResolveButtonIcon(button.Icon);
        var contentPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        if (!string.IsNullOrWhiteSpace(iconGlyph))
        {
            contentPanel.Children.Add(new FontIcon { Glyph = iconGlyph });
        }
        contentPanel.Children.Add(new TextBlock { Text = button.Label });

        if (button.Style == ButtonWidget.Types.Style.Link)
        {
            var link = new HyperlinkButton
            {
                Content = contentPanel,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            link.Click += (s, e) => ActionTriggered?.Invoke(button.ActionId, button.PayloadJson);
            return link;
        }

        var btn = new Button
        {
            Content = contentPanel,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        switch (button.Style)
        {
            case ButtonWidget.Types.Style.Primary:
                btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                break;
            case ButtonWidget.Types.Style.Destructive:
                btn.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                btn.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                break;
        }

        btn.Click += (s, e) => ActionTriggered?.Invoke(button.ActionId, button.PayloadJson);

        return btn;
    }

    private FrameworkElement RenderTextInput(TextInputWidget input)
    {
        var stack = new StackPanel { Spacing = 8 };

        if (!string.IsNullOrEmpty(input.Label))
        {
            stack.Children.Add(new TextBlock { Text = input.Label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        }

        Control box;
        if (input.IsSecure)
        {
            var pb = new PasswordBox { PlaceholderText = input.Placeholder };
            pb.PasswordChanged += (s, e) => UpdateFormValue(input.BoundFieldId, pb.Password);
            box = pb;

            if (!string.IsNullOrEmpty(input.InitialValue)) pb.Password = input.InitialValue;
        }
        else
        {
            var tb = new TextBox { PlaceholderText = input.Placeholder };
            tb.TextChanged += (s, e) => UpdateFormValue(input.BoundFieldId, tb.Text);
            box = tb;

            if (!string.IsNullOrEmpty(input.InitialValue)) tb.Text = input.InitialValue;
        }

        stack.Children.Add(box);
        return stack;
    }

    private FrameworkElement RenderToggle(ToggleWidget toggle)
    {
        var current = FormValues.TryGetValue(toggle.BoundFieldId, out var value)
            ? value
            : toggle.InitialValue.ToString().ToLowerInvariant();

        var control = new ToggleSwitch
        {
            Header = toggle.Label,
            IsOn = string.Equals(current, "true", StringComparison.OrdinalIgnoreCase)
        };

        control.Toggled += (s, e) => UpdateFormValue(toggle.BoundFieldId, control.IsOn.ToString().ToLowerInvariant());
        return control;
    }

    private FrameworkElement RenderImage(ImageWidget imageWidget)
    {
        var image = new Image { Stretch = Stretch.UniformToFill };
        if (!string.IsNullOrWhiteSpace(imageWidget.Url))
        {
            ImageCache.SetSource(image, imageWidget.Url);
        }

        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Child = image
        };

        var height = imageWidget.Size switch
        {
            ImageWidget.Types.Size.Small => 90,
            ImageWidget.Types.Size.Medium => 140,
            ImageWidget.Types.Size.Large => 220,
            ImageWidget.Types.Size.FullWidth => 260,
            _ => 140
        };

        border.Height = height;
        border.HorizontalAlignment = imageWidget.Size == ImageWidget.Types.Size.FullWidth
            ? HorizontalAlignment.Stretch
            : HorizontalAlignment.Left;

        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(border);

        if (!string.IsNullOrWhiteSpace(imageWidget.Caption))
        {
            stack.Children.Add(new TextBlock
            {
                Text = imageWidget.Caption,
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
        }

        return stack;
    }

    private FrameworkElement ApplyWidgetStyle(FrameworkElement content, UIWidget widget)
    {
        var style = widget.Style;
        if (style == null)
        {
            return content;
        }

        var hasPadding = style.HasPaddingVertical || style.HasPaddingHorizontal;
        var hasBackground = style.HasBackgroundColor && !string.IsNullOrWhiteSpace(style.BackgroundColor);

        if (!hasPadding && !hasBackground)
        {
            return content;
        }

        var border = new Border
        {
            Child = content,
            Padding = new Thickness(
                style.HasPaddingHorizontal ? style.PaddingHorizontal : 0,
                style.HasPaddingVertical ? style.PaddingVertical : 0,
                style.HasPaddingHorizontal ? style.PaddingHorizontal : 0,
                style.HasPaddingVertical ? style.PaddingVertical : 0)
        };

        if (hasBackground)
        {
            border.Background = new SolidColorBrush(style.BackgroundColor.ToColor());
            border.CornerRadius = new CornerRadius(10);
        }

        return border;
    }

    private Brush? ResolveTextBrush(string colorToken)
    {
        if (string.IsNullOrWhiteSpace(colorToken))
        {
            return null;
        }

        switch (colorToken.Trim().ToLowerInvariant())
        {
            case "primary":
                return (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            case "secondary":
                return (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            case "accent":
                return (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];
        }

        if (colorToken.StartsWith("#", StringComparison.Ordinal))
        {
            return new SolidColorBrush(colorToken.ToColor());
        }

        return null;
    }

    private string? ResolveButtonIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        var iconMap = (Application.Current as App)?.Services.GetService(typeof(IconMapService)) as IconMapService;
        return iconMap?.ToGlyph(iconName);
    }

    private void UpdateFormValue(string key, string value)
    {
        if (FormValues != null && !string.IsNullOrEmpty(key))
        {
            FormValues[key] = value;
        }
    }
}
