# Implementation Plan: Build and Release on GitHub Actions

**Branch**: `003-github-actions` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-github-actions/spec.md`

## Summary

One GitHub Actions workflow, `.github/workflows/build.yml`, replaces `cicd/build.yml` and
`cicd/release.yml` ([research.md](research.md) R1).

- **`check` job:** runs on every pull request and every push to `main`. It fails if `<Version>`
  isn't greater than the highest release (R4), then builds and runs every test in the `Stress`
  configuration with Cobertura coverage. It fails if line or branch coverage is lower than in the
  `coverage.cobertura.xml` attached to the highest release (R5).
- **`publish` job:** on `main` only. It pushes the package to NuGet.org with Trusted Publishing
  from the `Prod` environment, with no stored key and no `--skip-duplicate` (R3).
- **`release` job:** on `main` only. It creates the GitHub release with the package and coverage
  report attached, which creates the tag last (R9).

Because publishing and releasing are separate jobs, "Re-run failed jobs" recovers a failed release
without touching NuGet.org.

The two checks are small PowerShell scripts, specified first by Pester tests (R6). Tool versions
float (R2).

Other changes in the repository:
- The README badge points at the new workflow (R10).
- Stress's constitution names it, as version 2.0.2.
- `cicd/` is deleted.
- The release is 5.0.6 (R12).

Outside the repository, the maintainer creates `Prod` and the nuget.org policy, attaches a 5.0.5
coverage report, enables the `main` ruleset with `check` required, and disables Azure DevOps before
merging. The order is in [quickstart.md](quickstart.md) (R11, R13).

## Technical Context

**Language/Version**: GitHub Actions workflow YAML. PowerShell scripts compatible with Windows
PowerShell 5.1 and PowerShell 7. The package itself stays C# on .NET 10, unchanged.

**Primary Dependencies**:
- Actions, at major versions: `actions/checkout@v7`,
  `actions/upload-artifact@v7`, `actions/download-artifact@v8`, `NuGet/login@v1`.
- The .NET 10 SDK, the `gh` CLI, and Pester 5, all preinstalled on the runners.
- Microsoft's code coverage collector, already used through `Microsoft.NET.Test.Sdk`.

**Storage**: GitHub release assets only: `coverage.cobertura.xml` on each release from 5.0.5 on.

**Testing**:
- Pester 5 tests for the two check scripts, in `.github/scripts/*.Tests.ps1`, run by `check` and
  locally.
- The existing NUnit suite, unchanged.
- The quickstart's deliberate failures on the pull request.

**Target Platform**: GitHub-hosted runners, `windows-latest` for `check` and `ubuntu-latest` for
`publish` and `release`.

**Project Type**: Library (NuGet package); this feature changes its build and release only.

**Performance Goals**: The version check fails within the first minutes of a run. The full run is
bounded by the Stress configuration's hour, as today, with no second test run.

**Constraints**:
- No stored NuGet credential.
- No storage outside GitHub releases.
- No pinned tool versions.
- No change to the package's code, so coverage of `DnDGen.Stress.dll` stays at 100%.

**Scale/Scope**:
- New: 1 workflow with 3 jobs; 2 scripts with 2 Pester test files.
- Edited: 1 README line, 1 constitution sentence, the csproj version and notes.
- Deleted: 2 files.
- Done by hand outside the repository: 6 steps.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Checked against the [DnDGen constitution](https://github.com/DnDGen/.github/blob/main/constitution/CONSTITUTION.md)
1.0.0, read live on 2026-11-09, and Stress's additions 2.0.1.

| Rule | Applies? | Status |
|------|----------|--------|
| **I.1** Emulate what exists | Yes | Pass. Keeps today's pipeline behaviour: Windows build, the `Stress` configuration, `CodeCoverage.runsettings`, `-v normal`, a release named after the version with generated notes and the package attached. Takes the prototype's shape where it fits (R1). Reuses the existing `main` ruleset (R11). No DnDGen repository uses GitHub Actions yet, so there is nothing further to emulate. |
| **I.2** A new pattern must be worth it | Yes | Pass, with justification. The new patterns are GitHub Actions itself, the checks against the latest release, tagging last, and Pester for pipeline scripts. Each one is either needed (no keyless publishing from Azure Pipelines, issue #58), enforces an existing rule that was only shown before (II.2.c, version bumps), or gives pipeline logic the same test-first treatment as code (R6). Each is recorded here as the pattern for every DnDGen package (spec FR-013). None is extracted to DnDGen/.github until a second repository uses it. |
| **I.3** Code reads like prose; no comments | Yes | Pass. Workflow steps have descriptive names; scripts are named for what they assert; no comments are added. |
| **II** Tests first | Yes | Pass. The scripts are specified by Pester tests written and seen to fail first. The workflow's gates can't be run before the workflow exists, so they are specified by the quickstart's deliberate failures (step 6), written now and run on the pull request as soon as the workflow is pushed. |
| **II.1** What to test | Yes | Pass. Each script case is what the maintainer sees: a version that is or isn't accepted, coverage that does or doesn't pass, each with its own case (II.1.c). |
| **II.2** How much to test | Yes | Pass. Version boundaries are tested at, below, and above the highest release, including `5.0.10` against `5.0.9` and the `v` prefix. Coverage boundaries are tested at equal, one line or branch more, one less, and missing (II.2.b.iii, [contracts/scripts.md](contracts/scripts.md)). **II.2.c (Stress)**: no package code changes, so coverage stays at 100%. The new gate enforces II.2.c from now on. |
| **II.3** How a test fails | Yes | Pass. Every failure, whether a script, its tests, or a gate in the job, states both values. Script tests compare whole messages (II.3.a). The script tests take inputs as parameters, so they don't depend on the network, GitHub, or the machine (II.3.c). |
| **III.1** Maintenance in scope | Yes | Pass. Updates the build platform (III.1.a), replaces the outdated pipeline (III.1.b), and corrects the constitution's stale sentence (III.1.c). |
| **III.2** Improve where you are, in its own commit | Yes | Pass. Scripts with tests, workflow, README, constitution, removal of `cicd/`, and the release are separate commits. |
| **III.3** Behaviour out of scope | Yes | Pass. The iterations-per-second gate is recorded as out of scope, to become its own issue (spec, Assumptions). |
| **Stack** | Yes | Pass. `net10.0` unchanged; the SDK floats within .NET 10. |
| **Versioning and release** | Yes | Pass. PATCH, 5.0.6, for a pipeline change, and the README badge is the only change to the package. The notes say so, name 5.0.5, and point to the releases page. Titled `5.0.6: Build and release on GitHub Actions`. Stress's constitution names the new pipeline (FR-010). |
| **README as contract** | Yes | Pass. Only the badge changes. The README tests pass unchanged. Theme song check: no update needed (R12). |
| **Development workflow** | Yes | Pass. Branch and pull request to `main`, reviewed by `cidthecoatrack`. From now on, merging requires `check` to pass. |
| **Governance: compliance** | Yes | Pass. This check cites the DnDGen constitution's labels. Stress's constitution amendment is PATCH, 2.0.1 to 2.0.2. |

**Post-design re-check**: The Phase 1 design adds no violations. The new patterns are justified
under I.2 above, so Complexity Tracking stays empty.

## Project Structure

### Documentation (this feature)

```text
specs/003-github-actions/
├── plan.md                      # This file
├── research.md                  # Phase 0: R1–R13
├── data-model.md                # Phase 1: versions, coverage report, release states, settings
├── quickstart.md                # Phase 1: steps outside the repository, in order, and checks
├── contracts/
│   ├── workflow.md              # Phase 1: triggers, jobs, permissions, recovery
│   ├── scripts.md               # Phase 1: check scripts' parameters, messages, test cases
│   └── repository-changes.md    # Phase 1: README, constitution, csproj, removals, pull request
├── checklists/
│   └── requirements.md          # Spec quality checklist
└── tasks.md                     # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
.github/
├── workflows/
│   └── build.yml                           # New: check, publish, release
└── scripts/
    ├── Assert-VersionIsNewer.ps1           # New
    ├── Assert-VersionIsNewer.Tests.ps1     # New
    ├── Assert-CoverageNotLower.ps1         # New
    └── Assert-CoverageNotLower.Tests.ps1   # New
.specify/memory/constitution.md             # Versioning and release sentence; 2.0.2
README.md                                   # Build badge
DnDGen.Stress/DnDGen.Stress.csproj          # Version 5.0.6 and release notes
cicd/                                       # Deleted
```

**Structure Decision**: GitHub requires workflows in `.github/workflows/`, and the scripts sit beside
them in `.github/scripts/`, which keeps the pipeline in one folder now that `cicd/` is gone. The
package and its tests are untouched.

## Complexity Tracking

No violations.
