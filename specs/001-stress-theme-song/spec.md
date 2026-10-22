# Feature Specification: DnDGen.Stress Theme Song

**Feature Branch**: `001-stress-theme-song`

**Created**: 2026-10-23

**Status**: Draft

**Input**: User description: "Give DnDGen.Stress its theme song, as part of the DnDGen soundtrack documented in DnDGen/.github theme-song/README.md. The song is a Suno song whose lyrics come strictly from the DnDGen.Stress README, following the main theme's conventions: names in Latin or anagrams, descriptions in Middle High German, call-and-response in parentheses, and it brings back the Stress leitmotif (an anagram name Stress doesn't have yet) alongside the main theme's \"Rogante Marendor\". The soundtrack plan proposes a dwarven forge song (folk metal with anvil percussion) for the forge area, \"proving steel by striking it over and over\", but the genre is a proposal to confirm. Deliverables: the Suno style prompt, title, lyrics with a translation table back to the README, album art, a \"Theme Song\" badge next to the build badge in the Stress README, and the soundtrack and leitmotif tables in the org theme-song README updated with Stress."

## Clarifications

### Session 2026-10-23

- Q: Confirm the forge folk metal proposal, or choose another genre? → A: Forge folk metal, aiming for the vibe of
  Wind Rose's "Diggy Diggy Hole". The main theme used Latin, Middle High German, and anagrams for a cinematic,
  vocals-as-texture sound; this song doesn't need that, so its lyrics are in English.
- Q: Does the song record live in DnDGen.Stress or in DnDGen/.github? → A: In DnDGen.Stress. The exact folder is
  left to the plan.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Listen from the README (Priority: P1)

Someone reading the DnDGen.Stress README, on GitHub or on NuGet, sees a "Theme Song" badge beside the build
badge and follows it to the published song.

**Why this priority**: The song is the deliverable. Without a published song that readers can reach, nothing else
in this feature has anything to describe.

**Independent Test**: Open the README on GitHub, click the badge, and hear the published song.

**Acceptance Scenarios**:

1. **Given** the README on GitHub, **When** a reader looks at the badges, **Then** a "Theme Song" badge sits next to
   the build badge, in the same style as the main theme's badge in the organization README.
2. **Given** the badge, **When** a reader clicks it, **Then** the published song opens.
3. **Given** the package page on NuGet, **When** a reader views the README there, **Then** the badge displays, or
   the README still reads correctly without it.

---

### User Story 2 - Trace and remix the song (Priority: P2)

The maintainer, or anyone after them, can read the song's record and see where every lyric came from in the
README, which leitmotifs it uses, and how to remix it when the README changes.

**Why this priority**: Theme songs last and follow their README. A song that can't be traced back to the README
can't be kept in step with it.

**Independent Test**: Hand the record to someone who did not write the song. Using only the record, they can match
every lyric to the README and produce a remix.

**Acceptance Scenarios**:

1. **Given** the song's record, **When** a reader looks up any lyric line, **Then** the source table gives the
   README phrase it came from, or marks it as not from the README.
2. **Given** a README change the song should reflect, **When** the maintainer follows the record's remix steps,
   **Then** they know which lyrics, sources, and links to update.
3. **Given** the record, **When** a reader views it, **Then** it shows the album art, the style, the title, the
   lyrics, and the lessons learned while generating the song.

---

### User Story 3 - Find Stress in the soundtrack (Priority: P3)

Someone reading the organization's theme-song page sees Stress in the soundtrack plan, with its genre confirmed
and its song linked, and sees Stress's leitmotif in the leitmotif table.

**Why this priority**: The soundtrack ties the repositories together, but Stress's own song is useful without it.

**Independent Test**: Read the organization theme-song page and find Stress's leitmotif, confirmed genre, and song
link.

**Acceptance Scenarios**:

1. **Given** the organization theme-song page, **When** a reader looks at the leitmotif table, **Then** Stress has a
   row with its leitmotif and that leitmotif's meaning.
2. **Given** the soundtrack plan, **When** a reader finds the Stress row, **Then** it shows the confirmed genre and
   links to the song.

### Edge Cases

- **The README changes after the song is written.** Most changes don't need a remix. The record says what kind of
  change does, as the main theme's record does.
