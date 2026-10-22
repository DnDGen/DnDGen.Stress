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
- **A new pattern must be worth everyone conforming to it.** Adopting one is a decision for the
  whole codebase, though the move happens incrementally as code is touched (Principle III). If the
  benefit doesn't justify that, the existing pattern stays. IDE and analyzer suggestions are new
  patterns too, and new ones keep arriving as the language changes, so each is judged by three
  criteria rather than a fixed list. A suggestion is applied only if all three hold:
  1. **Safe, one-step fix.** The IDE offers a quick fix (Ctrl+.) and applying it doesn't change
     behaviour. A suggestion with no fix is ignored. One that changes behaviour, such as making an
     `async` test helper synchronous, is not a style fix.
  2. **Clearer to a reader**, not just shorter or more explicit. Collection expressions and
     removing a redundant cast pass. Explicit types instead of `var` fail, because a good name
     should already say what the variable is; so do ternaries in place of `if`, and `_ =` in front
     of every ignored return value.
  3. **One answer the whole codebase can follow.** Rules that flip-flop with their own opposite,
     such as block versus expression bodies, aren't enforced either way: expression bodies are
     preferred for one-line members. Braces may be left off only when every branch of an
     `if`/`else` is a single line.
- **Code reads like well-written prose.** Names and structure carry the meaning, so a reader can
  follow the code without comments. Design follows SOLID, with each concern kept in its own place.
  Comments are added only by a human, or when a human asks for one, and only to record what the
  code cannot say:
  - `//INFO:` why a choice was made, typically so a later change doesn't undo it and break things
  - `//HACK:` an acknowledged poor solution
  - `//BUG:` an acknowledged known defect

### II. Tests Are the Spec

Behaviour is defined by tests before it is implemented: write or change the tests first, see them
fail for the expected reason, then change the code until they pass.

- **What to test: what a consumer does and sees.** Each case is a sentence from the consumer's
  side: "if I do X, I expect Y, and I'll confirm that with Z." The list of cases is written that
  way, as if the implementation had never been seen, and cases are named as consumer outcomes in
  the consumer's vocabulary. Every capability and option has its own case, even when two cases
  run the same lines of code.
- **How much to test: everything when it's cheap, chosen values when it isn't.**
  - When the valid range of an input is small enough, test all of it.
  - Otherwise, test anything that counts or repeats at 0, 1, and many, where "many" is chosen to
    expose off-by-one and batching mistakes (10, or 1337, not 2). Test every boundary and limit,
    along with the value just past it.
  - Coverage confirms nothing was missed. Line and branch coverage of `DnDGen.Stress.dll` stays at
    100%. An uncovered line is either a missing case or code that can never run. Code that can
    never run is removed only after showing why it can't run, what it was originally there for,
    and that removing it changes nothing a consumer can observe, and after the maintainer agrees.
- **How a test fails: when (not if) it fails, it says exactly where, why, and what to fix.**
  - Use the strongest assertion available: compare the whole observed value, so the failure shows
    what actually happened.
  - No implied assertions: everything a test relies on is written down in the test. The current
    pattern of chained mocks (input → mock A → mock B → mock C → result), each answering only its
    exact expected input, counts as written down, because a correct result proves every link. A
    broken link shows up only as a wrong final result; a better pattern for that can replace this
    one once it's agreed (Principle I).
  - A failure means one thing: this case found a defect in the code. Each case runs, passes, and
    fails on its own, and none fails because of the machine (core count, CPU speed), timing slack,
    or another test.

### III. Leave It Better Than You Found It

Every change may also improve the code it touches, as long as the improvement doesn't change
behaviour.

- **In scope: maintenance.** Updating packages (including major versions), applying IDE
  suggestions that pass Principle I's criteria, and replacing outdated patterns or practices with
  the codebase's agreed ones. A package update that breaks something is fixed in the same change,
  or held back with the reason stated in the pull request.
- **Improve where you are, in its own commit.** Improvements are made in the code already being
  changed, each in its own commit. Improving one file doesn't oblige anyone to make the same
  improvement everywhere; the rest of the codebase gets it the next time it is touched.
- **Out of scope: behaviour.** Bug fixes, performance work, or features discovered along the way
  are recorded as GitHub issues so they aren't lost, and handled as their own changes.

**How the three interact:** an improvement (III) moves the code toward the codebase's agreed
patterns (I) and stays green against the tests (II). Extensive automated coverage is what makes
routine cleanup safe in this repository, and improving only where you are keeps each change small.

## Technology & Release Constraints

- **Stack**: C# on Long-Term Support .NET releases only (for example `net8.0`, `net10.0`), never
  Standard-Term Support. NUnit, as both the package's dependency and its test framework, with Moq
  for test doubles and Microsoft.Extensions.Logging for output.
- **Versioning and release**: Semantic versioning in the csproj `<Version>`. MAJOR for breaking
  changes, including removing or renaming public members, changing option defaults or the
  time-limit formula, and raising the minimum .NET or NUnit major version. MINOR for additive
  public API or options. PATCH for fixes and internal changes. Release pull requests bump
  `<Version>`, replace `<PackageReleaseNotes>` with notes for that release only, and are titled
  `X.Y.Z: <summary>`. On `main`, `cicd/build.yml` builds and runs every test in the `Stress`
  configuration with code coverage, then tags the commit with `<Version>`; `cicd/release.yml`
  publishes to NuGet.org and creates a GitHub release.
  - TODO(NON_RELEASE_MERGES): Because every build on `main` tags `<Version>`, a merge that doesn't
    bump it fails the build at the tagging step. Decide how documentation, tooling, and Spec Kit
    changes reach `main`.
- **README as contract**: The README is packed into the NuGet package and is the only
  documentation consumers see. It documents every `Stressor` constructor and method and every
  `StressorOptions` setting with its default, states which .NET and NUnit versions each major
  version requires, and is updated in the same pull request as any change to them.

## Development Workflow

- Changes are made on a branch, reach `DnDGen/DnDGen.Stress` `main` by pull request, and are
  reviewed by the maintainer.
- Pull request descriptions are written from the actual diff against `main`, and state what was
  verified and what was not.
- Spec Kit artifacts under `specs/` record one change at the time it was made. They are not
  maintained after the change merges, and MAY be deleted later. The lasting record of how the
  package behaves is its tests (Principle II), the README, and this constitution.

## Governance

This constitution supersedes other practices for this repository.

- **Rule of three, at every level.** There are about three core principles, and about three
  points under each. Needing more usually means some points are expressions of one underlying
  idea; naming that idea tends to reshape the other principles too. A new principle or point
  merges with or replaces an existing one rather than simply being added.
- **Amendments** are made by pull request, with the reasoning in the pull request description, and
  versioned semantically: MAJOR for removing or redefining a principle, MINOR for a new principle
  or materially expanded guidance, PATCH for clarifications and wording. Principles that apply
  across the DnDGen organisation MAY later move to a shared, organisation-wide document.
- **Compliance**: Every plan and pull request is checked against these principles. A deviation
  MUST be justified in the pull request, or the change is not merged.

**Version**: 1.0.0 | **Ratified**: 2026-10-22 | **Last Amended**: 2026-10-22
