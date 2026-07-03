using System.Diagnostics;
using Cli.Services.Contracts;

namespace Cli.Services;

public sealed class ShellService : IShellService
{
    public Task<string> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        return RunAsync(fileName, arguments, workingDirectory, null, cancellationToken);
    }

    public Task<string> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        AddEnvironment(startInfo, environment);

        return RunAsync(startInfo, $"{fileName} {arguments}", cancellationToken);
    }

    public Task<string> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        return RunAsync(fileName, arguments, workingDirectory, null, cancellationToken);
    }

    public Task<string> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        AddEnvironment(startInfo, environment);

        return RunAsync(startInfo, $"{fileName} {string.Join(' ', arguments)}", cancellationToken);
    }

    private static void AddEnvironment(
        ProcessStartInfo startInfo,
        IReadOnlyDictionary<string, string>? environment)
    {
        if (environment is null)
        {
            return;
        }

        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }
    }

    private static async Task<string> RunAsync(
        ProcessStartInfo startInfo,
        string command,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = string.Join(Environment.NewLine, await stdout, await stderr).Trim();
        if (process.ExitCode == 0)
        {
            return output;
        }

        throw new InvalidOperationException($"{command} failed with exit code {process.ExitCode}.{Environment.NewLine}{output}");
    }
}
