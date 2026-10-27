# Implementation Plan: README Documents the Public API

**Branch**: `002-readme-public-api` | **Date**: 2026-10-28 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-readme-public-api/spec.md`

## Summary

Integration tests read the real README and fail when it doesn't document every public member of
`Stressor` and `StressorOptions`, or documents one that doesn't exist, or states an option's
default wrongly. A member counts as documented when it is in the first column of a README table
whose first header names its kind (Constructor, Method, Property, Option), so the README can be
restructured freely ([research.md](research.md) R1). One test per kind compares the whole
documented list with the code's ([research.md](research.md) R4), and one case per option compares
its stated default with a new `StressorOptions` ([research.md](research.md) R5). The tests are
written first and fail on today's README, which is then brought up to date with Constructors and
Properties tables, a `CountStressTestsIn` row, and a `StressorOptions` Properties table
([contracts/readme.md](contracts/readme.md)). It ships as 5.0.4, with the DnDGen constitution
adoption already on this branch.

## Technical Context

**Language/Version**: C# on .NET 10 for the tests; Markdown for the README.

**Primary Dependencies**: NUnit 5, and reflection over `DnDGen.Stress`. No new packages.

**Storage**: N/A. The tests read `README.md`, already copied to the test output.

**Testing**: NUnit integration tests in `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`, run
with `dotnet test`.

**Target Platform**: Any machine that runs the test suite: local, and Azure Pipelines
`windows-latest` in the `Stress` configuration.

**Project Type**: Library (NuGet package).

**Performance Goals**: N/A. A handful of tests reading one file.

**Constraints**: No package code changes, so coverage stays at 100% without new unit tests. No
test depends on the machine (II.3.c).

**Scale/Scope**: 2 types, 4 kinds, about 25 members, 5 new tests and 8 new test cases.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Checked against the [DnDGen constitution](https://github.com/DnDGen/.github/blob/main/constitution/CONSTITUTION.md)
1.0.0, read live on 2026-10-28, and Stress's additions 2.0.1.

| Rule | Applies? | Status |
|------|----------|--------|
| **I.1** Emulate what exists | Yes | Pass. The tests extend `ReadmeTests` and reuse its helpers. Members are written the way the Methods table already writes them (R2), and tables are the README's existing way to document members (R1). Option cases use `[TestCase]`, and the every-option check reads test attributes the way `StressorTests` does (R5). |
| **I.2** A new pattern must be worth it | Yes | Pass. Finding README tables by their first header is new. It is clearer than finding them by section heading, which would pin the structure the spec keeps free (FR-008), and it gives one answer for every kind (I.2.a, I.2.c). `[TestCaseSource]` was rejected as a new pattern (R5). |
| **I.3** Code reads like prose; no comments | Yes | Pass. Tests are named as consumer outcomes; no comments are added. |
| **II.1** What to test | Yes | Pass. The consumer is the maintainer changing the code, and each case is what they see when the README falls behind. Each kind and each option has its own case (II.1.c, R4, R5). |
| **II.2** How much to test | Yes | Pass. The members and options are a small, closed range, so every one is tested (II.2.a). II.2.b doesn't apply: there is no range to sample. **II.2.c (Stress)**: no package code changes, so coverage of `DnDGen.Stress.dll` stays at 100%. |
| **II.3** How a test fails | Yes | Pass. List tests compare whole lists, reporting missing and extra members together (II.3.a). Each default case shows both values. The machine-dependent default is evaluated on both sides, so no case depends on the machine (II.3.c). |
| **III.1** Maintenance in scope | Yes | Pass. The constitution adoption, and its 2.0.1 wording fix, are on this branch as their own commits. The README gains what the contract already required. |
| **III.2** Improve where you are, in its own commit | Yes | Pass. Tests, README, and release are separate commits. |
| **III.3** Behaviour out of scope | Yes | Pass. No behaviour changes. Protected members and the version-requirements part of the contract are recorded as out of scope (R6), to raise as issues if wanted. |
| **Stack** | Yes | Pass. `net10.0`, the latest .NET LTS. No new dependencies. |
| **Versioning and release** | Yes | Pass. PATCH: 5.0.4. The package changes only in its README; the notes say so and name 5.0.1 as the last code change. Titled `5.0.4: README documents the public API`. |
| **README as contract** | Yes | Pass. This feature enforces it. Theme song check: no update needed (R7), confirmed by its tests. |
| **Development workflow** | Yes | Pass. Branch and pull request to `main`, reviewed by the maintainer; the description is written from the diff. |
| **Governance: compliance** | Yes | Pass. This check cites the DnDGen constitution's labels, confirming the pointer was followed. |

**Post-design re-check**: The Phase 1 design adds no violations. Complexity Tracking stays empty.

## Project Structure

### Documentation (this feature)

```text
specs/002-readme-public-api/
├── plan.md              # This file
├── research.md          # Phase 0: R1–R7
├── data-model.md        # Phase 1: kinds, members, stated defaults
├── quickstart.md        # Phase 1: validation checks
├── contracts/
│   └── readme.md        # Phase 1: README, csproj, and pull request changes
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
README.md                                    # Constructors and Properties tables, new rows
DnDGen.Stress/DnDGen.Stress.csproj           # Version and release notes
DnDGen.Stress.Tests/
├── Integration/ReadmeTests.cs               # New tests beside the theme song tests
└── Unit/StressorTests.cs                    # TestCount and TestCaseCount
```

**Structure Decision**: The README's tests already live in `Integration/ReadmeTests.cs`, which
mirrors `README.md`, so the new tests go there. No package code changes.

## Complexity Tracking

No violations.
