using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using BibleFamilyTreeBuilder.App.Services;

namespace BibleFamilyTreeBuilder.App.Theming;

/// <summary>
/// Central theme service. Applies a <see cref="ThemePalette"/> by writing brushes into
/// <c>Application.Current.Resources</c> (so all {DynamicResource} references update live) and
/// exposes <see cref="Current"/> for code-behind (the tree canvas) to read colors directly.
/// </summary>
public static class ThemeManager
{
    private static readonly SettingsService SettingsService = new();
    private static AppSettings _settings = new();

    public static IReadOnlyList<AppTheme> BuiltInThemes => ThemePresets.BuiltIn;

    public static AppTheme Current { get; private set; } = ThemePresets.Default;

    /// <summary>Raised after a new theme is applied so listeners (e.g. the canvas) can repaint.</summary>
    public static event Action? ThemeChanged;

    /// <summary>Loads saved settings and applies the saved theme. Call once at startup.</summary>
    public static void Initialize()
    {
        _settings = SettingsService.Load();

        AppTheme theme;
        if (string.Equals(_settings.SelectedThemeName, ThemePresets.Custom, StringComparison.OrdinalIgnoreCase) &&
            _settings.CustomPalette is not null)
        {
            theme = BuildCustomTheme(_settings.CustomPalette);
        }
        else
        {
            theme = ThemePresets.FindBuiltIn(_settings.SelectedThemeName) ?? ThemePresets.Default;
        }

        Apply(theme, persist: false);
    }

    /// <summary>Applies a built-in theme by name and persists the choice.</summary>
    public static void ApplyByName(string name)
    {
        var theme = ThemePresets.FindBuiltIn(name);
        if (theme is not null)
        {
            Apply(theme, persist: true);
        }
    }

    /// <summary>Applies a palette live without saving, for the customize dialog's preview.</summary>
    public static void PreviewCustom(ThemePalette palette)
    {
        Apply(BuildCustomThemeFromPalette(palette), persist: false);
    }

    /// <summary>Applies and persists a custom palette as the "Custom" theme.</summary>
    public static void SaveCustom(ThemePalette palette)
    {
        Apply(BuildCustomThemeFromPalette(palette), persist: true);
    }

    /// <summary>The palette the customize dialog should open from: the saved custom one, or the current.</summary>
    public static ThemePalette GetEditableStartingPalette()
    {
        if (_settings.CustomPalette is not null)
        {
            return BuildCustomTheme(_settings.CustomPalette).Palette.Clone();
        }

        return Current.Palette.Clone();
    }

    public static void Apply(AppTheme theme, bool persist = true)
    {
        Current = theme;

        var resources = Application.Current?.Resources;
        if (resources is not null)
        {
            foreach (var (key, color) in theme.Palette.ToResourceMap())
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                resources[key] = brush;
            }
        }

        if (persist)
        {
            _settings.SelectedThemeName = theme.Name;
            if (string.Equals(theme.Name, ThemePresets.Custom, StringComparison.OrdinalIgnoreCase))
            {
                _settings.CustomPalette = theme.Palette.ToHexMap();
            }

            SettingsService.Save(_settings);
        }

        ThemeChanged?.Invoke();
    }

    private static AppTheme BuildCustomTheme(IReadOnlyDictionary<string, string> map)
    {
        var palette = ThemePresets.Default.Palette.Clone();
        palette.ApplyHexMap(map);
        return BuildCustomThemeFromPalette(palette);
    }

    private static AppTheme BuildCustomThemeFromPalette(ThemePalette palette)
    {
        return new AppTheme
        {
            Name = ThemePresets.Custom,
            IsDark = false,
            IsBuiltIn = false,
            Palette = palette,
        };
    }
}
