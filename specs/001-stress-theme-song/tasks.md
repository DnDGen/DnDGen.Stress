---

description: "Task list for the DnDGen.Stress theme song"
---

# Tasks: DnDGen.Stress Theme Song

**Input**: Design documents from `specs/001-stress-theme-song/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Requested (FR-014 to FR-016). Integration tests, one file per file under test, are written first in
each phase and seen to fail for the expected reason before the record or README changes that
make them pass (II). Write them with the karls-test-style skill. What they can't judge is checked by hand in the
final phase, per [quickstart.md](quickstart.md).

**Organization**: Tasks are grouped by user story. The song itself is Foundational, because every story needs it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 to US4)
- **(maintainer)**: Only the maintainer can do this task, because it needs Suno, a judgment call, or their
  personal GitHub account. `/speckit-implement` stops and hands it over.

## Path Conventions

- The record and its art: `docs/theme-song/`
- The README: `README.md` at the repository root
- The tests mirror the path of the file they test, under `DnDGen.Stress.Tests/Integration/`, one test file per file
  under test:
  - `docs/theme-song/README.md` (the record) → `DnDGen.Stress.Tests/Integration/Docs/ThemeSong/ReadmeTests.cs`
    (folder names become namespace names, so the folder matches the namespace
    `DnDGen.Stress.Tests.Integration.Docs.ThemeSong`)
  - `README.md` (the repository README) → `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`
- DnDGen/.github: changed only by the maintainer, from a patch prepared here

---

## Phase 1: Setup

**Purpose**: The folder for the record, and a test project that can read it.

- [X] T001 Create `docs/theme-song/` and copy `Cinzel.ttf` and `Cinzel-OFL.txt` into it unchanged from DnDGen/.github `theme-song/` (for example with `gh api repos/DnDGen/.github/contents/theme-song/<file> --jq .content`, base64-decoded), per [research.md](research.md) R5
- [X] T002 [P] In `DnDGen.Stress.Tests/DnDGen.Stress.Tests.csproj`, copy `..\README.md` into the build output as `README.md`, and every Markdown and PNG file under `..\docs\` into `docs\` keeping its subfolders (`None` items with `Link`/`LinkBase` and `CopyToOutputDirectory` set to `PreserveNewest`; wildcards, so the build succeeds before the record exists), per R9

---

## Phase 2: Foundational (the song)

**Purpose**: Write, generate, and publish the song, with its Suno limits under test. Every user story links to it
or describes it.

**⚠️ CRITICAL**: No user story can be completed until the song is published (T011).

- [X] T003 Create `DnDGen.Stress.Tests/Integration/Docs/ThemeSong/ReadmeTests.cs` for the record, matching the style of `DnDGen.Stress.Tests/Integration/StressorTests.cs` (an `internal` NUnit class, here in `DnDGen.Stress.Tests.Integration.Docs.ThemeSong`). It reads `docs/theme-song/README.md` and `README.md` from `TestContext.CurrentContext.TestDirectory`, with minimal private helpers that return the text of the code block under a heading and the rows of the `## Sources` table (R9)
- [X] T004 Add the record's structure and Suno tests to `DnDGen.Stress.Tests/Integration/Docs/ThemeSong/ReadmeTests.cs`, per R9: `Record_HasSectionsInOrder` (the whole ordered list of level-2 headings is `## Updating`, `## Title`, `## Style`, `## Lyrics`, `## Sources`, `## Suno Notes`, `## Album Art`); `SunoField_HasOneCodeBlock`, one case each for Title, Style, and Lyrics; `Sources_HasLyricAndFromTheReadmeColumns`; `Title_FitsSunoLimit` ("80 characters or fewer"); and `Style_FitsSunoLimit` ("1,000 characters or fewer"). Run them and see them fail because the record doesn't exist yet
- [X] T005 Create the record skeleton `docs/theme-song/README.md` with the sections, in order, from [contracts/song-record.md](contracts/song-record.md): `# <Title>` heading, cover image, introduction, Listen on Suno, `## Updating`, `## Title`, `## Style`, `## Lyrics`, `## Sources` (a table with columns `Lyric` and `From the README`), `## Suno Notes`, `## Album Art`. Title, Style, and Lyrics each go in a code block of their own under their heading. Run the T004 tests and see the structure tests pass
- [X] T006 Draft the Style in `docs/theme-song/README.md` with the pr-song skill's style rules, overridden per R3: forge folk metal, in the vibe of "Diggy Diggy Hole"; "1,000 characters or fewer, measured; names the sound, not bands". Name the three example tracks it is based on ("Diggy Diggy Hole" and two more) when handing it over
- [X] T007 Draft the Lyrics in `docs/theme-song/README.md` with the pr-song skill's lyric format and length budget, overridden per R3: "English; captures the README's high-level ideas and vibe; about 2,800 to 3,400 characters, measured; Suno formatting". Every lyric comes from `README.md`, and anything checkable (method names, option names, defaults, commands) is exact. Keep a note of the README phrase behind each line, for T016
- [X] T008 Draft the Title in the `## Title` code block of `docs/theme-song/README.md`, and the same title in its `# <Title>` heading, in the form `<Name> -DnDGen.Stress Theme-`, "80 characters or fewer, measured". Run the T004 tests and see them pass
- [ ] T009 (maintainer) Review the Title, Style, and Lyrics in `docs/theme-song/README.md` and confirm they capture the README's high-level ideas and vibe; revise with the maintainer until approved (quickstart step 3)
- [ ] T010 (maintainer) Generate the song in Suno's Custom mode with the approved Title, Style, and Lyrics and Variety all the way down; pick the take; publish it; and hand back its share link for `docs/theme-song/README.md`. There is no current song yet, so no Cover (R4)
- [ ] T011 Put the Suno link in the Listen on Suno section of `docs/theme-song/README.md`, and record in Suno Notes anything the chosen take drops or changes, and any other lessons from generating it

