using global::Aether.Protos;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

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
        Content = widget.ContentCase switch
        {
            UIWidget.ContentOneofCase.Text => RenderText(widget.Text),
            UIWidget.ContentOneofCase.Button => RenderButton(widget.Button),
            UIWidget.ContentOneofCase.TextInput => RenderTextInput(widget.TextInput),
            UIWidget.ContentOneofCase.FolderPicker => RenderFolderPicker(widget.FolderPicker),
            UIWidget.ContentOneofCase.FilePicker => RenderFilePicker(widget.FilePicker),
            UIWidget.ContentOneofCase.Container => RenderContainer(widget.Container),
            // TODO: Implement other widgets
            _ => new TextBlock { Text = $"Unsupported Widget: {widget.ContentCase}", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) }
        };
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

        return block;
    }

    private FrameworkElement RenderButton(ButtonWidget button)
    {
        var btn = new Button
        {
            Content = button.Label,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (button.Style == ButtonWidget.Types.Style.Primary)
        {
            btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        }

        btn.Click += (s, e) =>
        {
            ActionTriggered?.Invoke(button.ActionId, button.PayloadJson);
        };

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

    private void UpdateFormValue(string key, string value)
    {
        if (FormValues != null && !string.IsNullOrEmpty(key))
        {
            FormValues[key] = value;
        }
    }
}
