# Implementation Plan: DnDGen.Stress Theme Song

**Branch**: `001-stress-theme-song` | **Date**: 2026-10-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-stress-theme-song/spec.md`

## Summary

Write, generate, and publish a forge folk metal theme song for DnDGen.Stress whose English lyrics capture the
README's high-level ideas and vibe, and record it in `docs/theme-song/` beside the album art. The record follows
the shape of the main theme's record in DnDGen/.github, minus the parts that only make sense for the main theme
(translation, leitmotifs, soundtrack plan), and adds an update procedure. The Stress README gains the main theme's
badge style and a short Theme Song section linking to both the song and the record. The constitution's README
contract gains a general rule that documents derived from the README are kept in step with it, with the song as
its example, so every plan and pull request applies the song check. A separate pull request in
DnDGen/.github confirms the genre, links the song, and says support libraries have no leitmotif. The lyrics and
style are drafted with the pr-song skill's Suno mechanics, overridden where a theme song differs from a PR song
([research.md](research.md) R3). Integration tests read the real README and record and enforce their mechanical
rules, above all that every phrase the song quotes is still in the README; they are written first and fail until
the record exists ([research.md](research.md) R9).

## Technical Context

**Language/Version**: Markdown for the record and README; HTML with inline SVG for the album art source; C# on
.NET 10 with NUnit 5 for the tests.

**Primary Dependencies**: Suno (free plan) to generate and publish the song; shields.io for the badge; the Cinzel
typeface (SIL OFL) and the dndgen.com d20 shape, both reused from the main theme's cover; headless Microsoft Edge
to render the cover; the pr-song skill for drafting the style and lyrics.

**Storage**: Files in the repository under `docs/theme-song/`. The song itself lives on Suno.

**Testing**: NUnit integration tests, each file mirroring the path of the file it tests:
`DnDGen.Stress.Tests/Integration/docs/theme-song/ReadmeTests.cs` for the record and
`DnDGen.Stress.Tests/Integration/ReadmeTests.cs` for the README. The test project
copies `README.md` and `docs/theme-song/README.md` into its build output, and the tests read those copies (R9).
They run in the existing pipeline step. The package's code doesn't change, so its 100% coverage is unaffected.
What the tests can't judge, such as the vibe, the take, the art, and anything needing the network, is checked by
hand per [quickstart.md](quickstart.md).

**Target Platform**: The README as rendered on GitHub and on nuget.org; the record as rendered on GitHub.

**Project Type**: Documentation and media for an existing NuGet library.

**Performance Goals**: A song that finishes in about 3 to 5 minutes. For sung and shouted metal that means lyrics
of roughly 2,800 to 3,400 characters, measured, per pr-song's length budgets.

**Constraints**: Suno's limits (1,000 characters for the style, 80 for the title, tracks clipped at 8:00, no
Replace Section on the free plan). Links in the README must be absolute, because nuget.org doesn't resolve links
relative to the repository. Changes to DnDGen/.github are made by the maintainer from their personal account.

**Scale/Scope**: One song, one record of five files, one test file, edits to the README, both csproj files, and the constitution in
DnDGen.Stress, and edits to two pages in DnDGen/.github.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Rule | Applies? | Status |
|------|----------|--------|
| **I.1** Emulate what exists, here first, then across DnDGen | Yes | Pass. The record, cover, and badge copy the main theme's record in DnDGen/.github ([contracts/song-record.md](contracts/song-record.md)). |
| **I.2** A new pattern must be worth everyone conforming to it | Yes | Pass, with justification. `docs/` is new to this repository and will be the pattern for every repository's song. It is the common convention for supporting documentation, and keeps non-code files out of the root beside `DnDGen.Stress/` and `cicd/` ([research.md](research.md) R1). |
| **I.3** Code reads like prose; comments only by a human | Partly | Pass. No code changes. The cover's HTML comment header is copied from the main theme's cover, so it is an existing pattern rather than a new comment. No `INFO:` comment is added to the README ([research.md](research.md) R2). |
| **II** Tests Are the Spec | Yes | Pass. The record's mechanical rules are tests, written first and seen to fail because the record doesn't exist yet (R9). |
| **II.1** What to test: what a consumer does and sees | Yes | Pass. The consumers are a contributor changing the README and a reader following its links; each case is named for what they see. |
| **II.2** How much to test | Yes | Pass. Limits are tested at the boundary by the real values; coverage of `DnDGen.Stress.dll` stays at 100% because no package code changes. |
| **II.3** How a test fails | Yes | Pass. Assertions compare whole lists, such as every missing phrase at once, and each failure says what to do. No test needs the network, so none fails because of the machine (II.3.c). |
| **III.1** Maintenance in scope | Yes | Pass. The ratification-line fix (`c490d9b`) is a stale-docs correction where we already are. |
| **III.2** Improve where you are, in its own commit | Yes | Pass. The ratification fix and the constitution amendment are separate commits. |
| **III.3** Behaviour out of scope | Yes | Pass. No behaviour changes. Tests that the README documents the whole public API are recorded as a GitHub issue, not built here. |
| **Versioning and release** | Yes | Pass. PATCH: 5.0.3. The README is packed into the package, so the release notes say the package changes only in its README, and that 5.0.1 was the last version with code changes. Pull request titled `5.0.3: Theme song`. |
| **README as contract** | Yes | Pass. The README still documents every constructor, method, and option. The Theme Song section links to supporting documentation, and the amendment adds that documents derived from the README are kept in step with it ([research.md](research.md) R2). |
| **Development workflow** | Yes | Pass. Branch and pull request to `main`, reviewed by the maintainer; the description is written from the diff. |
| **Governance: amendments** | Yes | Pass. The amendment is made by this pull request, with its reasoning in the description. It adds an obligation, so it is MINOR: constitution 1.0.0 → 1.1.0. |

**Post-design re-check**: The Phase 1 design adds no violations. The new patterns are `docs/theme-song/` and
tests that read repository files copied to the build output, both justified (R1, R9); Complexity Tracking stays
empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-stress-theme-song/
├── spec.md              # Feature specification (/speckit-specify, /speckit-clarify)
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0: decisions and alternatives
├── data-model.md        # Phase 1: the song, record, sources, and procedure
├── quickstart.md        # Phase 1: how to validate the feature
├── contracts/
│   ├── song-record.md   # Shape of docs/theme-song/README.md, including the update procedure
│   ├── readme.md        # Changes to the Stress README, csproj, and constitution
│   └── org-pages.md     # Changes to DnDGen/.github
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 (/speckit-tasks, not created here)
```

