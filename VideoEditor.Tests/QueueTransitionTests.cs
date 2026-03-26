using System.Collections.Concurrent;
using VideoEditor.Application.Abstractions;
using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class QueueTransitionTests
{
    public static TheoryData<string> NewUiOperations => new()
    {
        "extract_audio",
        "extract_video",
        "concat"
    };

    [Fact]
    public async Task PauseAndResumeAsync_TransitionsQueuedJob()
    {
        var store = new FakeStore();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        var executor = new FakeExecutor(async _ =>
        {
            var current = Interlocked.Increment(ref executions);
            if (current == 1)
            {
                await gate.Task;
            }

            return SuccessArtifact();
        });

        await using var sut = new InMemoryJobQueueService(store, executor, maxConcurrency: 1);
        await sut.InitializeAsync();

        var runningJob = await sut.EnqueueAsync(CreateJob());
        await Task.Delay(50);

        var queuedJob = await sut.EnqueueAsync(CreateJob());
        var paused = await sut.PauseAsync(queuedJob.Id);
        var resumed = await sut.ResumeAsync(queuedJob.Id);
        gate.SetResult();
        await Task.Delay(150);

        var current = (await sut.GetAllAsync()).Single(x => x.Id == queuedJob.Id);
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

    [Theory]
    [MemberData(nameof(NewUiOperations))]
    public async Task CancelAsync_MarksCancelled_ForEachNewUiOperation(string operation)
    {
        var store = new FakeStore();
        var executor = new FakeExecutor(_ => Task.FromResult(SuccessArtifact()));
        await using var sut = new InMemoryJobQueueService(store, executor);
        await sut.InitializeAsync();

        var job = await sut.CreateDraftAsync(CreateJob(operation: operation));
        var cancelled = await sut.CancelAsync(job.Id);
        var current = (await sut.GetAllAsync()).Single(x => x.Id == job.Id);

        Assert.True(cancelled);
        Assert.Equal(JobState.Cancelled, current.State);
        Assert.Equal(operation, current.Operation);
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

    [Theory]
    [MemberData(nameof(NewUiOperations))]
    public async Task RetryAsync_RequeuesFailedJob_ForEachNewUiOperation(string operation)
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

        var job = await sut.EnqueueAsync(CreateJob(operation: operation));
        await Task.Delay(150);
        var retried = await sut.RetryAsync(job.Id);
        await Task.Delay(150);

        var succeeded = (await sut.GetAllAsync()).Single(x => x.Id == job.Id);
        Assert.True(retried);
        Assert.Equal(JobState.Succeeded, succeeded.State);
        Assert.Equal(operation, succeeded.Operation);
    }

    private static MediaJob CreateJob(string operation = "transcode")
        => new(
            Guid.NewGuid(),
            "Queue transition",
            operation,
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
