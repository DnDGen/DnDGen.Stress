# Contract: `.github/workflows/build.yml`

What the workflow must do, observable from GitHub. The decisions behind it are in
[research.md](../research.md).

## Triggers and concurrency

| Event | Jobs that run |
|---|---|
| `pull_request` targeting `main` | `check` |
| `push` to `main` | `check`, then `publish`, then `release` |
| `push` to a fork's `main` | `check` only |

- Concurrency group `${{ github.workflow }}-${{ github.ref }}`; `cancel-in-progress` only for
  `pull_request` (R8).
- Workflow-level permissions: `contents: read`. Jobs widen only what they need.

## Job `check`

- **Name shown on pull requests**: `check` (the ruleset requires it, R11).
- **Runner**: `windows-latest`. **Permissions**: `contents: read`.
- **Steps, in order** (each failure stops the job):
  1. Check out; set up the .NET `10.x` SDK.
  2. Run the Pester tests in `.github/scripts` (R6).
  3. Find the highest released version (R4) and run `Assert-VersionIsNewer.ps1`. Fails, stating
     both versions, unless `<Version>` is greater (FR-007).
  4. Build `DnDGen.Stress/DnDGen.Stress.csproj` in `Release`, producing the package.
  5. Test `DnDGen.Stress.Tests/DnDGen.Stress.Tests.csproj` in `Stress`, `-v normal`, collecting
     Cobertura coverage with `CodeCoverage.runsettings` (FR-001, R5, R7).
  6. Download `coverage.cobertura.xml` from the highest release and run
     `Assert-CoverageNotLower.ps1`. Fails, stating both figures, if lines or branches dropped, or
     if the release has no report (FR-003).
  7. Upload `coverage.cobertura.xml` as the `coverage` artifact unless the run was cancelled, so a
     failed coverage check's report can be inspected too; then upload the package as the `package`
     artifact.

## Job `publish`

- **Runs when**: `push` to `main` in `DnDGen/DnDGen.Stress` (not a fork), after `check` passes.
- **Runner**: `ubuntu-latest`. **Environment**: `Prod`.
  **Permissions**: `id-token: write`, `contents: read`.
- **Steps**: download the package artifact; `NuGet/login@v1` with `user: cidthecoatrack`;
  `dotnet nuget push` with the login's `NUGET_API_KEY` to `https://api.nuget.org/v3/index.json`,
  with no `--skip-duplicate` (FR-005, FR-008).

## Job `release`

- **Runs when**: `push` to `main` in `DnDGen/DnDGen.Stress` (not a fork), after `check` and
  `publish` pass.
- **Runner**: `ubuntu-latest`. **Permissions**: `contents: write`.
- **Steps**: download the package and coverage artifacts;
  `gh release create <Version> <package> coverage.cobertura.xml --target <commit SHA>
  --title <Version> --generate-notes` (FR-004, R9). The version comes from `check` as a job
  output.

## Recovery

| Failed job | What the maintainer does | Result |
|---|---|---|
| `check` | Fix on a branch | Nothing was published or released |
| `publish` | Re-run failed jobs | `publish` and `release` run again |
| `release` | Re-run failed jobs | Only `release` runs; NuGet.org isn't touched |
| `release`, but **Re-run all jobs** chosen | Re-run failed jobs instead | `publish` fails on the duplicate: the safeguard (FR-008) |
