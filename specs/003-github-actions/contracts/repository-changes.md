# Contract: Repository changes outside the workflow

## README.md

The build badge, line 4, from:

```markdown
[![Build Status](https://dev.azure.com/dndgen/DnDGen/_apis/build/status%2FDnDGen.Stress%20-%20Build?branchName=main)](https://dev.azure.com/dndgen/DnDGen/_build/latest?definitionId=11&branchName=main) [![Theme Song](...)](...)
```

to:

```markdown
[![Build Status](https://github.com/DnDGen/DnDGen.Stress/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/DnDGen/DnDGen.Stress/actions/workflows/build.yml?query=branch%3Amain) [![Theme Song](...)](...)
```

The Theme Song badge is unchanged. Nothing else in the README changes: "This repository's own
build pipeline does exactly that" is still true (FR-009, [research.md](../research.md) R10).

## `.specify/memory/constitution.md`

In **Versioning and release**, the sentence starting "On `main`, `cicd/build.yml`…" becomes:

> `.github/workflows/build.yml` checks every pull request and every push to `main`: it builds and
> runs every test in the `Stress` configuration with code coverage, and fails if `<Version>` isn't
> greater than the latest release's, or if coverage is lower than the latest release's coverage
> report. On `main`, it then publishes to NuGet.org with Trusted Publishing and creates a GitHub
> release, tagged with `<Version>`, with the package and coverage report attached.

The footer becomes `**Version**: 2.0.2 | **Ratified**: 2026-10-23 | **Last Amended**: 2026-11-09`
(PATCH: a correction of fact, FR-010).

## `DnDGen.Stress/DnDGen.Stress.csproj`

- `<Version>5.0.6</Version>`
- `<PackageReleaseNotes>`: "Moved the build and release to GitHub Actions. The package changes only
  in its README's build badge; 5.0.5 was the last version with other changes. See every version's
  changes at https://github.com/DnDGen/DnDGen.Stress/releases" (FR-015, R12).

## Removed

- `cicd/build.yml`, `cicd/release.yml`, and the `cicd/` folder (FR-011).

## Added

- `.github/workflows/build.yml` ([workflow.md](workflow.md)).
- `.github/scripts/Assert-VersionIsNewer.ps1` and `Assert-VersionIsNewer.Tests.ps1`.
- `.github/scripts/Assert-CoverageNotLower.ps1` and `Assert-CoverageNotLower.Tests.ps1`
  ([scripts.md](scripts.md)).

## Pull request

- Title: `5.0.6: Build and release on GitHub Actions`, reviewer `cidthecoatrack`.
- The description lists the steps outside the repository from [quickstart.md](../quickstart.md),
  which must be done before merging, records the theme song check (no update needed, R12), and
  closes #58.
