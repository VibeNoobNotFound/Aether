using System;
using System.Collections.Generic;

namespace Aether.PluginSDK.UI;

public static class WidgetBuilder
{
    // Text Widgets
    public static Widget Text(string content, TextVariant variant = TextVariant.Body, string? color = null)
    {
        return new Widget(new TextContent
        {
            Text = content,
            Variant = variant,
            Color = color
        });
    }

    public static Widget Header(string content) => Text(content, TextVariant.Headline);

    // Layout Widgets
    public static Widget Container(ContainerOrientation orientation, params Widget[] children)
    {
        return new Widget(new ContainerContent
        {
            Orientation = orientation,
            Children = new List<Widget>(children)
        });
    }

    public static Widget Row(params Widget[] children) => Container(ContainerOrientation.Horizontal, children);

    public static Widget Column(params Widget[] children) => Container(ContainerOrientation.Vertical, children);

    public static Widget Section(string title, string description, params Widget[] children)
    {
        var sectionHeader = Text(title, TextVariant.SectionHeader);
        var descWidget = Text(description, TextVariant.Caption);

        var list = new List<Widget> { sectionHeader, descWidget };
        list.AddRange(children);

        return Column(list.ToArray());
    }

    // Form Inputs
    public static Widget Form(string id, string submitLabel, string submitActionId, params Widget[] fields)
    {
        var container = new ContainerContent
        {
            Orientation = ContainerOrientation.Form,
            Children = new List<Widget>(fields),
            Actions = new List<WidgetAction>
            {
                new WidgetAction { Id = submitActionId, Label = submitLabel, Type = "Submit" }
            }
        };
        return new Widget(container) { Id = id };
    }

    public static Widget TextInput(string id, string label, string? placeholder = null, bool required = false, bool secure = false, string initialValue = "")
    {
        return new Widget(new TextInputContent
        {
            BoundFieldId = id,
            Label = label,
            Placeholder = placeholder,
            IsRequired = required,
            IsSecure = secure,
            InitialValue = initialValue
        });
    }

    public static Widget FolderPicker(string id, string label, bool required = true)
    {
        return new Widget(new FolderPickerContent
        {
            BoundFieldId = id,
            Label = label,
            IsRequired = required
        });
    }

    public static Widget FilePicker(string id, string label, bool required = false, string allowedExtensions = "")
    {
        return new Widget(new FilePickerContent
        {
            BoundFieldId = id,
            Label = label,
            IsRequired = required,
            AllowedExtensions = allowedExtensions
        });
    }

    public static Widget Toggle(string id, string label, bool initialValue = false)
    {
        return new Widget(new ToggleContent
        {
            BoundFieldId = id,
            Label = label,
            InitialValue = initialValue
        });
    }

    // Actions
    public static Widget Button(string label, string actionId, ButtonStyle style = ButtonStyle.Default, string? icon = null)
    {
        return new Widget(new ButtonContent
        {
            Label = label,
            ActionId = actionId,
            Style = style,
            Icon = icon
        });
    }

    public static Widget PrimaryButton(string label, string actionId) => Button(label, actionId, ButtonStyle.Primary);
}
