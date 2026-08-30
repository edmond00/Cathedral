---
name: release
description: Publish a new version of the game to itch.io — work out what changed since the last published build, write the changelog, name and number the new Volume, review the build for regressions, merge develop into main, package and upload. Use whenever asked to release, publish, ship or update the live version of Proscribed Palimpsest, or to prepare release notes for one.
---

# Releasing Proscribed Palimpsest

One release is one **Volume**. It has a number, a subtitle, a changelog, and a merge commit on
`main` carrying that changelog. Development happens on `develop`; `main` is only ever what was
published, so `main`'s history reads as the release history and nothing else.

The publishing machinery already exists and is not this skill's business: `package.ps1` stages and
zips, `publish.ps1` uploads with butler. Read the "Packaging a release" and "Publishing" sections of
`CLAUDE.md` before running either — everything they know about payload allow-lists, the `Ship` flag
and the smoke test still applies. This skill is the procedure *around* them.

## The track record

`.claude/skills/release/last-published.txt` holds the last commit whose content went live. It is the
only state this skill keeps, and every changelog is derived from it:

```bash
BASE=$(grep -v '^#' .claude/skills/release/last-published.txt | grep -v '^$' | awk '{print $1}')
git log --no-merges --pretty='%h %ad %s' --date=short $BASE..HEAD
git diff --stat $BASE..HEAD
```

**Skip `chore(release):` commits** when reading that log — the record is written by a commit of its
own after each release, so exactly one of those falls inside every range and it is bookkeeping, not
news.

Written **last**, and only once butler reports the upload finished. A record updated before the
upload succeeds silently swallows a whole release: the next run diffs from a commit nobody ever
played, and everything between them never appears in any changelog.

## Hotfixes, and why the record is the only authority

A hotfix is uploaded **by hand, outside this skill**: it keeps the Volume number and subtitle it
shipped under, writes no changelog file, and need not have a merge commit of its own. That is
expected, and nothing here tries to prevent it. Two consequences, both of which have bitten:

- **Never infer what is live from `dist/`.** A zip there is whatever was last packaged *on this
  machine*, which can be older than the live build (the hotfix was packaged somewhere else, or
  pushed from a staged folder that was never re-zipped) or newer (a package that was built and
  never uploaded). Its timestamp is evidence of nothing. The record file is the only statement of
  what players are running.
- **The record can lag.** Whoever hotfixes is supposed to move it to the commit they pushed,
  keeping the Volume label unchanged — `/release record <sha>` does exactly that. When they forget,
  the base sits behind the live build and the next changelog re-announces fixes already shipped.

So at step 2, if the range is longer than the calendar suggests or its early commits read as fixes
rather than features, **ask whether anything went out by hand since the recorded commit** and move
the base before writing a word. Asking costs one question; guessing costs a changelog that tells
players about work they already have.

**Recording a hotfix** is the whole of `/release record <sha>`, and it runs none of the ten steps:
resolve the sha, take its date, keep the Volume label from the line already there, rewrite the one
uncommented line, and commit it as `chore(release): hotfix recorded`. Nothing else moves — no
Volume bump, no changelog file, no merge. If the hotfix was cut from a commit that is not on
`develop`, say so rather than recording it: a base off the branch makes `$BASE..HEAD` meaningless.

## The ten steps

Do them in order. Steps 1–6 change nothing outside the working tree and can be abandoned freely;
step 8 onward is where a mistake costs a rewritten history or a bad build on the store page.

### 1. Preconditions

| check | why |
|---|---|
| `git rev-parse --abbrev-ref HEAD` is `develop` | the release is cut from develop and nowhere else |
| no modified **tracked** files (`git status --porcelain` shows only `??` lines) | they would be swept into the release commit, or lost in the merge |
| `git rev-list --count develop..main` is `0` | main must be an ancestor of develop; anything else means somebody committed to main directly, and the merge below is no longer the fast, safe shape it assumes |
| no `Cathedral.exe` running | `run_tests.sh` refuses to start beside another run, and a leftover process from an interrupted suite races the new one |

Untracked files are none of this skill's business — the repository root collects `log-crash-*.txt`,
scratch images and one-off scripts under `tools/`, and a release neither commits nor deletes them.

### 2. The range

Read the record, take the log and the diffstat (the commands above). Read enough of the actual diff
to write honestly about it — commit subjects in this repository are terse ("emotions", "broken mm",
"first blow") and name a system rather than describing a change. `git show --stat <sha>` per commit
is usually enough to tell a feature from a fix; open the diff where it is not.

If the range is empty, say so and stop. There is nothing to publish.

### 3. The changelog

Write `changelog/volume-<N>-<slug>.txt`, where the slug is the new subtitle in lower kebab case.
`changelog/` is committed — the note is the release's own record, and it is also what gets pasted
into the itch devlog.

```
Volume 2 - Of Choler and the Opened Carcass
2026-08-30

Added
  - ...

Changed
  - ...

Fixed
  - ...
```

Rules, in order of how often they are broken:

