---
name: manual
description: Write and maintain the player manual under docs/manual/. Use whenever a manual chapter must be written, corrected or brought back in line with the code — including after a change to anatomy, modi mentis, humors, items, travel, narration, routines, fighting or social interaction. Carries the manual's style rules, its chapter map, and the procedure for deriving a chapter from the source.
---

# Writing the Proscribed Palimpsest manual

`docs/manual/` is the player-facing manual. It explains **how the game's systems work** — not what
content the game contains, not which key does what.

`design/` is drafts and development history. It is **mostly deprecated and must not be used as a
source**. The code is the only authority. Where a design note and the code disagree, the code is
right and the note is stale.

## What the manual is

A tabletop rulebook, not a video-game guide. The reader wants to know how a die pool is assembled,
what a wound costs, and why an organ score matters — the way a TTRPG core book explains its
resolution system. They can find the buttons themselves.

**Three exclusions, applied strictly:**

| Excluded | Why | What to write instead |
|---|---|---|
| **Content** | The 180 modi mentis, the 54 verbs, the 30 humors, the item catalogue, the archetypes, the biomes | Explain the *type*: what a modus mentis is, what properties it has, what it does in a roll. Name individual instances only as illustration, and only where one is structurally load-bearing (the four sanguific organs, the four cardinal humors). |
| **Interface** | Screens, buttons, menus, keys, clicks, popups, tabs | The rule the interface expresses. Not "press CONTINUE to clear the preview" but "an action is committed in stages, and each stage is settled before the next is offered." |
| **Narrative flavour** | Plot, setting fiction, what the world is *like* | The mechanism. The register carries the atmosphere; the content is technical throughout. |

**Human anatomy is a system, not content.** Organs feed derived stats, gate modi mentis, set level
ceilings and carry wounds, so the manual explains them one by one. This is the single deliberate
exception to the content rule.

**Call a modus mentis a modus mentis.** Singular *modus mentis*, plural *modi mentis* — the same
words the game itself uses in its chips and its memory panel. Do not substitute "skill", "faculty" or
"discipline"; a synonym invented for variety costs the reader the ability to match the manual to the
screen. The one exception is quoting the body panel, whose organ descriptions call an organ's modi
mentis its "disciplines" — Chapter I notes the equivalence once, and that is the only place the word
should appear.

**No implementation vocabulary.** `Verb`, `VerbAction`, `Outcome`, `Variant`, `PoV`, `ledger` and
the rest are how the code is organised, not how the world works. The manual says *a manner of
acting*, *an act*, *a consequence*, *the particular form it took*. Where a code type genuinely names
a player-visible thing (an area, a point of interest, an item, a modus mentis) the name is fine; the
test is whether a player could learn the word from playing.

**Never describe what does not exist yet.** No "inert until a crafting system exists", no "planned",
no "not yet". A reader cannot tell a design statement from a to-do list, and both read as apology.
Describe what is there. If a system is genuinely absent, say nothing about it at all.

**Names must match what the game shows on screen.** A measure called *Countenance* in the manual is
called Countenance in the body panel. When they disagree, decide which name is better and change the
other one — changing the C# `DisplayName` is entirely acceptable and often the right call, since the
manual is where the vocabulary is thought about. Check both the stat's `DisplayName` and any place a
renderer hardcodes its own label (the dialogue footer does).

## Register

Write as an old treatise on a pseudo-science would write — a work of natural philosophy explaining
a doctrine of humors and faculties, which happens to be exactly correct about the world it
describes.

**Hold these together:**

- **Neutral and technical.** State the mechanism. No enthusiasm, no address to the reader
  ("you will find that…"), no promises about how it feels to play.
- **Precise over accessible.** Give the formula, the bound, the exact tier. Where a rule is
  intricate, write the intricate version — do not simplify into a half-truth. A reader who wants
  the shape can read the first sentence of the paragraph.
- **Period register, modern clarity.** Slightly archaic vocabulary and a taste for definition and
  consequence: *the frame*, *the faculties*, *whence*, *it is to be observed that*. But sentences
  parse on first reading, and every number is stated plainly.
- **Light on flavour.** The tone is a thin varnish over an exact description. If a sentence is more
  ornament than information, cut it.

Anchor the vocabulary on what the code already uses. The organ and body-region `Description`
strings in `src/game/narrative/body/` are written in precisely this register and are the reference
for it — *"The central vital organ and index of the constitution's warmth, its span of years, and
its capacity for attachment."*

**Do not invent doctrine.** Every claim must be traceable to code. Where a system is unfinished,
say so once, plainly, and do not describe intended behaviour as though it were current.

## Chapters

