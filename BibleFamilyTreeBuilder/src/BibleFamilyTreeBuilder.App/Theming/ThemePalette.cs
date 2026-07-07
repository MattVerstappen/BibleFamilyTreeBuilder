using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;

namespace BibleFamilyTreeBuilder.App.Theming;

/// <summary>
/// A complete set of colors for one theme. Every themeable color in the app lives here.
/// XAML consumes these via the brush resource keys produced by <see cref="ToResourceMap"/>;
/// the tree canvas code-behind reads the <see cref="Color"/> properties directly.
/// </summary>
public class ThemePalette
{
    // Chrome (window, panels, toolbar, menu, inputs, text, accent)
    public Color WindowBackground { get; set; }
    public Color PanelBackground { get; set; }
    public Color PanelBorder { get; set; }
    public Color ToolBarBackground { get; set; }
    public Color MenuBackground { get; set; }
    public Color InsetBackground { get; set; }
    public Color ControlBackground { get; set; }
    public Color ControlBorder { get; set; }
    public Color TextPrimary { get; set; }
    public Color TextSecondary { get; set; }
    public Color Accent { get; set; }
    public Color AccentText { get; set; }

    // Canvas (background + generation bands + generation labels)
    public Color CanvasBackground { get; set; }
    public Color BandEven { get; set; }
    public Color BandOdd { get; set; }
    public Color BandBorder { get; set; }
    public Color GenLabelBackground { get; set; }
    public Color GenLabelBorder { get; set; }
    public Color GenLabelText { get; set; }

    // Person cards (fill + border per CardType) and card text
    public Color CardDefaultFill { get; set; }
    public Color CardDefaultBorder { get; set; }
    public Color CardJesusFill { get; set; }
    public Color CardJesusBorder { get; set; }
    public Color CardUnknownFill { get; set; }
    public Color CardUnknownBorder { get; set; }
    public Color CardUnknownDescFill { get; set; }
    public Color CardUnknownDescBorder { get; set; }
    public Color CardGroupedFill { get; set; }
    public Color CardGroupedBorder { get; set; }
    public Color CardTitleText { get; set; }
    public Color CardSubtitleText { get; set; }
    public Color SelectedCardBorder { get; set; }

    // Relationships (lines + relationship labels)
    public Color MarriageLine { get; set; }
    public Color ParentChildLine { get; set; }
    public Color AdoptedLegalLine { get; set; }
    public Color RelLabelBackground { get; set; }
    public Color RelLabelBorder { get; set; }
    public Color RelLabelText { get; set; }

    /// <summary>
    /// Maps every color to the resource key XAML uses via {DynamicResource Key}.
    /// Keep the keys here in sync with the keys referenced in Styles.xaml and the windows.
    /// </summary>
    public IReadOnlyDictionary<string, Color> ToResourceMap()
    {
        return new Dictionary<string, Color>
        {
            ["Brush.Window"] = WindowBackground,
            ["Brush.Panel"] = PanelBackground,
            ["Brush.PanelBorder"] = PanelBorder,
            ["Brush.ToolBar"] = ToolBarBackground,
            ["Brush.Menu"] = MenuBackground,
            ["Brush.Inset"] = InsetBackground,
            ["Brush.Control"] = ControlBackground,
            ["Brush.ControlBorder"] = ControlBorder,
            ["Brush.TextPrimary"] = TextPrimary,
            ["Brush.TextSecondary"] = TextSecondary,
            ["Brush.Accent"] = Accent,
            ["Brush.AccentText"] = AccentText,
            ["Brush.Canvas"] = CanvasBackground,
            ["Brush.GenLabel"] = GenLabelBackground,
            ["Brush.GenLabelBorder"] = GenLabelBorder,
            ["Brush.GenLabelText"] = GenLabelText,
            ["Brush.CardDefaultFill"] = CardDefaultFill,
            ["Brush.CardDefaultBorder"] = CardDefaultBorder,
            ["Brush.CardJesusFill"] = CardJesusFill,
            ["Brush.CardJesusBorder"] = CardJesusBorder,
            ["Brush.CardUnknownDescFill"] = CardUnknownDescFill,
            ["Brush.CardUnknownDescBorder"] = CardUnknownDescBorder,
            ["Brush.MarriageLine"] = MarriageLine,
            ["Brush.AdoptedLegalLine"] = AdoptedLegalLine,
        };
    }

    public ThemePalette Clone()
    {
        return (ThemePalette)MemberwiseClone();
    }

    /// <summary>Serializes every color property to a name -&gt; "#RRGGBB" map for saving.</summary>
    public Dictionary<string, string> ToHexMap()
    {
        var map = new Dictionary<string, string>();
        foreach (var property in typeof(ThemePalette).GetProperties())
        {
            if (property.PropertyType == typeof(Color) && property.CanRead)
            {
                map[property.Name] = ToHex((Color)property.GetValue(this)!);
            }
        }

        return map;
    }

    /// <summary>Overwrites color properties from a saved name -&gt; hex map. Unknown/bad keys are ignored.</summary>
    public void ApplyHexMap(IReadOnlyDictionary<string, string>? map)
    {
        if (map is null)
        {
            return;
        }

        foreach (var property in typeof(ThemePalette).GetProperties())
        {
            if (property.PropertyType == typeof(Color) &&
                property.CanWrite &&
                map.TryGetValue(property.Name, out var hex) &&
                TryParseHex(hex, out var color))
            {
                property.SetValue(this, color);
            }
        }
    }

    public static Color Hex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex)!;
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim();
        if (!value.StartsWith('#'))
        {
            value = "#" + value;
        }

        if (value.Length != 7)
        {
            return false;
        }

        try
        {
            if (byte.TryParse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
                byte.TryParse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
                byte.TryParse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                color = Color.FromRgb(r, g, b);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