- **Short lines, most of the changes.** One line per item, no sub-bullets, no paragraph of
  explanation. Aim for 15–30 lines total. Mentioning nearly everything briefly is the goal;
  explaining anything in depth is not. A release that genuinely did one big thing gets a short file.
- **Player-facing voice.** Say what the player can now do, see or expect. No type names, no file
  paths, no `--flags`, no commit shas. "A wound can now cripple a discipline outright" — not
  "`GetMaxLevelForModusMentis` contributes −2 for a High-handicap wound".
- **Use the game's own vocabulary**: modus mentis / modi mentis, humors, organs, areas, verbs as the
  game words them. The same discipline the `manual` skill applies.
- **Internal work is one line or none.** Packaging, audits, test scripts, refactors and CLI plumbing
  changed nothing a player meets. Fold them into a single "Internal" line at the end, or leave them
  out. Do not pad the file with them.
- **No unreleased work.** If something in the range is half-built and switched off, it is not in the
  changelog. A changelog is read as a promise.

Send the finished file to the user (`SendUserFile`) before going any further — it is the one artefact
they will want to read and correct, and it becomes a commit message that cannot be edited afterwards
without rewriting the merge.

### 4. The Volume

Two constants in `src/Config.cs`, under `Config.Name`, drawn by `MainMenuRenderer` as the two lines
under the title:

```csharp
public const string Chapter         = "Volume 1";
public const string ChapterSubtitle = "Turnips and Radishes";
```

**Increment the number, keep the noun.** It read "Chapter 1" earlier in the history and "Volume 1"
now; whatever it currently says is what the next one says, plus one.

A hotfix ships under the number already in the file and changes neither constant, so what is in
`Config.Name.Chapter` is always the **live** Volume however many hand uploads have gone out since it
was set — and the next one is always exactly one more. Do not try to reconcile the number against
`changelog/`, which has an entry per Volume and none per hotfix.

**The subtitle is a chapter heading from an old encyclopedia**, and the register is the whole point:
a work of natural philosophy naming a section of itself, with the flat specificity of somebody who
believed the taxonomy. It should *loosely* evoke what this release changed — loosely, because a
reader must not be able to reconstruct the changelog from it.

| | |
|---|---|
| shape | two or three concrete nouns, joined by *and* or *of*. Title case. No colon, no dash, no clause |
| length | under about 40 characters — it is centred as `·  Subtitle  ·` on a 100-column grid |
| register | *Turnips and Radishes*. *Of Choler and the Opened Carcass*. *The First Blow and What It Teaches*. *Sinews and Their Ruin* |
| never | a version number, a feature name, a verb in the imperative, anything that reads as marketing |

Offer the user two or three candidates and let them pick. This is the one part of a release that is
taste rather than procedure, and it is on the main menu until the next one.

### 5. The review

The point of this step is that the changelog is a set of claims, and every claim needs something
behind it. Work in this order — cheapest first, and stop to fix rather than pressing on:

1. **`dotnet build`** — clean, and read the warnings.
2. **`./run_tests.sh audits`** — seconds, and it is the whole of the content-rule enforcement
   (crime, mm, verb, building, dialogue, npc, item, outcome, save). Zero failures required.
3. **`./run_tests.sh cli`** — the full script suite. Exit code is the failure count. A failure here
   blocks the release; do not publish around a red suite on the theory that the failing script is
   stale.
4. **`tests/save_reload.sh`** — the two-launch save check, deliberately not wired into
   `run_tests.sh` because it spans two processes. `CLAUDE.md` says to run it by hand before a
   release; this is that moment.
5. **Cover the changelog.** Walk the file line by line and ask what proves each line. Most will be
   covered by a `cli/verb/`, `cli/outcome/` or `cli/system/` script or by an audit — say which. For
   anything left uncovered, write a script and run it. **Copy a known-good script from the range it
   belongs to and adapt it**; never compose a `.cli` sequence out of the command vocabulary in
   `CLAUDE.md`, because the preview box between a keyword click and the action list will stall it
   and the failure says nothing about why. A feature that cannot be reached from a cold start needs
   a debug flag, and adding one is expected — see "Adding a debug flag" in `CLAUDE.md`.
6. **Read the diff for the classes of bug the tests cannot see.** The save round trip catches a
   field added to a party type only if the audit was updated with it (`dotnet run -- --save-audit`);
   ray picking, camera framing, the glyph atlas and narration *quality* are outside `--cli`
   entirely. If the range touched any of them, say so in the report and click through once by hand.

Report what passed, what was newly covered, and what could not be verified. **Do not soften it**: a
release note that omits a known-shaky area is worse than a delayed release.

### 6. The manual

`package.ps1` rebuilds the PDF from `docs/manual/` and stages it beside the executable, so the
manual ships whether or not anyone looked at it. An unsynced manual is therefore not a chore left
undone — it is a manual on the store page describing rules the build next to it no longer has.

**Hand the changelog to the `manual` skill rather than re-deriving the work from the diff.** Invoke
it (the Skill tool, `manual`) and give it three things:

1. the changelog file just written — every line of it is already a player-facing statement of a
   rule, which is the manual's own unit of thought, where a diff is not;
