# Feature Specification: DnDGen.Stress Theme Song

**Feature Branch**: `001-stress-theme-song`

**Created**: 2026-10-23

**Status**: Draft

**Input**: User description: "Give DnDGen.Stress its theme song, as part of the DnDGen soundtrack documented in DnDGen/.github theme-song/README.md. The song is a Suno song whose lyrics come strictly from the DnDGen.Stress README, following the main theme's conventions: names in Latin or anagrams, descriptions in Middle High German, call-and-response in parentheses, and it brings back the Stress leitmotif (an anagram name Stress doesn't have yet) alongside the main theme's \"Rogante Marendor\". The soundtrack plan proposes a dwarven forge song (folk metal with anvil percussion) for the forge area, \"proving steel by striking it over and over\", but the genre is a proposal to confirm. Deliverables: the Suno style prompt, title, lyrics with a translation table back to the README, album art, a \"Theme Song\" badge next to the build badge in the Stress README, and the soundtrack and leitmotif tables in the org theme-song README updated with Stress."

## Clarifications

### Session 2026-10-23

- Q: Confirm the forge folk metal proposal, or choose another genre? → A: Forge folk metal, aiming for the vibe of
  Wind Rose's "Diggy Diggy Hole". The main theme used Latin, Middle High German, and anagrams because its genre
  wanted a cinematic, vocals-as-texture sound. Most repository songs won't, and this one is in English.
- Q: Does the song record live in DnDGen.Stress or in DnDGen/.github? → A: In DnDGen.Stress. The folder is
  `docs/theme-song`, which the plan records.
- Q: Do the support libraries (Stress, Infrastructure, Web) get leitmotif anagrams like the generators? → A: No.
  Their songs describe their READMEs as they are. The generators' songs bring back the leitmotifs the main theme
  gave them.
- Q: Does the README link only to the song, or to its record too? → A: Both. The record is supporting
  documentation, so the README is still the contract.
- Q: What makes a take "the" theme song? → A: Whichever take the maintainer picks. The spec sets no criteria for
  that choice.
- Added User Story 3: changing the README comes with clear instructions to update the song, and changes that
  don't touch the README need no song change.
- Q: How much of the README does the song need to cover? → A: The writer chooses. The test is whether the song
  captures the README's high-level ideas and vibe, not whether every section gets a line. A README change that
  touches only the song's own badge, links, or mentions doesn't count, so the song never has to follow itself.
- Q: Which README changes require the song to be updated? → A: Every README change gets a song check. The song is
  updated only if the change alters the README's high-level ideas or vibe, or changes or removes a phrase the
  source table cites.
- Q: Does the song update land in the same pull request as the README change that prompts it? → A: Yes. The
  contributor updates the lyrics and source table, and the maintainer remixes and updates the song link before
  merging.
- Q: When the song is remixed, what happens to the previous version? → A: It is replaced. The record shows only
  the current song; git history and Suno keep the old versions.
- Q: Should the rules for the README and the record be enforced by automated tests? → A: Yes, the mechanical ones,
  as integration tests that read the real README and record. They never judge the song's ideas, vibe, or chosen
  take, and never need the network. Tests that the README documents the whole public API are a separate feature,
  recorded as a GitHub issue.

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
3. **Given** the README, **When** a reader wants to know more about the song, **Then** a link takes them to its
   record.
4. **Given** the package page on NuGet, **When** a reader views the README there, **Then** the badge displays and
   both links work, or the README still reads correctly without the badge.

---

### User Story 2 - Trace the song back to the README (Priority: P2)

The maintainer, or anyone after them, can read the song's record and see where every lyric came from in the
README.

**Why this priority**: Theme songs last and follow their README. A song that can't be traced back to the README
can't be kept in step with it.

**Independent Test**: Hand the record to someone who did not write the song. Using only the record, they can match
every lyric to the README.

**Acceptance Scenarios**:

1. **Given** the song's record, **When** a reader looks up any lyric line, **Then** the source table gives the
   README phrase it came from, or marks it as not from the README.
2. **Given** the record, **When** a reader views it, **Then** it shows the album art, the style, the title, the
   lyrics, and the lessons learned while generating the song.

