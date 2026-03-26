using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface ICommandBuilder
{
    string BuildTrim(OperationParameters parameters);

    string BuildTranscode(OperationParameters parameters);

    string BuildConcat(OperationParameters parameters);

    string BuildProbe(string inputPath);
}
