# Brief: wills with several targets

You are in a cloud session on the branch `multi-target-wills`, cut from `biome-overhaul`. Commit and
push to it as you go. This branch is meant to be merged, so the usual standards hold in full.

## The problem

A will (`ModusMentisFunction.Will` + `ModusMentis.Will`, a `WillTarget`) wants one kind of act and
refuses everything else. In play it feels too restricted: in the wrong place it does nothing at all.
Two concrete complaints from playtests:

- a fighting will is useless in a city, where there is nobody to attack;
- wills idle in places that plainly should serve them: herblore with nothing to do in a forest,
  mycology with nothing to do in a mushroom cell. Mycology should also find something in an
  ordinary forest.

## What to build

1. **Several targets per will, 1 to 5.** `ModusMentis.Will` becomes a list of `WillTarget`s, each its
   own goal with its own purpose. Example: a fighting will wants to attack an enemy, but also to buy a
   weapon that suits its persona, so it has a use in a city. The planner (`ActionPathPlanner`) and
   every narrowing in `WillPriority` must treat the will as wanting *any* of its targets, and pursue
   the nearest. Keep the existing guarantees: one shortest path, any equally short one accepted,
   `WillMaxPathSteps`, `WillActionNeverRefusesItsPathRule`, the step-naming narration, `inspect will`.
   With several targets reachable at the same distance, decide the order (nearest first, then
   declaration order, or the goal judge) and document it.
2. **The memory submenu** shows each target of a will as a list in the modus mentis's info, not one
   line.
3. **The will share moves to roughly 40% will / 60% other**, from today's 25–33%
   (`WillAudit.MinShare`/`MaxShare`, and the learned share `MinLearned`/`MaxLearned`, which must move
   coherently). The rule for who joins: a modus mentis whose persona is **specific** (it wants
   recognisable things: mycology, anglery, armoury lore) may become a will, now with several targets
   that its persona covers. One whose persona is **generic or vague** (finesse, patience, scrutiny,
   the senses) stays an ordinary mind. Read the history in `src/game/CLAUDE.md` ("A will: one target,
   and the shortest way to it") first. Fourteen crafts were made ordinary because one target made
   them refuse most of what their description says they do. Several targets is the answer to exactly
   that, so revisit those fourteen, but keep the generic ones generic. List every modus mentis you
   convert, with its targets, in the report.
4. **Wills find something to do in related places.** For each will, decide the location kinds where
   it ought to have a target (herblore: forest, meadow; mycology: mushroom cell, forest, damp places),
   and make sure it does. Add targets, or **add content** (points of interest, items, spawns) where
   the place lacks the thing. Content follows the rules in CLAUDE.md: kinds are types (`PoiKinds.cs`,
   `AreaKinds.cs`), never strings. Run `--verb-audit` and `--item-audit` after adding any.
5. **Target items by tag, so a target covers a family.** Mycology targets "a mushroom", and truffle,
   amanita and every other mushroom answer to it. Items already carry `ItemTag`s (`src/game/narrative/ItemTag.cs`),
   but today these exist only to stock shelves. Either extend them to serve as will targets or
   introduce a separate kind tag. Choose, and justify the choice in the report. It must be an enum
   or a type, **never a string**: the CLAUDE.md rule "Never identify content by a string" explains
   the modi mentis that once shipped unreachable that way. Add a `WillTarget` narrowing for it (like
   `.Yielding<T>()` / `.YieldingAny(...)`), and tag the existing items generously.

Keep the six rules in CLAUDE.md "Rules that always apply". Startup rules R17/R18 must be updated to
the new shape and stay fatal (every target's verbs performable by every anatomy that can learn it).

## Metrics: before, then after

Before changing behaviour, build the measurement, run it on the untouched code and **commit the
baseline output**. Then measure again at the end with the same tool. The planner is deterministic and
the location factories run headless, so none of this needs the language model.

At least:

- **Will share:** in the catalogue and among the learned lessons (what `WillAudit` already reports).
- **Coverage:** for each will × sampled location kind (both hours `WillAudit` uses, several seeds),
  whether a plan exists (`steps != none`), and its length. Report the share of (will, place) pairs
  with something to do, overall and per will.
- **Related coverage:** the same, restricted to the places you declared related to each will in
  point 4. This is the headline number for the playtest complaint. Keep the declared relations as
  data in the code, typed, so the audit keeps checking them afterwards.
- **Breadth guard:** the share of places where a will has a target. A will that has a target
  everywhere is no longer a will. Flag any above a ceiling you choose and justify.
- **Refusals in play:** if you can, a count of will refusals over a batch of `--playground --seed`
  runs on fixed routes, before and after. Say so if you cannot get it reliable.

Fold the lasting parts into `--mm-audit` (or a new audit flag registered in the `audits` skill), so
the checks keep running after this task. Make the ✗ thresholds reflect the new targets.

**The report:** a single HTML page with the before/after tables, a per-will chart of coverage in
related places, the list of converted modi mentis with their targets, the content added, and anything
that got worse. If the Artifact tool is available in this session, publish the page with it and give
the link. Either way, commit it as `reports/multi-target-wills.html`.

## Before anything

1. Read `CLAUDE.md` and `src/game/CLAUDE.md` (the will section, the noetic economy, verbs and
   outcomes). Invoke the **`audits`** skill (and `mm-grants` if lessons change). Invoke the
   **`choice-judge`** skill before touching a modus mentis's description or persona, or adding a
   decision for the goal judge.
2. Set up the machine: `bash tools/linux/cloud-setup.sh`, then the GloVe vectors
   (`tools/linux/fetch-assets.sh --glove`, or the gensim GitHub mirror described in the `music`
   skill's "A fresh cloud session" section). Build with `dotnet build Cathedral.csproj -r linux-x64`.
   Run the game under `xvfb-run -a` with `export ALSOFT_DRIVERS=null`. `tools/linux/README.md` has
   the rest. The model (`fetch-assets.sh --model`, about 2 GB; check its SHA-256 against
   `models/README.md`) is needed only if you test a will's choices with the real LLM rather than
   `--playground`.

## Verifying

- `./run_tests.sh audits` must pass.
- All `cli/system/will_*.cli` must pass, plus any script that reaches what you touched (grep `cli/`).
  Add scripts for: a will with two targets taking the nearer one, the fighting will that buys a
  weapon in a city, mycology finding a target in a forest, a tagged item answering a tag target, and
  the memory submenu listing the targets. **Invoke the `verifying` skill before writing any `.cli`
  script.**
- Do not run the full suite unless the rest is green and you have time left. Say which scripts you
  ran.

## Documentation

- Rewrite the will section of `src/game/CLAUDE.md` to the new design. Its premise "a desire for one
  act, never a competence" changes, so say what replaces it and why. Update the share numbers there
  and in the `audits` skill.
- This changes a rule players are subject to, so **invoke the `manual` skill** and update
  `docs/manual/02-memory.md` (it says "somewhat under a third of the catalogue are wills" and "it
  wants one thing").

## When you finish

Push the branch. Your last message gives: the before/after headline numbers, the report's link or
path, the modi mentis converted, the content added, the scripts you ran and what they showed, and
anything left undone.
