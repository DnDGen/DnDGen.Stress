# DnDGen.Stress Constitution

## Core Principles

### I. Consistency Is King

A codebase that does everything one way is easier to work in than a "cleaner" one that does
things a hundred ways. Consistency is the first test of any change.

- **Emulate what exists, here first, then across DnDGen.** The preferred implementation follows
  the patterns and practices already in this codebase. When this codebase has no precedent, look
  to the other DnDGen repositories before inventing one: what's new to this repository is often
  established elsewhere. For example, RollGen's caching was added as a caching proxy that follows
  the structure in DnDGen.Infrastructure, not as caching inside the generator itself. Confirm a
  pattern from more than one example before treating it as the convention.
- **A new pattern must be worth everyone conforming to it.** Introducing one means deciding that
  the whole codebase should move to it. The move itself happens incrementally, as code is touched
  (Principle III). If the benefit doesn't justify that, the existing pattern stays.
- **Code reads like well-written prose.** Names and structure carry the meaning, so a reader can
  follow the code without comments. Design follows SOLID, with each concern kept in its own place.
- **No comments except the three human tags.** Code comments are added only by a human, or when a
  human asks for one, and only to record what the code cannot say:
  - `//INFO:` why a choice was made, typically so a later change doesn't undo it and break things
  - `//HACK:` an acknowledged poor solution
  - `//BUG:` an acknowledged known defect
- **IDE suggestions are applied when they pass the criteria** under Development Workflow.

### II. Tests Are the Spec

Behaviour is defined by tests before it is implemented.

- **Red, then green.** Write or change the tests first, see them fail for the expected reason,
  then change the code until they pass.
- **What to test: what a consumer does and sees.** Each case is a sentence from the consumer's
  side: "if I do X, I expect Y, and I'll confirm that with Z." The list of cases is written that
  way, as if the implementation had never been seen, and cases are named as consumer outcomes in
  the consumer's vocabulary. Every capability and option has its own case, even when two cases
  run the same lines of code.
- **How much to test: everything when it's cheap, chosen values when it isn't.** When the valid
  range of an input is small enough, test all of it. Otherwise:
  - Anything that counts or repeats is tested at 0, 1, and many. "Many" is chosen to expose
    off-by-one and batching mistakes (10, or 1337), not 2.
  - Every boundary and limit is tested, along with the value just past it.
- **How a test fails: when (not if) it fails, it says exactly where, why, and what to fix.**
  - Use the strongest assertion available: compare the whole observed value, so the failure shows
    what actually happened.
  - No implied assertions: everything a test relies on is written down in the test. A mock setup
    that answers only the exact expected input counts as written down, so a chain of them
    (input → mock A → mock B → mock C → result) proves every link when the result is right. When a
    broken link would only show up as a wrong final result, make the failure name the link, for
    example with a strict mock, which fails on the unexpected call and shows its arguments.
  - Each case runs, passes, and fails on its own.
  - A test fails only because the code is wrong, never because of the machine (core count, CPU
    speed), timing slack, or another test.
- **Coverage follows.** Line and branch coverage of `DnDGen.Stress.dll` stays at 100%. An
  uncovered line is either a missing case or code that can never run. Code that can never run is
  removed only after showing why it can't run, what it was originally there for, and that removing
  it changes nothing a consumer can observe, and after the maintainer agrees.

### III. Leave It Better Than You Found It

Every change may also improve the code it touches, as long as the improvement doesn't change
behaviour.

- **Always in scope:** updating packages (including major versions), applying IDE suggestions,
  and replacing outdated patterns or practices with the codebase's agreed ones (Principle I).
  Package updates that break something are fixed in the same change, or held back with the reason
  stated in the pull request.
- **Improve where you are.** Improvements are made in the code already being changed. Improving
  one file doesn't oblige anyone to make the same improvement everywhere; the rest of the codebase
  gets it the next time it is touched.
- **Kept apart from the change itself:** each improvement goes in its own commit.
- **Never in scope:** bug fixes, performance work, or features discovered along the way. Those are
  recorded as GitHub issues so they aren't lost, and handled as their own changes.

