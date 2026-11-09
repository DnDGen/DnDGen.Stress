---

description: "Task list for moving the build and release to GitHub Actions"
---

# Tasks: Build and Release on GitHub Actions

**Input**: Design documents from `specs/003-github-actions/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: The two check scripts are specified by Pester tests, written first with the
karls-test-style skill and seen to fail for the expected reason before the scripts exist (II,
[research.md](research.md) R6). The workflow itself is validated on the pull request by the
quickstart's deliberate failures.

**Organization**: Tasks are grouped by user story. US1 and US2 are both P1; US2 comes first here
because `publish` and `release` (US1) run only after `check` (US2) passes. US3 and US4 follow in
priority order. The cut-over, done mostly by the maintainer outside the repository, is the final
phase, in the order the quickstart requires.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)
- **(maintainer)**: Only the maintainer can do this task. `/speckit-implement` stops and hands it
  over.

## Path Conventions

- Workflow: `.github/workflows/build.yml`
- Check scripts and their Pester tests: `.github/scripts/`
- Commits are separate per concern (III.2): spec documents, scripts with their tests, workflow,
  README, constitution, removal of `cicd/`, and the release.

---

## Phase 1: Setup

- [X] T001 Commit the feature's spec documents in `specs/003-github-actions/` as their own commit.

---

## Phase 2: Foundational

**Purpose**: Agree the script test lists, and create the workflow every story adds jobs to.

**⚠️ CRITICAL**: No Pester test is written until T002 is done.

- [X] T002 (maintainer) Approve the Pester test lists in
  [contracts/scripts.md](contracts/scripts.md), each case written as a consumer sentence (II.1.a),
  the consumer being a workflow run checking a change:
  - `Assert-VersionIsNewer`: against a highest release of `5.0.5`, `5.0.6`, `5.1.0`, and `6.0.0`
    pass; `5.0.5` and `5.0.4` fail with "Version <Version> must be greater than the highest
    released version, <highest>."; `5.0.10` passes against `5.0.9`; `v3.0.0` counts as `3.0.0`;
    the highest release is used whatever the list's order; an empty list passes.
  - `Assert-CoverageNotLower`: equal passes; one more covered line passes; one fewer covered line
    fails; one fewer covered branch fails; both dropping reports both; 430/430 against 215/215
    passes; 216/217 against 215/215 fails; a missing baseline fails with "No coverage report for
    the latest release: <BaselinePath>."; zero valid branches on both sides passes.
- [ ] T003 Create `.github/workflows/build.yml` with `name: Build`, triggers `pull_request` to
  `main` and `push` to `main`, workflow-level `permissions: contents: read`, and concurrency group
  `${{ github.workflow }}-${{ github.ref }}` with `cancel-in-progress` only for `pull_request`
  ([contracts/workflow.md](contracts/workflow.md), [research.md](research.md) R8).

**Checkpoint**: Test lists agreed; the workflow file exists for jobs to be added.

---

## Phase 3: User Story 2 - Every change is checked, and can't make things worse (Priority: P1)

**Goal**: Every pull request and push runs `check`, which fails on a failing test, a version not
greater than the highest release, or coverage lower than the highest release's report.

**Independent Test**: On the pull request, a version of `5.0.5`, a deliberately failing test, and
a deleted test that alone covers a branch each make `check` fail and say why (quickstart step 6).

### Tests for User Story 2 (write first, see fail)

- [ ] T004 [P] [US2] Write `.github/scripts/Assert-VersionIsNewer.Tests.ps1` (Pester 5, Windows
  PowerShell 5.1 and PowerShell 7) with T002's cases, comparing whole messages (II.3.a); run
  `Invoke-Pester -Path .github/scripts/Assert-VersionIsNewer.Tests.ps1` and see every case fail
  because the script doesn't exist.
- [ ] T005 [P] [US2] Write `.github/scripts/Assert-CoverageNotLower.Tests.ps1` with T002's cases,
  building small Cobertura files in Pester's `TestDrive:` with root attributes `lines-covered`,
  `lines-valid`, `branches-covered`, and `branches-valid`; see every case fail because the script
  doesn't exist.

### Implementation for User Story 2

- [ ] T006 [US2] Write `.github/scripts/Assert-VersionIsNewer.ps1` with parameters `-Version` and
  `-ReleasedVersions`: strip a leading `v`, compare as `[version]`, pass when `-Version` is greater
  than every released version or there are none, otherwise throw "Version <Version> must be
  greater than the highest released version, <highest>."; on success write one line saying what
  passed. T004's tests pass. No comments (I.3).
- [ ] T007 [US2] Write `.github/scripts/Assert-CoverageNotLower.ps1` with parameters `-ReportPath`
  and `-BaselinePath`: throw "No coverage report for the latest release: <BaselinePath>." if the
  baseline is missing; compare lines and branches fractions exactly by cross-multiplying the
  counts; throw, for each that dropped, "Line coverage dropped from <b covered>/<b valid> (<b %>)
  to <c covered>/<c valid> (<c %>)." and likewise for branches, both in one message when both
  dropped; on success write one line with both figures. T005's tests pass.
- [ ] T008 [US2] Run `Invoke-Pester -Path .github/scripts` locally in Windows PowerShell and see
  every test pass; commit the scripts and their tests as one commit.
- [ ] T009 [US2] Add job `check` to `.github/workflows/build.yml`, in the order of
  [contracts/workflow.md](contracts/workflow.md): `runs-on: windows-latest`; checkout
  (`actions/checkout@v7`); .NET `10.x` (`actions/setup-dotnet@v6`); `Invoke-Pester -Path
  .github/scripts -CI`, with Pester's result file written under `TestResults/` (or not at all) so
  nothing stray lands in the working directory; read `<Version>` from `DnDGen.Stress/DnDGen.Stress.csproj`, list release
  tags with `gh release list --limit 1000` (`GH_TOKEN: ${{ github.token }}`), run
  `Assert-VersionIsNewer.ps1`, and expose the version and highest release tag as step and job
  outputs; `dotnet build DnDGen.Stress/DnDGen.Stress.csproj --configuration Release`; `dotnet test
  DnDGen.Stress.Tests/DnDGen.Stress.Tests.csproj -v normal --configuration Stress --collect "Code
  Coverage;Format=cobertura" --settings CodeCoverage.runsettings --results-directory TestResults`;
  copy the `*.cobertura.xml` to `coverage.cobertura.xml`; `gh release download <highest>
  --pattern coverage.cobertura.xml --dir baseline` without failing the step if absent; run
  `Assert-CoverageNotLower.ps1`; upload `DnDGen.Stress/bin/Release/DnDGen.Stress.*.nupkg` and
  `coverage.cobertura.xml` as artifacts with `actions/upload-artifact@v7`. Commit the workflow.

