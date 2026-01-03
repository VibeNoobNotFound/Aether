using System;
using System.Collections.Generic;

namespace Aether.PluginSDK.UI;

public class Widget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public WidgetStyle Style { get; set; } = new();
    public int SortOrder { get; set; }
    public IWidgetContent Content { get; set; }

    public Widget(IWidgetContent content)
    {
        Content = content;
    }
}

public class WidgetStyle
{
    public string? BackgroundColor { get; set; }
    public double? PaddingVertical { get; set; }
    public double? PaddingHorizontal { get; set; }
}

public interface IWidgetContent { }

public enum TextVariant { Body, Headline, Caption, SectionHeader, Error }

public class TextContent : IWidgetContent
{
    public string Text { get; set; } = "";
    public TextVariant Variant { get; set; } = TextVariant.Body;
    public string? Color { get; set; }
}

public enum ButtonStyle { Default, Primary, Destructive, Link }

public class ButtonContent : IWidgetContent
{
    public string Label { get; set; } = "";
    public string? Icon { get; set; }
    public string ActionId { get; set; } = "";
    public string? PayloadJson { get; set; }
    public ButtonStyle Style { get; set; } = ButtonStyle.Default;
}

public enum ImageSize { Small, Medium, Large, FullWidth }

public class ImageContent : IWidgetContent
{
    public string Url { get; set; } = "";
    public ImageSize Size { get; set; } = ImageSize.Medium;
    public string? Caption { get; set; }
}

public class TextInputContent : IWidgetContent
{
    public string Label { get; set; } = "";
    public string? Placeholder { get; set; }
    public string? InitialValue { get; set; }
    public string BoundFieldId { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool IsSecure { get; set; }
}

public class FolderPickerContent : IWidgetContent
{
    public string Label { get; set; } = "";
    public string BoundFieldId { get; set; } = "";
    public bool IsRequired { get; set; }
}

public class FilePickerContent : IWidgetContent
{
    public string Label { get; set; } = "";
    public string BoundFieldId { get; set; } = "";
    public bool IsRequired { get; set; }
    public string AllowedExtensions { get; set; } = ""; // .exe,.sh
}

public class ToggleContent : IWidgetContent
{
    public string Label { get; set; } = "";
    public string BoundFieldId { get; set; } = "";
    public bool InitialValue { get; set; }
}

public enum ContainerOrientation { Vertical, Horizontal, Form }

public class ContainerContent : IWidgetContent
{
    public ContainerOrientation Orientation { get; set; } = ContainerOrientation.Vertical;
    public List<Widget> Children { get; set; } = new();
    public List<WidgetAction> Actions { get; set; } = new();
}

public class WidgetAction
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "Submit"; // Submit, Cancel
}