---

### User Story 3 - Keep the song in step with the README (Priority: P3)

Someone changing the DnDGen.Stress README is told that the theme song follows the README, and is given a clear
procedure for updating it. Someone making a change that doesn't touch the README has nothing to do for the song.

**Why this priority**: Without this, the song drifts from the README with the first change after it is written.
It matters only once the song exists.

**Independent Test**: Ask someone to make a README change, without mentioning the song. They find the update
instructions on their own and can follow them.

**Acceptance Scenarios**:

1. **Given** a change to the README, **When** the contributor makes it, **Then** they find instructions that the
   theme song should be updated, and where the update procedure is.
2. **Given** a README change, **When** the contributor follows the procedure, **Then** they know whether the song
   needs updating and, if it does, which lyrics, sources, record entries, and links to update and who remixes the
   song.
3. **Given** a change that doesn't touch the README, **When** it is reviewed, **Then** no song change is expected.
4. **Given** a README change that rewords or removes a phrase the song quotes, **When** the tests run, **Then**
   they fail, name each missing phrase, and point to the update procedure.

---

### User Story 4 - Find Stress in the soundtrack (Priority: P4)

Someone reading the organization's theme-song page sees Stress in the soundtrack plan, with its genre confirmed
and its song linked, and learns why Stress has no leitmotif.

**Why this priority**: The soundtrack ties the repositories together, but Stress's own song is useful without it.

**Independent Test**: Read the organization theme-song page and find Stress's confirmed genre and song link, and
the note on support libraries.

**Acceptance Scenarios**:

1. **Given** the soundtrack plan, **When** a reader finds the Stress row, **Then** it shows the confirmed genre and
   links to the song.
2. **Given** the leitmotif section, **When** a reader looks for Stress, **Then** it says that the support libraries
   (Stress, Infrastructure, Web) have no leitmotif, and that their songs describe their READMEs as they are.

### Edge Cases

- **Suno drops, garbles, or growls lines in the take the maintainer picks.** The take is still the song. The
  record notes what it drops or changes, as the main theme notes its dropped *Argenteus Re*.
- **The style or title is over Suno's limits** (1,000 characters for the style, 80 for the title). It is shortened
  before generating, not truncated by Suno.
- **The README is read on NuGet**, where links relative to the repository don't resolve. Links in the README still
  reach the song and its record.
- **The person changing the README can't remix the song**, for example because they have no access to the Suno
  account. They update the lyrics and source table, and the maintainer remixes before merging.
- **A README change shifts the README's ideas or vibe without touching any phrase the song quotes.** The tests
  pass, so the human half of the song check still applies.
- **The README changes only where it mentions the song**, such as a new Suno link after a remix. That isn't a
  README change the song follows, or every remix would call for another.
- **The two repositories merge at different times.** Neither change links to something the other has not yet
  published.

## Requirements *(mandatory)*

### Functional Requirements

**The song**

- **FR-001**: DnDGen.Stress MUST have a theme song published publicly on Suno, so anyone with the link can find and
  hear it, with a title and a style prompt within Suno's limits.
- **FR-002**: The song MUST be forge folk metal, aiming for the vibe of Wind Rose's "Diggy Diggy Hole".
- **FR-003**: The lyrics MUST be in English and capture the high-level ideas and vibe of the DnDGen.Stress README
  as it is. The writer chooses which ideas to sing; the song doesn't have to cover every section. Every lyric MUST
  come from the README, except lines explicitly marked as not from the README in the source table, as the main
  theme marks its *Nibelungenlied* opening.
- **FR-004**: The song MUST NOT need a leitmotif. Stress is a support library, so it has no anagram name.

**The record**

- **FR-005**: The song MUST have a written record in the DnDGen.Stress repository, containing its title, Suno link,
  style, lyrics, source table, update procedure, lessons learned while generating it, and album art with
  instructions to render the art again. It MUST show only the current song: a remix replaces the previous version,
  which git history and Suno keep.
- **FR-006**: The album art MUST be square, MUST be recognizably part of the same family as the main theme's cover,
  and MUST credit and license everything it reuses.

