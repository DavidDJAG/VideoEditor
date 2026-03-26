using VideoEditor.Domain.Models;

namespace VideoEditor.Infrastructure.Settings;

public interface ISettingsPersistence
{
    ToolPaths LoadToolPaths();

    void SaveToolPaths(ToolPaths toolPaths);
}
