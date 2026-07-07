using System.Collections.Generic;
using static BibleFamilyTreeBuilder.App.Theming.ThemePalette;

namespace BibleFamilyTreeBuilder.App.Theming;

/// <summary>
/// The three built-in themes: Parchment (the refined original look), Light, and Dark.
/// </summary>
public static class ThemePresets
{
    public const string Parchment = "Parchment";
    public const string Light = "Light";
    public const string Dark = "Dark";
    public const string Custom = "Custom";

    public static IReadOnlyList<AppTheme> BuiltIn { get; } =
    [
        CreateParchment(),
        CreateLight(),
        CreateDark(),
    ];

    public static AppTheme Default => BuiltIn[0];

    public static AppTheme? FindBuiltIn(string? name)
    {
        foreach (var theme in BuiltIn)
        {
            if (string.Equals(theme.Name, name, System.StringComparison.OrdinalIgnoreCase))
            {
                return theme;
            }
        }

        return null;
    }

    private static AppTheme CreateParchment()
    {
        return new AppTheme
        {
            Name = Parchment,
            IsDark = false,
            IsBuiltIn = true,
            Palette = new ThemePalette
            {
                WindowBackground = Hex("#EEEAE0"),
                PanelBackground = Hex("#FAF8F2"),
                PanelBorder = Hex("#D4CEC0"),
                ToolBarBackground = Hex("#F4F0E6"),
                MenuBackground = Hex("#F4F0E6"),
                InsetBackground = Hex("#F0ECE2"),
                ControlBackground = Hex("#FFFDF8"),
                ControlBorder = Hex("#CBC3B2"),
                TextPrimary = Hex("#37322B"),
                TextSecondary = Hex("#6A6458"),
                Accent = Hex("#9C6B3F"),
                AccentText = Hex("#FFFFFF"),

                CanvasBackground = Hex("#F5F1E8"),
                BandEven = Hex("#F4EFE5"),
                BandOdd = Hex("#FAF7F0"),
                BandBorder = Hex("#E2DACD"),
                GenLabelBackground = Hex("#EAE4D8"),
                GenLabelBorder = Hex("#CBC1B1"),
                GenLabelText = Hex("#4F493F"),

                CardDefaultFill = Hex("#90D995"),
                CardDefaultBorder = Hex("#4D9C5B"),
                CardJesusFill = Hex("#FFE071"),
                CardJesusBorder = Hex("#B8860B"),
                CardUnknownFill = Hex("#D8E2E5"),
                CardUnknownBorder = Hex("#697B84"),
                CardUnknownDescFill = Hex("#7DB9FF"),
                CardUnknownDescBorder = Hex("#2E72CD"),
                CardGroupedFill = Hex("#C4A9E1"),
                CardGroupedBorder = Hex("#6D4696"),
                CardTitleText = Hex("#22201C"),
                CardSubtitleText = Hex("#302D28"),
                SelectedCardBorder = Hex("#211F1D"),

                MarriageLine = Hex("#D43168"),
                ParentChildLine = Hex("#676660"),
                AdoptedLegalLine = Hex("#434774"),
                RelLabelBackground = Hex("#FFFDF7"),
                RelLabelBorder = Hex("#979284"),
                RelLabelText = Hex("#443D34"),
            },
        };
    }

