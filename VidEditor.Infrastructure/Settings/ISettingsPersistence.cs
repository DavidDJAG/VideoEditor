using VidEditor.Domain.Models;

namespace VidEditor.Infrastructure.Settings;

public interface ISettingsPersistence
{
    AppSettings LoadAppSettings();

    void SaveAppSettings(AppSettings settings);

    ToolPaths LoadToolPaths();

    void SaveToolPaths(ToolPaths toolPaths);

    ConvertPresetRecord[] LoadConvertPresets();

    void SaveConvertPresets(ConvertPresetRecord[] convertPresets);
}
