# Data Model: README Documents the Public API

There is no stored data. The tests compare two lists of members: the one the code has and the one
the README documents.

## Kind

What a member is to a consumer, and the first header cell of the README tables that document it
(research R1, R3).

| Kind | Members from the code | README tables |
|------|-----------------------|---------------|
| Constructor | `Stressor`'s public constructors | `Constructor` |
| Method | Public methods declared on `Stressor` or `StressorOptions`, static or not, excluding property accessors | `Method` |
| Property | Public fields, constants, and read-only properties declared on `Stressor` or `StressorOptions` | `Property`, one under Usage and one under Options |
| Option | Public settable properties declared on `StressorOptions` | `Option` |

Members inherited from `object`, and protected members, belong to no kind (FR-005, research R6).

## Member

| Field | Rule | Source |
|-------|------|--------|
| Kind | One of the kinds above | R1 |
| Written as | `Name(parameter, names)` for constructors and methods, with the type name for constructors and no generic type parameters; the bare name otherwise | R2 |

Two members of one kind are the same when they are written the same. Overloads differ by their
parameter names.

## Stated default

The Default cell of an Option row (research R5).

| Field | Rule |
|-------|------|
| Text | The cell, without backticks, a trailing parenthetical, or thousands separators |
| Value | The text, unless it is a known named value (`Environment.ProcessorCount`), which is evaluated |
| Matches when | Its value equals the option's value on a new `StressorOptions`, written as the README writes values: `false`, `null`, `0`, `1`, `3600` |