**Checkpoint**: Locally, Pester passes. On GitHub, `check` validates once the pull request is open
(T022).

---

## Phase 4: User Story 1 - A merge releases without a stored key (Priority: P1) 🎯 MVP

**Goal**: A merge to `main` publishes to NuGet.org with Trusted Publishing, then creates the GitHub
release and tag, with the package and coverage report attached.

**Independent Test**: The merge releases 5.0.6 once: NuGet.org lists it, and its GitHub release has
the package, `coverage.cobertura.xml`, and tag `5.0.6` (quickstart step 10).

- [ ] T010 [US1] Add job `publish` to `.github/workflows/build.yml`: `needs: check`, `if:
  github.repository == 'DnDGen/DnDGen.Stress' && github.event_name == 'push' && github.ref ==
  'refs/heads/main'`, so a fork's own `main` never publishes (FR-006), `runs-on: ubuntu-latest`,
  `environment: Prod`, `permissions: id-token: write, contents: read`; download the package
  artifact (`actions/download-artifact@v8`); `NuGet/login@v1` with `user: cidthecoatrack`;
  `dotnet nuget push` with `${{ steps.login.outputs.NUGET_API_KEY }}` to
  `https://api.nuget.org/v3/index.json`, with no `--skip-duplicate` (FR-005, FR-008, R3).
