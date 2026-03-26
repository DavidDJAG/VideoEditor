using System.Text.Json;
using Microsoft.Data.Sqlite;
using VideoEditor.Application.Abstractions;
using VideoEditor.Domain.Models;

namespace VideoEditor.Infrastructure.Services;

public sealed class SqliteJobStore : IJobStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqliteJobStore(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS jobs (
                id TEXT PRIMARY KEY,
                payload TEXT NOT NULL,
                created_at TEXT NOT NULL,
                state TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MediaJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = new List<MediaJob>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT payload FROM jobs ORDER BY created_at DESC";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var payload = reader.GetString(0);
            var job = JsonSerializer.Deserialize<MediaJob>(payload, JsonOptions);
            if (job is not null)
            {
                jobs.Add(job);
            }
        }

        return jobs;
    }

    public async Task UpsertAsync(MediaJob job, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO jobs (id, payload, created_at, state)
            VALUES ($id, $payload, $createdAt, $state)
            ON CONFLICT(id) DO UPDATE SET
                payload = excluded.payload,
                created_at = excluded.created_at,
                state = excluded.state;
            """;

        cmd.Parameters.AddWithValue("$id", job.Id.ToString("D"));
        cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(job, JsonOptions));
        cmd.Parameters.AddWithValue("$createdAt", job.CreatedAt.UtcDateTime.ToString("O"));
        cmd.Parameters.AddWithValue("$state", job.State.ToString());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MediaJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT payload FROM jobs WHERE id = $id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", id.ToString("D"));

        var payload = await cmd.ExecuteScalarAsync(cancellationToken) as string;
        return payload is null ? null : JsonSerializer.Deserialize<MediaJob>(payload, JsonOptions);
    }
}
