using System.Collections.Concurrent;
using VideoEditor.Application.Abstractions;
using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class QueueTransitionTests
{
    [Fact]
    public async Task PauseAndResumeAsync_TransitionsQueuedJob()
    {
        var store = new FakeStore();
        var gate = new TaskCompletionSource();
        var executor = new FakeExecutor(async _ =>
        {
            await gate.Task;
            return SuccessArtifact();
        });

        await using var sut = new InMemoryJobQueueService(store, executor, maxConcurrency: 1);
        await sut.InitializeAsync();

        var job = await sut.EnqueueAsync(CreateJob());
        var paused = await sut.PauseAsync(job.Id);
        var resumed = await sut.ResumeAsync(job.Id);
        gate.SetResult();
        await Task.Delay(150);

        var current = (await sut.GetAllAsync()).Single(x => x.Id == job.Id);
        Assert.True(paused);
        Assert.True(resumed);
        Assert.Equal(JobState.Succeeded, current.State);
    }

    [Fact]
    public async Task CancelAsync_MarksJobCancelled()
    {
        var store = new FakeStore();
        var executor = new FakeExecutor(_ => Task.FromResult(SuccessArtifact()));
        await using var sut = new InMemoryJobQueueService(store, executor);
        await sut.InitializeAsync();

        var job = await sut.CreateDraftAsync(CreateJob());
        var cancelled = await sut.CancelAsync(job.Id);

        var current = (await sut.GetAllAsync()).Single(x => x.Id == job.Id);
        Assert.True(cancelled);
        Assert.Equal(JobState.Cancelled, current.State);
        Assert.True(current.IsCancellationRequested);
    }

    [Fact]
    public async Task RetryAsync_RequeuesFailedJob()
    {
        var store = new FakeStore();
        var attempts = 0;
        var executor = new FakeExecutor(_ =>
        {
            attempts++;
            var exitCode = attempts == 1 ? 1 : 0;
            return Task.FromResult(new JobExecutionArtifact(Guid.NewGuid(), "ffmpeg", "", "", exitCode, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ["out.mp4"]));
        });

        await using var sut = new InMemoryJobQueueService(store, executor);
        await sut.InitializeAsync();

        var job = await sut.EnqueueAsync(CreateJob());
        await Task.Delay(150);

        var failed = (await sut.GetAllAsync()).Single(x => x.Id == job.Id);
        Assert.Equal(JobState.Failed, failed.State);

        var retried = await sut.RetryAsync(job.Id);
        await Task.Delay(150);

        var succeeded = (await sut.GetAllAsync()).Single(x => x.Id == job.Id);
        Assert.True(retried);
        Assert.Equal(JobState.Succeeded, succeeded.State);
    }

    private static MediaJob CreateJob()
        => new(
            Guid.NewGuid(),
            "Queue transition",
            "transcode",
            new OperationParameters("in.mp4", "out.mp4", null, null, null, 1.0, [], new Dictionary<string, string>(), null),
            DateTimeOffset.UtcNow,
            "tester",
            JobState.Draft,
            RetryPolicy: new RetryPolicy(1));

    private static JobExecutionArtifact SuccessArtifact()
        => new(Guid.NewGuid(), "ffmpeg", "", "", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ["out.mp4"]);

    private sealed class FakeStore : IJobStore
    {
        private readonly ConcurrentDictionary<Guid, MediaJob> _items = new();

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyCollection<MediaJob>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyCollection<MediaJob>)_items.Values.ToArray());

        public Task UpsertAsync(MediaJob job, CancellationToken cancellationToken = default)
        {
            _items[job.Id] = job;
            return Task.CompletedTask;
        }

        public Task<MediaJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.TryGetValue(id, out var job) ? job : null);
    }

    private sealed class FakeExecutor : IJobExecutionService
    {
        private readonly Func<MediaJob, Task<JobExecutionArtifact>> _execute;

        public FakeExecutor(Func<MediaJob, Task<JobExecutionArtifact>> execute)
        {
            _execute = execute;
        }

        public Task<JobExecutionArtifact> ExecuteAsync(MediaJob job, CancellationToken cancellationToken = default)
            => _execute(job);
    }
}
