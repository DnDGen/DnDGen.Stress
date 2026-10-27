# Quickstart: Validating README Public-API Tests

## Prerequisites

- The branch checked out, and the .NET 10 SDK.

## Checks

1. **Tests first (II)**: with only the new tests added, `dotnet test` fails each new list test,
   naming the members the README doesn't document yet (`CountStressTestsIn`, the `Stressor`
   properties, `AreValid`, the constants, the constructors' table). The default cases pass,
   because today's defaults are already right.
2. **README updated (FR-006, SC-001)**: with the [contracts/readme.md](contracts/readme.md)
   additions, the full `dotnet test` passes, with no `--filter`, so the assembly's test counts are
   checked too (FR-007).
3. **Failures name the fix (SC-002, SC-003, SC-005)**, by temporary local edits, reverted after:
   - Delete the `TimeLimit` row: only the Property test fails, listing `TimeLimit` as missing.
   - Rename a Methods row to `Stress(tests)`: only the Method test fails, listing `Stress(test)`
     as missing and `Stress(tests)` as extra.
   - Change `TimeLimitPercentage`'s default in the README to `0.5`: only that option's case
     fails, showing `0.5` and `1`.
   - Delete a `[TestCase]` from `OptionDefault_IsDocumented`: the test that every option has a
     case fails, naming the option.
4. **Structure is free (FR-008)**: move the Options section above Usage locally: every test still
   passes. Revert.
5. **Coverage (SC-004)**: `dotnet test --settings CodeCoverage.runsettings --collect "Code
   Coverage"` reports 100% line and branch coverage of `DnDGen.Stress.dll`.
6. **Theme song (FR-006)**: the theme song tests pass, confirming no quoted phrase was changed
   (research R7).
