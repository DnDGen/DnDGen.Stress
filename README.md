# DnDGen.Stress
A framework for running stress tests against random generators

[![Build Status](https://dev.azure.com/dndgen/DnDGen/_apis/build/status%2FDnDGen.Stress%20-%20Build?branchName=main)](https://dev.azure.com/dndgen/DnDGen/_build/latest?definitionId=11&branchName=main) [![Theme Song](https://img.shields.io/badge/theme_song-Suno-ff69b4)](https://suno.com/s/TLKVK477Efrel4u8)

Random generators are hard to test: a bug might only show up once in a few thousand runs. `Stressor` runs a test over and over,
until either a time limit expires or enough iterations have completed to be confident in the result.
Any failure on any iteration fails the test, with the original stack trace preserved.

It is built for [NUnit](https://nunit.org/) and is used by the other DnDGen generator libraries.

## Installing

```
dotnet add package DnDGen.Stress
```

Version 5 requires .NET 10 and NUnit 5. Use version 4.x if you are still on .NET 8 or NUnit 4.

## Usage

Create a `Stressor` once per test fixture, then call it from your tests.

```c#
using DnDGen.Stress;
using NUnit.Framework;

[TestFixture]
public class MyGeneratorStressTests
{
    private Stressor stressor;
    private MyGenerator generator;

    [OneTimeSetUp]
    public void StressSetup()
    {
        var options = new StressorOptions();
        options.RunningAssembly = typeof(MyGeneratorStressTests).Assembly;

#if STRESS
        options.IsFullStress = true;
#else
        options.IsFullStress = false;
#endif

        stressor = new Stressor(options);
    }

    [SetUp]
    public void Setup()
    {
        generator = new MyGenerator();
    }

    [Test]
    public void StressGenerate()
    {
        stressor.Stress(() =>
        {
            var thing = generator.Generate();
            Assert.That(thing.Name, Is.Not.Empty);
        });
    }
}
```

### Methods

| Method | What it does |
| --- | --- |
| `Stress(test)` | Runs `test` repeatedly until the time limit or the confidence iterations are reached. |
| `Stress(setup, test, teardown)` | Same as above, but runs `setup` before and `teardown` after every iteration. `teardown` runs even if `test` throws. |
| `StressAsync(test)` | Async version of `Stress`. Runs `MaxAsyncBatch` tests concurrently per batch. |
| `StressAsync(setup, test, teardown)` | Async version with per-iteration setup and teardown. |
| `Generate(generate, isValid)` | Calls `generate` until `isValid` returns `true`, then returns the result. It has no time limit of its own, so use it inside `Stress` or `GenerateOrFail`. |
| `GenerateOrFail(generate, isValid)` | Calls `generate` until `isValid` returns `true`, and fails the test if the time limit is reached first. Useful for verifying that a rare result *can* be generated. |

After each run, `Stressor` logs a summary (duration, iterations, iterations per second, and likely status) through `ILogger`.
By default it logs to the console; pass your own `ILogger` to the `Stressor(options, logger)` constructor to change that.

## Options

| Option | Default | Description |
| --- | --- | --- |
| `IsFullStress` | `false` | When `false`, every stress test runs for 1 second. When `true`, the time limits below apply. |
| `RunningAssembly` | `null` | The test assembly. Used to count the active (non-ignored) `[Test]` and `[TestCase]` methods in it. |
| `TestCount` | `0` | The number of stress tests, if you would rather set it directly. Set exactly one of `RunningAssembly` or `TestCount`. |
| `BuildTimeLimitInSeconds` | 3600 (1 hour) | The total time budget for all stress tests, divided evenly among them. |
| `OutputTimeLimitInSeconds` | 600 (10 minutes) | The maximum time a single stress test may run. Useful for build systems that fail a step with no output for too long. |
| `TimeLimitPercentage` | `1` | Scales both time limits, to leave headroom for setup and non-stress tests. Must be greater than 0 and at most 1. |
| `ConfidenceIterations` | 1,000,000 | A stress test stops early once it completes this many iterations. |
| `MaxAsyncBatch` | `Environment.ProcessorCount` | How many async tests run concurrently per batch. |

In full stress mode, each test's time limit is:

```
min(BuildTimeLimitInSeconds * TimeLimitPercentage / testCount, OutputTimeLimitInSeconds * TimeLimitPercentage)
```

## Running a full stress build

The usage example above switches on full stress with a `STRESS` compilation symbol. The .NET SDK defines a symbol from the
build configuration name, so building with a configuration named `Stress` is enough:

```
dotnet test -c Stress
```

This repository's own build pipeline does exactly that.

## Theme Song

DnDGen.Stress has its own theme song: [Over and Over -DnDGen.Stress Theme-](https://suno.com/s/TLKVK477Efrel4u8).
It follows this README, so a change here may need the song updated too.
See the song's [lyrics, notes, and update procedure](https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md).
