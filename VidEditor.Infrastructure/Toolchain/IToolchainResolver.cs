using VidEditor.Domain.Models;

namespace VidEditor.Infrastructure.Toolchain;

public interface IToolchainResolver
{
    ToolchainBinaryDiagnostic ResolveBinary(string toolName, string configuredValue);

    (ToolchainBinaryDiagnostic Ffmpeg, ToolchainBinaryDiagnostic Ffprobe, ToolchainBinaryDiagnostic? Ffplay) ResolveAll();

    ToolPaths ResolvePathsOrThrow();
}
