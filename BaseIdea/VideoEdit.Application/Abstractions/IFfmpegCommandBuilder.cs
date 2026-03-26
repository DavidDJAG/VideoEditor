using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface IFfmpegCommandBuilder
{
    CommandDefinition Build(MediaJobRequest request, ToolPaths toolPaths);
}
