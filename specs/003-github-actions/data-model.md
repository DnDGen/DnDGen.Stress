# Data Model: Build and Release on GitHub Actions

There is no stored data beyond GitHub releases. These are the things the workflow reads and
writes, and how a release moves through its states.

## Version

- **Source**: `<Version>` in `DnDGen.Stress/DnDGen.Stress.csproj`, such as `5.0.6`.
- **Format**: `X.Y.Z`, compared as `[version]` ([research.md](research.md) R4).
- **Rule**: Greater than the highest released version, or the check fails (FR-007).

## Released version

- **Source**: The tag of each GitHub release, listed with `gh release list`. A leading `v` is
  ignored, so `v3.0.0` is `3.0.0`.
- **Highest released version**: The greatest of them as `[version]`, not the one GitHub flags as
  Latest and not the newest by date. It is the baseline for both checks.

## Coverage report

- **What**: A Cobertura XML report for `DnDGen.Stress.dll`, named `coverage.cobertura.xml`.
- **Fields used**: The root `coverage` element's `lines-covered`, `lines-valid`,
  `branches-covered`, and `branches-valid` attributes.
- **Where**: Produced by every `check` run; uploaded as a run artifact; attached to every release
  from 5.0.6 on, and by hand to 5.0.5 ([research.md](research.md) R13).
- **Rule**: The change's line fraction and branch fraction are each at least the highest release's,
  compared exactly; a missing baseline fails (FR-003).

## Package

- **What**: `DnDGen.Stress.<Version>.nupkg`, built by `check` in the `Release` configuration
  (`GeneratePackageOnBuild`), uploaded as a run artifact, pushed to NuGet.org by `publish`, and
  attached to the release by `release`.

## Release

The tag, the NuGet.org listing, and the GitHub release for one version.

```text
check passed ──► published (on NuGet.org, no tag) ──► released (GitHub release + tag)
     │                     │
     └─ check fails:       └─ release job fails: re-run failed jobs → released
        nothing happens       re-run all jobs → publish fails (duplicate), stays published
```

- **Published**: NuGet.org has the version. The checks don't see it yet, because they read
  releases.
- **Released**: The GitHub release exists with the package and coverage report attached, and the
  tag points at the released commit. It becomes the baseline for the next change.

## Trusted Publishing policy (nuget.org)

| Field | Value |
|---|---|
| Owner | `cidthecoatrack` |
| Repository Owner | `DnDGen` |
| Repository | `DnDGen.Stress` |
| Workflow File | `release.yml` |
| Environment | `Prod` |

## Release environment (GitHub)

- **Name**: `Prod`.
- **Deployment branches**: `main` only.
- **Required reviewers**: none; every merge releases automatically (spec, Assumptions).

## `main` ruleset (GitHub)

- **Existing**: id 13204315, targeting the default branch, blocking deletion and force pushes;
  currently disabled.
- **After**: active, also requiring the `check` job to pass, without requiring branches to be up to
  date (FR-016).
