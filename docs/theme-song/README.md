# Over and Over -DnDGen.Stress Theme-

[![Album art](cover.png)](https://suno.com/)

The DnDGen.Stress theme song: forge folk metal. Its lyrics come from the
[DnDGen.Stress README](https://github.com/DnDGen/DnDGen.Stress/blob/main/README.md).

**Listen on [Suno](https://suno.com/).**

## Updating

## Title

```
Over and Over -DnDGen.Stress Theme-
```

## Style

```
Dwarven forge folk metal, a stomping mid-tempo singalong anthem, Chugging palm-muted rhythm guitars and galloping double-kick drums, Hammer-on-anvil strikes ringing on every backbeat, with bellows hiss and clanging steel as percussion, Bright folk melodies on fiddle, accordion and tin whistle, doubled by a soaring synth orchestra, A deep, proud, clean baritone lead vocal in heroic power metal style, A massive gang-vocal choir of dwarven smiths chanting the choruses, with call-and-response shouts and rhythmic "hey!" grunts, Verses tight and rhythmic like a work song, choruses huge and anthemic, A stripped-down breakdown of anvil and choir alone, then a key change into a triumphant final chorus, Polished, punchy, festival-ready production, Cheerful, stubborn and deadly serious about striking the same steel over and over until it breaks or holds
```

## Lyrics

```
[Intro - a lone anvil rings, then the whole band stomps in]
(Hey! Hey! Hey! Hey!)
Over and over!
(Over and over!)

[Verse 1 - tight and rhythmic, like a work song]
Random generators are hard to test
A bug might only show up once
Once in a few thousand runs
And then it's gone again
So the Stressor runs the test
Over and over and over again
Built for NUnit, forged for the generators
Of DnDGen

[Pre-Chorus]
Until the time limit expires
(Expires!)
Or enough iterations have completed
To be confident in the result
(Confident!)

[Chorus - huge gang vocals, call and response]
Over and over!
(Over and over!)
Strike it again!
(Strike it again!)
Any failure on any iteration
Fails the test!
(Fails the test!)
With the original stack trace preserved
Over and over, again and again!

[Verse 2]
Add DnDGen.Stress to your tests
Version five needs .NET ten and NUnit five
Use version four if you're still on .NET eight
Create a Stressor once per test fixture
Then call it from your tests
Call on the Stressor, and the hammer falls
Assert the name is never empty
(Never empty!)

[Pre-Chorus]
Setup before and teardown after
Every iteration, every one
And the teardown runs
Even if the test throws!
(Even if it throws!)

[Chorus - huge gang vocals, call and response]
Over and over!
(Over and over!)
Strike it again!
(Strike it again!)
Any failure on any iteration
Fails the test!
(Fails the test!)
With the original stack trace preserved
Over and over, again and again!

[Verse 3 - faster, galloping]
Async, batch after batch
A batch of tests at a time
As many as the processor count
Running side by side
Generate until the result is valid
Or fail if the time runs out first
A rare result can be generated
And we'll prove it, or we'll fail!

[Breakdown - anvil and choir alone, slow and heavy]
Without full stress
One second for every test
(One second!)
With full stress
One hour for the build
Divided evenly among them
Ten minutes at most for a single test
So the build never sits in silence
Build it in the Stress configuration!
(Full stress!)

[Verse 4 - building back up]
Count the active tests in the running assembly
Or set the count yourself
Set exactly one of them, never both
Scale the time to leave some headroom
Greater than zero, and at most one
Whichever limit is smaller
(That is the time limit!)

[Final Chorus - key change up, full band and choir at full power]
Over and over!
(Over and over!)
Strike it again!
(Strike it again!)
Any failure on any iteration
Fails the test!
(Fails the test!)
A million iterations
And we're confident it holds!
Over and over, again and again!

[Outro - the band drops out, the choir chants over the anvil]
Duration!
Iterations!
Iterations per second!
And the likely status...
Logged to the console by default
Or to a logger of your own
(Over and over...)
[A last anvil strike rings out]
```

## Sources

| Lyric | From the README |
|---|---|
| Over and over! | Introduction: "`Stressor` runs a test over and over" |
| Random generators are hard to test | Introduction: "Random generators are hard to test" |
| A bug might only show up once<br>Once in a few thousand runs<br>And then it's gone again | Introduction: "a bug might only show up once in a few thousand runs" |
| So the Stressor runs the test<br>Over and over and over again | Introduction: "`Stressor` runs a test over and over" |
| Built for NUnit, forged for the generators<br>Of DnDGen | Introduction: "It is built for [NUnit](https://nunit.org/) and is used by the other DnDGen generator libraries." |
| Until the time limit expires<br>Or enough iterations have completed<br>To be confident in the result | Introduction: "until either a time limit expires or enough iterations have completed to be confident in the result" |
| Strike it again! | Introduction: "`Stressor` runs a test over and over" |
| Any failure on any iteration<br>Fails the test!<br>With the original stack trace preserved | Introduction: "Any failure on any iteration fails the test, with the original stack trace preserved." |
| Over and over, again and again! | Introduction: "`Stressor` runs a test over and over" |
| Add DnDGen.Stress to your tests | Installing: "dotnet add package DnDGen.Stress" |
| Version five needs .NET ten and NUnit five<br>Use version four if you're still on .NET eight | Installing: "Version 5 requires .NET 10 and NUnit 5. Use version 4.x if you are still on .NET 8 or NUnit 4." |
| Create a Stressor once per test fixture<br>Then call it from your tests | Usage: "Create a `Stressor` once per test fixture, then call it from your tests." |
| Call on the Stressor, and the hammer falls | Usage: "stressor.Stress(() =>" |
| Assert the name is never empty | Usage: "Assert.That(thing.Name, Is.Not.Empty);" |
| Setup before and teardown after<br>Every iteration, every one<br>And the teardown runs<br>Even if the test throws! | Methods: "runs `setup` before and `teardown` after every iteration. `teardown` runs even if `test` throws." |
| Async, batch after batch<br>A batch of tests at a time | Methods: "Async version of `Stress`. Runs `MaxAsyncBatch` tests concurrently per batch." |
| As many as the processor count<br>Running side by side | Options: "`Environment.ProcessorCount`" and "How many async tests run concurrently per batch." |
| Generate until the result is valid | Methods: "Calls `generate` until `isValid` returns `true`, then returns the result." |
| Or fail if the time runs out first<br>A rare result can be generated<br>And we'll prove it, or we'll fail! | Methods: "fails the test if the time limit is reached first. Useful for verifying that a rare result *can* be generated." |
| Without full stress<br>One second for every test | Options: "When `false`, every stress test runs for 1 second." |
| With full stress | Options: "When `true`, the time limits below apply." |
| One hour for the build<br>Divided evenly among them | Options: "3600 (1 hour)" and "The total time budget for all stress tests, divided evenly among them." |
| Ten minutes at most for a single test<br>So the build never sits in silence | Options: "600 (10 minutes)" and "The maximum time a single stress test may run. Useful for build systems that fail a step with no output for too long." |
| Build it in the Stress configuration! | Running a full stress build: "building with a configuration named `Stress` is enough" |
| Count the active tests in the running assembly | Options: "Used to count the active (non-ignored) `[Test]` and `[TestCase]` methods in it." |
| Or set the count yourself<br>Set exactly one of them, never both | Options: "The number of stress tests, if you would rather set it directly. Set exactly one of `RunningAssembly` or `TestCount`." |
| Scale the time to leave some headroom<br>Greater than zero, and at most one | Options: "Scales both time limits, to leave headroom for setup and non-stress tests. Must be greater than 0 and at most 1." |
| Whichever limit is smaller | Options: "min(BuildTimeLimitInSeconds * TimeLimitPercentage / testCount, OutputTimeLimitInSeconds * TimeLimitPercentage)" |
| A million iterations<br>And we're confident it holds! | Options: "1,000,000" and "A stress test stops early once it completes this many iterations." |
| Duration!<br>Iterations!<br>Iterations per second!<br>And the likely status... | Methods: "logs a summary (duration, iterations, iterations per second, and likely status)" |
| Logged to the console by default<br>Or to a logger of your own | Methods: "By default it logs to the console; pass your own `ILogger` to the `Stressor(options, logger)` constructor to change that." |

## Suno Notes

## Album Art
