# Contract: README and Package Changes

## README.md

Every existing sentence stays as it is (research R7). Every link stays absolute. Additions:

**Under Usage, after the code sample and before `### Methods`:**

```markdown
### Constructors

| Constructor | What it does |
| --- | --- |
| `Stressor(options)` | Creates a stressor that logs to the console. Throws an `ArgumentException` if `options` aren't valid or count no tests. |
| `Stressor(options, logger)` | Same as above, but logs through your own `ILogger`. |
```

**In the Methods table, as its last row:**

```markdown
| `CountStressTestsIn(runningAssembly)` | Static. Counts the active (non-ignored) `[Test]` and `[TestCase]` methods in `runningAssembly`, the way `RunningAssembly` is counted. |
```

**Under Usage, after the paragraph on logging:**

```markdown
### Properties

| Property | What it is |
| --- | --- |
| `Options` | The options the stressor was created with. |
| `StressTestCount` | The number of stress tests the build time limit is divided among. |
| `TimeLimit` | The time limit for each stress test. |
| `TestDuration` | How long the current or most recent stress test has run. |
| `TestIterations` | How many iterations the current or most recent stress test has completed. |
```

**Under Options, after the time-limit formula:**

```markdown
`StressorOptions` also has:

| Property | What it is |
| --- | --- |
| `AreValid` | `true` when exactly one of `RunningAssembly` or a positive `TestCount` is set, `TestCount` isn't negative, and `TimeLimitPercentage` is greater than 0 and at most 1. The `Stressor` constructors throw if it is `false`. |
| `DefaultBuildTimeLimitInSeconds` | 3600, the default `BuildTimeLimitInSeconds`. |
| `DefaultOutputTimeLimitInSeconds` | 600, the default `OutputTimeLimitInSeconds`. |
| `DefaultConfidenceIterations` | 1,000,000, the default `ConfidenceIterations`. |
```

The Options table keeps its rows and Default formats (research R5).

## DnDGen.Stress/DnDGen.Stress.csproj

- `<Version>`: `5.0.4`.
- `<PackageReleaseNotes>`: replaced with notes for 5.0.4 only: "The README now documents every
  public member. No changes to the code since 5.0.1. If you are upgrading across several versions,
  see each version's changes at https://github.com/DnDGen/DnDGen.Stress/releases".

## Pull request

Titled `5.0.4: README documents the public API`, and covers the DnDGen constitution adoption
already on this branch.
