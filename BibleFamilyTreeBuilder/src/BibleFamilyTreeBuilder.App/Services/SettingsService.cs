using System;
using System.IO;
using System.Text.Json;

namespace BibleFamilyTreeBuilder.App.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> to a JSON file under the local app data folder.
/// Local-first and best-effort: any failure falls back to defaults and never throws to callers.
/// </summary>
public class SettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BibleFamilyTreeBuilder");
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Ignore corrupt/unreadable settings and fall back to defaults.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, Options);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Best effort: never let a settings write failure interrupt the app.
        }
    }
}
