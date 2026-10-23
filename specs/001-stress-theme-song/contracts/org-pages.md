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

## profile/README.md

A Stress row in the **Soundtrack** table, after the main theme, in the same form:

```markdown
| <a href="https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md"><img src="https://github.com/DnDGen/DnDGen.Stress/raw/main/docs/theme-song/cover.png" alt="<Name> album art" width="80"></a> | **<Name>** -DnDGen.Stress Theme- | | [Listen](<Suno link>) · [Lyrics and notes](https://github.com/DnDGen/DnDGen.Stress/blob/main/docs/theme-song/README.md) |
```

The Leitmotif cell is empty, because Stress has none.
