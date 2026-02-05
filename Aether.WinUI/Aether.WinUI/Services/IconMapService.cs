using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Aether.WinUI.Services;

public sealed class IconMapService
{
    private readonly Dictionary<string, string> _sfToGlyph = new(StringComparer.OrdinalIgnoreCase)
    {
        // Navigation
        ["house.fill"] = "\uE80F", // Home
        ["square.grid.2x2.fill"] = "\uE8A5", // Grid
        ["storefront.fill"] = "\uE719", // Shop
        ["gearshape.fill"] = "\uE713", // Settings

        // Collections / Common
        ["heart.fill"] = "\uEB51",
        ["flame.fill"] = "\uE73D",
        ["folder"] = "\uE8B7",
        ["square.grid.3x3"] = "\uE8A5",
        ["sparkles"] = "\uE74E",
        ["photo.on.rectangle"] = "\uE91B",
        ["arrow.clockwise"] = "\uE72C",
        ["plus"] = "\uE710",
        ["trash"] = "\uE74D",
        ["pencil"] = "\uE70F",
        ["eye"] = "\uE722",
        ["eye.slash"] = "\uE8ED",
        ["star.fill"] = "\uE735",
        ["gamecontroller"] = "\uE7FC"
    };

    private readonly ILogger<IconMapService> _logger;

    public IconMapService(ILogger<IconMapService> logger)
    {
        _logger = logger;
        _logger.LogDebug("IconMapService initialized");
    }

    public string ToGlyph(string iconName, string fallbackGlyph = "\uE8A5")
    {
        _logger.LogTrace("IconMapService.ToGlyph iconName={IconName}", iconName);
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return fallbackGlyph;
        }

        // If the iconName is already a glyph, just return it.
        if (iconName.Length == 1)
        {
            return iconName;
        }

        return _sfToGlyph.TryGetValue(iconName, out var glyph) ? glyph : fallbackGlyph;
    }
}
