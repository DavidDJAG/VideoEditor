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
            return loaded with { ConvertPresets = loaded.ConvertPresets ?? [] };
        }

        return LoadLegacyToolPathsFallback(json);
    }

    public void SaveAppSettings(AppSettings settings)
    {
        var normalized = settings with { ConvertPresets = settings.ConvertPresets ?? [] };
        var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
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

    public ConvertPresetRecord[] LoadConvertPresets()
    {
        return LoadAppSettings().ConvertPresets;
    }

    public void SaveConvertPresets(ConvertPresetRecord[] convertPresets)
    {
        var settings = LoadAppSettings() with { ConvertPresets = convertPresets ?? [] };
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
