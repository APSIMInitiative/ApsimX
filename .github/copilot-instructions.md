# Copilot Cloud Agent Instructions for APSIMInitiative/ApsimX

## Repository overview
- APSIM Next Generation is a large .NET solution (`/home/runner/work/ApsimX/ApsimX/ApsimX.sln`) with core projects including:
  - `Models` (simulation/model logic)
  - `ApsimNG` (GUI)
  - `APSIM.Cli` (CLI entrypoint)
  - `Tests/UnitTests` (NUnit test suite)
- Primary framework target is .NET 8 (`net8.0`).

## First steps for any coding task
1. Read relevant docs before editing:
   - `/home/runner/work/ApsimX/ApsimX/Docs/content/Development/compile.md`
   - `/home/runner/work/ApsimX/ApsimX/Docs/content/Development/Software/CodingStyle.md`
   - `/home/runner/work/ApsimX/ApsimX/Docs/content/Contribute/pullrequests.md`
2. Keep changes tightly scoped to one concern.
3. Prefer modifying existing files and patterns over introducing new abstractions.

## Build and test commands (verified)
Run from `/home/runner/work/ApsimX/ApsimX`:

```bash
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Notes:
- CI workflow `.github/workflows/run-apsimx-unit-tests.yml` also uses `dotnet build --configuration Release` and `dotnet test`.
- Unit tests are in `Tests/UnitTests` and use NUnit.

## Coding conventions to follow
- Follow Microsoft C# conventions with project-specific guidance in:
  - `/home/runner/work/ApsimX/ApsimX/Docs/content/Development/Software/CodingStyle.md`
  - `/home/runner/work/ApsimX/ApsimX/.editorconfig`
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
- Core model code: `/home/runner/work/ApsimX/ApsimX/Models`
- Unit tests: `/home/runner/work/ApsimX/ApsimX/Tests/UnitTests`
- GUI code: `/home/runner/work/ApsimX/ApsimX/ApsimNG`
- CLI code: `/home/runner/work/ApsimX/ApsimX/APSIM.Cli`
- Developer docs: `/home/runner/work/ApsimX/ApsimX/Docs/content/Development`

## Known errors and workarounds
Documented issues from repository docs/workflows:

1. **Linux runtime error loading sqlite**
   - Error: `System.DllNotFoundException: Unable to load shared library 'sqlite3'... libsqlite3.so: cannot open shared object file`
   - Workaround: install `libsqlite3-dev` (preferred) or create a symlink from `libsqlite3.so.0` to `libsqlite3.so`.
   - Source: `/home/runner/work/ApsimX/ApsimX/Docs/content/Development/compile.md`

2. **Package install issue on Debian/Ubuntu**
   - Error: `E: Unable to locate package dotnet-runtime-8.0`
   - Workaround: install .NET packages from Microsoft package repositories (not default Debian repos).
   - Source: `/home/runner/work/ApsimX/ApsimX/Docs/content/Development/compile.md`

3. **Model validation workflow can fail when branch is stale**
   - Error condition: merge step in `.github/workflows/run-model-validations.yml` fails when PR branch cannot merge cleanly with `upstream/master`.
   - Workaround: update/rebase/merge latest `master` into the PR branch and resolve conflicts before rerunning validation.
   - Source: `/home/runner/work/ApsimX/ApsimX/.github/workflows/run-model-validations.yml`

## Agent efficiency tips
- Before broad changes, use targeted searches in `Models` and `Tests/UnitTests` to mirror existing patterns.
- For behavior changes, update/add tests in the mirrored folder structure under `Tests/UnitTests`.
- Avoid editing unrelated docs or workflow files unless the task requires it.
