using System.CommandLine;
using System.Reflection;

namespace Cli.Commands;

public sealed class VersionCommand : Command
{
    public VersionCommand()
        : base("version", "Print XRayne CLI version")
    {
        SetAction(_ =>
        {
            Console.WriteLine(GetVersion());

            return 0;
        });
    }

    private static string GetVersion()
    {
        var assembly = typeof(VersionCommand).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var versionParts = informationalVersion.Split('+', 2);
            var version = versionParts[0];

            if (versionParts.Length == 2 && !string.IsNullOrWhiteSpace(versionParts[1]))
            {
                return $"CLI Version: {version}{Environment.NewLine}Commit: {versionParts[1]}";
            }

            return $"CLI Version: {version}";
        }

        return $"CLI Version: {assembly.GetName().Version?.ToString() ?? "unknown"}";
    }
}