**Checkpoint**: The song is published, its title, style, lyrics, and link are in the record, and its Suno limit
tests pass.

---

## Phase 3: User Story 1 - Listen from the README (Priority: P1) 🎯 MVP

**Goal**: A reader of the README reaches the song in one click, on GitHub or NuGet.

**Independent Test**: Open the README on GitHub, click the Theme Song badge, and hear the song; the US1 tests pass.

- [ ] T012 [US1] Create `DnDGen.Stress.Tests/Integration/ReadmeTests.cs` for the repository README, in the same style as T003 (in `DnDGen.Stress.Tests.Integration`), with the README tests from R9: `ThemeSong_HasBadgeBesideTheBuildBadge` (the badge from [contracts/readme.md](contracts/readme.md) on the build badge's line); `ThemeSong_HasSectionAtTheEnd` (`## Theme Song` is the last level-2 heading); `ThemeSong_LinksToTheRecord` (to `https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md`, which exists in the build output); `ThemeSong_LinksToTheSameSongAsTheRecord` (every `suno.com` link in `README.md` and in the record is the same link); and `DocumentLinks_AreAbsolute` (every link and image in `README.md`, Markdown and HTML, is absolute `http` or `https`). Each failure lists every offending link. Run them and see the Theme Song tests fail because the README has no badge or section yet
- [ ] T013 [US1] Add the Theme Song badge `[![Theme Song](https://img.shields.io/badge/theme_song-Suno-ff69b4)](<Suno link>)` to `README.md`, on the build badge's line and separated from it by a space, per [contracts/readme.md](contracts/readme.md)
- [ ] T014 [US1] Add the `## Theme Song` section at the end of `README.md` with the text from [contracts/readme.md](contracts/readme.md), filling in the real title and Suno link. Every link is absolute (R6). Run the T012 tests and see them pass

**Checkpoint**: The README links to the song and to the record, and its links work wherever it's read.

---

## Phase 4: User Story 2 - Trace the song back to the README (Priority: P2)

**Goal**: The record shows the song, its art, and where every lyric came from.

**Independent Test**: Someone who did not write the song matches every lyric to the README using only the record;
the US2 tests pass.

- [ ] T015 [US2] Add the lyric coverage and album art tests to `DnDGen.Stress.Tests/Integration/Docs/ThemeSong/ReadmeTests.cs`: `Lyrics_EveryLineHasASource` ("Every lyric line is covered by exactly one row (SC-001)" of `## Sources`) and `Sources_EveryLyricIsInTheSong` (every line in a row's Lyric cell, split on ` / `, is in the Lyrics). Section markers in square brackets, and lines wholly in parentheses, need no row ([data-model.md](data-model.md)). A failure lists every uncovered or unknown line. Also `AlbumArt_LinksToTheSong` (the record shows `cover.png`, linked to the same song as Listen on Suno) and `AlbumArt_ImageExists` (`cover.png` is in the build output). Run them and see them fail because the Sources table is empty and there is no cover yet
- [ ] T016 [US2] Write the Sources table in `docs/theme-song/README.md`, with columns `Lyric` and `From the README`. Each row quotes the README phrase in double quotes, with its section, or says `Not from the README:` and where the line came from. Quote the raw Markdown exactly as it is in `README.md`, including backticks and other formatting; only line breaks may become spaces. Run the T015 tests and see them pass
- [ ] T017 [P] [US2] Create `docs/theme-song/cover.html` from the main theme's `cover.html` in DnDGen/.github `theme-song/`: the same d20 and ghost copies and a Cinzel title, recoloured for the forge (ember orange and hot metal on soot black), with the ghost copies read as repeated strikes, the song's name as the title, and the header comment updated for this song (R5)
- [ ] T018 [US2] Render `docs/theme-song/cover.png` at 1500 × 1500 from `docs/theme-song/cover.html` with `msedge --headless --hide-scrollbars --allow-file-access-from-files --window-size=1500,1500 --screenshot=cover.png cover.html`, run from `docs/theme-song/`; then (maintainer) approve the art, revising T017 until approved
- [ ] T019 [US2] Fill in the title heading, cover image linked to the song, introduction (forge folk metal, lyrics from the DnDGen.Stress README, with a link to it), and Album Art section (credits for the d20 and Cinzel, the licence, and the render command) in `docs/theme-song/README.md`, per [contracts/song-record.md](contracts/song-record.md)

**Checkpoint**: The record is complete apart from its update procedure, and every lyric is traced.

---

## Phase 5: User Story 3 - Keep the song in step with the README (Priority: P3)

**Goal**: Someone changing the README is told about the song check, by the build and by the docs.

**Independent Test**: Reword a phrase the song quotes; the build fails, names the phrase, and points to the update
procedure. Someone asked to make a README change finds the procedure (quickstart step 8).

- [ ] T020 [US3] Add `Sources_EveryQuotedPhraseIsStillInTheReadme` to `DnDGen.Stress.Tests/Integration/Docs/ThemeSong/ReadmeTests.cs` (it tests the record's Sources, so it belongs with the record): every phrase in double quotes in the `From the README` column (rows not starting `Not from the README:`) appears in the raw Markdown of `README.md`, comparing with runs of whitespace collapsed to one space, since the README wraps lines, and with nothing else stripped or rendered. A failure lists every missing phrase and says to follow the Updating section of `docs/theme-song/README.md`, whose existence `Record_HasSectionsInOrder` already checks. The Sources table already exists (T016), so see the test fail for the expected reason by temporarily rewording a quoted phrase in `README.md`, then revert
- [ ] T021 [US3] Write the Updating section of `docs/theme-song/README.md` with the text from [contracts/song-record.md](contracts/song-record.md), including the maintainer's Suno steps: Cover the current song, leave the Style as it is, turn Variety all the way down, pick the take, and publish it
- [ ] T022 [P] [US3] Amend `.specify/memory/constitution.md` with the `/speckit-constitution` skill: add the derived-documents sentence from [contracts/readme.md](contracts/readme.md) to the README as contract bullet, bump the version from 1.0.0 to 1.1.0 (MINOR), and set Last Amended to today. Commit it on its own (III.2)

**Checkpoint**: The build, the README section, the record, and the constitution all point to the song check.

---

## Phase 6: User Story 4 - Find Stress in the soundtrack (Priority: P4)

**Goal**: The organization's pages show Stress's confirmed genre and song, and say why it has no leitmotif.

**Independent Test**: On the organization theme-song page, find Stress's confirmed genre and song link, and the
note on support libraries.

- [ ] T023 [US4] Prepare the DnDGen/.github changes: clone DnDGen/.github into the scratchpad, apply the edits in [contracts/org-pages.md](contracts/org-pages.md) to `theme-song/README.md` and `profile/README.md` with the real title and Suno link, and save the `git diff` as `specs/001-stress-theme-song/dndgen-github.patch`
- [ ] T024 [US4] (maintainer) From the maintainer's personal account, apply `specs/001-stress-theme-song/dndgen-github.patch` to DnDGen/.github, open its pull request, and merge it only after the DnDGen.Stress pull request has merged (R8)

**Checkpoint**: The organization's pages link to the Stress song and record.

---

## Phase 7: Polish & Release

**Purpose**: Version, record the follow-up, validate, and open the pull request.

- [ ] T025 [P] Set `<Version>` to 5.0.3 and replace `<PackageReleaseNotes>` with the 5.0.3 notes from [contracts/readme.md](contracts/readme.md) in `DnDGen.Stress/DnDGen.Stress.csproj`
- [ ] T026 [P] Create a GitHub issue in DnDGen/DnDGen.Stress for tests that `README.md` documents the public API: every public `Stressor` constructor and method appears in the Methods table, and every `StressorOptions` property appears in the Options table with a default matching `new StressorOptions()`. Mention a possible test of the README's overall structure as a candidate case (R9). Link it to the constitution's README as contract rule and to [research.md](research.md) R9 as its origin (III.3). Confirm the title and body with the maintainer before creating it
- [ ] T027 Run `dotnet test -c Stress` and `dotnet pack` for `DnDGen.Stress.sln`; confirm every test passes, including both new `ReadmeTests` classes, and the package contains `README.md` (quickstart step 6)
- [ ] T028 Run quickstart steps 1, 2, 4, and 5 against `docs/theme-song/README.md` and `docs/theme-song/cover.png`: the lyrics length, the rows marked as not from the README, a published take, and the rendered art
- [ ] T029 Run the fresh-eyes check (quickstart step 8): a new agent session, given only the repository, is asked to make a small change to `README.md`, and reports whether it found the update procedure and what it would do
- [ ] T030 Push the branch and open the pull request titled `5.0.3: Theme song`, with `cidthecoatrack` as reviewer and a description written from the diff against `main` that states what was verified and what wasn't, and gives the reasoning for constitution amendment 1.1.0, as the constitution's Governance section requires; then check `README.md` renders on GitHub with the badge and both links (quickstart step 7). Confirm with the maintainer before pushing
- [ ] T031 (maintainer) After the release: check the badge and links on the nuget.org page for 5.0.3, and the DnDGen/.github pages once that pull request merges (quickstart steps 9 and 10)

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: none. T001 and T002 touch different files.
- **Foundational (Phase 2)**: needs T001 and T002. Blocks every user story, because they all need the published
  song (T011).
- **US1 (Phase 3)**: needs T011 for the Suno link.
- **US2 (Phase 4)**: needs T011. T017 and T018 also need the song's name from T008.
- **US3 (Phase 5)**: T020 needs the Sources table from T016, so US3 follows US2. T021 needs only T005. T022 needs
  nothing from this feature except its contract text.
- **US4 (Phase 6)**: T023 needs T011 and T018 (the link and the cover). T024 waits for the DnDGen.Stress pull
  request to merge.
- **Polish (Phase 7)**: T025 and T026 can be done at any time. T027 to T030 need US1 to US3 complete. T031 comes
  after the release.

### Within each story

- Tests first: write them, run them, and see them fail for the expected reason before the change that makes them
  pass (II).
- `DnDGen.Stress.Tests/Integration/Docs/ThemeSong/ReadmeTests.cs` is edited by T003, T004, T015, and T020, one
  after another. `DnDGen.Stress.Tests/Integration/ReadmeTests.cs` is created by T012 alone.
- `docs/theme-song/README.md` is edited by T005 to T008, T011, T016, T019, and T021, one after another.

### Maintainer hand-offs

T009, T010, T018 (approval), T024, and T031 need the maintainer, as does confirming T026 and T030 before they go
out. T009 and T010 are on the critical path: nothing after Phase 2 can finish until the song is published.

---

## Parallel Examples

```text
# Phase 1: the folder and the test project are different files
Task: "T001 Create docs/theme-song/ and copy Cinzel.ttf and Cinzel-OFL.txt"
Task: "T002 Copy README.md and the docs Markdown and PNG files into the test build output"

# Phase 4: the album art is independent of the Sources work
Task: "T015/T016 Lyric coverage tests, then the Sources table"
Task: "T017 Create docs/theme-song/cover.html from the main theme's cover.html"

# Phase 5: the constitution is a different file from the record
Task: "T021 Write the Updating section of docs/theme-song/README.md"
Task: "T022 Amend .specify/memory/constitution.md with /speckit-constitution"

# Any time: the version bump and the follow-up issue
Task: "T025 Set <Version> to 5.0.3 in DnDGen.Stress/DnDGen.Stress.csproj"
Task: "T026 Create the GitHub issue for README public-API tests"
```

---

## Implementation Strategy

### MVP first (User Story 1)

1. Phase 1 and Phase 2: the test scaffolding, then the song written, approved, generated, and published.
2. Phase 3: the README links to it, with its link tests passing.
3. **Stop and validate**: the badge plays the song.

The MVP isn't releasable on its own, because the README section links to a record that isn't finished yet. Every
merge to `main` is a release, so all four stories ship in one pull request.

### Incremental delivery within the pull request

1. Song (Phase 2) → README links (US1) → record and sources (US2) → update procedure, source test, and
   constitution (US3).
2. Validate, record the follow-up issue, and open the pull request (Phase 7).
3. After it merges, the maintainer's DnDGen/.github pull request (US4).

---

## Notes

- Commit at the end of each turn, and give the constitution amendment its own commit (III.2).
- The drafts in T006 to T008 are reviewed with the maintainer before generating (R3); they are not pasted into Suno
  as they are.
- The tests check only mechanical rules and never use the network (FR-016).
- Avoid: editing the same file from two tasks at once, and linking anything to the DnDGen/.github changes before
  they exist.
