using System.Text.Json;
using VideoEditor.Domain.Models;

namespace VideoEditor.Infrastructure.Settings;

public sealed class JsonSettingsPersistence : ISettingsPersistence
{
    private readonly string _settingsPath;

    public JsonSettingsPersistence()
    {
        var settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoEditor");
        Directory.CreateDirectory(settingsDirectory);
        _settingsPath = Path.Combine(settingsDirectory, "settings.json");
    }

    public AppSettings LoadAppSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return AppSettings.Default;
        }

        var json = File.ReadAllText(_settingsPath);
        var loaded = JsonSerializer.Deserialize<AppSettings>(json);

        if (loaded?.ToolPaths is not null && loaded.ModuleFlags is not null && loaded.BetaCriteria is not null)
        {
            return loaded;
        }

        return LoadLegacyToolPathsFallback(json);
    }

    public void SaveAppSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    public ToolPaths LoadToolPaths()
    {
        return LoadAppSettings().ToolPaths;
    }

    public void SaveToolPaths(ToolPaths toolPaths)
    {
        var settings = LoadAppSettings() with { ToolPaths = toolPaths };
        SaveAppSettings(settings);
    }

    private static AppSettings LoadLegacyToolPathsFallback(string json)
    {
        var legacyToolPaths = JsonSerializer.Deserialize<ToolPaths>(json);
        return legacyToolPaths is null
            ? AppSettings.Default
            : AppSettings.Default with { ToolPaths = legacyToolPaths };
    }
}
