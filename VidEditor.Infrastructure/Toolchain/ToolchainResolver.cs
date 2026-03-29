using VidEditor.Domain.Models;
using VidEditor.Infrastructure.Settings;

namespace VidEditor.Infrastructure.Toolchain;

public sealed class ToolchainResolver : IToolchainResolver
{
    private readonly ISettingsPersistence _settingsPersistence;

    public ToolchainResolver(ISettingsPersistence settingsPersistence)
    {
        _settingsPersistence = settingsPersistence;
    }

    public ToolchainBinaryDiagnostic ResolveBinary(string toolName, string configuredValue)
    {
        var toolPaths = _settingsPersistence.LoadToolPaths();
        return Resolve(toolName, configuredValue, toolPaths.ToolsDirectory);
    }

    public (ToolchainBinaryDiagnostic Ffmpeg, ToolchainBinaryDiagnostic Ffprobe, ToolchainBinaryDiagnostic? Ffplay) ResolveAll()
    {
        var toolPaths = _settingsPersistence.LoadToolPaths();
        var ffmpeg = Resolve("ffmpeg", toolPaths.FfmpegPath, toolPaths.ToolsDirectory);
        var ffprobe = Resolve("ffprobe", toolPaths.FfprobePath, toolPaths.ToolsDirectory);
        ToolchainBinaryDiagnostic? ffplay = null;

        if (!string.IsNullOrWhiteSpace(toolPaths.FfplayPath))
        {
            ffplay = Resolve("ffplay", toolPaths.FfplayPath, toolPaths.ToolsDirectory);
        }

        return (ffmpeg, ffprobe, ffplay);
    }

    public ToolPaths ResolvePathsOrThrow()
    {
        var resolved = ResolveAll();
        if (resolved.Ffmpeg.IsMissing)
        {
            throw new ToolchainNotFoundException(resolved.Ffmpeg.MissingRemediation ?? "ffmpeg binary is missing.");
        }

        if (resolved.Ffprobe.IsMissing)
        {
            throw new ToolchainNotFoundException(resolved.Ffprobe.MissingRemediation ?? "ffprobe binary is missing.");
        }

        if (resolved.Ffplay is { IsMissing: true })
        {
            throw new ToolchainNotFoundException(resolved.Ffplay.MissingRemediation ?? "ffplay binary is missing.");
        }

        return new ToolPaths(
            resolved.Ffmpeg.ResolvedPath,
            resolved.Ffprobe.ResolvedPath,
            resolved.Ffplay?.ResolvedPath,
            _settingsPersistence.LoadToolPaths().ToolsDirectory);
    }

    private static ToolchainBinaryDiagnostic Resolve(string toolName, string configuredValue, string? configuredFolder)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            configuredValue = toolName;
        }

        if (LooksLikePath(configuredValue))
        {
            var explicitPath = Path.GetFullPath(configuredValue);
            var exists = File.Exists(explicitPath);
            return BuildDiagnostic(toolName, configuredValue, explicitPath, false, false, !exists, configuredFolder);
        }

        if (!string.IsNullOrWhiteSpace(configuredFolder))
        {
            foreach (var candidate in GetBinaryNameCandidates(configuredValue))
            {
                var fullPath = Path.Combine(configuredFolder, candidate);
                if (File.Exists(fullPath))
                {
                    return BuildDiagnostic(toolName, configuredValue, fullPath, true, false, false, configuredFolder);
                }
            }
        }

        var fromPath = ResolveFromPath(configuredValue);
        if (fromPath is not null)
        {
            return BuildDiagnostic(toolName, configuredValue, fromPath, false, true, false, configuredFolder);
        }

        return BuildDiagnostic(toolName, configuredValue, configuredValue, false, false, true, configuredFolder);
    }

    private static bool LooksLikePath(string value) =>
        Path.IsPathRooted(value) || value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar);

    private static string? ResolveFromPath(string binaryName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var pathSegment in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var candidate in GetBinaryNameCandidates(binaryName))
            {
                var fullPath = Path.Combine(pathSegment, candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> GetBinaryNameCandidates(string name)
    {
        yield return name;
        if (OperatingSystem.IsWindows() && !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{name}.exe";
        }
    }

    private static ToolchainBinaryDiagnostic BuildDiagnostic(
        string toolName,
        string configuredValue,
        string resolvedPath,
        bool fromConfiguredFolder,
        bool fromPathEnvironment,
        bool isMissing,
        string? configuredFolder)
    {
        string? remediation = null;
        if (isMissing)
        {
            remediation =
                $"{toolName} was not found. Install FFmpeg and ensure '{toolName}' is available in PATH, " +
                $"or set Settings > Tools directory to the folder containing {toolName}. " +
                (string.IsNullOrWhiteSpace(configuredFolder)
                    ? "Current tools directory is not configured."
                    : $"Current tools directory: '{configuredFolder}'.");
        }

        return new ToolchainBinaryDiagnostic(
            toolName,
            configuredValue,
            resolvedPath,
            fromConfiguredFolder,
            fromPathEnvironment,
            isMissing,
            remediation);
    }
}
