namespace BibleFamilyTreeBuilder.App.Theming;

/// <summary>
/// A named theme: a friendly name plus the full <see cref="ThemePalette"/> it applies.
/// </summary>
public class AppTheme
{
    public required string Name { get; init; }
    public bool IsDark { get; init; }
    public bool IsBuiltIn { get; init; }
    public required ThemePalette Palette { get; init; }
}
