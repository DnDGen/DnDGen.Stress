# Feature Specification: Build and Release on GitHub Actions

**Feature Branch**: `003-github-actions`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: "Move DnDGen.Stress's build and release from Azure Pipelines (cicd/build.yml, cicd/release.yml) to GitHub Actions, per issue https://github.com/DnDGen/DnDGen.Stress/issues/58: nuget.org now expires API keys every 30 days and doesn't support Trusted Publishing from Azure Pipelines, so publishing should use NuGet Trusted Publishing (OIDC, no stored API key) from GitHub Actions. Keep the existing behaviour: every PR and push to main builds and runs every test in the Stress configuration with code coverage; on main, the commit is tagged with the csproj <Version>, the package is pushed to NuGet.org, and a GitHub release named after the version is created with the .nupkg attached. Test results and line/branch coverage must stay visible on every change (Azure DevOps showed them automatically). The README's build badge moves to the GitHub Actions badge (keeping the "[![Build Status]" alt text that ReadmeTests relies on), the repository constitution's mention of cicd/build.yml and cicd/release.yml is amended, the cicd/ folder is removed, and the Azure DevOps pipelines are retired without both systems releasing the same version. This is the first DnDGen repository on GitHub Actions, so the workflow becomes the pattern for every DnDGen NuGet package. Starting point: a prototype written for DnDGen.RollGen at C:\Users\KarlS\Downloads\Action prototypes\Action prototypes\Nuget packages\build.yml."

## Clarifications

### Session 2026-11-09

- Q: Should the check fail when coverage drops, or only show it? → A: Fail. Nothing is stored
  outside GitHub's own releases, so trends and history are whatever GitHub shows natively, or
  nothing. Showing results is secondary: a failed test's cause readable in the run is enough.
- Q: Is "the version isn't tagged yet" strict enough? → A: No. The pull request's version must be
  greater than the highest version already released, so an out-of-date branch can't release a lower
  or equal version.
- Q: When is the commit tagged, and how is a release that fails part-way recovered? → A: Tagging is
  last: the tag is created with the GitHub release, after NuGet.org has the package. Publishing to
  NuGet.org never skips a version it already has; that rejection is a deliberate last safeguard
  against a missing version bump. A release that fails after publishing is recovered by re-running
  only the part that failed.
- Q: How do other DnDGen repositories reuse this workflow? → A: The workflow file isn't shared yet:
  Stress's build differs from the generator packages' (which have more test projects and use Stress
  in their own stress tests) and will probably stay independent. As soon as two repositories do the
  same thing, it is extracted to DnDGen/.github as a template they share. The release practices
  Stress establishes, though, are a deliberate new pattern every DnDGen package is meant to follow.
- Q: A generator's stress tests cover code randomly, so its coverage can vary between runs. Should
  the gate allow for that? → A: No. Line coverage is meant to come from unit and non-stress
  integration tests; stress tests check that random samples follow the rules.
- Q: What is the change's coverage compared against? → A: The coverage report attached to the
  latest GitHub release, instead of measuring `main` again in the same run. Every merge to `main`
  is a release, so the latest release is `main`.
- Q: Should the build's tools be pinned to specific versions? → A: No. A pinned version is more
  likely to go stale than the latest is to break in a way that can't be fixed or worked around.
- Q: Should merging a pull request into `main` be blocked until its check passes? → A: Yes. The
  repository's existing `main` ruleset, currently disabled, is enabled and requires the check to
  pass before merging. It doesn't require the branch to be up to date with `main`.
- Q: Where are the release practices and the setup steps outside the repository written down? →
  A: Only in this feature's spec, plan, and quickstart; no living guide. They are ways of meeting
  principles the DnDGen constitution already has, so it doesn't change. The next package's move is
  pointed at Stress as the example, and anything shared is discussed for extraction then.

## User Scenarios & Testing *(mandatory)*

Three people rely on the build and release. The **maintainer** opens pull requests and merges them,
and needs every change checked and every merge released without looking after credentials. The
**consumer** installs `DnDGen.Stress` from NuGet.org and reads its release notes on GitHub. The
**maintainer of another DnDGen package** will move that package's build later, following the
practices this one establishes (DnDGen constitution I.1, I.2).

Today the build and release run on Azure Pipelines. Publishing to NuGet.org uses a stored API key,
which nuget.org now expires every 30 days, and nuget.org's keyless alternative, Trusted Publishing,
isn't available from Azure Pipelines (issue #58).

### User Story 1 - A merge releases without a stored key (Priority: P1)

As the maintainer, when I merge a pull request to `main` that bumps the package version, I expect
the package to appear on NuGet.org, and a GitHub release named after the version to be created with
the package and its coverage report attached, tagging the commit, all without any long-lived
publishing credential stored anywhere.

**Why this priority**: This is the reason for the move. Without it, a release fails within 30 days
of the last key rotation.

**Independent Test**: Merge a version bump to `main` and confirm the NuGet.org listing, the GitHub
release, and the tag all appear for that version, and that no NuGet API key is stored in the
repository, the organisation, or any environment.

**Acceptance Scenarios**:

1. **Given** a merge to `main` with a new version, **When** the check passes, **Then** the package is
   published to NuGet.org, and then a GitHub release titled with the version is created with
   release notes, the package, and the coverage report attached, tagging the commit with the
   version.
2. **Given** a merge to `main` whose check fails, **When** the run ends, **Then** nothing is
   published, released, or tagged.
3. **Given** a run for a pull request or any branch other than `main`, **When** the check passes,
   **Then** nothing is published, released, or tagged.
4. **Given** no NuGet API key stored anywhere, **When** a release runs, **Then** it publishes using a
   credential that exists only for that run.
5. **Given** a release whose NuGet.org publish succeeded but whose GitHub release failed, **When**
   the maintainer re-runs only the failed part, **Then** the GitHub release and tag are created, and
   NuGet.org isn't published to again.
6. **Given** a version NuGet.org already has, **When** a release tries to publish it, **Then** the
   publish fails and no GitHub release or tag is created.

---

### User Story 2 - Every change is checked, and can't make things worse (Priority: P1)

As the maintainer, when I open or update a pull request, or push to `main`, I expect the package to
be built and every test to run in the full Stress configuration with code coverage, and the check to
fail if a test fails, if coverage is lower than the latest release's, or if the version isn't
greater than the latest release's.

**Why this priority**: The DnDGen constitution requires coverage to be measured on every change and
never to drop (II.2.c), and every merge to `main` to be a release with a bumped version. Azure
DevOps only showed coverage, so a drop relied on someone noticing; and a missing or stale version
bump has failed a release before, only after merge.

**Independent Test**: On a pull request, add a failing test, then separately remove a test so
coverage drops, then separately set the version to the latest released one. Each makes the check
fail and say why; reverting each makes it pass.

**Acceptance Scenarios**:

1. **Given** a pull request, **When** it is opened or updated, **Then** a check runs that builds the
   package and runs every test in the Stress configuration with coverage, and its pass or fail status
   shows on the pull request.
2. **Given** a failing test, **When** the check finishes, **Then** it fails, and the failed test's
   name and message are readable in the run without downloading anything.
3. **Given** a change whose line or branch coverage of `DnDGen.Stress.dll` is lower than in the
   latest release's coverage report, **When** the check finishes, **Then** it fails and states both
   figures for whichever dropped.
4. **Given** a change whose coverage equals or exceeds the latest release's, **When** the check
   finishes, **Then** coverage doesn't fail it.
5. **Given** a change whose `<Version>` is equal to or lower than the highest version already
   released, **When** the check runs, **Then** it fails and states both versions.
6. **Given** a pull request whose check failed or hasn't finished, **When** the maintainer tries to
   merge it, **Then** GitHub doesn't allow the merge.

---

### User Story 3 - Only one system releases, and the old one is gone (Priority: P2)

As the maintainer, I expect the first merge after this change to be released exactly once, by the
new build, and afterwards for the repository, its README, and its constitution to describe only the
new build.

**Why this priority**: If both systems are live when this merges, both try to publish the same
version, and one fails or the release is left half-done. Stale references mislead the next reader,
and the constitution is what every plan is checked against.

**Independent Test**: After the merge, confirm one NuGet.org version, one GitHub release, and one
tag for the new version; the old pipelines don't run; `cicd/` doesn't exist; and the README badge
reports the new build's status.

**Acceptance Scenarios**:

1. **Given** the merge of this change, **When** it lands on `main`, **Then** only the new build runs,
   and it releases the version exactly once.
2. **Given** the README on GitHub or NuGet.org, **When** a reader looks at the build badge, **Then**
   it shows the status of the new build on `main`, and links to it, and the Theme Song badge still
   sits beside it.
3. **Given** the repository constitution, **When** it is read, **Then** it names the new build and
   release, not `cicd/build.yml` or `cicd/release.yml`.
4. **Given** the repository, **When** it is searched, **Then** no build or release definition for
   Azure Pipelines remains.

---

### User Story 4 - Stress sets the release pattern for every DnDGen package (Priority: P3)

As the maintainer of another DnDGen package, when I move that package's build, I expect to point the
move at Stress as the example: its workflow, and this feature's spec, plan, and quickstart, which
record each release practice, the principle it meets, and the steps outside the repository in
order.

**Why this priority**: Stress is the first DnDGen repository on GitHub Actions, so its release
practices are a new pattern, adopted on purpose for every DnDGen package (DnDGen constitution I.2).
They are ways of meeting principles the DnDGen constitution already has, so the constitution
doesn't change, and the workflow file stays Stress's own until a second repository builds the same
way. It matters for the next move, not for this release.

**Independent Test**: Read this feature's spec, plan, and quickstart without this conversation, and
confirm a maintainer could apply each practice to another DnDGen package, knows which principle it
meets, and could do every step outside the repository in order.

**Acceptance Scenarios**:

1. **Given** the release practices Stress establishes (keyless publishing, the version and coverage
   checks against the latest release, the coverage report attached to each release, tagging last,
   never skipping a duplicate version), **When** another package moves, **Then** this feature's
   records name each one, the DnDGen constitution principle it meets, and why it is worth everyone
   conforming to it.
2. **Given** the steps a maintainer takes outside the repository (on nuget.org, in GitHub settings,
   and in Azure DevOps), **When** another package moves, **Then** this feature's quickstart lists
   them in order relative to the merge.
3. **Given** this change, **When** it merges, **Then** nothing is added to DnDGen/.github, the
   DnDGen constitution is unchanged, and no separate guide is added: the workflow file and this
   feature's records are the example.

---

### Edge Cases

- **Version not bumped, or bumped from a stale base**: A branch cut before the latest release can
  bump to a version that is already released, or lower than it (for example `5.0.4` after `5.0.5`).
  Its check fails, and so does `main`'s if it is merged anyway, before anything is published.
  Versions are compared by semantic version precedence, not by tag date or text, and the existing
  `v3.0.0` tag counts as `3.0.0`.
- **Two open pull requests with the same bump**: Both pass while open. Merging doesn't require a
  branch to be up to date with `main`, so once one merges, the other can still be merged on its
  earlier passing check. `main`'s check then fails its version check before anything is published,
  and the next pull request bumps the version.
- **No coverage report on the latest release**: The check fails rather than passing unchecked.
  Releases before this change have no report, so a report for `5.0.5` is attached to its release
  once, by hand, before this change's own check can pass.
- **Coverage that only a stress test reaches**: A generator's stress tests are random, so a line
  only they cover may be covered in one run and not the next. When it isn't, the check fails: a
  line covered only by chance is a missing unit or integration case. Stress's own tests cover the
  same code every run.
- **Release fails part-way**: If publishing to NuGet.org fails, nothing is released or tagged, and
  re-running the release retries it. If the GitHub release fails after NuGet.org has the package,
  re-running only the failed part creates the release and tag. Re-running the whole release instead
  fails at NuGet.org, which already has the version; that is the safeguard working.
- **On NuGet.org but not tagged**: While a failed release is being recovered, a version can be on
  NuGet.org without a tag or release. The version and coverage checks read releases, so they
  compare against the previous one, and NuGet.org's rejection of the duplicate is what stops a
  second publish.
- **Merges close together**: Releases run one at a time, in merge order, and a later one never
  publishes before an earlier one. If a third merge lands while one release runs and another
  waits, the waiting one is dropped and its version is skipped, which is intended: the third
  release contains the second's changes, so releasing the second adds nothing. The version check
  still passes for the third, since its version is greater.
- **New tool versions**: The build uses the latest runner images, the latest .NET 10 SDK, and the
  latest version of each action rather than pinned ones. When an update breaks the build, fixing it
  is maintenance (DnDGen constitution III.1.a).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every pull request to `main`, and every push to `main`, MUST build the package and
  run every test in the Stress configuration with code coverage of `DnDGen.Stress.dll`, as
  `cicd/build.yml` does today.
- **FR-002**: The check's pass or fail status MUST show on the pull request, and each failed test's
  name and message MUST be readable in the run without downloading files. Storing results or
  coverage anywhere but the releases, and showing trends, is out of scope.
- **FR-003**: Every check MUST compare the change's line and branch coverage of `DnDGen.Stress.dll`
  with the coverage report attached to the latest release, and MUST fail, stating both figures, if
  either is lower for the change, or if the latest release has no report. Equal or higher passes.
- **FR-004**: On `main` only, after the check passes, the release MUST publish the csproj
  `<Version>`'s package to NuGet.org and then create a GitHub release with the version as its tag
  and title, generated release notes, and the package and the run's coverage report attached,
  matching the existing releases (for example `5.0.5`). The tag MUST be created last, with the
  GitHub release, so that a tag always means the version is on NuGet.org.
- **FR-005**: Publishing to NuGet.org MUST use NuGet Trusted Publishing, so that no NuGet API key is
  stored in the repository, the organisation, or any environment, and the credential used exists
  only for the run that publishes.
- **FR-006**: Only pushes to `main` in the DnDGen/DnDGen.Stress repository MUST be able to publish,
  release, or tag; pull requests MUST NOT.
- **FR-007**: Every check MUST fail, stating both versions, when the csproj `<Version>` isn't greater
  than the highest version already released, compared by semantic version precedence. On `main`,
  this happens before anything is published.
- **FR-008**: Publishing to NuGet.org MUST fail when NuGet.org already has the version, rather than
  skip it. A release whose GitHub release fails after publishing MUST be completed by re-running
  only the failed part, without manual clean-up and without publishing again.
- **FR-009**: The README's build badge MUST show the status of the new build on `main` and link to
  it, keep its `[![Build Status]` alt text, and keep the Theme Song badge beside it, so the existing
  README tests pass unchanged.
- **FR-010**: The repository constitution's Versioning and release constraint MUST name the new
  build and release instead of `cicd/build.yml` and `cicd/release.yml`, as a PATCH amendment.
- **FR-011**: `cicd/` MUST be removed, and the Azure DevOps build and release pipelines for
  DnDGen.Stress MUST be retired so that they don't run for this merge or any later one.
- **FR-012**: The steps outside the repository that the build depends on (the nuget.org Trusted
  Publishing policy, the GitHub release environment, the `5.0.5` coverage report, enabling the
  `main` ruleset with the required check, retiring the Azure DevOps pipelines) MUST be listed in this
  feature's quickstart, in the order they're done in relative to the merge.
- **FR-013**: This feature's spec and plan MUST record each release practice in User Story 4 as the
  pattern for every DnDGen package, with the DnDGen constitution principle it meets and its
  justification under I.2. They are the only record: no guide is added, the DnDGen constitution
  doesn't change, and the workflow file lives in this repository only until a second repository
  does the same thing.
- **FR-014**: The build MUST use the latest runner images, the latest .NET 10 SDK, and the latest
  version of each action, not pinned versions.
- **FR-015**: The change MUST ship as a PATCH release, `5.0.6`, whose release notes say the package
  changes only in its README's build badge and name 5.0.5 as the last version with other changes,
  as the DnDGen constitution requires.
- **FR-016**: Merging a pull request into `main` MUST require its check to pass, through the
  repository's `main` ruleset. It MUST NOT require the branch to be up to date with `main`.

### Key Entities

- **Latest release**: The GitHub release with the highest version, compared as version numbers,
  not the one GitHub flags as Latest and not the newest by date. Both the version check and the
  coverage check compare against it; "the highest version already released" is its version.
- **Check**: The build and test run for a pull request or a push, with its status, test results, and
  coverage.
- **Release**: For one version, the NuGet.org listing, and the GitHub release and its tag, with the
  package and the coverage report attached.
- **Coverage report**: Line and branch coverage of `DnDGen.Stress.dll` for one release, attached to
  that release, and the baseline the next change is compared against.
- **Trusted Publishing policy**: The nuget.org setting that lets this repository's release publish
  the `DnDGen.Stress` package without an API key.
- **Release environment**: The protected setting in GitHub that the publishing part of the release
  runs in.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The merge of this change releases `5.0.6` exactly once: one NuGet.org version, one
  GitHub release with the package and coverage report attached, and one tag.
- **SC-002**: Zero long-lived NuGet publishing credentials exist for DnDGen.Stress after the move,
  and no release needs credential maintenance, however long it has been since the last one.
- **SC-003**: For every pull request, the maintainer can tell from the pull request page whether it
  passed, and from the run, without downloading anything, which test failed and why.
- **SC-004**: Line and branch coverage of `DnDGen.Stress.dll` stays at 100% (II.2.c (Stress)), and
  a pull request that lowers either one can't pass its check.
- **SC-005**: Zero references to Azure Pipelines or `cicd/` remain in the repository outside the
  historical `specs/` records.
- **SC-006**: A maintainer moving another DnDGen package can apply every release practice, and do
  every step outside the repository, from Stress's workflow and this feature's records alone.
- **SC-007**: A pull request whose version isn't greater than the latest release can't pass its
  check.

## Assumptions

- The maintainer owns `DnDGen.Stress` on nuget.org and administers the DnDGen/DnDGen.Stress
  repository and the DnDGen Azure DevOps project, so can create the Trusted Publishing policy and
  the GitHub environment, enable the `main` ruleset, and retire the old pipelines. Those are manual steps this change documents
  (FR-012) but can't perform.
- Releases stay automatic: every merge to `main` is a release, with no manual approval step, as in
  the DnDGen constitution and the current pipeline. The release environment, created for this change, is used for protection
  and as the Trusted Publishing policy's scope, not for approvals.
- The build keeps running the whole test project in the Stress configuration, as today. Splitting
  fast tests from stress tests is out of scope.
- The DnDGen.RollGen prototype is a starting point, not a requirement: its names are replaced with
  Stress's, and its gaps (the version and coverage checks, the README badge, the constitution, the
  cut-over) are covered by this spec.
- Pull requests from forks need nothing extra: by default they can't read the release environment
  or publish, and nothing they run is released (FR-006).
- The `specs/` records of earlier features mention `cicd/` as it was then. They are history and
  stay unchanged.
- Dependency update automation, such as Dependabot, is out of scope; the repository removed it
  before.
- A performance gate, failing a pull request whose stress tests' iterations per second drop by more
  than 10% against the latest release (DnDGen constitution I.2.b), is out of scope. If the coverage
  report attached to releases works as a baseline, it becomes its own issue, with iterations per
  second attached to releases the same way.
- No code in the package changes, and the only README change, which is also the only change to the
  package, is the build badge. Under the theme
  song's Updating check, it rewords no quoted phrase and doesn't change the README's ideas or vibe,
  so the song needs no update; the pull request still records that check.
