# DnDGen.Stress Constitution

## Core Principles

### I. Consistency Is King

A codebase that does everything one way is easier to work in than a "cleaner" one that does
things a hundred ways. Consistency is the first test of any change.

**I.1 Emulate what exists, here first, then across DnDGen.** The preferred implementation follows
the patterns and practices already in this codebase. When this codebase has no precedent, look to
the other DnDGen repositories before inventing one: what's new to this repository is often
established elsewhere. Confirm a pattern from more than one example before treating it as the
convention.

- *Example:* RollGen's expression evaluation gained caching through
  `ExpressionEvaluatorCachingProxy`, a caching proxy around `AlbatrossExpressionEvaluator` that
  follows the proxy structure in DnDGen.Infrastructure, instead of caching inside the evaluator.

**I.2 A new pattern must be worth everyone conforming to it.** Adopting one is a decision for the
whole codebase, though the move happens incrementally as code is touched (III.2). A pattern is
worth it when it is clearer (I.2.a) or measurably better (I.2.b), and gives one answer the whole
codebase can follow (I.2.c); otherwise the existing pattern stays. IDE and analyzer suggestions are
new patterns too, judged the same way, and only when the IDE offers a quick fix (Ctrl+.) that
doesn't change behaviour. A suggestion with no fix is ignored.

- *Example:* Making an `async` test helper synchronous changes how its exceptions reach
  `StressAsync`, so it is not a style fix.
- **I.2.a Clearer to a reader**, not just shorter or more explicit.
  - *Examples:* Collection expressions and removing a redundant cast pass. Explicit types instead
    of `var` fail, because a good name should already say what the variable is; so do ternaries
    in place of `if`, and `_ =` in front of every ignored return value.
- **I.2.b Measurably better.** The benefit can be measured and is big enough to notice: a change
  within about ±10% counts as no change. Work like this is its own change, not part of another
  (III.3).
  - *Example:* In CreatureGen, pre-caching template-based creature groups made the verifier much
    faster. A 17% speed-up would qualify; a 7% one would not.
- **I.2.c One answer the whole codebase can follow.**
  - *Examples:* Block and expression bodies flip-flop with each other, so neither is enforced;
    expression bodies are preferred for one-line members. Braces may be left off only when every
    branch of an `if`/`else` is a single line.

**I.3 Code reads like well-written prose.** Names and structure carry the meaning, so a reader can
follow the code without comments. Design follows SOLID, with each concern kept in its own place.
Comments are added only by a human, or when a human asks for one, and only to record what the code
cannot say:

- **I.3.a `//INFO:`** why a choice was made, typically so a later change doesn't undo it and break
  things.
- **I.3.b `//HACK:`** an acknowledged poor solution.
- **I.3.c `//BUG:`** an acknowledged known defect.

### II. Tests Are the Spec

Behaviour is defined by tests before it is implemented: write or change the tests first, see them
fail for the expected reason, then change the code until they pass.

**II.1 What to test: what a consumer does and sees.**

- **II.1.a** Each case is a sentence from the consumer's side: "if I do X, I expect Y, and I'll
  confirm that with Z."
- **II.1.b** The list of cases is written as if the implementation had never been seen, and cases
  are named as consumer outcomes, in the consumer's vocabulary.
- **II.1.c** Every capability and option has its own case, even when two cases run the same lines
  of code.

**II.2 How much to test: everything when it's cheap, a deliberate sample when it isn't.**

- **II.2.a** When the valid range of an input is small enough, test all of it.
- **II.2.b** Otherwise, sample the range deliberately:
  - **II.2.b.i** Anything that counts or repeats is tested at 0, 1, and many. "Many" is the
    largest realistic count: a number that would expose off-by-one or batching mistakes, such as
    10 or 1337, is stronger, but 2 is fine where larger counts aren't realistic.
    - *Example:* CreatureGen tests chaining 0, 1, and 2 templates, and a few chains of 3. Chaining
      10 templates isn't realistic.
  - **II.2.b.ii** A wide range is tested at each order of magnitude.
    - *Example:* For 1 to 1,000,000: 1, 2, 10, 100, 1,000, 10,000, 100,000, and 1,000,000.
  - **II.2.b.iii** Every boundary and limit is tested, along with the value just inside it and
    values one step and more than one step past it: 0, 1, and many applied to the distance from
    the boundary.
    - *Example:* For a lower limit of 1: 2, 1, 0, and -1.
    - *Example:* Converting a double to a bool, 1 and 2 are true and 0 is false. Testing past the
      boundary showed that -1, 0.5, and -0.5 are all true: only 0 is false.
