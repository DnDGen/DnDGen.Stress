# Contract: DnDGen/.github Changes

Made by the maintainer, from their personal account, in a separate pull request that merges after the
DnDGen.Stress one (R8).

## theme-song/README.md

**Leitmotifs** introduction. Current text:

> Each generator has a secret name: an anagram of its repository name.
> The choir shouts them in the Boss Battle, and each repository's own theme song should bring its name back.

Proposed:

> Each generator has a secret name: an anagram of its repository name.
> The choir shouts them in the Boss Battle, and each generator's own theme song should bring its name back.
> The support libraries (Stress, Infrastructure, and Web) have no leitmotif: their songs describe their READMEs as
> they are.

**Soundtrack Plan** introduction. Current text:

> These genres are proposals, and each is confirmed when its song is written.

Proposed:

> These genres are proposals until each song is written; a written song's row links to it.

**Soundtrack Plan**, Stress row. The genre cell becomes:

```markdown
Forge folk metal: [listen](<Suno link>) · [lyrics and notes](https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md)
```

**New Theme Songs**, a new section after the Soundtrack Plan, so that the next repository's song, including any
repository added later, starts the same way:

```markdown
## New Theme Songs

Every repository gets a theme song, including any new one:

1. **Plan it.** Add the repository to the Soundtrack Plan with a proposed genre and area. A generator also gets a
   leitmotif, an anagram of its repository name; the support libraries don't.
2. **Write it from the README.** The lyrics come from the repository's README, and the song is recorded in that
   repository's `docs/theme-song/README.md`, with its sources, album art, and update procedure.
   [DnDGen.Stress](https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md) is the first example.
3. **Start from the main theme.** Generate the first version with Suno's Sample of this song's "Rogante Marendor"
   snippet, 3:00.0 to 3:17.1. After that, each update Covers the repository's current song instead.
```

## profile/README.md

A Stress row in the **Soundtrack** table, after the main theme, in the same form:

```markdown
| <a href="https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md"><img src="https://github.com/DnDGen/DnDGen.Stress/raw/main/docs/theme-song/cover.png" alt="<Name> album art" width="80"></a> | **<Name>** -DnDGen.Stress Theme- | | [Listen](<Suno link>) · [Lyrics and notes](https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md) |
```

The Leitmotif cell is empty, because Stress has none.
