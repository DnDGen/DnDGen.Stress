---

description: "Task list for README public-API tests"
---

# Tasks: README Documents the Public API

**Input**: Design documents from `specs/002-readme-public-api/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: The tests are the feature. They are written first, with the karls-test-style skill,
and seen to fail for the expected reason before the README changes that make them pass (II).

**Organization**: Tasks are grouped by user story. Both stories are P1; US1 comes first because
today's README fails it, so it proves the tests-first loop.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)
- **(maintainer)**: Only the maintainer can do this task. `/speckit-implement` stops and hands it
  over.

## Path Conventions

- The README: `README.md`
- Its tests: `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`, beside the theme song tests
- The assembly's test counts: `DnDGen.Stress.Tests/Unit/StressorTests.cs`

---

## Phase 1: Setup

None. `README.md` is already copied to the test output, and `ReadmeTests` already reads it.

---

## Phase 2: Foundational

**Purpose**: Agree the test list, then give `ReadmeTests` what every new test needs.

**⚠️ CRITICAL**: No test code is written until T001 is done.

- [ ] T001 (maintainer) Approve the test list, each case a consumer sentence (II.1.a), with the
  maintainer changing the code as the consumer:
  1. `Constructors_AreAllDocumented`: if I add, remove, or change a `Stressor` constructor, I
     expect the README's Constructor tables to list exactly the constructors there are, and I'll
     confirm that by comparing the two lists, missing and extra together.
  2. `Methods_AreAllDocumented`: the same, for the public methods of `Stressor` and
     `StressorOptions`, against the Method tables.
  3. `Properties_AreAllDocumented`: the same, for their public fields, constants, and read-only
     properties, against the Property tables.
  4. `Options_AreAllDocumented`: the same, for `StressorOptions`' settable properties, against
     the Option tables.
  5. `OptionDefault_IsDocumented`, one `[TestCase]` per option (`IsFullStress`,
     `RunningAssembly`, `TestCount`, `BuildTimeLimitInSeconds`, `OutputTimeLimitInSeconds`,
     `TimeLimitPercentage`, `ConfidenceIterations`, `MaxAsyncBatch`): if I change an option's
     default, I expect the README's stated default to match it, and I'll confirm that by comparing
     the stated value with the value on a new `StressorOptions`.
  6. `OptionDefault_IsCheckedForEveryOption`: if I add an option, I expect a default case for it,
     and I'll confirm that by comparing case 5's `[TestCase]` names with the options.
- [ ] T002 In `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`, add private helpers, reusing
  `GetLinesOutsideCodeBlocks`: one that returns the first-column code spans of every README table
  whose first header cell is a given kind, merged across tables (research R1), and one that writes
  a member "`Name(parameter, names)` for constructors and methods, with the type name for
  constructors and no generic type parameters; the bare name otherwise" (data-model.md, Member)

**Checkpoint**: The test list is agreed, and the helpers build.

---

## Phase 3: User Story 1 - A new or changed member can't go undocumented (Priority: P1) 🎯 MVP

**Goal**: Every public constructor, method, and property of `Stressor` and `StressorOptions` is
documented, and the tests fail when one isn't, or when the README documents one that doesn't
exist.

**Independent Test**: Delete the `TimeLimit` row: only `Properties_AreAllDocumented` fails, listing
`TimeLimit` as missing (quickstart check 3).

### Tests for User Story 1 ⚠️

- [ ] T003 [US1] Add `Constructors_AreAllDocumented`, `Methods_AreAllDocumented`, and
  `Properties_AreAllDocumented` to `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`, each comparing
  the documented list with the code's using `Is.EquivalentTo` (research R4), with kinds as in
  data-model.md, Kind. Add 3 to `TestCount` in `DnDGen.Stress.Tests/Unit/StressorTests.cs`
- [ ] T004 [US1] Run the full `dotnet test` and see the three tests fail for the expected reason:
  Constructors lists `Stressor(options)` and `Stressor(options, logger)` as missing; Methods lists
  `CountStressTestsIn(runningAssembly)`; Properties lists `Options`, `StressTestCount`,
  `TimeLimit`, `TestDuration`, `TestIterations`, `AreValid`, and the three `Default…` constants.
  Every other test passes

### Implementation for User Story 1

- [ ] T005 [US1] In `README.md`, add the Constructors table, the `CountStressTestsIn` row, and both
  Properties tables, worded and placed as in [contracts/readme.md](contracts/readme.md), leaving
  every existing sentence as it is
- [ ] T006 [US1] Run the full `dotnet test` and see every test pass, including the theme song tests
  (research R7)

**Checkpoint**: US1 is done: the README documents every member, and the tests hold it there.

---

## Phase 4: User Story 2 - Every option and its default are documented correctly (Priority: P1)

**Goal**: The Options table lists exactly the options there are, each with its correct default.

**Independent Test**: Change `TimeLimitPercentage`'s default in the README to `0.5`: only that
option's case fails, showing `0.5` and `1` (quickstart check 3).

### Tests for User Story 2 ⚠️

- [ ] T007 [US2] Add `Options_AreAllDocumented` to `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`,
  as in T003
- [ ] T008 [US2] Add `OptionDefault_IsDocumented` with a `[TestCase]` per option by `nameof`, and
  `OptionDefault_IsCheckedForEveryOption`, to `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`.
  The stated default is "the cell, without backticks, a trailing parenthetical, or thousands
  separators", evaluated if it is "a known named value (`Environment.ProcessorCount`)", and
  matches when it equals the default "written as the README writes values: `false`, `null`, `0`,
  `1`, `3600`" (data-model.md, Stated default; research R5). Add 2 to `TestCount` and 8 to
  `TestCaseCount` in `DnDGen.Stress.Tests/Unit/StressorTests.cs`
- [ ] T009 [US2] Run the full `dotnet test`. Today's Options table is already right, so these
  tests pass at once; see each fail for the expected reason with the temporary edits in
  quickstart check 3 (a wrong default, a deleted `[TestCase]`), then revert them

**Checkpoint**: US2 is done. Both stories hold independently.

---

## Phase 5: Polish & Release

- [ ] T010 In `DnDGen.Stress/DnDGen.Stress.csproj`, set `<Version>` to `5.0.4` and replace
  `<PackageReleaseNotes>` as in [contracts/readme.md](contracts/readme.md)
- [ ] T011 Run the remaining quickstart checks: 3 (failures name the fix), 4 (structure is free),
  and 5 (100% line and branch coverage of `DnDGen.Stress.dll`, with `-c Stress --collect "Code
  Coverage" --settings CodeCoverage.runsettings`), reverting every temporary edit
- [ ] T012 (maintainer) Review the branch and approve pushing it and opening the pull request
  `5.0.4: README documents the public API` against `DnDGen/DnDGen.Stress` `main`, with
  `cidthecoatrack` as reviewer and the description written from the diff, including the DnDGen
  constitution adoption

---

## Dependencies & Execution Order

- **Phase 2** blocks everything: T001 before any test code, then T002.
- **US1 (Phase 3)** and **US2 (Phase 4)** both depend only on Phase 2. They touch the same two
  files, so they run one after the other, not in parallel.
- **Phase 5** follows both stories.

Within each story: tests, then see them fail, then the README, then see them pass.

## Parallel Opportunities

Few: every task but T010 edits `ReadmeTests.cs`, `StressorTests.cs`, or `README.md`. T010 can run
alongside either story.

## Implementation Strategy

1. **MVP**: Phases 2 and 3. The README documents every member, and the list tests hold it there.
2. **Then**: Phase 4, for the options and their defaults.
3. **Then**: Phase 5, and the pull request.

Each task ends in a commit (III.2), and nothing is pushed before T012.
