---
name: proxrayne-cli-project
description: Bootstrap guidance for the Proxrayne xrayne-cli repository. Use when Codex is asked about this repository, future CLI extraction, standalone CLI planning, or adding initial project structure. The repository is currently empty; current CLI source remains in xrayne-panel/Cli until an explicit split task.
---

# Proxrayne CLI Project

## Current State

This repository is an empty future split target for the XRayne CLI. The current production CLI source lives in `xrayne-panel/Cli`.

## Rules

- Do not add CLI code here unless the user explicitly asks to start or perform the CLI split.
- Use `$proxrayne-project` before planning any split from `xrayne-panel/Cli`.
- Keep any future bootstrap work aligned with current `xrayne-panel` CLI command behavior, release artifact names, installer expectations, and runtime migration contracts.
- When the split begins, document the migration plan in the meta repo and update both `xrayne-panel` and `xrayne` docs.

## Validation

There are no code-level validation commands until the repository is initialized with source code. Validate this skill with `quick_validate.py`.
