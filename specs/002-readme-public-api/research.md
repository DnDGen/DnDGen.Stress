# Research: README Documents the Public API

Decisions made while planning [spec.md](spec.md). Labels such as I.1 and II.3.a cite the
[DnDGen constitution](https://github.com/DnDGen/.github/blob/main/constitution/CONSTITUTION.md);
labels ending in (Stress) cite this repository's additions.

## R1: What counts as "documented"

- **Decision**: A member is documented when it is an inline code span in the first column of a
  README table, outside code samples, whose first header cell names its kind: `Constructor`,
  `Method`, `Property`, or `Option`. A kind may have more than one table; their rows are merged.
- **Rationale**: Tables are already how the README documents members (Methods, Options), so this
  emulates what exists (I.1). A table's first column gives a closed list of what the README claims
  to document, which is what makes the reverse check possible (FR-002). Finding tables by their
  header, not by their section heading, leaves the README free to restructure (FR-008).
- **Alternatives considered**:
  - *Any code span anywhere outside code samples.* Prose like "pass your own `ILogger`" makes the
    reverse check impossible, because there is no way to tell a documented member from a passing
    mention.
  - *Tables found by section heading.* Pins the section names, which the spec rules out (FR-008).

## R2: How a member is written

- **Decision**: Constructors and methods are written as `Name(parameter, names)`, using the
  parameter names from the code; constructors use the type name, `Stressor(options)`. Generic
  type parameters are left out, as in `Generate(generate, isValid)`. Every other member is written
  as its bare name.
- **Rationale**: This is exactly how the README's Methods table already writes them (I.1), and the
  parameter names are what tell overloads apart (spec, Edge Cases).
- **Alternatives considered**: *Parameter types*, such as `Stress(Action)`: longer, and the
  README's readers already know the overloads by their parameter names.

## R3: Where the undocumented members go

- **Decision**: The README gains a **Constructors** table and a **Properties** table for
  `Stressor` under Usage, `CountStressTestsIn(runningAssembly)` joins the Methods table, and a
  **Properties** table for `StressorOptions` (`AreValid` and the three `Default…` constants)
  follows the time-limit formula under Options. Wording is in
  [contracts/readme.md](contracts/readme.md).
- **Rationale**: Each member sits beside the members a consumer uses it with: constructors and
  properties beside the methods, `AreValid` and the default constants beside the options they
  check and define. `Stressor`'s fields (`Options`, `StressTestCount`) are listed as properties,
  because a consumer reads them the same way and can't tell the difference.
- **Alternatives considered**:
  - *Constants in the Options table's Default column*, such as
    `` `DefaultBuildTimeLimitInSeconds` (3600) ``: puts two facts in one cell and makes the default
    harder to compare (R5).
  - *A separate `Constant` kind*: a fifth kind of table for three rows. A consumer reads a
    constant like a read-only property.

## R4: One test per kind, comparing whole lists

- **Decision**: One test per kind (constructors, methods, properties, options) compares the list
  documented in the README with the list from the code, using `Is.EquivalentTo`, so one assertion
  reports both the missing and the extra members.
- **Rationale**: Each kind is its own capability, so it has its own case (II.1.c). Comparing the
  whole list is the strongest assertion, and its failure lists every missing and extra member at
  once (II.3.a), which is what FR-001 and FR-002 ask for.
- **Alternatives considered**: *Separate "missing" and "extra" tests per kind*: twice the tests,
  for a failure the equivalence message already splits into missing and extra items.

## R5: Checking defaults

- **Decision**: `OptionDefault_IsDocumented` has a `[TestCase]` per option, by name. Each reads the
  Default cell, removes backticks, a trailing parenthetical such as `(1 hour)`, and thousands
  separators, and compares it with the default on a new `StressorOptions` written the way the
  README writes values: `false`, `null`, `0`, `1`, `3600`. A stated default may instead be a
  named value the test knows, and for now there is one: `Environment.ProcessorCount`. A further
  test confirms the `[TestCase]`s name every option, so a new option can't skip the check.
- **Rationale**: One case per option, so a wrong default fails alone and shows both values
  (II.1.c, FR-004, SC-003). `[TestCase]` is the pattern everywhere in these tests (I.1), and
  `Stressor` counts only `[Test]` and `[TestCase]`, so `[TestCaseSource]` would also be a new
  pattern. Reading the tests' own attributes follows `StressorTests.TestCaseCountIsCorrect`.
  `MaxAsyncBatch`'s default depends on the machine, so the README states the expression and both
  sides evaluate it on the machine running the tests (II.3.c).
- **Alternatives considered**:
  - *One test comparing every default at once*: covers new options automatically, but a wrong
    default doesn't fail on its own (II.1.c).
  - *`[TestCaseSource]` over the options*: covers new options automatically, but is a new pattern
    that `Stressor` doesn't count (I.2).
  - *Expected README text in each `[TestCase]`*: copies the README into the tests, so a changed
    default would need two edits and the test would compare the README with itself.

## R6: Out of scope

- **Protected members** (`iterations`, `logger`, `StressSetup`, `StressTearDown`) are visible only
  to subclasses. The clarified scope is public members (FR-005), so they aren't required.
- **The .NET and NUnit versions each major version requires** are part of the README contract but
  not of this feature (spec, Assumptions).
- Both can be their own issues if wanted (III.3).

## R7: Theme song check

- **Decision**: No song update. The change adds tables and rows, and leaves every existing README
  sentence as it is, so no phrase in the record's Sources table is reworded or removed (check 1),
  and the README's high-level ideas and vibe don't change (check 2). The theme song tests confirm
  check 1.
- **Confirmed against the diff** (2026-10-28): `git diff main -- README.md` adds 28 lines and
  removes none, and the theme song tests pass (check 1). For check 2, each addition is reference
  detail for an idea the song already sings:

  | Addition | Already in the song |
  |---|---|
  | Constructors table | "Create a Stressor once per test fixture", "Or to a logger of your own" |
  | `CountStressTestsIn` row | "Count the active tests in the running assembly" |
  | `Stressor` Properties table | The time limit (Breakdown, Verse 4), and duration and iterations (Outro) |
  | `AreValid` row | "Set exactly one of them, never both", "Greater than zero, and at most one" |
  | `Default…` constants | "One hour for the build", "Ten minutes at most", "A million iterations" |

  The one new fact, that the constructors throw on invalid options, is an error case, not a
  high-level idea. So the song needs no update (check 3), and the record stays as it is.
