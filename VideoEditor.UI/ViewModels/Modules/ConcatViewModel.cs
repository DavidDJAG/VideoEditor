using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.UI.ViewModels.Modules;

public sealed class ConcatViewModel
{
    private readonly IFfmpegService _ffmpegService;
    private readonly ICommandBuilder _commandBuilder;

    public ConcatViewModel(IFfmpegService ffmpegService, ICommandBuilder commandBuilder)
    {
        _ffmpegService = ffmpegService;
        _commandBuilder = commandBuilder;
    }

    public Task<int> ExecuteAsync(OperationParameters parameters, CancellationToken cancellationToken = default)
        => _ffmpegService.ExecuteAsync(_commandBuilder.BuildConcat(parameters), cancellationToken);
}
