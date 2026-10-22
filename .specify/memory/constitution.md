# DnDGen.Stress Constitution

## Core Principles

### I. Failures Surface Unaltered

Stressor exists to find the one iteration in thousands where a generator misbehaves, so a failure
inside a consumer's test MUST reach the test runner exactly as it was thrown: same exception, same
stack trace starting at the consumer's own frame. Stressor MUST NOT catch, wrap, rethrow, or
swallow exceptions from consumer code. A per-iteration `teardown` MUST run even when `test`
throws. A stress run MUST stop at the first failing iteration, or, for `StressAsync`, once the
batch containing it completes.

**Rationale**: A rare failure that arrives wrapped, re-thrown, or with Stressor's frames repeated
in the trace costs the consumer the one piece of evidence they cannot easily reproduce.

### II. Consumer-Derived Tests (NON-NEGOTIABLE)

- Every change to behaviour ships with its tests in the same pull request.
- Test cases are derived from what a consumer of the package can observe ("when I call X with Y,
  I get Z"), not from the shape of the implementation. Each capability, overload, and option has
  its own explicit case.
- For net-new tests, the list of case names is reviewed and approved before the tests are written.
- Anything that counts or repeats is tested at 0, 1, and many, where "many" is chosen to expose
  off-by-one and batch-rounding mistakes.
- Each case passes or fails on its own; one failing case MUST NOT prevent another from running.
- Assertions compare the whole observed value (the full log message, the full result), so a
  failure prints what actually happened.
- Unit tests (`DnDGen.Stress.Tests/Unit`) observe output through a mocked `ILogger`. Integration
  tests (`DnDGen.Stress.Tests/Integration`) exercise the public API the way a consumer's fixture
  does, including the `STRESS` compilation symbol.

**Rationale**: DnDGen.Stress is test infrastructure for every other DnDGen generator. A defect here
quietly weakens the test suites that depend on it, so its own tests are held to a higher bar than
the code they cover.

### III. Full Coverage by Reachability

Line and branch coverage of `DnDGen.Stress.dll`, as collected by the build with
`CodeCoverage.runsettings`, MUST stay at 100%. Coverage is a consequence of Principle II, not a
target in itself: an uncovered line means either a consumer-visible case is missing a test, or the
code is unreachable. Unreachable code MUST be removed, not tested or excluded from coverage.
The build does not yet fail below 100%, so pull request review checks the build's Code Coverage
results.

**Rationale**: In a library this small, 100% is reachable and cheap to keep. Holding the line makes
any uncovered code a clear signal rather than background noise.

### IV. Hardware-Independent Tests

Tests MUST pass on any machine, regardless of core count or CPU speed:

- Tests MUST NOT depend on `Environment.ProcessorCount`; tests of async batching set
  `MaxAsyncBatch` explicitly and derive expected counts from that value.
- Tests of time limits MUST make the time limit the only possible stopping condition (for example,
  an `int.MaxValue` iteration cap with a deliberately slow test body), and assert both a lower and
  an upper bound on the elapsed time.
- Timing assertions use an explicit tolerance and MUST NOT rely on how many iterations a machine
  completes in a given time.

**Rationale**: A test suite that fails on a faster or wider machine teaches people to ignore red
builds, which defeats a library whose purpose is to make failures trustworthy.

### V. LTS .NET and Declared Compatibility

- The package targets only Long-Term Support .NET releases (for example `net8.0`, `net10.0`),
  never Standard-Term Support releases.
- Raising the minimum .NET version or the NUnit major version is a breaking change and MUST be a
  MAJOR release.
- The README MUST state which package major version requires which .NET and NUnit versions, and
  which earlier major version to use otherwise.

**Rationale**: Consumers are other test suites that upgrade on their own schedule. LTS-only targets
and an explicit compatibility line let them stay on a supported release without guessing.

### VI. Documented Public Surface

The public API (`Stressor`, `StressorOptions`) and its observable behaviour are the contract.

- Every `Stress`, `StressAsync`, `Generate`, and `GenerateOrFail` overload, every `Stressor`
  constructor, and every `StressorOptions` setting with its default MUST be documented in
  `README.md`, which is packed into the NuGet package.
- Any pull request that changes a public member, an option default, the time-limit formula, or the
  content of Stressor's log messages MUST update the README in the same pull request.
- Removing or renaming a public member, changing an option default, or changing how the time limit
  is computed is a breaking change (MAJOR).

**Rationale**: The README is the only documentation consumers see, on NuGet.org and in the package
itself. If it drifts from the code, it misleads every consumer at once.

## Technology & Release Constraints

- **Stack**: C# on the current LTS .NET SDK; NUnit for both the package's dependency and its tests;
  Moq for test doubles; Microsoft.Extensions.Logging for output.
- **Runtime dependencies** flow to every consumer. Adding one SHOULD be justified in the pull
  request, and the package SHOULD depend only on NUnit and Microsoft.Extensions.Logging.
- **CI/CD**: Azure Pipelines. `cicd/build.yml` builds the package and runs all tests in the
  `Stress` configuration with code coverage collected. On `main`, it tags the commit with the
  csproj `Version` and publishes the package as a build artifact. `cicd/release.yml` then pushes
  the package to NuGet.org and creates a GitHub release.
- **Versioning**: Semantic versioning in the csproj `<Version>`. MAJOR for breaking changes
  (Principles V and VI); MINOR for additive public API or options; PATCH for fixes and internal
  changes.
- **Release pull requests** bump `<Version>`, replace `<PackageReleaseNotes>` with notes for that
  release only, and are titled `X.Y.Z: <summary>`.
- TODO(NON_RELEASE_MERGES): Because every build on `main` tags the current `<Version>`, a merge that
  does not bump it fails the build at the tagging step. Decide how documentation, tooling, and
  Spec Kit changes reach `main` (for example: always bump PATCH, or skip tagging when the tag
  already exists).

## Development Workflow

- Changes are made on a branch, and reach `DnDGen/DnDGen.Stress` `main` by pull request, reviewed
  by the maintainer.
- Code follows the conventions of the surrounding code; there is no separate style guide.
- Pull request descriptions are written from the actual diff against `main`, and state what was
  verified (for example, the full suite run with the pipeline's arguments) and what was not.
- Spec Kit artifacts under `specs/` record a single change at the time it was made. They are not
  maintained after the change merges, and MAY be deleted later. The lasting record of how the
  package behaves is this constitution, the tests, and the README.

## Governance

This constitution supersedes other practices for this repository.

- **Amendments** are made by pull request, with the reasoning in the pull request description.
- **Versioning of this constitution** follows semantic versioning: MAJOR for removing or redefining
  a principle, MINOR for a new principle or materially expanded guidance, PATCH for clarifications
  and wording.
- **Compliance**: Every pull request is reviewed against these principles. A deviation MUST be
  justified in the pull request, or the change is not merged.
- Principles that prove to apply across the DnDGen organisation MAY later move to a shared,
  organisation-wide document, with this file keeping only what is specific to DnDGen.Stress.

**Version**: 1.0.0 | **Ratified**: 2026-10-22 | **Last Amended**: 2026-10-22
