namespace VideoEditor.Infrastructure.Execution;

public interface IProcessExecutor
{
    Task<ProcessExecutionResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
}
