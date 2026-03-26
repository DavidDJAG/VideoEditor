using VideoEditor.Domain.Models;

namespace VideoEditor.Application.Abstractions;

public interface IOperationRequestFactory
{
    IFfmpegOperationRequest Create(OperationKind kind, OperationParameters parameters);
}