- **II.2.c** Coverage confirms nothing was missed. Line and branch coverage of `DnDGen.Stress.dll`
  stays at 100%. An uncovered line is either a missing case or code that can never run. Code that
  can never run is removed only after showing why it can't run, what it was originally there for,
  and that removing it changes nothing a consumer can observe, and after the maintainer agrees.

**II.3 How a test fails: when (not if) it fails, it says exactly where, why, and what to fix.**

- **II.3.a** Use the strongest assertion available: compare the whole observed value, so the
  failure shows what actually happened.
  - *Example:* Compare the entire collection, not just its count.
  - *Example:* For a complex object, a run of assertions on each of its properties is fine, as
    long as together they cover the whole object.
- **II.3.b** No implied assertions: everything a test relies on is written down in the test.
  - *Example:* The current pattern of chained mocks (input → mock A → mock B → mock C → result),
    each answering only its exact expected input, counts as written down, because a correct result
    proves every link. A broken link shows up only as a wrong final result; a better pattern for
    that can replace this one once it's agreed (I.2).
- **II.3.c** A failure means one thing: this case found a defect in the code. Each case runs,
  passes, and fails on its own, and none fails because of the machine, timing slack, or another
  test.
  - *Example:* Async batching tests set `MaxAsyncBatch` explicitly instead of inheriting the
    machine's core count.

### III. Leave It Better Than You Found It

Every change may also improve the code it touches, as long as the improvement doesn't change
behaviour.

**III.1 In scope: maintenance.**

- **III.1.a** Updating packages, including major versions. An update that breaks something is fixed
  in the same change, or held back with the reason stated in the pull request.
- **III.1.b** Replacing outdated patterns and practices with the codebase's agreed ones, including
  IDE suggestions that pass I.2.
- **III.1.c** Correcting stale comments and documentation, or removing them once they're no longer
  true. New comments still follow I.3.

**III.2 Improve where you are, in its own commit.** Improvements are made in the code already being
changed, each in its own commit. An improvement made in one place, especially a new pattern, does
commit the codebase to the same improvement everywhere, but not immediately: the rest of the
codebase gets it the next time it is touched.

**III.3 Out of scope: behaviour.** Bug fixes, performance work, or features discovered along the
way are recorded as GitHub issues so they aren't lost, and handled as their own changes.

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
  public API or options. PATCH for fixes, internal changes, and changes outside the package itself,
  such as dependencies, the pipeline, documentation, or Spec Kit. Every merge to `main` is a
  release: its pull request bumps `<Version>`, replaces `<PackageReleaseNotes>` with notes for that
  release only, and is titled `X.Y.Z: <summary>`. A release that doesn't change the package says
  so, names the last version that did, and points to the GitHub releases page; anyone upgrading
  across several versions reads each version's changes there. On `main`, `cicd/build.yml` builds and runs every
  test in the `Stress` configuration with code coverage, then tags the commit with `<Version>`;
  `cicd/release.yml` publishes to NuGet.org and creates a GitHub release.
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
  package behaves is its tests (II), the README, and this constitution.

## Governance

This constitution supersedes other practices for this repository.

- **Rule of three, at every level.** There are about three core principles, and about three
  points under each. Needing more usually means some points are expressions of one underlying
  idea; naming that idea tends to reshape the other principles too. A new principle or point
  merges with or replaces an existing one rather than simply being added.
- **Amendments** are made by pull request, with the reasoning in the pull request description, and
  versioned semantically: MAJOR for removing or redefining a principle, MINOR for a new principle
  or materially expanded guidance, PATCH for clarifications and wording. Principles are labelled
  I, II, III; their points I.1, I.2; sub-points I.2.a; and a further level II.2.b.i, so that plans
  and reviews can cite them. Examples sit unlabelled under the rule they illustrate. Principles
  that apply across the DnDGen organisation MAY later move to a shared, organisation-wide document.
- **Compliance**: Every plan and pull request is checked against these principles, citing them by
  label. A deviation MUST be justified in the pull request, or the change is not merged.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): the date this constitution is first
merged to `main` | **Last Amended**: 2026-10-23
