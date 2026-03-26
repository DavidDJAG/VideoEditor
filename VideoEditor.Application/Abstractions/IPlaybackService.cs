namespace VideoEditor.Application.Abstractions;

public interface IPlaybackService
{
    Task PlayAsync(string inputPath, CancellationToken cancellationToken = default);

    Task PauseAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