- [ ] T011 [US1] Add job `release`: `needs: [check, publish]`, the same `if`, `runs-on:
  ubuntu-latest`, `permissions: contents: write`; download the package and coverage artifacts;
  `gh release create <version> <package> coverage.cobertura.xml --target ${{ github.sha }} --title
  <version> --generate-notes`, with the version from `check`'s output and `GH_TOKEN: ${{
  github.token }}` (FR-004, R9). Commit.

**Checkpoint**: The workflow is complete; it can only be exercised for real by the merge (T026).

---

## Phase 5: User Story 3 - Only one system releases, and the old one is gone (Priority: P2)

**Goal**: The repository, README, and constitution describe only the new build.

**Independent Test**: `dotnet test` passes, including `ReadmeTests`, and
`git grep -n -e "dev.azure.com" -e "cicd/" -- ":!specs"` finds nothing (quickstart step 11).

- [ ] T012 [P] [US3] Replace the build badge on line 4 of `README.md` with the GitHub Actions badge
  in [contracts/repository-changes.md](contracts/repository-changes.md), keeping the
  `[![Build Status]` alt text and the Theme Song badge after it (FR-009). Commit.
- [ ] T013 [P] [US3] In `.specify/memory/constitution.md`, replace the Versioning and release
  sentence starting "On `main`, `cicd/build.yml`" with the wording in
  [contracts/repository-changes.md](contracts/repository-changes.md), and set the footer to
  `**Version**: 2.0.2 | **Ratified**: 2026-10-23 | **Last Amended**: 2026-11-09` (FR-010). Commit.
- [ ] T014 [P] [US3] Delete `cicd/build.yml` and `cicd/release.yml` (FR-011). Commit.
- [ ] T015 [US3] In `DnDGen.Stress/DnDGen.Stress.csproj`, set `<Version>5.0.6</Version>` and
  `<PackageReleaseNotes>` to the notes in
  [contracts/repository-changes.md](contracts/repository-changes.md) (FR-015). Commit.
- [ ] T016 [US3] Run `dotnet test` with no `--filter` and see every test pass, including the README
  and theme song tests; run the `git grep` above and see no reference to `dev.azure.com` or
  `cicd/` outside `specs/`.

**Checkpoint**: The repository is ready for the pull request.

---

## Phase 6: User Story 4 - Stress sets the release pattern for every DnDGen package (Priority: P3)

**Goal**: This feature's records name each release practice, the principle it meets, and the steps
outside the repository in order.

**Independent Test**: Reading spec, plan, research, and quickstart alone, each practice and each
step can be applied to another package (SC-006).

- [ ] T017 [US4] Compare `.github/workflows/build.yml` and the scripts with
  [research.md](research.md), [contracts/](contracts/), and [quickstart.md](quickstart.md), and
  correct the documents wherever the implementation differs, so that they stay an accurate example
  (FR-013). Commit with the spec documents if anything changed.

---

## Phase 7: Cut-over

**Purpose**: The steps outside the repository, in [quickstart.md](quickstart.md)'s order, around
the pull request.

- [ ] T018 (maintainer) Create the `Prod` environment in DnDGen/DnDGen.Stress, deployments from
  `main` only, no required reviewers (quickstart step 1).
- [ ] T019 (maintainer) Create the nuget.org Trusted Publishing policy: Owner `cidthecoatrack`,
  Repository Owner `DnDGen`, Repository `DnDGen.Stress`, Workflow File `build.yml`, Environment
  `Prod` (quickstart step 2).
- [ ] T020 Produce the 5.0.5 coverage report from tag `5.0.5` with the quickstart step 3 commands,
  and confirm its root shows every line and branch covered. Uploading it to the 5.0.5 release is
  public: ask the maintainer before running `gh release upload 5.0.5 coverage.cobertura.xml`.
- [ ] T021 Push the branch and open the pull request, titled `5.0.6: Build and release on GitHub
  Actions`, with reviewer `cidthecoatrack`, the description from
  [contracts/repository-changes.md](contracts/repository-changes.md) (the steps outside the
  repository, the theme song check, `Closes #58`), and the session's attribution line.
- [ ] T022 Watch the pull request's first run: `check` passes, and `publish` and `release` are
  skipped (FR-006).
- [ ] T023 (maintainer) Enable the `main` ruleset with `check` required and "Require branches to be
  up to date" off (quickstart step 5, FR-016).
- [ ] T024 Validate the gates with temporary commits, reverted after (quickstart step 6): version
  `5.0.5` fails in the version step and the merge is blocked; a deliberate `Assert.Fail` shows the
  test's name and message in the log; deleting a test that alone covers a branch fails the
  coverage step with both branch figures. Push each only after the previous run finished, since a
  new push cancels the running check.
- [ ] T025 (maintainer) Disable the Azure DevOps build and release pipelines for DnDGen.Stress,
  before merging (quickstart step 8).
- [ ] T026 (maintainer) Merge the pull request.
- [ ] T027 Verify the release (quickstart steps 10–11): NuGet.org lists 5.0.6 with the new badge;
  the 5.0.6 GitHub release has generated notes, the package, `coverage.cobertura.xml`, and tag
  `5.0.6` at the merge commit; only one 5.0.6 exists; Azure DevOps shows no new run; the README
  badge is green.
- [ ] T028 (maintainer) Revoke the old credentials: the nuget.org API key Azure DevOps used, and the
  `NuGet.org` and `github.com_cidthecoatrack` service connections (quickstart step 12, SC-002).

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002, T003)** → **US2 (T004–T009)** → **US1 (T010, T011)**.
- **US3 (T012–T016)** depends only on Foundational, and can run alongside US2 and US1.
- **US4 (T017)** depends on US1, US2, and US3.
- **Cut-over**: T018 and T019 can be done any time before T026. T020 before T022, since `check`
  fails without a baseline. T021 after US1–US4. T023 after T022, since a required check must have
  run once. T024 after T023. T025 before T026. T027 after T026. T028 after T027.

### Within User Story 2

- T004 → T006 and T005 → T007 (tests first, seen to fail); T008 after both; T009 after T008.

## Parallel Opportunities

- T004 and T005: different test files.
- T012, T013, and T014: different files, independent of each other and of US2.
- T018, T019, and T020: outside the repository, independent of each other and of the code.

```text
Parallel example, after T002 and T003:
  T004 Assert-VersionIsNewer.Tests.ps1   |  T005 Assert-CoverageNotLower.Tests.ps1
  T012 README badge  |  T013 constitution  |  T014 delete cicd/
```

## Implementation Strategy

1. **Tests first**: T002 approval, then T004 and T005 seen to fail, then the scripts.
2. **MVP**: US2 and US1 together are the smallest useful increment, since nothing releases without
   `check`. The workflow is complete after T011.
3. **Clean-up**: US3 and US4 make the repository and its records describe only the new build.
4. **Cut-over**: The maintainer's settings, the baseline report, the pull request and its
   validation, retiring Azure DevOps, the merge, and verification, in that order.
