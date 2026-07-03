# XRayne CLI

Standalone source repository for the XRayne `xrayne` command-line tool.

## Projects

- `Cli`: executable command surface for install, update, certificate, API, admin, and xray runtime operations.
- `Contracts`: shared configuration, values, enums, and utility contracts used by the CLI.
- `Data`: EF Core persistence, entities, migrations, repositories, and runtime config-file utilities.
- `Infrastructure`: CLI-safe runtime services and cross-cutting utilities.
- `Tests`: focused unit tests for the Octokit-backed release helper, runtime config utilities, migrations, and CLI support code.

## Validation

```powershell
dotnet restore XRayne.Cli.sln
dotnet build XRayne.Cli.sln
dotnet test XRayne.Cli.sln
dotnet run --project Cli -- --help
```

Release archives keep the public artifact names:

- `xrayne-cli-win-x64.zip`
- `xrayne-cli-osx-arm64.tar.gz`
- `xrayne-cli-linux-x64.tar.gz`
