using VidEditor.Domain.Models;
using VidEditor.Infrastructure.Services;

namespace VidEditor.Tests;

public sealed class JsonJobStoreTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "VidEditor.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task UpsertAndGetByIdAsync_PersistsSingleJobFile()
    {
        var store = CreateStore();
        await store.InitializeAsync();

        var job = CreateJob(createdAt: DateTimeOffset.UtcNow.AddMinutes(-10), state: JobState.Queued);
        await store.UpsertAsync(job);

        var persisted = await store.GetByIdAsync(job.Id);
        var files = Directory.GetFiles(Path.Combine(_rootDirectory, "jobs"), "*.json", SearchOption.TopDirectoryOnly);

        Assert.NotNull(persisted);
        Assert.Equal(job, persisted);
        Assert.Single(files);
        Assert.Equal($"{job.Id:D}.json", Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task UpsertAsync_ReplacesExistingFileWithoutCreatingDuplicates()
    {
        var store = CreateStore();
        await store.InitializeAsync();

        var job = CreateJob(createdAt: DateTimeOffset.UtcNow.AddMinutes(-10), state: JobState.Draft);
        await store.UpsertAsync(job);

        var updated = job with { State = JobState.Succeeded, Progress = 100, FinishedAt = DateTimeOffset.UtcNow };
        await store.UpsertAsync(updated);

        var all = await store.GetAllAsync();
        var files = Directory.GetFiles(Path.Combine(_rootDirectory, "jobs"), "*.json", SearchOption.TopDirectoryOnly);

        Assert.Single(all);
        Assert.Single(files);
        Assert.Equal(JobState.Succeeded, all.Single().State);
        Assert.Equal(100, all.Single().Progress);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsJobsOrderedByCreatedAtDescending()
    {
        var store = CreateStore();
        await store.InitializeAsync();

        var older = CreateJob(createdAt: DateTimeOffset.UtcNow.AddHours(-2), state: JobState.Succeeded);
        var newer = CreateJob(createdAt: DateTimeOffset.UtcNow.AddHours(-1), state: JobState.Failed);

        await store.UpsertAsync(older);
        await store.UpsertAsync(newer);

        var all = await store.GetAllAsync();

        Assert.Collection(
            all,
            first => Assert.Equal(newer.Id, first.Id),
            second => Assert.Equal(older.Id, second.Id));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private JsonJobStore CreateStore()
        => new(Path.Combine(_rootDirectory, "jobs"));

    private static MediaJob CreateJob(DateTimeOffset createdAt, JobState state)
        => new(
            Guid.NewGuid(),
            "Persisted job",
            "transcode",
            new OperationParameters("input.mp4", "output.mp4", null, null, null, 1.0, [], new Dictionary<string, string>(), null),
            createdAt,
            "tester",
            state,
            RetryPolicy: RetryPolicy.Default);
}