    private static AppTheme CreateLight()
    {
        return new AppTheme
        {
            Name = Light,
            IsDark = false,
            IsBuiltIn = true,
            Palette = new ThemePalette
            {
                WindowBackground = Hex("#F4F5F7"),
                PanelBackground = Hex("#FFFFFF"),
                PanelBorder = Hex("#E2E5EA"),
                ToolBarBackground = Hex("#FFFFFF"),
                MenuBackground = Hex("#FFFFFF"),
                InsetBackground = Hex("#F2F4F7"),
                ControlBackground = Hex("#FFFFFF"),
                ControlBorder = Hex("#CDD3DB"),
                TextPrimary = Hex("#1F2733"),
                TextSecondary = Hex("#5B6472"),
                Accent = Hex("#2F6FEB"),
                AccentText = Hex("#FFFFFF"),

                CanvasBackground = Hex("#FBFCFE"),
                BandEven = Hex("#F1F4F9"),
                BandOdd = Hex("#FAFBFD"),
                BandBorder = Hex("#E1E6EE"),
                GenLabelBackground = Hex("#E8EDF5"),
                GenLabelBorder = Hex("#CBD4E0"),
                GenLabelText = Hex("#364152"),

                CardDefaultFill = Hex("#86D98F"),
                CardDefaultBorder = Hex("#3E9E55"),
                CardJesusFill = Hex("#FFD866"),
                CardJesusBorder = Hex("#C79A17"),
                CardUnknownFill = Hex("#DBE2E8"),
                CardUnknownBorder = Hex("#8593A0"),
                CardUnknownDescFill = Hex("#86BCFF"),
                CardUnknownDescBorder = Hex("#2E72CD"),
                CardGroupedFill = Hex("#C9AEE8"),
                CardGroupedBorder = Hex("#7A52B5"),
                CardTitleText = Hex("#1B222C"),
                CardSubtitleText = Hex("#33404F"),
                SelectedCardBorder = Hex("#1F2733"),

                MarriageLine = Hex("#E23A78"),
                ParentChildLine = Hex("#7A8798"),
                AdoptedLegalLine = Hex("#4B58C0"),
                RelLabelBackground = Hex("#FFFFFF"),
                RelLabelBorder = Hex("#C6CCD6"),
                RelLabelText = Hex("#2B3542"),
            },
        };
    }

    private static AppTheme CreateDark()
    {
        return new AppTheme
        {
            Name = Dark,
            IsDark = true,
            IsBuiltIn = true,
            Palette = new ThemePalette
            {
                WindowBackground = Hex("#1E2127"),
                PanelBackground = Hex("#262A31"),
                PanelBorder = Hex("#363C45"),
                ToolBarBackground = Hex("#22262C"),
                MenuBackground = Hex("#22262C"),
                InsetBackground = Hex("#2C313A"),
                ControlBackground = Hex("#2E333C"),
                ControlBorder = Hex("#454C57"),
                TextPrimary = Hex("#E7EAEE"),
                TextSecondary = Hex("#A2AAB5"),
                Accent = Hex("#4C8DFF"),
                AccentText = Hex("#0E1116"),

                CanvasBackground = Hex("#191C21"),
                BandEven = Hex("#22262D"),
                BandOdd = Hex("#1D2127"),
                BandBorder = Hex("#333842"),
                GenLabelBackground = Hex("#2C323B"),
                GenLabelBorder = Hex("#3E4550"),
                GenLabelText = Hex("#C7CED8"),

                CardDefaultFill = Hex("#3E7D4B"),
                CardDefaultBorder = Hex("#6FBF7E"),
                CardJesusFill = Hex("#C9A63E"),
                CardJesusBorder = Hex("#E8CE6A"),
                CardUnknownFill = Hex("#495663"),
                CardUnknownBorder = Hex("#6E7E8D"),
                CardUnknownDescFill = Hex("#3B6DA8"),
                CardUnknownDescBorder = Hex("#74A7E0"),
                CardGroupedFill = Hex("#6B4E96"),
                CardGroupedBorder = Hex("#A98BD6"),
                CardTitleText = Hex("#F2F4F7"),
                CardSubtitleText = Hex("#D5DAE1"),
                SelectedCardBorder = Hex("#FFFFFF"),

                MarriageLine = Hex("#FF5C93"),
                ParentChildLine = Hex("#8A93A0"),
                AdoptedLegalLine = Hex("#8493E6"),
                RelLabelBackground = Hex("#2C313A"),
                RelLabelBorder = Hex("#4A515C"),
                RelLabelText = Hex("#E1E5EA"),
            },
        };
    }
}
