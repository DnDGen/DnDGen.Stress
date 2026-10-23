# Quickstart: Validating the DnDGen.Stress Theme Song

There is no code to test, so the feature is validated by these checks. Each names the requirement or success
criterion it proves.

## Prerequisites

- The branch checked out, with `docs/theme-song/` in place.
- Microsoft Edge, to render the cover.
- The .NET 10 SDK, to confirm the package still builds and packs.

## Before generating

1. **Limits (FR-001)**: `dotnet test --filter FullyQualifiedName~ThemeSong` passes its Suno limit tests (Title ≤ 80,
   Style ≤ 1,000). Measure the Lyrics with `wc -m`; aim for about 2,800 to 3,400.
2. **Sources (FR-003, FR-014, FR-015, SC-001)**: the same test run passes its source tests: every lyric line has a
   row, and every quoted phrase is in the README. Read the rows marked as not from the README by eye.
3. **Maintainer review (SC-006)**: the maintainer agrees the lyrics capture the README's high-level ideas and vibe
   before anything is generated.

## After generating

4. **The song (FR-001, FR-002, SC-006)**: the maintainer picks a take, generated with Variety all the way down.
   Anything it drops or changes is in the Suno Notes. The song is published: its link plays in a private browser
   window, signed out of Suno.
5. **Album art (FR-006)**: render it, then check that `cover.png` is 1500 × 1500 and the maintainer approves it:

   ```
   msedge --headless --hide-scrollbars --allow-file-access-from-files --window-size=1500,1500 --screenshot=cover.png cover.html
   ```

6. **Package (FR-013, FR-015)**: `dotnet test -c Stress` passes, including every theme song test, and `dotnet pack`
   includes the README. The version is 5.0.3,
   and the release notes are for 5.0.3 only.

## On GitHub, before merging

7. **README (FR-011, SC-002)**: on the branch, the Theme Song badge sits next to the build badge and opens the song
   in one click; the Theme Song section links to the song and the record.
8. **Fresh eyes (SC-003, SC-004)**: someone who hasn't seen this work, such as a new AI session given only the
   repository, is asked to make a small README change. They find the update procedure without being told, and can
   say whether the song needs updating and what they would do.

## After release

9. **NuGet (FR-011)**: on nuget.org's 5.0.3 page, the badge displays and both links work.
10. **Organization (FR-012, SC-005)**: once the DnDGen/.github pull request merges, the Stress row in the
    soundtrack plan links to the song and the record, the leitmotif section explains why Stress has none, and the
    profile's Soundtrack table shows Stress's cover.
