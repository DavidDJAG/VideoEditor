using System.Collections.Concurrent;
using VideoEditor.Application.Abstractions;
using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class JobQueueServiceTests
{
    public static TheoryData<string> NewUiOperations => new()
    {
        "extract_audio",
        "extract_video",
        "concat"
    };

    [Fact]
    public async Task EnqueueAsync_PersistsAndCompletesJob()
    {
        var store = new FakeStore();
        var executor = new FakeExecutor(_ => Task.FromResult(new JobExecutionArtifact(Guid.Empty, "ffmpeg -i in out", "ok", "", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(1), new[] { "out.mp4" })));
        await using var sut = new InMemoryJobQueueService(store, executor);
        await sut.InitializeAsync();

        var job = CreateJob();
        await sut.EnqueueAsync(job);

        await Task.Delay(150);
        var all = await sut.GetAllAsync();
        Assert.Contains(all, x => x.Id == job.Id && x.State == JobState.Succeeded);
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersBySearchAndState()
    {
        var store = new FakeStore();
        var executor = new FakeExecutor(_ => Task.FromResult(new JobExecutionArtifact(Guid.Empty, "cmd", "", "", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [])));
        await using var sut = new InMemoryJobQueueService(store, executor);
        await sut.InitializeAsync();

        await sut.CreateDraftAsync(CreateJob(name: "Draft one"));
        await sut.EnqueueAsync(CreateJob(name: "Encode trailer"));
        await Task.Delay(150);

        var filtered = await sut.GetHistoryAsync(new JobHistoryFilter("trailer", new[] { JobState.Succeeded }));
        Assert.Single(filtered);
    }

    [Theory]
    [MemberData(nameof(NewUiOperations))]
    public async Task EnqueueAsync_Completes_ForEachNewUiOperation(string operation)
    {
        var store = new FakeStore();
        var executor = new FakeExecutor(_ => Task.FromResult(new JobExecutionArtifact(Guid.Empty, "cmd", "", "", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [])));
        await using var sut = new InMemoryJobQueueService(store, executor);
        await sut.InitializeAsync();

        var job = CreateJob(name: $"Job {operation}", operation: operation);
        await sut.EnqueueAsync(job);
        await Task.Delay(150);

        var all = await sut.GetAllAsync();
        Assert.Contains(all, x => x.Id == job.Id && x.State == JobState.Succeeded && x.Operation == operation);
    }

    private static MediaJob CreateJob(string name = "Encode", string operation = "transcode")
        => new(
            Guid.NewGuid(),
            name,
            operation,
            new OperationParameters("in.mp4", "out.mp4", null, null, null, 1.0, [], new Dictionary<string, string>(), null),
            DateTimeOffset.UtcNow,
            "tester",
            JobState.Draft,
            RetryPolicy: new RetryPolicy(1));

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