- **Suno drops, garbles, or growls lines.** The chosen take is the one that sings the Stress leitmotif and
  "Rogante Marendor". Anything it drops or alters is recorded in the record's lessons, as the main theme records
  its dropped *Argenteus Re*.
- **"Stress" gives no satisfying anagram.** The leitmotif may instead be an anagram of another name the repository
  is known by, such as its main type, as long as the record says which name it comes from.
- **The style or title is over Suno's limits** (1,000 characters for the style, 80 for the title). It is shortened
  before generating, not truncated by Suno.
- **The two repositories merge at different times.** Neither change links to something the other has not yet
  published.

## Requirements *(mandatory)*

### Functional Requirements

**The song**

- **FR-001**: DnDGen.Stress MUST have a theme song published on Suno, with a title and a style prompt within Suno's
  limits.
- **FR-002**: Every lyric MUST come from the DnDGen.Stress README, except lines explicitly marked as not from the
  README in the source table, as the main theme marks its *Nibelungenlied* opening.
- **FR-003**: The lyrics MUST be in English, apart from the leitmotif names, with a second voice for
  call-and-response written in parentheses.
- **FR-004**: The song MUST be forge folk metal, aiming for the vibe of Wind Rose's "Diggy Diggy Hole".
- **FR-005**: The song MUST sing both its own leitmotif and the main theme's "Rogante Marendor".

**The leitmotif**

- **FR-006**: DnDGen.Stress MUST have a leitmotif: a secret name that is an anagram of the repository's name, with a
  meaning, following the convention of the main theme's leitmotif table.
- **FR-007**: The maintainer MUST confirm the leitmotif before it is published anywhere.

**The record**

- **FR-008**: The song MUST have a written record in the DnDGen.Stress repository, next to the README it comes
  from, containing its title, Suno link, style, lyrics, source table, the leitmotifs it uses, remix steps, lessons
  learned while generating it, and album art with instructions to render the art again.
- **FR-009**: The album art MUST be square, MUST be recognizably part of the same family as the main theme's cover,
  and MUST credit and license everything it reuses.

**Linking it in**

- **FR-010**: The DnDGen.Stress README MUST show a "Theme Song" badge next to the build badge, linking to the song.
- **FR-011**: The organization theme-song page MUST list Stress in its leitmotif table and show Stress's confirmed
  genre and song link in its soundtrack plan.
- **FR-012**: The DnDGen.Stress change MUST be released as the constitution requires for documentation changes.

### Key Entities

- **Theme song**: the published song. It has a title, a Suno link, a genre, a style prompt, and lyrics.
- **Source entry**: one lyric line and the README phrase it came from.
- **Leitmotif**: a repository's secret name. It has the name it is an anagram of, and a meaning.
- **Song record**: the written page that holds the song's details, its art, and how to remix it.
- **Soundtrack plan entry**: a repository's row in the organization's plan. It has a genre, an area, and a link to
  the song once it exists.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every lyric line, 100%, has an entry in the source table that traces it to the README or marks it as
  not from the README.
- **SC-002**: A reader gets from the Stress README to the song in one click.
- **SC-003**: The chosen take sings the Stress leitmotif and "Rogante Marendor" at least once each.
- **SC-004**: Someone who did not write the song can remix it after a README change using only its record.
- **SC-005**: The organization's soundtrack plan no longer shows Stress's genre as a proposal.
- **SC-006**: The maintainer approves the chosen take, the leitmotif, and the album art.

## Assumptions

- Suno is the given way to make and publish the song. The free plan's limits apply, including the lack of Replace
  Section.
- "Diggy Diggy Hole" is a reference for the maintainer, not words for the style prompt. As the main theme's
  lessons say, the style names the sound itself (a stomping singalong chorus, anvil hits, a dwarven war chant)
  rather than a band or tradition.
- "Strictly from the README" follows the main theme's practice: a line that is not from the README is allowed only
  when the source table says so.
- The main theme's lines that already sing about Stress ("Aber unde aber", the lament about a flaw hidden in a
  thousand rolls) may come back as motifs, in English or in the original, since they come from the same idea in
  the README.
- The album art reuses the main theme's ingredients (the dndgen.com d20 and the Cinzel typeface) and their licenses
  carry over.
- Changes to DnDGen/.github are a separate pull request in that repository, made by the maintainer from their
  personal account.
- The package's code and behaviour do not change. The README is packed into the package, so the change still ships
  as a PATCH release under the constitution's versioning rules.
