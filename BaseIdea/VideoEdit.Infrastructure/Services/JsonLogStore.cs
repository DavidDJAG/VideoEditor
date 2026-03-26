using System.Text.Json;
using VideoEdit.Application.Abstractions;
using VideoEdit.Domain.Models;

namespace VideoEdit.Infrastructure.Services;

public sealed class JsonLogStore : ILogStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _logPath;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public JsonLogStore(string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        _logPath = Path.Combine(baseDirectory, "history.json");
    }

    public async Task AppendAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<LogEntry> current = [];
            if (File.Exists(_logPath))
            {
                await using var readStream = File.OpenRead(_logPath);
                current = await JsonSerializer.DeserializeAsync<List<LogEntry>>(readStream, JsonOptions, cancellationToken).ConfigureAwait(false)
                          ?? [];
            }

            current.Insert(0, entry);
            await using var stream = File.Create(_logPath);
            await JsonSerializer.SerializeAsync(stream, current.Take(100).ToList(), JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<IReadOnlyList<LogEntry>> LoadRecentAsync(int maxEntries, CancellationToken cancellationToken)
    {
        if (!File.Exists(_logPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_logPath);
        var entries = await JsonSerializer.DeserializeAsync<List<LogEntry>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                      ?? [];
        return entries.Take(maxEntries).ToList();
    }
}