2. the commit range `$BASE..HEAD`, so it can read the source behind any line;
3. the question: **which of these changed a rule a player is subject to, and does the chapter that
   owns that rule still describe it correctly?**

Four things to tell it, because they are particular to running it this way:

- **Work line by line, and report the lines that map to no chapter.** That list is the useful
  output: a changelog line owned by nothing is either purely internal (fine) or a rule with no
  chapter to live in (not fine, and the manual's chapter map may need a new section).
- **`docs/manual/.synced` is the manual's base, and it is not this release's base.** It can be
  older (chapters have been behind for several Volumes) or newer (a sync ran mid-cycle).
  Reconcile from **whichever of the two is older**, or the gap between them falls through.
- **Internal lines touch nothing.** Rendering, packaging, the CLI driver, audits and test scripts
  change no rule a player is subject to, and the manual explicitly does not describe the interface.
- **Finish the job**: revise only the chapters the mapping reached, write `HEAD` into
  `docs/manual/.synced`, and rebuild with `python tools/build_manual.py` so what gets reviewed is
  what gets shipped.

`/manual-sync` is the standalone equivalent and is a fine substitute when the range is long enough
that the changelog has compressed too much away. The difference is only where the list of things to
check comes from: it derives one from the diff, this hands it one that has already been written and
read.

Report which chapters changed, which were checked and left alone, and which changelog lines mapped
to nothing. Revised chapters and the rebuilt PDF go into the release commit below.

### 7. The release commit

On `develop`, commit exactly the changelog file and the two Volume constants (plus whatever steps 5 and 6
new test scripts and manual chapters produced):

```bash
git add changelog/volume-<N>-<slug>.txt src/Config.cs
git commit -m "Volume <N> - <Subtitle>"
```

### 8. Merge into main

`main` is a strict ancestor of `develop`, so this would fast-forward — and a fast-forward has no
commit of its own to carry a message, which is exactly what is wanted here. Force the merge commit:

```bash
git checkout main
git merge --no-ff develop -F changelog/volume-<N>-<slug>.txt
```

`-F` rather than `-m`: the changelog is the message, verbatim, and this is what makes `git log main`
a readable release history.

### 9. Package and publish

From `main`, with `main` checked out — the artifact must come from the branch that was merged, not
from develop:

```powershell
./package.ps1                 # rebuilds the manual, stages, zips. Minutes.
./publish.ps1 -Status         # read-only: what is on the channel now
./publish.ps1 -Yes
```

**`-Yes` is not optional here, and the reason is a trap.** `publish.ps1` prompts for confirmation and
*cancels rather than throwing* when nothing can answer it — which is what a non-interactive session
is. Without the flag the script exits cleanly having uploaded nothing, and looks like it worked. So:
**ask the user in the conversation, get an explicit yes, and only then pass `-Yes`.** Never pass it
on your own initiative; this is the irreversible, outward-facing step of the whole procedure.

Let the smoke test run. It launches the staged build with an empty command line and waits for a
window and `LLM Server is ready`, and it is the only check that the self-contained runtime resolves,
the GGUF loads and the fonts open. `-SkipSmokeTest` is for re-pushing something already started once
and for nothing else.

When butler finishes, **remind the user to upload the manual PDF by hand**. `publish.ps1` cannot:
butler only replaces builds in channels it owns and the PDF on the page has no channel. The script
closes with a reminder naming the freshly built file; repeat it, with the full path.

### 10. Reset develop and record

Development continues from what was published, so `develop` is moved onto the merge commit rather
than left behind it:

```bash
git checkout develop
git merge --ff-only main
```

Then write the record — the merge commit's sha, which is now `develop`'s tip and is the commit whose
content is live. Replace the single uncommented line of `last-published.txt` with
`<sha> <YYYY-MM-DD> Volume <N> - <Subtitle>`, then:

```bash
git add .claude/skills/release/last-published.txt
git commit -m "chore(release): Volume <N> published"
```

Finally, **ask before pushing.** `main` and `develop` both have origin counterparts and pushing them
is the ordinary end of a release, but it is another outward-facing step and it is the user's call:

```bash
git push origin main develop
```

## Traps

- **The changelog is the merge message and cannot be edited afterwards** without rewriting a commit
  that has been pushed. Get the user's eyes on the file at step 3, not at step 7.
- **Never `git branch -f develop`** to move the branch. `--ff-only` fails loudly if the shape is not
  what step 1 asserted; forcing hides the fact that somebody committed to main behind your back.
- **`run_tests.sh` takes a long time.** Run it in the background and let the notification come back;
  do not poll it. And take the leftover processes down before starting — a killed runner does not
  stop the `dotnet run` instances it spawned, and they keep opening windows for minutes.
- **A red suite is not a stale test.** Every script in the suite is a regression that somebody hit
  once. Fix the code or fix the script deliberately; do not publish past it.
- **The volume constants are player-facing text and nothing more.** They are not read by the save,
  the packaging or the store metadata, so bumping them cannot break anything — which is also why
  forgetting them is silent, and why they are their own numbered step.
