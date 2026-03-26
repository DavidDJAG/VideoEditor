namespace VideoEditor.Domain.Models;

public sealed record ToolchainBinaryDiagnostic(
    string Name,
    string ConfiguredValue,
    string ResolvedPath,
    bool FromConfiguredFolder,
    bool FromPathEnvironment,
    bool IsMissing,
    string? MissingRemediation);
