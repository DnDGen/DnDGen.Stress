# Research: DnDGen.Stress Theme Song

Decisions for the "how" that the spec left open. Each records what was chosen, why, and what else was considered.

## R1. Where the record lives

- **Decision**: `docs/theme-song/` in DnDGen.Stress.
- **Rationale**: Agreed with the maintainer during clarification. `docs/` is the common convention for supporting
  documentation, and keeps non-code files out of the repository root beside `DnDGen.Stress/` and `cicd/`. This
  becomes the pattern for every repository's song (I.2).
- **Alternatives considered**: `theme-song/` at the root, matching DnDGen/.github. Rejected because the `.github`
  repository has no code, so its whole root is effectively documentation; `docs/theme-song/` is the same idea in a
  code repository.

## R2. Where contributors learn about the song check (FR-007)

- **Decision**: Two places.
  1. A short **Theme Song section at the end of the README**, saying the song follows the README and linking to
     the record's update procedure. Someone editing the README sees it, and so does anyone reading it.
  2. An **amendment to the constitution's README contract**: documents derived from the README say how they are
     kept in step with it, and are updated in the same pull request. Every plan and pull request is already
     checked against the constitution, so the maintainer's review and any Spec Kit plan both apply it. Adding an
     obligation is MINOR: 1.0.0 → 1.1.0.
     - The amendment names the theme song only as an example. The song's own details stay in its record, so the
       constitution stays at the level of the rest of it, and the rule still reads correctly in a repository with
       no song, or if the constitution later becomes organization-wide.
- **Rationale**: The README section reaches a first-time contributor (SC-003); the constitution makes the check
  part of review, so it doesn't rely on the contributor noticing.
- **Alternatives considered**:
  - An `<!-- INFO: -->` comment at the top of the README. It would be the most visible spot for someone editing,
    but under I.3 comments are added only by a human. The maintainer can still add one.
  - A pull request template. This repository has none, and an organization-wide template in DnDGen/.github would
    reach every repository, which is out of scope here.
  - A `CLAUDE.md` note. It reaches only AI agents.

## R3. Drafting the song with the pr-song skill

- **Decision**: Use pr-song for its Suno mechanics, with theme-song overrides.
- **Kept from pr-song**: describe the sound rather than naming bands; measure the style (≤1,000 characters), the
  title (≤80), and the lyrics, never estimate; the length budget for sung and shouted metal (about 2,800 to 3,400
  characters for a 4 to 5 minute song); Suno formatting (section markers in brackets, backing vocals and
  call-and-response in parentheses, plain text, one lyric per line); anything checkable, such as option names,
  defaults, and method names, must be exact.
- **Overridden for a theme song**:
  - The genre is fixed: forge folk metal, in the vibe of "Diggy Diggy Hole". No random draw. "Diggy Diggy Hole" is
    the first example track; the other two are chosen when drafting, and the maintainer may name them.
  - The source is the README as it is, not a pull request diff.
  - The lyrics capture the README's high-level ideas and vibe. pr-song packs in specifics for comedy; here a few
    real names (`Stressor`, `dotnet test -c Stress`) can carry the "sung completely straight" joke, but coverage of
    every option is not the goal.
  - Not "let the AI be AI": drafts are reviewed and revised with the maintainer before generating, because theme
    songs last.
  - The title is the song's own name, in the main theme's form: `<Name> -DnDGen.Stress Theme-`.
- **Alternatives considered**: Writing without pr-song. Rejected because its measured limits and budgets came from
  real generations and would otherwise be relearned.

## R4. How much Suno detail goes in the update procedure

- **Decision**: Name the Suno features and settings that matter, but don't describe clicks. The record's Title,
  Style, and Lyrics blocks are named for the Suno fields they are pasted into (Custom mode), and the maintainer
  added the settings that keep the song consistent: Cover the current song, leave the Style unchanged, turn
  Variety all the way down, and publish the result. Click paths and menu locations are left out, because Suno's
  interface changes and those steps would go stale.
- **Rationale**: SC-004 needs someone new to be able to remix from the record alone. Feature and setting names
  last longer than click paths, and they carry the intent: the song evolves without being replaced by a different
  one.
- **Alternatives considered**: Field mapping only, as first drafted. The maintainer's Suno steps were added on
  review, because without Cover and low Variety a remix can come back as a different-sounding song.
- **Initial generation**: The same settings apply except Cover, since there is no current song to cover yet.

## R5. Album art

- **Decision**: A new cover built from the main theme's `cover.html`: the same composition (the dndgen.com d20 with
  ghost copies behind it, and the title set in Cinzel), recoloured for the forge, with ember orange and hot metal on
  soot black. The ghost copies, which on the main theme are its spin, become repeated strikes: the same test run
  over and over. Rendered to 1500 × 1500 with headless Edge, as the main theme is.
- **Rationale**: Recognizably the same family as the main theme (FR-006) with its own identity.
- **Font**: `Cinzel.ttf` and `Cinzel-OFL.txt` are copied from DnDGen/.github, so the cover renders from a plain
  checkout and the licence travels with the font.
