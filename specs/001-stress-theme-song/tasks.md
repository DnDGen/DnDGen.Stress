---

description: "Task list for the DnDGen.Stress theme song"
---

# Tasks: DnDGen.Stress Theme Song

**Input**: Design documents from `specs/001-stress-theme-song/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: None. There is no code to test (Constitution Check, II). Validation is the manual checks in
[quickstart.md](quickstart.md), run in the final phase.

**Organization**: Tasks are grouped by user story. The song itself is Foundational, because every story needs it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 to US4)
- **(maintainer)**: Only the maintainer can do this task, because it needs Suno, a judgment call, or their
  personal GitHub account. `/speckit-implement` stops and hands it over.

## Path Conventions

- The record and its art: `docs/theme-song/`
- The README: `README.md` at the repository root
- DnDGen/.github: changed only by the maintainer, from a patch prepared here

---

## Phase 1: Setup

**Purpose**: The folder for the record, with the font it needs.

- [ ] T001 Create `docs/theme-song/` and copy `Cinzel.ttf` and `Cinzel-OFL.txt` into it unchanged from DnDGen/.github `theme-song/` (for example with `gh api repos/DnDGen/.github/contents/theme-song/<file> --jq .content`, base64-decoded), per [research.md](research.md) R5

---

## Phase 2: Foundational (the song)

**Purpose**: Write, generate, and publish the song. Every user story links to it or describes it.

**⚠️ CRITICAL**: No user story can be completed until the song is published (T008).

- [ ] T002 Create the record skeleton `docs/theme-song/README.md` with the sections, in order, from [contracts/song-record.md](contracts/song-record.md): title heading, cover image, introduction, Listen on Suno, Updating, Style, Lyrics, Sources, Suno Notes, Album Art. Title, Style, and Lyrics each go in a code block of their own
- [ ] T003 Draft the Style in `docs/theme-song/README.md` with the pr-song skill's style rules, overridden per [research.md](research.md) R3: forge folk metal, in the vibe of "Diggy Diggy Hole"; "1,000 characters or fewer, measured; names the sound, not bands". Name the three example tracks it is based on ("Diggy Diggy Hole" and two more) when handing it over
- [ ] T004 Draft the Lyrics in `docs/theme-song/README.md` with the pr-song skill's lyric format and length budget, overridden per R3: "English; captures the README's high-level ideas and vibe; about 2,800 to 3,400 characters, measured; Suno formatting". Every lyric comes from `README.md`, and anything checkable (method names, option names, defaults, commands) is exact. Keep a note of the README phrase behind each line, for T011
- [ ] T005 Draft the Title in `docs/theme-song/README.md` in the form `<Name> -DnDGen.Stress Theme-`, "80 characters or fewer, measured"
- [ ] T006 (maintainer) Review the Title, Style, and Lyrics in `docs/theme-song/README.md` and confirm they capture the README's high-level ideas and vibe; revise with the maintainer until approved (quickstart step 3)
- [ ] T007 (maintainer) Generate the song in Suno's Custom mode with the approved Title, Style, and Lyrics and Variety all the way down; pick the take; publish it; and hand back its share link for `docs/theme-song/README.md`. There is no current song yet, so no Cover (R4)
- [ ] T008 Put the Suno link in the Listen on Suno section of `docs/theme-song/README.md`, and record in Suno Notes anything the chosen take drops or changes, and any other lessons from generating it

**Checkpoint**: The song is published, and its title, style, lyrics, and link are in the record.

---

## Phase 3: User Story 1 - Listen from the README (Priority: P1) 🎯 MVP

**Goal**: A reader of the README reaches the song in one click.

**Independent Test**: Open the README on GitHub, click the Theme Song badge, and hear the song.

- [ ] T009 [US1] Add the Theme Song badge `[![Theme Song](https://img.shields.io/badge/theme_song-Suno-ff69b4)](<Suno link>)` to `README.md`, on the build badge's line and separated from it by a space, per [contracts/readme.md](contracts/readme.md)
- [ ] T010 [US1] Add the `## Theme Song` section at the end of `README.md` with the text from [contracts/readme.md](contracts/readme.md), filling in the real title and Suno link. Every link is absolute (R6)

**Checkpoint**: The README links to the song and to the record.

---

## Phase 4: User Story 2 - Trace the song back to the README (Priority: P2)

**Goal**: The record shows the song, its art, and where every lyric came from.

**Independent Test**: Someone who did not write the song matches every lyric to the README using only the record.

- [ ] T011 [P] [US2] Write the Sources table in `docs/theme-song/README.md`, with columns Lyric and From the README. "Every lyric line is covered by exactly one row (SC-001)"; each row quotes the README phrase and its section, or says `Not from the README:` and where the line came from. Section markers, and echoes in parentheses of a line already covered, need no row
- [ ] T012 [P] [US2] Create `docs/theme-song/cover.html` from the main theme's `cover.html` in DnDGen/.github `theme-song/`: the same d20 and ghost copies and a Cinzel title, recoloured for the forge (ember orange and hot metal on soot black), with the ghost copies read as repeated strikes, the song's name as the title, and the header comment updated for this song (R5)
- [ ] T013 [US2] Render `docs/theme-song/cover.png` at 1500 × 1500 from `docs/theme-song/cover.html` with `msedge --headless --hide-scrollbars --allow-file-access-from-files --window-size=1500,1500 --screenshot=cover.png cover.html`, run from `docs/theme-song/`; then (maintainer) approve the art, revising T012 until approved
- [ ] T014 [US2] Fill in the title heading, cover image linked to the song, introduction (forge folk metal, lyrics from the DnDGen.Stress README, with a link to it), and Album Art section (credits for the d20 and Cinzel, the licence, and the render command) in `docs/theme-song/README.md`, per [contracts/song-record.md](contracts/song-record.md)

**Checkpoint**: The record is complete apart from its update procedure.

---

## Phase 5: User Story 3 - Keep the song in step with the README (Priority: P3)

**Goal**: Someone changing the README finds the song check and the update procedure.

**Independent Test**: Someone asked to make a README change, without mention of the song, finds the procedure and
can follow it (quickstart step 8).

- [ ] T015 [US3] Write the Updating section of `docs/theme-song/README.md` with the text from [contracts/song-record.md](contracts/song-record.md), including the maintainer's Suno steps: Cover the current song, leave the Style as it is, turn Variety all the way down, pick the take, and publish it
- [ ] T016 [P] [US3] Amend `.specify/memory/constitution.md` with the `/speckit-constitution` skill: add the derived-documents sentence from [contracts/readme.md](contracts/readme.md) to the README as contract bullet, bump the version from 1.0.0 to 1.1.0 (MINOR), and set Last Amended to today. Commit it on its own (III.2)

**Checkpoint**: The README section, the record, and the constitution all point to the song check.

---

## Phase 6: User Story 4 - Find Stress in the soundtrack (Priority: P4)

**Goal**: The organization's pages show Stress's confirmed genre and song, and say why it has no leitmotif.

**Independent Test**: On the organization theme-song page, find Stress's confirmed genre and song link, and the
note on support libraries.

- [ ] T017 [US4] Prepare the DnDGen/.github changes: clone DnDGen/.github into the scratchpad, apply the edits in [contracts/org-pages.md](contracts/org-pages.md) to `theme-song/README.md` and `profile/README.md` with the real title and Suno link, and save the `git diff` as `specs/001-stress-theme-song/dndgen-github.patch`
- [ ] T018 [US4] (maintainer) From the maintainer's personal account, apply `specs/001-stress-theme-song/dndgen-github.patch` to DnDGen/.github, open its pull request, and merge it only after the DnDGen.Stress pull request has merged (R8)

**Checkpoint**: The organization's pages link to the Stress song and record.

---

## Phase 7: Polish & Release

**Purpose**: Version, validate, and open the pull request.

- [ ] T019 [P] Set `<Version>` to 5.0.3 and replace `<PackageReleaseNotes>` with the 5.0.3 notes from [contracts/readme.md](contracts/readme.md) in `DnDGen.Stress/DnDGen.Stress.csproj`
- [ ] T020 Run `dotnet test -c Stress` and `dotnet pack` for `DnDGen.Stress.sln`; confirm the tests pass and the package contains `README.md` (quickstart step 6)
- [ ] T021 Run quickstart steps 1, 2, 4, and 5 against `docs/theme-song/README.md` and `docs/theme-song/cover.png`: measured limits, a complete Sources table, a published take, and the rendered art
- [ ] T022 Run the fresh-eyes check (quickstart step 8): a new agent session, given only the repository, is asked to make a small change to `README.md`, and reports whether it found the update procedure and what it would do
- [ ] T023 Push the branch and open the pull request titled `5.0.3: Theme song`, with `cidthecoatrack` as reviewer and a description written from the diff against `main` that states what was verified and what wasn't; then check `README.md` renders on GitHub with the badge and both links (quickstart step 7). Confirm with the maintainer before pushing
- [ ] T024 (maintainer) After the release: check the badge and links on the nuget.org page for 5.0.3, and the DnDGen/.github pages once that pull request merges (quickstart steps 9 and 10)

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: none.
- **Foundational (Phase 2)**: needs T001 only for the folder. Blocks every user story, because they all need the
  published song (T008).
- **US1 (Phase 3)**: needs T008 for the Suno link.
- **US2 (Phase 4)**: needs T008. T012 and T013 also need the song's name from T005.
- **US3 (Phase 5)**: needs T002 for the record; it doesn't need US1 or US2. T016 needs nothing from this feature
  except its contract text.
- **US4 (Phase 6)**: T017 needs T008 and T013 (the link and the cover). T018 waits for the DnDGen.Stress pull
  request to merge.
- **Polish (Phase 7)**: T019 can be done at any time. T020 to T023 need US1 to US3 complete. T024 comes after the
  release.

### Within the record

T002, T003, T004, T005, T008, T011, T014, and T015 all edit `docs/theme-song/README.md`, so they run one after
another. Of those, only T011 is marked [P], because it can run alongside T012, which is in a different file.

### Maintainer hand-offs

T006, T007, T013 (approval), T018, and T024 need the maintainer. T006 and T007 are on the critical path: nothing
after Phase 2 can finish until the song is published.

---

## Parallel Examples

```text
# Phase 4: the Sources table and the album art are in different files
Task: "T011 Write the Sources table in docs/theme-song/README.md"
Task: "T012 Create docs/theme-song/cover.html from the main theme's cover.html"

# Phase 5: the procedure and the constitution are in different files
Task: "T015 Write the Updating section of docs/theme-song/README.md"
Task: "T016 Amend .specify/memory/constitution.md with /speckit-constitution"

# Any time: the version bump touches only the csproj
Task: "T019 Set <Version> to 5.0.3 in DnDGen.Stress/DnDGen.Stress.csproj"
```

---

## Implementation Strategy

### MVP first (User Story 1)

1. Phase 1 and Phase 2: the song is written, approved, generated, and published.
2. Phase 3: the README links to it.
3. **Stop and validate**: the badge plays the song.

The MVP isn't releasable on its own, because the README section links to a record that isn't finished yet. Every
merge to `main` is a release, so all four stories ship in one pull request.

### Incremental delivery within the pull request

1. Song (Phase 2) → README links (US1) → record (US2) → update procedure and constitution (US3).
2. Validate and open the pull request (Phase 7).
3. After it merges, the maintainer's DnDGen/.github pull request (US4).

---

## Notes

- Commit at the end of each turn, and give the constitution amendment its own commit (III.2).
- The drafts in T003 to T005 are reviewed with the maintainer before generating (R3); they are not pasted into Suno
  as they are.
- Avoid: editing `docs/theme-song/README.md` from two tasks at once, and linking anything to the DnDGen/.github
  changes before they exist.
