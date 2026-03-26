using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Abstractions;

public interface IToolResolver
{
    ToolPaths Resolve();
}
