# DnDGen.Stress Constitution

> **Read the [DnDGen constitution](https://github.com/DnDGen/.github/blob/main/constitution/CONSTITUTION.md)
> first, in full, every time this file is read.** Fetch it from
> https://raw.githubusercontent.com/DnDGen/.github/main/constitution/CONSTITUTION.md rather than
> relying on memory or an earlier copy. Its principles, constraints, workflow, and governance apply
> to this repository, and every plan, task list, analysis, and review is checked against it as well
> as against this file. This file adds only what is specific to this repository, under the DnDGen
> constitution's labels. If the DnDGen constitution can't be read, stop and say so: don't continue
> from this file alone.
<!-- Keep the pointer above exactly as written: it is what makes every Spec Kit command read the
DnDGen constitution. See https://github.com/DnDGen/.github/tree/main/constitution -->

## Core Principles

### I. Consistency Is King

**I.2 (Stress)**

- *Example:* Making an `async` test helper synchronous changes how its exceptions reach
  `StressAsync`, so it is not a style fix.

### II. Tests Are the Spec

**II.2.c (Stress)** Coverage is the line and branch coverage of `DnDGen.Stress.dll`. It is at
100%, so it stays there.

**II.3.c (Stress)**

- *Example:* Async batching tests set `MaxAsyncBatch` explicitly instead of inheriting the
  machine's core count.

### III. Leave It Better Than You Found It

No additions.

## Technology & Release Constraints

- **Stack**: C# on the latest .NET LTS, currently `net10.0`. NUnit, as both the package's
  dependency and its test framework, with Moq for test doubles and Microsoft.Extensions.Logging
  for output.
- **Versioning and release**: A NuGet package, versioned and released as the DnDGen constitution
  describes. MAJOR also covers changing the time-limit formula and raising the minimum .NET or
  NUnit major version. On `main`, `cicd/build.yml` builds and runs every test in the `Stress`
  configuration with code coverage, then tags the commit with `<Version>`; `cicd/release.yml`
  publishes to NuGet.org and creates a GitHub release.
- **README as contract**: The README documents every `Stressor` constructor and method and every
  `StressorOptions` setting with its default, and states which .NET and NUnit versions each major
  version requires. The theme song is in `docs/theme-song`.

## Governance

This repository follows the DnDGen constitution's Governance. The additions in this file are
amended by pull request to this repository and versioned the same way.

**Version**: 2.0.0 | **Ratified**: 2026-10-23 | **Last Amended**: 2026-10-28