**How the three interact:** an improvement (III) moves the code toward the codebase's agreed
patterns (I) and stays green against the tests (II). Extensive automated coverage is what makes
routine cleanup safe in this repository, and improving only where you are keeps each change small.

## Technology & Release Constraints

- **Stack**: C# on Long-Term Support .NET releases only (for example `net8.0`, `net10.0`), never
  Standard-Term Support. NUnit, as both the package's dependency and its test framework, with Moq
  for test doubles and Microsoft.Extensions.Logging for output.
- **Versioning**: Semantic versioning in the csproj `<Version>`. MAJOR for breaking changes,
  including removing or renaming public members, changing option defaults or the time-limit
  formula, and raising the minimum .NET or NUnit major version. MINOR for additive public API or
  options. PATCH for fixes and internal changes.
- **README as contract**: The README is packed into the NuGet package and is the only
  documentation consumers see. It documents every `Stressor` constructor and method and every
  `StressorOptions` setting with its default, states which .NET and NUnit versions each major
  version requires, and is updated in the same pull request as any change to them.
- **Release flow**: Azure Pipelines. `cicd/build.yml` builds and runs every test in the `Stress`
  configuration with code coverage. On `main` it tags the commit with `<Version>`, and
  `cicd/release.yml` publishes to NuGet.org and creates a GitHub release. Release pull requests
  bump `<Version>`, replace `<PackageReleaseNotes>` with notes for that release only, and are
  titled `X.Y.Z: <summary>`.
- TODO(NON_RELEASE_MERGES): Every build on `main` tags `<Version>`, so a merge that doesn't bump it
  fails the build at the tagging step. Decide how documentation, tooling, and Spec Kit changes
  reach `main`.

## Development Workflow

- Changes are made on a branch, reach `DnDGen/DnDGen.Stress` `main` by pull request, and are
  reviewed by the maintainer.
- Pull request descriptions are written from the actual diff against `main`, and state what was
  verified and what was not.
- **IDE and analyzer suggestions** keep changing as the language does, so they are judged against
  three criteria rather than a fixed list. A suggestion is applied only if all three hold:
  1. **Safe, one-step fix.** The IDE offers a quick fix (Ctrl+.) and applying it doesn't change
     behaviour. A suggestion with no fix is ignored. One that changes behaviour, such as making an
     `async` test helper synchronous, is not a style fix.
  2. **Clearer to a reader**, not just shorter or more explicit. Collection expressions and
     removing a redundant cast pass. Explicit types instead of `var` fail, because a good name
     should already say what the variable is; so do ternaries in place of `if`, and `_ =` in front
     of every ignored return value.
  3. **One answer the whole codebase can follow** (Principle I). Rules that flip-flop with their
     own opposite, such as block versus expression bodies, aren't enforced either way: expression
     bodies are preferred for one-line members. Braces may be left off only when every branch of
     an `if`/`else` is a single line.
- Spec Kit artifacts under `specs/` record one change at the time it was made. They are not
  maintained after the change merges, and MAY be deleted later. The lasting record of how the
  package behaves is its tests (Principle II), the README, and this constitution.

## Governance

This constitution supersedes other practices for this repository.

- **Few principles.** The core principles are kept to about three, so they can be held in mind;
  detail is derived from how they interact. A new principle should merge with or replace an
  existing one rather than simply being added.
- **Amendments** are made by pull request, with the reasoning in the pull request description.
- **Versioning of this constitution** follows semantic versioning: MAJOR for removing or redefining
  a principle, MINOR for a new principle or materially expanded guidance, PATCH for clarifications
  and wording.
- **Compliance**: Every plan and pull request is checked against these principles. A deviation
  MUST be justified in the pull request, or the change is not merged.
- Principles that apply across the DnDGen organisation MAY later move to a shared,
  organisation-wide document, with this file keeping only what is specific to DnDGen.Stress.

**Version**: 1.0.0 | **Ratified**: 2026-10-22 | **Last Amended**: 2026-10-22
