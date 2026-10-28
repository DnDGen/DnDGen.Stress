# Feature Specification: README Documents the Public API

**Feature Branch**: `002-readme-public-api`

**Created**: 2026-10-28

**Status**: Draft

**Input**: User description: "README public-API tests, from DnDGen/DnDGen.Stress#54. Short name: readme-public-api (branch 002-readme-public-api already created by hand). The README is the package's contract: tests should confirm every public Stressor constructor and method appears in the README, and every public StressorOptions property appears in the Options table with a default matching new StressorOptions(). The issue also asks for a deliberate decision on whether to pin the README's overall structure (sections in order). Tests live beside the existing theme song README tests in DnDGen.Stress.Tests/Integration/ReadmeTests.cs."

## Clarifications

### Session 2026-10-28

- Q: Which public members must the README document? → A: Every public member of `Stressor` and
  `StressorOptions`: constructors, methods, properties, fields, and constants, as the DnDGen
  constitution's README as contract requires. Stress's constitution is reworded to match.
- Q: Should the tests pin the README's overall structure? → A: No. The tests check only what the
  contract requires, so restructuring the README stays free. The existing test that Theme Song is
  the last section stays.

## User Scenarios & Testing *(mandatory)*

The README is packed into the NuGet package and is the only documentation a package consumer
sees (DnDGen constitution, README as contract). Two people rely on it: the **consumer**, who reads
it to learn what the package does, and the **maintainer**, who changes the package and needs to
know when the README has fallen behind.

### User Story 1 - A new or changed member can't go undocumented (Priority: P1)

As the maintainer, if I add, rename, or re-shape a public member of `Stressor` or
`StressorOptions` (a constructor, method, property, field, or constant) and don't update the
README, I expect the build to fail, and the failure to name exactly which member the README is
missing.

**Why this priority**: Members are what consumers call and read. An undocumented one is invisible
to them, and nothing catches it today.

**Independent Test**: Add a public method to `Stressor` without touching the README and run the
tests: one test fails and names the new method. Document it and the test passes.

**Acceptance Scenarios**:

1. **Given** every public member is documented, **When** the tests run, **Then** they pass.
2. **Given** a public member or overload that the README doesn't document, **When** the tests run,
   **Then** a test fails and lists every undocumented one, methods and constructors by name and
   parameters.
3. **Given** the README documents a member or overload that no longer exists, **When** the tests
   run, **Then** a test fails and lists every documented one that doesn't exist.

---

### User Story 2 - Every option and its default are documented correctly (Priority: P1)

As the maintainer, if I add an option to `StressorOptions` or change a default without updating
the README's Options table, I expect the build to fail and say which option is missing, or which
default the README states wrongly, with both the stated and the actual value.

**Why this priority**: A consumer relies on the stated defaults to decide which options to set. A
wrong default is worse than a missing one, because it is believed.

**Independent Test**: Change one default in `StressorOptions` without touching the README: one
test fails and shows both values. Add an option without documenting it: a test fails and names it.

**Acceptance Scenarios**:

1. **Given** the Options table lists every option with its correct default, **When** the tests
   run, **Then** they pass.
2. **Given** an option missing from the Options table, **When** the tests run, **Then** a test
   fails and names it.
3. **Given** an Options table row for an option that doesn't exist, **When** the tests run,
   **Then** a test fails and names it.
4. **Given** a default in the Options table that doesn't match a freshly created
   `StressorOptions`, **When** the tests run, **Then** a test fails for that option alone and
   shows the stated and the actual default.

---

### Edge Cases

- **Overloads**: `Stress` and `StressAsync` each have two overloads, and `Stressor` has two
  constructors. Each overload is its own documented item, told apart by its parameters, so
  documenting one overload doesn't cover the other.
- **Generic methods**: `Generate<T>` and `GenerateOrFail<T>` are documented by name and
  parameters; the type parameter isn't required in the README.
- **Defaults that depend on the machine**: `MaxAsyncBatch` defaults to the machine's processor
  count. The README states it as that expression, not a number, and the test confirms the actual
  default equals it on whatever machine runs the tests (DnDGen constitution II.3.c).
- **Defaults written for people**: The README writes some defaults for readers, such as
  `3600 (1 hour)` and `1,000,000`. The test compares the value, not the formatting, so these
  readings stay allowed.
- **Code samples**: A name that appears only inside a code sample doesn't count as documented: a
  sample shows usage, it doesn't describe the member.
- **Inherited members**: Members every .NET object has (`ToString`, `Equals`, `GetHashCode`,
  `GetType`) are not part of the package's API and aren't required.
- **Restructuring**: Adding, renaming, or reordering README sections fails no new test, as long as
  every member is still documented.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The tests MUST fail when a public member of `Stressor` or `StressorOptions`,
  including each overload, isn't documented in the README outside a code sample, and MUST list
  every one that isn't.
- **FR-002**: The tests MUST fail when the README documents a member of `Stressor` or
  `StressorOptions` that doesn't exist, and MUST list every one that doesn't.
- **FR-003**: The tests MUST fail when a public settable `StressorOptions` property has no row in
  the Options table, or a row names one that doesn't exist, and MUST list each.
- **FR-004**: For every option, the tests MUST compare the Options table's default with the value
  on a freshly created `StressorOptions`, as its own test case per option, so that each wrong
  default fails on its own and shows the stated and actual values.
- **FR-005**: "Public API" MUST mean every public constructor, method, property, field, and
  constant of `Stressor` and `StressorOptions`. Members inherited from `object` are excluded.
- **FR-006**: The README MUST be brought up to date in the same change, so that every new test
  passes, and the theme song's Updating check MUST be applied to that README change.
- **FR-007**: The new tests MUST sit beside the theme song README tests and follow their
  patterns, and the assembly's test counts MUST be updated for them.
- **FR-008**: The tests MUST NOT pin the README's overall structure: adding, renaming, or
  reordering sections fails a test only when it breaks one of the requirements above or an
  existing theme song test.

### Key Entities

- **Documented item**: A member as the README names it. A method or constructor is identified by
  its name and parameter names, such as `Stress(setup, test, teardown)`; an option by its name in
  the Options table; any other member by its name.
- **Public API**: Every public constructor, method, property, field, and constant of `Stressor`
  and `StressorOptions` (FR-005).
- **Stated default**: The value in the Options table's Default column for an option.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With the README updated, every new test passes, and the README documents 100% of
  the public API.
- **SC-002**: Removing any single documented item from the README makes exactly the test for that
  kind of item fail, naming that item.
- **SC-003**: Changing any single default in `StressorOptions` makes exactly one test case fail,
  for that option, showing both values.
- **SC-004**: Line and branch coverage of `DnDGen.Stress.dll` stays at 100% (II.2.c (Stress)).
- **SC-005**: A maintainer reading a failure knows what to change in the README without opening
  the test code (II.3).

## Assumptions

- The README's existing Methods and Options tables are where methods and options are documented;
  the constructors may be documented in prose, as `Stressor(options, logger)` already is.
- Formatting of the README's existing tables stays as it is unless a test needs it to change.
- This is a tests-and-documentation change: no code in the package changes, so it ships as a
  PATCH release, together with the DnDGen constitution adoption already on this branch.
- Today's README doesn't document `CountStressTestsIn`, the `Stressor` members `Options`,
  `StressTestCount`, `TimeLimit`, `TestDuration`, and `TestIterations`, `StressorOptions.AreValid`,
  or the `StressorOptions` default constants, so this change adds all of them to the README.
- Other parts of the README contract, such as the .NET and NUnit versions each major version
  requires, are out of scope here and can be their own issue.
