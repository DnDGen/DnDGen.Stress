# Over and Over -DnDGen.Stress Theme-

[![Over and Over album art](cover.png)](https://suno.com/s/TLKVK477Efrel4u8)

The DnDGen.Stress theme song: forge folk metal, where steel is proven by striking it over and over.
Its lyrics come from the [DnDGen.Stress README](https://github.com/DnDGen/DnDGen.Stress/blob/main/README.md), sung in plain English,
and it Samples the "Rogante Marendor" motif from the [DnDGen main theme](https://github.com/DnDGen/.github/blob/main/theme-song/README.md).

**Listen on [Suno](https://suno.com/s/TLKVK477Efrel4u8).**

## Updating

This song follows the [DnDGen.Stress README](https://github.com/DnDGen/DnDGen.Stress/blob/main/README.md).
Every pull request that changes the README gets a song check:

1. If the change rewords or removes a phrase in the Sources table below, the song needs updating.
   The tests catch this one: they fail and list every quoted phrase that is no longer in the README.
2. Otherwise, if the change alters the README's high-level ideas or vibe, the song needs updating.
3. Otherwise, it doesn't. Changes to the README's Theme Song badge or section never need a song update.

When the song needs updating, it happens in the same pull request:

1. The contributor updates the Lyrics and Sources here to match the new README.
   Names are sung as words, never as code.
2. The maintainer makes the new song in Suno:
   - **Cover the current song**, so the theme stays recognizably the same song as it evolves.
   - Use the Title, Style, and Lyrics from this page. **Leave the Style as it is**: only the lyrics change.
   - **Turn Variety all the way down**, so Suno keeps to the Style instead of straying from it.
   - Pick the take. The new take replaces the old one.
   - **Publish it**, so anyone with the link can find and hear it.
3. The maintainer updates the Suno link here and in the README, and adds anything learned to the Suno Notes.
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

Lessons from generating this song, for the next update:

- **Drafts and the Sample.** Every draft before this song first merged (v0.1 to v0.4) was a fresh generation from
  the same inputs, Sampling the main theme's "Rogante Marendor" snippet (3:00.0 to 3:17.1), with Variety all the way
  down. The sample came through every time: "Over and over" is sung on the Rogante Marendor melody, and it brings the
  main theme's orchestral colour into the intro. This take is v0.4, chosen because it leans on the sample in the outro.
- **No code in the lyrics.** v0.1 and v0.2 sang code literals. Rewriting them as plain English for v0.3 sounded much
  better, and from then on Suno didn't drop a single call-and-response.
- **No anvils.** The Style asks for anvil strikes, but no draft played one. Instead, the final chorus of v0.3 and
  v0.4 drops to pounding drums on every other beat, which gives the slow anvil feel anyway.
- **Folk metal and the orchestral sample blend.** Folk metal already wants fiddle and strings, so the orchestral
  sample sits naturally in the song.
- **Suno hears power metal.** Suno summarized the style as power metal every time. It fits the song, but it shows
  how strongly Suno pulls towards metal.

## Album Art

[cover.png](cover.png) is rendered from [cover.html](cover.html), which you can open in any browser.
To render it again at 1500 × 1500, from this folder:

```
msedge --headless --hide-scrollbars --allow-file-access-from-files --window-size=1500,1500 --screenshot=cover.png cover.html
```

If Edge reads `cover.html` as a web address, pass it as a full `file:///` URL instead.

- It is the main theme's [cover](https://github.com/DnDGen/.github/blob/main/theme-song/cover.html), recoloured for the forge.
  The d20 strikes an anvil, glowing like hot metal, and the ghost copies above it are its repeated strikes: the same test, over and over.
- The d20 is the dndgen.com loading spinner, from [DnDGen.Web](https://github.com/DnDGen/DnDGen.Web/blob/main/dndgen-web/src/assets/loadingDice.svg), originally from [a gist by hperantunes](https://gist.github.com/hperantunes/f4c2fd407c2ea74c6c9d).
- The title is set in [Cinzel](https://github.com/NDISCOVER/Cinzel), included as [Cinzel.ttf](Cinzel.ttf) under the [SIL Open Font License](Cinzel-OFL.txt).
