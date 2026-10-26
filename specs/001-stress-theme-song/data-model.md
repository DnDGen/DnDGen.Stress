# Data Model: DnDGen.Stress Theme Song

There is no database. The "data" is the song on Suno and the Markdown that describes it. This sets out what each
piece holds and the rules it must follow.

## Theme song

The published song on Suno.

| Field | Rule | Source |
|-------|------|--------|
| Title | `<Name> -DnDGen.Stress Theme-`, 80 characters or fewer, measured | FR-001, R3 |
| Suno link | A `suno.com/s/...` share link | FR-001 |
| Genre | Forge folk metal, in the vibe of "Diggy Diggy Hole" | FR-002 |
| Style | 1,000 characters or fewer, measured; names the sound, not bands | FR-001, R3 |
| Lyrics | English; captures the README's high-level ideas and vibe; about 2,800 to 3,400 characters, measured; Suno formatting | FR-003, R3 |

## Source entry

One row of the record's Sources table, for each lyric line.

| Field | Rule |
|-------|------|
| Lyric | The line as sung, or a run of consecutive lines with one source, separated by ` / ` as in the main theme |
| From the README | The README phrase it came from, in double quotes, with its section; or `Not from the README:` and where it came from. The phrase is copied exactly from the raw Markdown of `README.md`, backticks and all, apart from line breaks |

Every lyric line is covered by exactly one row (SC-001). Section markers, and lines wholly in parentheses (backing
vocals and echoes), need no row of their own. These rules are what the tests check (R9).

## Song record

`docs/theme-song/README.md`. Its sections are set out in [contracts/song-record.md](contracts/song-record.md). It
shows only the current song (FR-005).

## Update procedure

A section of the song record: the song check that every README change gets, and the steps for updating the song
when the check says to. See [contracts/song-record.md](contracts/song-record.md).

## Soundtrack plan entry

A row in DnDGen/.github `theme-song/README.md`. Stress's row changes from a proposed genre to a confirmed one with
links. See [contracts/org-pages.md](contracts/org-pages.md).

## Lifecycle

```text
Drafted ──► Generated ──► Chosen ──► Published ──► (README change) ──► Checked
   ▲          (takes)    (maintainer)  (linked)                          │
   │                                                                     ├── no update needed ──► Published
   └──────────────────────── update needed: lyrics and sources revised ──┘
```

- **Drafted**: Title, style, and lyrics written and measured, and the sources table complete.
- **Generated**: Suno has produced takes.
- **Chosen**: The maintainer picks a take. That take is the song (SC-006).
- **Published**: The Suno link is in the record and the README.
- **Checked**: A README change has had the song check. Either it needs nothing, or the song goes back to Drafted
  in the same pull request, and the remix replaces the previous version.