```
docs/manual/README.md            index and reading order
docs/manual/01-anatomy.md        regions, organs, organ parts, species, anatomies,
                                 capabilities, derived stats, wounds, the making of a protagonist
docs/manual/02-memory.md         modi mentis: properties, functions, levels, experience,
                                 the five memory modules, acquisition and displacement
docs/manual/03-humors.md         the four queues, secretion, vital heat, transmuting virtues,
                                 black bile and the critical state
docs/manual/04-items.md          burden, bulk, anchors, vessels and storage, categories,
                                 consumables (recipe, richness, composition), implements,
                                 weapons, garments (armour and the eight social standings), coin
docs/manual/05-travel.md         the sphere, range, waypoints, the cost of a crossing,
                                 encounters, the clock, periods of the day
docs/manual/06-narration.md      the phase, noetic points, observation, thinking, action,
                                 the die pool, tools, companions, failure and its consequences
docs/manual/07-social.md         affinity, the conversation check, introduction, trade, work
docs/manual/08-fighting.md       turns, cinetic points, mediums, fighting skills, the attack
                                 and defence pools, wounds, terrain, flight
docs/manual/09-routines.md       recording, termini and prefixes, constraints, replay
```

Order is deliberate: the body, then what it carries, then the world it crosses, then the four
things it does there. Chapter numbers are stable — insert rather than renumber.

## Deriving a chapter

1. **Read the code, not the docs.** Start from the types named in the chapter map above. Class-level
   XML comments in this codebase are unusually rich and frequently explain *why* a rule is as it is;
   they are the best available source.
2. **Prefer registries and base classes** — `Verb`, `Outcome`, `ModusMentis`, `DerivedStat`,
   `FightingSkill`, `Item` — over individual subclasses. The base class is the system; the
   subclasses are the content.
3. **Quote numbers exactly.** Point budget, capacities, tiers, thresholds, probability tables.
   A number in the manual that is not in the code is a bug in the manual.
4. **Where a rule has a non-obvious reason, give it** — one clause, not a paragraph. Rules that
   sound arbitrary are the ones a reader most needs justified.
5. **Cross-reference between chapters** with plain relative links, and keep each chapter readable
   alone.

## What must never appear

- A key, a button, a menu name, a screen name.
- An exhaustive list of content instances.
- A number not present in the source.
- Second-person address, or any sentence about what playing feels like.
- Content sourced from `design/` without confirming it against the code.

## Building the PDF

The Markdown is the source; the PDF is a build artifact. **Rebuild it whenever a chapter changes**,
as the last step of any manual edit:

```bash
python tools/build_manual.py              # -> docs/manual/ProscribedPalimpsest-Manual.pdf
python tools/build_manual.py --html-only  # dumps the styled HTML to docs/manual/_html/ for debugging
```

Needs Chrome or Edge (found automatically) and, for page numbers and running heads,
`pip install pypdf reportlab`. Without those two it still builds, with a warning and no furniture.
Takes about a minute; it renders each chapter separately, which is what makes the page counts —
and therefore the folios, the recto-opening rule and the page-numbered contents — computable.

**All design lives in `tools/build_manual.py`, never in the Markdown.** No inline HTML, no
`<div>`, no style attributes, no hard page breaks in the chapters. If a chapter needs to look
different, that is a change to the stylesheet in the script.

The design is monochrome by instruction — black, white and grey, no accent ink — so hierarchy is
carried by size, letterspacing, small capitals, rules and grey value alone. Keep it that way.
Notable choices, so they are not undone by accident:

- **6×9 inch page**, ~65-character measure. A4 would give a 100-character line.
- **Constantia**, whose oldstyle figures are its default — the most period-correct face on a stock
  Windows machine. Sitka, Cambria and Georgia are the fallbacks.
- **Oldstyle figures in prose, lining tabular figures in tables.** The contrast is deliberate.
- **Booktabs tables**: three horizontal rules, no fills, no zebra, no vertical rules.
- The Markdown's trailing navigation line (`Previous: … · Next: …`) is hidden in print by the rule
  that follows `hr.rule`. Keep that footer format, or it will start appearing on paper.

The parser in the script covers only what the manual uses: ATX headings, paragraphs, pipe tables,
flat lists, rules, and the four inline forms. **Nested lists, blockquotes and images are not
supported** — if a chapter needs one, extend the parser rather than working around it.

## Keeping it current

`docs/manual/.synced` holds the commit the manual was last reconciled against. To bring the manual
up to date:

```bash
git diff --stat $(cat docs/manual/.synced)..HEAD -- src/
```

Map each changed area to its chapter using the map above, revise only the chapters the diff
actually touches, then write the new `HEAD` sha into `docs/manual/.synced`. A diff that touches
only rendering, CLI plumbing, packaging or tests changes no chapter — record the new sha and stop.
