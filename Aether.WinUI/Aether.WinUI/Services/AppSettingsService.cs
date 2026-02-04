using System;
using Windows.Storage;

namespace Aether.WinUI.Services;

public sealed class AppSettingsService
{
    private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

    private const string UseTopNavigationKey = "useTopNavigation";
    private const string UseLiquidGlassCardsKey = "useLiquidGlassCards";
    private const string IncludeBetaUpdatesKey = "includeBetaUpdates";
    private const string AutoUpdateEnabledKey = "automaticallyCheckForUpdates";
    private const string HasCompletedOnboardingKey = "hasCompletedOnboarding";
    private const string SelectedThemeIndexKey = "selectedThemeIndex";

    public bool UseTopNavigation
    {
        get => ReadBool(UseTopNavigationKey, false);
        set => WriteBool(UseTopNavigationKey, value);
    }

    public bool UseLiquidGlassCards
    {
        get => ReadBool(UseLiquidGlassCardsKey, true);
        set => WriteBool(UseLiquidGlassCardsKey, value);
    }

    public bool IncludeBetaUpdates
    {
        get => ReadBool(IncludeBetaUpdatesKey, false);
        set => WriteBool(IncludeBetaUpdatesKey, value);
    }

    public bool AutoUpdateEnabled
    {
        get => ReadBool(AutoUpdateEnabledKey, true);
        set => WriteBool(AutoUpdateEnabledKey, value);
    }

    public bool HasCompletedOnboarding
    {
        get => ReadBool(HasCompletedOnboardingKey, false);
        set => WriteBool(HasCompletedOnboardingKey, value);
    }

    public int SelectedThemeIndex
    {
        get => ReadInt(SelectedThemeIndexKey, 0);
        set => WriteInt(SelectedThemeIndexKey, value);
    }

    public void ClearAll()
    {
        _settings.Values.Clear();
    }

    private bool ReadBool(string key, bool fallback)
    {
        if (_settings.Values.TryGetValue(key, out var value) && value is bool b)
        {
            return b;
        }
        return fallback;
    }

    private void WriteBool(string key, bool value)
    {
        _settings.Values[key] = value;
    }

    private int ReadInt(string key, int fallback)
    {
        if (_settings.Values.TryGetValue(key, out var value) && value is int i)
        {
            return i;
        }
        return fallback;
    }

    private void WriteInt(string key, int value)
    {
        _settings.Values[key] = value;
    }
}
