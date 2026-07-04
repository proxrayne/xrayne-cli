# Codex Rules

This repository contains the standalone XRayne CLI. Before making .NET backend
or CLI changes, Codex must read and follow the shared styleguide at
`../docs/DOTNET_STYLEGUIDE.md`.

The shared styleguide is mandatory when a task touches CLI commands, runtime
migrations, data access, EF Core, repositories/data projects, services, DTOs,
validation, errors, logging, configuration, dependency injection, tests, release
behavior, or backend documentation.

Keep CLI command behavior, executable name, runtime file layout, and release
artifact names backward compatible unless the task explicitly requests a
breaking change. Follow existing architecture first, then improve it
incrementally with small, reviewable changes.
