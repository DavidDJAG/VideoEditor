namespace VidEditor.Infrastructure.Toolchain;

public sealed class ToolchainNotFoundException : InvalidOperationException
{
    public ToolchainNotFoundException(string message) : base(message)
    {
    }
}
