---
name: proxrayne-cli-project
description: Project guidance for the Proxrayne xrayne-cli repository. Use when Codex changes the standalone XRayne CLI, release artifact workflow, CLI runtime migrations, installer/update behavior, or CLI data/infrastructure projects.
---

# Proxrayne CLI Project

## Current State

This repository owns the standalone XRayne CLI source. The executable assembly remains `xrayne`, and public release artifact names remain `xrayne-cli-<rid>`.

## Rules

- Read the shared meta styleguide `xrayne-ai/docs/DOTNET_STYLEGUIDE.md` before .NET backend, CLI command, EF/data, service, configuration, logging, test, or documentation changes.
- Use `$proxrayne-project` first when CLI changes affect release assets, installer behavior, `xrayne-panel`, `xrayne`, or shared runtime contracts.
- Keep CLI command behavior, executable name, runtime file layout, and release artifact names backward compatible unless the user explicitly asks for a breaking change.
- Keep EF persistence and config-file utilities in `Data`.
- Keep xray-core lifecycle and CLI-safe shared services in `Infrastructure`; do not add panel/node hosted services here.
- Keep release asset lookup compatible with the public release repository unless a task explicitly changes release ownership.

## Validation

```powershell
dotnet restore XRayne.Cli.sln
dotnet build XRayne.Cli.sln
dotnet test XRayne.Cli.sln
dotnet run --project Cli -- --help
```
