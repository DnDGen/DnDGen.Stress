# Contract: The Song Record

`docs/theme-song/README.md`. It follows the main theme's record in DnDGen/.github `theme-song/README.md`, section by
section, except where noted.

| Section | Main theme | This record |
|---------|------------|-------------|
| `# <Title>` | Song title as heading | Same |
| Cover image linked to the song | Yes | Same, with `cover.png` |
| Introduction | Genre, and where the lyrics come from | Same: forge folk metal, lyrics from the DnDGen.Stress README, and a link back to the README |
| **Listen on Suno** | Yes | Same |
| Remixing | Short steps | Replaced by **Updating**, below |
| Style | Code block | Same, headed for the Suno field it goes in |
| Lyrics | Code block | Same, headed for the Suno field it goes in |
| Translation | Lyric, language, English, source | Replaced by **Sources**: lyric and the README phrase it came from |
| Leitmotifs | Per-generator names | Left out: Stress has no leitmotif |
| Soundtrack Plan | Per-repository genres | Left out: that belongs to the organization |
| Suno Notes | Lessons from generating | Same, for this song |
| Album Art | How to render, and credits | Same, for this cover |

The Title, Style, and Lyrics are each written in a code block on their own, so they can be copied into Suno's
Custom mode fields of the same names exactly as they are (R4).

The tests read the record by its headings: the title heading, `## Updating`, `## Style`, `## Lyrics`, and the
`## Sources` table with columns `Lyric` and `From the README` (R9). Renaming any of these means changing the tests too.

## Updating (draft)

Reviewed by the maintainer, who added the Suno steps (R4). Text:

> This song follows the [DnDGen.Stress README](https://github.com/DnDGen/DnDGen.Stress/blob/main/README.md). Every
> pull request that changes the README gets a song check:
>
> 1. If the change rewords or removes a phrase in the Sources table below, the song needs updating.
> 2. Otherwise, if the change alters the README's high-level ideas or vibe, the song needs updating.
> 3. Otherwise, it doesn't. Changes to the README's Theme Song badge or section never need a song update.
>
> When the song needs updating, it happens in the same pull request:
>
> 1. The contributor updates the Lyrics and Sources here to match the new README.
> 2. The maintainer makes the new song in Suno:
>    - **Cover the current song**, so the theme stays recognizably the same song as it evolves.
>    - Use the Title, Style, and Lyrics from this page. **Leave the Style as it is**: only the lyrics change.
>    - **Turn Variety all the way down**, so Suno keeps to the Style instead of straying from it.
>    - Pick the take. The new take replaces the old one.
>    - **Publish it**, so anyone with the link can find and hear it.
> 3. The maintainer updates the Suno link here and in the README, and adds anything learned to the Suno Notes.

## Album Art section

Credits the d20 (the dndgen.com loading spinner, from DnDGen.Web, originally from a gist by hperantunes) and Cinzel
(SIL Open Font License, with `Cinzel-OFL.txt`), and gives the render command, as the main theme's record does:

```
msedge --headless --hide-scrollbars --allow-file-access-from-files --window-size=1500,1500 --screenshot=cover.png cover.html
```