**Keeping it in step with the README**

- **FR-007**: Someone changing the DnDGen.Stress README MUST be told, where they make the change, that the theme
  song follows the README and where its update procedure is.
- **FR-008**: Every README change MUST get a song check. The song MUST be updated when the change alters the
  README's high-level ideas or vibe, or changes or removes a phrase the source table cites, and not otherwise. The
  update procedure MUST give both the check and the steps for updating.
- **FR-009**: The song update MUST land in the same pull request as the README change that prompts it. The
  contributor updates the lyrics and source table; the maintainer remixes the song and updates its link before
  merging. A remix keeps the song recognizably the same song: its style stays the same and only its lyrics change.
- **FR-010**: A change that doesn't touch the README MUST NOT require a song change. Neither does a README change
  that touches only the song's own badge, links, or mentions.

**Linking it in**

- **FR-011**: The DnDGen.Stress README MUST show a "Theme Song" badge next to the build badge, linking to the song,
  and MUST link to the song's record. Both links MUST work wherever the README is read, including NuGet.
- **FR-012**: The organization theme-song page MUST show Stress's confirmed genre and song link in its soundtrack
  plan, and MUST say that the support libraries have no leitmotif and that their songs describe their READMEs as
  they are.
- **FR-013**: The DnDGen.Stress change MUST be released as the constitution requires for documentation changes.

**Checking it automatically**

- **FR-014**: Automated tests MUST fail when a phrase quoted in the source table no longer appears in the README,
  naming each missing phrase and pointing to the update procedure.
- **FR-015**: Automated tests MUST also check that the record has its update procedure, where FR-014's failures
  point; that every lyric line has a source entry and every source entry's lyric is in the lyrics; that the title and style are within Suno's limits; that the README and the record link to
  the same song; and that the README's links are absolute and its link to the record leads to a file that exists.
- **FR-016**: The automated tests MUST NOT judge the song's ideas, vibe, or chosen take, and MUST NOT need the
  network.

### Key Entities

- **Theme song**: the published song. It has a title, a Suno link, a genre, a style prompt, and lyrics.
- **Source entry**: one lyric line and the README phrase it came from.
- **Song record**: the written page that holds the current song's details, its art, and its update procedure.
- **Update procedure**: the steps for deciding whether a README change needs the song updated, and for updating it.
- **Soundtrack plan entry**: a repository's row in the organization's plan. It has a genre, an area, and a link to
  the song once it exists.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every lyric line, 100%, has an entry in the source table that traces it to the README or marks it as
  not from the README, and the automated tests confirm it on every build.
- **SC-002**: From the README, on GitHub or NuGet, a reader gets to the song in one click and to its record in one
  click.
- **SC-003**: Someone changing the README for the first time finds the update instructions without being told where
  to look.
- **SC-004**: Someone who did not write the song can update it after a README change using only its record.
- **SC-005**: The organization's soundtrack plan no longer shows Stress's genre as a proposal.
- **SC-006**: The theme song is the take the maintainer picks. The maintainer judges that its lyrics capture the
  README's high-level ideas and vibe, and approves the album art.

## Assumptions

- Suno is the given way to make and publish the song. The free plan's limits apply, including the lack of Replace
  Section.
- "Strictly from the README" follows the main theme's practice: a line that is not from the README is allowed only
  when the source table says so.
- "Diggy Diggy Hole" is a reference for the maintainer, not words for the style prompt. As the main theme's
  lessons say, the style names the sound itself rather than a band or tradition.
- The album art reuses the main theme's ingredients (the dndgen.com d20 and the Cinzel typeface) and their licenses
  carry over.
- The record is supporting documentation, not documentation of the package, so the README stays the contract the
  constitution describes.
- Changes to DnDGen/.github are a separate pull request in that repository, made by the maintainer from their
  personal account.
- Tests that the README documents every public method and option, with the right defaults, are out of scope here.
  They are recorded as a GitHub issue for a later feature (III.3).
- The package's code and behaviour do not change. The README is packed into the package, so the change still ships
  as a PATCH release under the constitution's versioning rules.
