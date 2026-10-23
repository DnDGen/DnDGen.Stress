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
  2. An **amendment to the constitution's README contract**, adding the song check and the same-pull-request rule.
     Every plan and pull request is already checked against the constitution, so the maintainer's review and any
     Spec Kit plan both apply it. Adding an obligation is MINOR: 1.0.0 → 1.1.0.
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

- **Decision**: Map the record to Suno's fields, but don't describe clicks. The record's Title, Style, and Lyrics
  blocks are named for the Suno fields they are pasted into (Custom mode), so a remix is copy and paste. Click paths
  and menu names are left out, because Suno's interface changes and those steps would go stale.
- **Rationale**: SC-004 needs someone new to be able to remix from the record alone; the field mapping is the part
  that doesn't go stale.
- **Open for the maintainer**: The maintainer has their own idea of the update process. The draft procedure in
  [contracts/song-record.md](contracts/song-record.md) is a starting point to revise before implementation.

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
- **Also included**: a Stress row in the organization profile's Soundtrack table. The spec names only the
  theme-song page (FR-012), but the profile's table is where User Story 4's reader first meets the soundtrack, and
  adding the row follows the existing pattern (I.1).
