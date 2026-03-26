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
        _settingsPath = Path.Combine(settingsDirectory, "tools.json");
    }

    public ToolPaths LoadToolPaths()
    {
        if (!File.Exists(_settingsPath))
        {
            return new ToolPaths("ffmpeg", "ffprobe", "ffplay");
        }

        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<ToolPaths>(json) ?? new ToolPaths("ffmpeg", "ffprobe", "ffplay");
    }

    public void SaveToolPaths(ToolPaths toolPaths)
    {
        var json = JsonSerializer.Serialize(toolPaths, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}
