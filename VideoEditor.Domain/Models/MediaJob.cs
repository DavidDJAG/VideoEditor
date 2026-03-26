namespace VideoEditor.Domain.Models;

public sealed record MediaJob(
    Guid Id,
    string Name,
    string Operation,
    OperationParameters Parameters,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string Status,
    double Progress = 0,
    string? Error = null,
    string? OutputPath = null);
