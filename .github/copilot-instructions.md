# Copilot Cloud Agent Instructions for APSIMInitiative/ApsimX

## Repository overview
- APSIM Next Generation is a large .NET solution (`ApsimX.sln`) with core projects including:
  - `Models` (simulation/model logic)
  - `ApsimNG` (GUI)
  - `APSIM.Cli` (CLI entrypoint)
  - `Tests/UnitTests` (NUnit test suite)
- Primary framework target is .NET 8 (`net8.0`).

## First steps for any coding task
1. Read relevant docs before editing:
   - `Docs/content/Development/compile.md`
   - `Docs/content/Development/Software/CodingStyle.md`
   - `Docs/content/Contribute/pullrequests.md`
2. Keep changes tightly scoped to one concern.
3. Prefer modifying existing files and patterns over introducing new abstractions.

## Build and test commands (verified)
Run from `/ApsimX`:

```bash
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Notes:
- CI workflow `.github/workflows/run-apsimx-unit-tests.yml` also uses `dotnet build --configuration Release` and `dotnet test`.
- Unit tests are in `Tests/UnitTests` and use NUnit.

## Coding conventions to follow
- Follow Microsoft C# conventions with project-specific guidance in:
  - `Docs/content/Development/Software/CodingStyle.md`
  - `.editorconfig`
- Prefer:
  - PascalCase for types/members, camelCase for method args/private fields.
  - Allman-style braces.
  - Minimal inheritance where composition is practical.
- Keep public property implementations trivial when possible.

## Contribution and PR expectations
- Ensure PRs are focused and small where possible.
- Link PRs to issues (`working on #...` or `resolves #...`) per contribution docs.
- Bug fixes should include/adjust unit tests where applicable.
- For science changes, include validation evidence and keep data-only changes isolated when possible.

## High-value paths for common tasks
- Core model code: `Models`
- Unit tests: `Tests/UnitTests`
- GUI code: `ApsimNG`
- CLI code: `Models`
- Developer docs: `Docs/content/Development`

## Known errors and workarounds
Documented issues from repository docs/workflows:

1. **Linux runtime error loading sqlite**
   - Error: `System.DllNotFoundException: Unable to load shared library 'sqlite3' or one of its dependencies... libsqlite3: cannot open shared object file: No such file or directory`
   - Workaround: on Debian/derivatives, install `libsqlite3-dev` (provides `libsqlite3.so`); alternatively create a symlink named `libsqlite3.so` pointing to `libsqlite3.so.0`.
   - Source: `Docs/content/Development/compile.md`

2. **Package install issue on Debian/Ubuntu**
   - Error: `E: Unable to locate package dotnet-runtime-8.0`
   - Workaround: install .NET packages from Microsoft package repositories (not default Debian repos).
   - Source: `Docs/content/Development/compile.md`

3. **Model validation workflow can fail when branch is stale**
   - Error condition: merge step in `.github/workflows/run-model-validations.yml` fails when PR branch cannot merge cleanly with `upstream/master`.
   - Workaround: update/rebase/merge latest `master` into the PR branch and resolve conflicts before rerunning validation.
   - Source: `.github/workflows/run-model-validations.yml`

## Agent efficiency tips
- Before broad changes, use targeted searches in `Models` and `Tests/UnitTests` to mirror existing patterns.
- For behavior changes, update/add tests in the mirrored folder structure under `Tests/UnitTests`.
- Avoid editing unrelated docs or workflow files unless the task requires it.
