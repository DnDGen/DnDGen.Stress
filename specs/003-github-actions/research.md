# Research: Build and Release on GitHub Actions

Decisions made while planning [spec.md](spec.md). Labels such as I.1 and II.3.a cite the
[DnDGen constitution](https://github.com/DnDGen/.github/blob/main/constitution/CONSTITUTION.md);
labels ending in (Stress) cite this repository's additions. Facts about outside services were
checked on 2026-11-09.

## R1: One workflow, three jobs

- **Decision**: One workflow, `.github/workflows/build.yml`, named `Build`, triggered by
  `pull_request` to `main` and `push` to `main`. It has three jobs:
  1. **`check`** (`windows-latest`), every run: checks the version (R4), builds, runs every test
     in the `Stress` configuration with coverage, runs the Pester tests (R6), checks coverage (R5),
     and uploads the package and coverage report as run artifacts.
  2. **`publish`** (`ubuntu-latest`), pushes to `main` only, needs `check`, runs in the `Prod`
     environment: logs in with Trusted Publishing (R3) and pushes the package to NuGet.org, without
     `--skip-duplicate` (FR-008).
  3. **`release`** (`ubuntu-latest`), pushes to `main` only, needs `check` and `publish`: creates
     the GitHub release, which creates the tag (R9).
- **Rationale**: Publishing and releasing are separate jobs so that GitHub's **Re-run failed
  jobs**, which keeps successful jobs and re-runs the failed ones and their dependents, re-runs only
  `release` when it fails after `publish` succeeded (FR-008). `check` runs on Windows, as
  `cicd/build.yml` does today (I.1). `publish` and `release` only move files and call web services,
  so they run on Linux, which starts faster. Only `publish` gets the OIDC token permission and the
  environment; only `release` gets write access to the repository's contents (FR-006).
- **Alternatives considered**:
  - *The prototype's two jobs* (build tags; publish pushes and releases): tags before publishing,
    and a failed release can't be retried without hitting NuGet.org's duplicate rejection.
  - *Two workflows* (check, then a release workflow triggered by its completion), like
    `cicd/build.yml` and `cicd/release.yml`: the Trusted Publishing policy names one workflow file,
    and a `workflow_run` trigger adds indirection without benefit.

## R2: Tool versions float

- **Decision**: Runners are `windows-latest` and `ubuntu-latest`, using the .NET SDK the image
  already has. Actions are referenced by their major version tag, the latest on 2026-11-09:
  `actions/checkout@v7`, `actions/upload-artifact@v7`, `actions/download-artifact@v8`,
  `NuGet/login@v1`. The .NET 10 SDK, the `gh` CLI, and Pester are preinstalled on both runner
  images.
- **No `actions/setup-dotnet`**: on the first run it found SDK 10.0.403 already installed on
  `windows-2025-vs2026` and did nothing. .NET 10 is supported until November 2028, and an image
  without it would fail the build loudly. Add the step back when the package targets a .NET
  version the image doesn't have yet.
- **Rationale**: FR-014. A major tag picks up every minor and patch release automatically. Actions
  publish no `latest` tag; the only floating alternative is a branch such as `@main`, which runs
  unreleased code. A new major version is a one-line change, made as maintenance (III.1.a) when
  GitHub warns about the old one, as it did for the prototype's `@v4` actions on Node 20.
- **Alternatives considered**: *Commit SHAs or exact versions*: rejected by the spec. *`@main`*:
  unreleased, and can break between releases.

## R3: Trusted Publishing

- **Decision**: `publish` runs in a new GitHub environment, `Prod`, with permission
  `id-token: write`. `NuGet/login@v1` exchanges the run's OIDC token for a NuGet API key valid for
  one hour, and `dotnet nuget push` uses it with `--source https://api.nuget.org/v3/index.json`.
  The `user` input is the nuget.org profile name `cidthecoatrack`, written in the workflow. The
  nuget.org policy is owned by `cidthecoatrack`, the package's only owner, with Repository Owner
  `DnDGen`, Repository `DnDGen.Stress`, Workflow File `build.yml` (file name only), and
  Environment `Prod`. `Prod` allows deployments from `main` only.
- **Rationale**: FR-005. Per Microsoft's
  [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) page,
  the policy matches on owner, repository, and workflow file, with environment optional; the key
  lasts one hour, and each token buys one key. Naming the environment in the policy, and limiting
  the environment to `main`, means a run of the same file from any other branch can't publish
  (FR-006). The profile name is public (it is on the package page), so it isn't a secret, and
  writing it in the workflow saves a setting outside the repository. A policy for a public
  repository is active immediately; the 7-day pending window applies mostly to private ones.
- **Alternatives considered**: *A `NUGET_USER` secret*, as the prototype and Microsoft's example
  do: secrets are for values whose disclosure matters, and this one is public; it adds a manual
  step and masks the name in logs. The username grants nothing alone: publishing needs this
  repository's, workflow's, and environment's OIDC token. *A repository variable*
  (`vars.NUGET_USER`), GitHub's place for non-secret settings: worth it only if the value changed
  between runs or repositories, and every DnDGen package has the same owner. *No environment*: any branch running
  `build.yml` could then publish, guarded only by the job's `if`.

## R4: Version check

- **Decision**: A script, `.github/scripts/Assert-VersionIsNewer.ps1`, reads `<Version>` from
  `DnDGen.Stress/DnDGen.Stress.csproj`, lists every release's tag with `gh release list`, strips a
  leading `v`, and fails, stating both versions, unless `<Version>` is greater than the highest
  tag, compared as `[version]`. It runs first in `check`, so a missing bump fails in seconds, on
  pull requests and on `main` alike (FR-007).
- **Rationale**: Comparing as `[version]` orders `5.0.10` after `5.0.9`, which text or date order
  doesn't, and handles the old `v3.0.0` tag once the `v` is stripped. Every DnDGen version is
  `X.Y.Z` with no pre-release suffix, so `[version]` is enough, and unlike `SemanticVersion` it
  exists in Windows PowerShell 5.1 as well as PowerShell 7, so the maintainer can run the script
  and its tests locally. Releases and tags are the same 25 versions today; releases are what the
  spec calls "released".
- **Alternatives considered**: *`git tag`*: needs a full-history checkout (the default fetches one
  commit, no tags). *`SemanticVersion`*: PowerShell 7 only, for pre-release ordering DnDGen
  doesn't use. *NuGet.org's version list*: NuGet.org isn't the release record, and its duplicate
  rejection already backs this check up (spec, Edge Cases).

## R5: Coverage report and check

- **Decision**: Tests run with `--collect "Code Coverage;Format=cobertura" --settings
  CodeCoverage.runsettings`, so Microsoft's collector writes a Cobertura report, limited to
  `DnDGen.Stress.dll` by the existing runsettings. A script,
  `.github/scripts/Assert-CoverageNotLower.ps1`, copies the report to `coverage.cobertura.xml`,
  downloads the asset of the same name from the highest release (R4), and fails, stating both
  figures, if `lines-covered / lines-valid` or `branches-covered / branches-valid` is lower than the
  release's, or if the release has no such asset (FR-003). Fractions are compared exactly, by
  cross-multiplying the counts. The report is uploaded as a run artifact, and `release` attaches it
  to the GitHub release (FR-004).
- **Rationale**: A local run on 2026-11-09 confirmed the report's root element carries
  `lines-covered="215" lines-valid="215" branches-covered="58" branches-valid="58"`, so no extra
  tool is needed. Comparing counts avoids the rounding in `line-rate`, so any drop fails, as II.2.c
  requires. Cobertura is a standard text format that stays readable across releases, and
  release assets don't expire, unlike run artifacts.
- **Alternatives considered**:
  - *Measuring `main` in the same run*: rejected in Clarifications.
  - *The binary `.coverage` format*: needs Microsoft's tools to read.
  - *Comparing `line-rate`*: rounded, so a small drop could pass.
  - *A coverage action from the Marketplace*: a dependency for a few lines of PowerShell.

## R6: The scripts are tested with Pester

- **Decision**: The version and coverage checks are PowerShell scripts in `.github/scripts/`, each
  with a Pester 5 test file beside it (`*.Tests.ps1`). `check` runs them with a Pester
  configuration (`Run.Path = '.github/scripts'`, `Run.Exit = $true`, detailed output, no result
  file) before the scripts are used. The scripts take their
  inputs as parameters (versions, report paths), and the workflow passes in what `gh` and the
  build produced, so the tests need no network or GitHub.
- **Rationale**: The two checks are behaviour with boundaries: equal, lower, and higher versions;
  `5.0.10` against `5.0.9`; the `v` prefix; a drop of one line or one branch; a missing baseline
  report. The constitution defines behaviour by tests first (II) and tests boundaries at, just
  inside, and past them (II.2.b.iii). Pester 5 is preinstalled on the runners and installed on the
  maintainer's machine. Scripts written for Windows PowerShell 5.1 also run in PowerShell 7.
- **Alternatives considered**: *Inline workflow scripts, checked only by the quickstart's
  deliberate failures*: the checks would be specified by a one-off manual run, and a later edit
  could break a boundary unnoticed. *C# tests in `DnDGen.Stress.Tests`*: would mix pipeline logic
  into the package's test assembly, and its test counts.
- **New pattern (I.2)**: Pester and `.github/scripts/` are new to DnDGen. They give pipeline logic
  the same test-first treatment as package code (I.2.c), and keep it out of the package's test
  assembly. Like the workflow, they stay in Stress until a second repository needs them.

## R7: Test failures in the log

- **Decision**: Tests run with `-v normal`, as today, and no test report is published.
- **Rationale**: FR-002 only needs a failed test's name and message readable in the run, which the
  test console output already prints in the `check` job's log. Storing or rendering results is out
  of scope.
- **Alternatives considered**: *A test-report action* rendering `.trx` files: the "display" value
  the maintainer said wasn't needed.

## R8: Concurrency

- **Decision**: The workflow's concurrency group is the workflow and ref
  (`${{ github.workflow }}-${{ github.ref }}`), with `cancel-in-progress` only for pull requests.
- **Rationale**: Pushing to a pull request cancels its superseded hour-long check. On `main`, a
  run never starts while another is in progress, so releases happen in merge order. GitHub keeps
  only one pending run per group: with three merges inside one run's time, the middle one is
  cancelled, and its version is skipped, because the third's version check passes. That is
  intended: the third release contains the second's changes, so releasing the second adds nothing.
- **Alternatives considered**: *No concurrency*: two releases could run at once. *Cancelling on
  `main`*: would cancel a release part-way.

## R9: Creating the release and tag

- **Decision**: `release` downloads the run's package and coverage report and runs
  `gh release create <version> <package> coverage.cobertura.xml --target <commit> --title <version>
  --generate-notes`. Without `--verify-tag`, GitHub creates the tag at `--target`.
- **Rationale**: FR-004. The tag is created with the release, after NuGet.org has the package, so
  a tag means the version is on NuGet.org. GitHub's generated notes list the merged pull requests
  since the previous release, the GitHub equivalent of today's Azure-generated commit list, so the
  practice is unchanged (Clarifications). Existing releases have the version as tag and an empty
  title, which GitHub shows as the tag; setting the title to the version shows the same.
- **Alternatives considered**: *`git tag` and `git push`*, as today: needs `contents: write` in a
  job that also runs the build, and tags before publishing.

## R10: README badge

- **Decision**: The badge becomes
  `[![Build Status](https://github.com/DnDGen/DnDGen.Stress/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/DnDGen/DnDGen.Stress/actions/workflows/build.yml?query=branch%3Amain)`,
  with the Theme Song badge after it as today.
- **Rationale**: FR-009. The alt text stays `Build Status`, which
  `ThemeSong_HasBadgeBesideTheBuildBadge` looks for. nuget.org's
  [allowed image domains](https://learn.microsoft.com/en-us/nuget/nuget-org/package-readme-on-nuget-org)
  include `github.com/.../workflows/.../badge.svg`, so it should render on the package page too;
  the quickstart confirms it on 5.0.6's page.

## R11: Merge blocking

- **Decision**: The existing `main` ruleset (id 13204315, currently disabled, blocking deletion
  and force pushes) is enabled, with a "Require status checks to pass" rule for the `check` job,
  and "Require branches to be up to date" left off (FR-016). It is enabled after this pull
  request's first run, so the check exists to be selected.
- **Rationale**: Reuses the ruleset that already exists (I.1). A required check can only be chosen
  once GitHub has seen it run.

## R12: Release notes, version, and documents

- **Decision**: `<Version>` becomes `5.0.6`. `<PackageReleaseNotes>` says the build moved to
  GitHub Actions, that the package changes only in its README's build badge, names 5.0.5 as the
  last version with other changes, and points to https://github.com/DnDGen/DnDGen.Stress/releases.
  The pull request is titled `5.0.6: Build and release on GitHub Actions`. Stress's constitution
  replaces the `cicd/` sentence in Versioning and release (wording in
  [contracts/repository-changes.md](contracts/repository-changes.md)) and goes from 2.0.1 to 2.0.2,
  a PATCH, as a correction of fact.
- **Rationale**: The README is packed into the package, so the badge change does change the
  package; the DnDGen constitution asks for the notes to say what changed and name the last version
  that changed otherwise. The spec's FR-015 is corrected to match.
- **Theme song**: The badge line has no quoted phrase in the song's Sources, and the badge doesn't
  change the README's ideas or vibe, so under its Updating check the song needs no update. The pull
  request records the check.

## R13: Order of the cut-over

- **Decision**: The steps outside the repository, in order, are in [quickstart.md](quickstart.md):
  create `Prod`, create the nuget.org policy, attach a `5.0.5` coverage report to its release, open
  the pull request, enable the ruleset, disable both Azure DevOps pipelines, merge, verify, then
  revoke the old NuGet API key and Azure DevOps service connections.
- **Rationale**: Everything the first release needs exists before the merge, and the Azure DevOps
  pipelines are disabled before the merge, so only one system releases 5.0.6 (User Story 3). The
  old key is revoked last, once the new path has published, so there is a fallback until then.
- **`5.0.5` coverage report**: produced by running the same test command at tag `5.0.5` and
  uploading the report with `gh release upload 5.0.5 coverage.cobertura.xml`. The package's code is
  the same at `5.0.5` and on this branch, so the pull request's check compares like with like.
