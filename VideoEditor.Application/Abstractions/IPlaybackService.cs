namespace VideoEditor.Application.Abstractions;

public interface IPlaybackService
{
    Task PlayAsync(
        string inputPath,
        TimeSpan? start = null,
        TimeSpan? end = null,
        double speedFactor = 1.0,
        TimeSpan? subtitleOffset = null,
        CancellationToken cancellationToken = default);

    Task PlayABPreviewAsync(
        string inputPath,
        TimeSpan? aStart,
        TimeSpan? aEnd,
        TimeSpan? bStart,
        TimeSpan? bEnd,
        double speedFactor = 1.0,
        TimeSpan? subtitleOffset = null,
        CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    string? LastError { get; }
}
