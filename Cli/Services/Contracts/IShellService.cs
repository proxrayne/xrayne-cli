namespace Cli.Services.Contracts;

public interface IShellService
{
    Task<string> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken);

    Task<string> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken);

    Task<string> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken);

    Task<string> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken);
}
