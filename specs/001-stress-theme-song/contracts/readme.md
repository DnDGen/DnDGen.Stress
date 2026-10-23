# Contract: DnDGen.Stress Changes Outside the Record

## README.md

**Badge**, on the same line as the build badge, separated by a space:

```markdown
[![Build Status](...)](...) [![Theme Song](https://img.shields.io/badge/theme_song-Suno-ff69b4)](<Suno link>)
```

**Theme Song section**, at the end of the README:

```markdown
## Theme Song

DnDGen.Stress has its own theme song: [<Title>](<Suno link>). It follows this README, so a change here may need
the song updated too. See the song's [lyrics, notes, and update procedure](https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md).
```

Every link is absolute (R6).

## DnDGen.Stress/DnDGen.Stress.csproj

- `<Version>`: `5.0.3`.
- `<PackageReleaseNotes>`: replaced with notes for 5.0.3 only (R7), e.g. "The README now links to the DnDGen.Stress
  theme song. No changes to the code since 5.0.1. If you are upgrading across several versions, see each version's
  changes at https://github.com/DnDGen/DnDGen.Stress/releases".

## .specify/memory/constitution.md

The **README as contract** bullet gains a sentence (R2):

> Every README change also gets the theme song check in `docs/theme-song/README.md`, and any song update lands in
> the same pull request.

Version 1.0.0 → 1.1.0 (MINOR: an added obligation), and **Last Amended** set to the date this merges. The
amendment is its own commit, separate from the ratification-line fix (III.2).
