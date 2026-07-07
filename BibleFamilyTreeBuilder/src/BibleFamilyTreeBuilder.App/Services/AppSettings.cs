using System.Collections.Generic;

namespace BibleFamilyTreeBuilder.App.Services;

/// <summary>
/// Small local-first settings blob saved next to the user profile. Currently just theme state.
/// </summary>
public class AppSettings
{
    /// <summary>Name of the selected theme: "Parchment", "Light", "Dark", or "Custom".</summary>
    public string SelectedThemeName { get; set; } = "Parchment";

    /// <summary>Saved custom palette as a property-name -&gt; "#RRGGBB" map (null when never customized).</summary>
    public Dictionary<string, string>? CustomPalette { get; set; }
}
