using VidEditor.Domain.Models;

namespace VidEditor.Application.Abstractions;

public interface IOperationRequestFactory
{
    IFfmpegOperationRequest Create(OperationKind kind, OperationParameters parameters);
}