### Repository changes

```text
DnDGen.Stress (this pull request)
├── README.md                          # Badge beside the build badge; Theme Song section at the end
├── DnDGen.Stress/DnDGen.Stress.csproj # Version 5.0.3 and its release notes
├── .specify/memory/constitution.md    # README contract gains the song check; version 1.1.0
├── DnDGen.Stress.Tests/
│   ├── DnDGen.Stress.Tests.csproj     # Copies README.md and docs/theme-song/README.md to the build output
│   └── Integration/
│       ├── ReadmeTests.cs             # Tests for README.md
│       └── docs/theme-song/
│           └── ReadmeTests.cs         # Tests for docs/theme-song/README.md
└── docs/theme-song/
    ├── README.md                      # The song record
    ├── cover.html                     # Album art source
    ├── cover.png                      # Album art, rendered at 1500 × 1500
    ├── Cinzel.ttf                     # Title typeface, copied from DnDGen/.github
    └── Cinzel-OFL.txt                 # Its licence

DnDGen/.github (separate pull request, by the maintainer)
├── theme-song/README.md               # Stress row confirmed and linked; support libraries have no leitmotif
└── profile/README.md                  # Stress row in the Soundtrack table
```

**Structure Decision**: Everything for the song lives in `docs/theme-song/`, so the record and its art stay
together and a remix touches one folder. The font is copied rather than linked so that the cover renders from a
plain checkout ([research.md](research.md) R5).

## Complexity Tracking

No violations to justify.