- **Alternatives considered**: Reusing the main theme's cover (no identity of its own); linking the font from the
  `.github` repository (rendering would need a network fetch from a local file); AI-generated art (licensing and
  consistency with the main theme).

## R6. Badge and links

- **Decision**: The main theme's badge, unchanged:
  `[![Theme Song](https://img.shields.io/badge/theme_song-Suno-ff69b4)](<Suno link>)`, on the same line as the
  build badge. Every link in the README is absolute, including the link to the record at
  `https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md`.
- **Rationale**: nuget.org doesn't resolve links relative to the repository, and img.shields.io is one of the image
  hosts nuget.org allows in READMEs (the build badge from dev.azure.com already displays there). The record link
  points at `main`, so it only works once this pull request merges; that's expected.

## R7. Release

- **Decision**: Version 5.0.3, pull request titled `5.0.3: Theme song`. Release notes: the package changes only in
  its README, which now links to the theme song; there are no code changes since 5.0.1; and anyone upgrading across
  several versions should see each version's changes on the GitHub releases page.
- **Rationale**: The constitution's versioning rule. The README is packed into the package, so the package does
  change, but only in its README. 5.0.1 was the last version to change `Stressor.cs`.

## R8. DnDGen/.github changes and merge order

- **Decision**: The maintainer makes a separate pull request in DnDGen/.github from their personal account, using
  the exact text in [contracts/org-pages.md](contracts/org-pages.md). It merges after this one.
- **Rationale**: This pull request links only to Suno and to itself, so it can merge first. The `.github` pull
  request links to the record on DnDGen.Stress `main`, which exists only once this one merges.
- **Also included**: a Stress row in the organization profile's Soundtrack table.

## R9. Automated tests for the README and the record

- **Decision**: NUnit tests in three groups, in two test files:
  1. **The song follows the README** (record tests): every phrase quoted in the Sources table appears in
     `README.md`; every lyric line has a Sources row, and every row's lyric is in the Lyrics. A failure lists every
     missing phrase or line at once and points to the Updating section, so a test also checks that the record has
     an `## Updating` section. Only that it exists: its content is for people to judge.
  2. **The record is ready for Suno** (record tests): the Title is 80 characters or fewer, and the Style 1,000 or
     fewer. The lyrics' 2,800 to 3,400 characters is a target for a good length, not a rule, so it isn't tested.
  3. **The README works wherever it's read** (README tests): the Theme Song badge and the record link to the same
     Suno song; every link in `README.md` is absolute; its link to the record is to `docs/theme-song/README.md`,
     which exists.
- **Rationale**: These are the spec's mechanical rules (FR-014, FR-015). The first group automates the first step
  of the song check, so a contributor who breaks a quoted phrase is told by the build, where they make the change.
- **Placement**: `Integration/`, not `Unit/`, because the tests read real files copied into the build output.
  Within it, each test file mirrors the path of the file it tests, one test file per file under test (the
  maintainer's convention, for separation of concerns). Folder names become valid namespace names, such as
  `Docs/ThemeSong` for `docs/theme-song`, so that each test file's folder matches its namespace:
  - `docs/theme-song/README.md` → `DnDGen.Stress.Tests/Integration/Docs/ThemeSong/ReadmeTests.cs`, in namespace
    `DnDGen.Stress.Tests.Integration.Docs.ThemeSong`
  - `README.md` → `DnDGen.Stress.Tests/Integration/ReadmeTests.cs`, in namespace `DnDGen.Stress.Tests.Integration`
- **Shared code**: none. Each file reads what it needs; the README tests only need the record's Suno link from it.
- **Reading the files**: the test project copies `..\README.md` and `..\docs\theme-song\README.md` into its build
  output, as the package project already does for `README.md`. That keeps the tests independent of the working
  directory.
- **Parsing**: minimal. Code blocks under the `## Style`, `## Lyrics`, and title headings, the rows of the `## Sources`
  table, and the links in `README.md`. Those headings become part of the record's contract
  ([contracts/song-record.md](contracts/song-record.md)).
- **Out of bounds (FR-016)**: the song's ideas, vibe, and take, and anything needing the network, such as whether
  the Suno link plays or nuget.org shows the badge. Those stay in [quickstart.md](quickstart.md).
- **Alternatives considered**: Manual checks only, as first planned. Rejected because the source check is exactly
  the kind of rule people forget. A separate test project. Rejected as more structure than a handful of tests
  needs.
- **Follow-up**: tests that `README.md` documents every public `Stressor` method and `StressorOptions` property,
  with defaults matching `new StressorOptions()`, belong to the constitution's README contract rather than the
  song. They are recorded as a GitHub issue for their own feature (III.3). The spec names only the
  theme-song page (FR-012), but the profile's table is where User Story 4's reader first meets the soundtrack, and
  adding the row follows the existing pattern (I.1).
