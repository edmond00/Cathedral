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

- **Plain, not literary. This is the rule that gets broken.** The changelog is a list of changes, and
  a reader must be able to tell what each line *is* on one pass — a feature, a fix, what it affects.
  The game's prose voice belongs in the game and in the Volume subtitle; it does not belong here.
  Concretely, in a changelog line: no metaphor, no allusion, no riddling compression, no rhetorical
  inversion, no sentence whose subject is an abstraction ("What you do now moves you"). State the
  change directly, subject first, in the order a player would meet it.

  | instead of | write |
  |---|---|
  | "What you do now moves you. One disposition you hold answers the act, secretes humors into the spleen, and says so in its own voice." | "Actions can now provoke an emotion. One of the dispositions you hold answers what you did, secretes humors into the spleen, and narrates its reaction." |
  | "Implements are judged before they are argued about." | "Tool use is now judged before the model is asked." |
  | "The knife survives the carcass." | "Combining an item with a tool no longer consumes the tool." |
  | "A wound no longer merely slows a discipline's growth — it can take it away." | "A wound can now disable a modus mentis outright, where before it only slowed its growth." |

  Flavour is not the same as vagueness, and cutting it must not cost detail: if a line names a number,
  a condition or a consequence, the plain rewrite keeps it. "Three new mind states: Constantia, which
  steadies a poor roll" becomes "Three new humors: Constantia, which turns a 1 into a 3" — shorter *and*
  more informative.
- **Short lines, most of the changes.** One line per item, no sub-bullets, no paragraph of
  explanation. Aim for 15–30 lines total. Mentioning nearly everything briefly is the goal;
  explaining anything in depth is not. A release that genuinely did one big thing gets a short file.
- **Player-facing voice.** Say what the player can now do, see or expect. No type names, no file
  paths, no `--flags`, no commit shas. "A wound can now disable a modus mentis outright" — not
  "`GetMaxLevelForModusMentis` contributes −2 for a High-handicap wound".
- **Use the game's own vocabulary**: modus mentis / modi mentis, humors, organs, areas, verbs as the
  game words them. That is the *nouns* only — borrowing the game's terms is not licence to borrow its
  register. The same discipline the `manual` skill applies.
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


---

# Reference: packaging, publishing and naming

Moved here from the root `CLAUDE.md`. This is the *how it works* behind steps 7-9.

## Packaging a release

```powershell
./package.ps1                      # dist/ProscribedPalimpsest/ + dist/ProscribedPalimpsest-win64-<date>.zip
./package.ps1 -NoModel -NoZip      # seconds, for checking the script itself
./package.ps1 -ReadyToRun          # ~30% larger, faster startup
```

Self-contained (`--self-contained`), so the .NET runtime ships inside and a player needs nothing
installed. About 200 MB, and it removes the commonest "it won't start" report.

**Single-file, so the folder a player opens is legible.** A plain self-contained publish is 301
files at the root and the game sits in the middle of them, alphabetically between
`PresentationFramework.dll` and `System.Private.CoreLib.dll`. `PublishSingleFile` bundles the
managed assemblies into the executable: 9 files at the root, of which one is the game and the rest
are unmanaged libraries that cannot be bundled.

This is **bundling, not trimming** — every assembly is still present, just inside the exe, so the
reflection the audits and Catalyst depend on is untouched. Managed code runs from the bundle
without being extracted, so there is no first-run unpacking cost.

Two more prunings, worth about 16 MB and 220 files: `SatelliteResourceLanguages=en` drops thirteen
folders of localised framework strings for a game with no localisation, and the staging step
deletes a macOS `.dylib` and a 32-bit MIDI native that an x64 process can never load. After
pruning the MIDI native, confirm audio still starts — the smoke test asserts a window and a ready
server but says nothing about sound, and the line to look for is
`Ambient music engine started`.

**The console is a build flag, not a code path.** `dotnet build` and `dotnet run` produce a console
`Exe` as always; `package.ps1` passes `-p:Ship=true`, which flips `OutputType` to `WinExe` so a
player double-clicking the game gets no black window filling with diagnostics. The two differ only
in a PE subsystem byte — read it with `[BitConverter]::ToUInt16($bytes, $peOffset + 92)`: 3 is
console, 2 is GUI. It is keyed on an explicit `Ship` property rather than on `Configuration`, so
building or profiling in Release still gives you a console; shipping is a deliberate act.

`WinExe` alone would make the shipped build permanently mute, including when run from a terminal on
purpose — which is how a package is verified at all. `ConsoleAttach.AttachToParentIfPresent()`, the
first line of `Program.cs`, rejoins the launching terminal when there is one and does nothing when
there is not. So `dist\ProscribedPalimpsest\ProscribedPalimpsest.exe --cli-script …` still works, piped capture and file
redirection both still work, and a double-click is still silent.

The cost to accept: a shipped build that dies before its window opens shows the player nothing.
Reproduce it by running that same exe from a terminal.

**`Ship` also strips the development command-line options.** `ShipArguments.Filter` runs as the
second line of `Program.cs` — after `ConsoleAttach`, before the seed parse — and reduces `args` to
an **allow-list**: `--cpu`, `--gpu`, `--no-llm-probe`, `--silent`, `--help`. Everything else is
dropped, along with its value, so `--seed 42` leaves no stray `42` behind. It reports how many it
ignored, which is visible when the exe was launched from a terminal.

A filter rather than 49 conditionals, for the same reason the packaging payload is an allow-list:
the option surface grows every time a feature turns out to be hard to reach, and a debug flag left
reachable does not announce itself — it just works. Filtering once, before anything reads `args`,
makes every handler unreachable by construction and means a flag added tomorrow is excluded from
shipped builds without anyone remembering to exclude it.

The five that survive all exist to get a player out of trouble rather than to change the game, and
`--help` in a shipped build lists exactly those. Printing the full development list would be worse
than printing nothing: every line is an instruction that silently does nothing, with no way for the
reader to tell which.

**`Ship` also compiles out the developer keyboard.** It defines a `SHIP` constant, which flips
`Config.Debug.DeveloperKeys` to false. That gates the render-debug keys (D, M), the post-process
tuning keys (F, G, H, J), the debug camera (C, V), the window's diagnostic dumps (D, G) — **and
camera zoom (W, S)**. Zoom is in the list although it is not a debug feature: the game sets the
camera distance itself per phase, and a player who zooms out of that framing has no control that
restores it.

What a player keeps: **arrows** rotate, **Space** re-centres on the protagonist, **Escape** opens
the pause menu and closes narration popups. Escape is never gated — without it there is no way out
of a scene. See "Escape, and the phases it must answer" below for what it does per phase.

Two things to keep in mind when touching that keyboard:

- **Gate the branch, not the chain.** In `LocationTravelModeLauncher`'s `KeyDown`, the D and G
  branches test `DeveloperKeys` as part of their own condition. Short-circuiting the whole
  `else if` chain instead would swallow every non-Escape key before it reached the final `else`,
  which is what forwards keys to fight and dialogue modes — taking the keyboard away from gameplay.
- **`--no-developer-keys` makes a development build behave like a shipped one.** Keys cannot be
  driven by `--cli` at all, so this flag plus the `Developer keys: …` line printed at startup is
  the only way to check the shipped keyboard without building and hand-testing a shipped exe.

**The payload is an allow-list.** Only paths named in `$Payload` are copied, and a missing required
one fails the build before anything is archived. The tempting alternative — copy the repo, delete
what looks unnecessary — ships whatever was added since someone last read the delete list, and
breaks silently when a new runtime asset arrives without being un-deleted.

Three things the script knows that are not obvious:

- **`dotnet publish` copies neither `assets/` nor `models/`.** Nothing in the csproj marks them as
  content, so both are staged by the script. Adding a runtime asset means adding it to `$Payload`.

- **`Compress-Archive` is not used.** The cmdlet in Windows PowerShell 5.1 fails above 2 GB, and
  this package is larger than that before the model is counted; `ZipFile.CreateFromDirectory`
  handles it.

Not shipped, and why: `data/` is design source nothing reads at runtime, `assets/old/` is
superseded art, and `src/` is unnecessary because the two shaders under `src/terminal/Shaders/` are
**`EmbeddedResource`s** — they travel inside the assembly, so a shipped build has them without the
folder. `ShaderSource.Load` reads the file from disk when it is there (a shader stays editable
without a rebuild) and from the manifest otherwise.

**That arrangement replaced three hand-maintained copies, and the story is why it is worth the
csproj entry.** The shaders used to fall back to string literals in *both* renderers, so the vertex
shader existed three times: the file, `TerminalRenderer`'s copy, `PopupRenderer`'s. Since `src/` is
not in the payload, a package ran the copies — and `TerminalRenderer`'s had drifted, missing
`uGlyphScale` entirely. **Every shipped build drew its main terminal text at scale 1.0 while
development drew it at 1.2**, and the popup, whose copy was correct, disagreed with the terminal
beside it. Nothing reported it, because a missing uniform is silent by design:
`GL.GetUniformLocation` returns -1 and `GL.Uniform1(-1, …)` is a defined no-op, so the value was set
every frame into nowhere. It surfaced only when the Settings screen made the value changeable and
the SIZE row did nothing in the package. Drift is now not fixed but impossible — there is one copy.

Two things that outlive the bug:

- **`TerminalRenderer.Uniform(name)` complains once when a lookup returns -1**, which turns that
  whole class of silent failure into a log line. Worth extending to any renderer that gains a
  uniform-heavy shader.
- **Reproduce a shipped build's shader path by renaming `src/terminal/Shaders/`.** That is the only
  way to exercise it, because `ShipArguments.Filter` strips `--cli-script` and a packaged build
  therefore cannot be driven by a script at all. Both paths should start the game with no
  `no uniform` line in the log.

A new shader file needs no code change — the csproj item is a wildcard, and `LogicalName` names the
resource for the file (`Shaders/terminal.vert`) rather than for its namespace-mangled path.

The zip lands around 2.2 GB, nearly all of it `model.gguf`, which is already-compressed
quantised weights and does not shrink. That is over itch.io's browser upload limit, so releases
go through `publish.ps1`.

### The manual in a release

`package.ps1` runs `python tools/build_manual.py` **before anything else** and stages the result at
the root of the package, beside the executable. The chapters are the source and the PDF is a build
artefact, so shipping whatever PDF happened to be in the working tree would eventually ship a
manual describing rules the game no longer has — with nothing about the file to say so. A build
failure there stops the package rather than falling back to the stale copy; `-SkipManual` overrides
that for a machine without Chrome, and says what it costs.

**`publish.ps1` deliberately does not upload it.** butler only ever replaces builds in channels it
owns, so it cannot touch the PDF that sits on the page — that was uploaded through the web form and
has no channel. It *could* push the manual as a channel of its own, but itch then serves it as an
archive rather than a one-click PDF, which is a worse page for a document. So the manual is a
hand-upload, and `publish.ps1` closes with a reminder naming the freshly built file and its full
path, because the failure this guards against is shipping a new build against last month's manual.

If that trade ever looks different — automation mattering more than the one-click download — the
push is one `butler push <pdf> user/game:manual` away.

### Publishing

```powershell
./publish.ps1 -Status     # read-only: what is on the channel now
./publish.ps1             # publishes to edmond00/proscribed:windows
```

The target is `edmond00/proscribed:windows`, from the page at edmond00.itch.io/proscribed.

### Naming

The game is **Proscribed Palimpsest**; "Cathedral" is the development name. The split is
deliberate and is drawn at exactly one line — what a player can see:

| player-facing, renamed | development, still Cathedral |
|---|---|
| window title (`Config.Name.WindowTitle`) | repository, csproj, namespaces |
| main menu (`Config.Name.GameTitle`, stylised lowercase) | `bin/Debug/Cathedral.exe` |
| `ProscribedPalimpsest.exe`, staged folder, zip | the dev-only launchers (fight area, image-to-text, music PoC) |
| `%APPDATA%\ProscribedPalimpsest\` (save + settings) | `%APPDATA%\Cathedral\` for a development build |
| the itch page | the repository name and the csproj file name |

**Only the shipped executable is renamed**, by an `AssemblyName` conditioned on the same `Ship`
flag. `run_tests.sh` guards the suite with `Get-Process -Name Cathedral`, and that guard is what
stops a leftover run from racing a new one; renaming the development binary would break it
*silently*, because the guard would simply stop finding anything and read as "all clear".

**The data folder is named per build** (`AppData.FolderName`, conditioned on the same SHIP
constant): `%APPDATA%\ProscribedPalimpsest` when shipped, `%APPDATA%\Cathedral` in development.
They shared one folder until it became clear that testing the packaged game was never testing a
clean install — a save left by a `dotnet run` session lit up Continue in the shipped build, and a
compute device probed by one was inherited by the other.

There is deliberately **no migration** between them. Copying development data into the shipped
folder is the coupling this removes; a shipped build starting empty is the point. The cost is that
the first launch after this change re-probes the compute device, because the new folder has no
probe result in it.

The shipped name has no space in it. A player sees it in a folder listing either way, and every
CLI invocation — the whole test driver, any future CI — would otherwise need quoting.

**It pushes the staged folder, not the zip**, and that is the whole reason it is worth having a
script. butler diffs a build against the previous one file by file; pushed as a folder, the 2 GB
model is uploaded once and skipped on every later release, so a code-only update sends a few
hundred MB. A zip is one opaque blob — change one byte and the entire archive is new. Players
still get a downloadable archive; itch builds one from the pushed files. The zip `package.ps1`
makes is for manual distribution and backups, not for this.

**No credentials live in the repo or on a command line.** `butler login` is a one-off interactive
browser flow that stores a token under `~/.config/itch/butler_creds`; `BUTLER_API_KEY` in the
environment is the unattended alternative. The script only checks that one of the two exists and
lets butler do the rest.

Pre-flight refuses to upload rather than publish something broken: the same required paths
`package.ps1` verifies, plus a **PE subsystem check** that fails a console build (which would mean
`-p:Ship=true` was lost and players would get a diagnostic window behind the game), plus deleting
any `logs/` left in the staged folder — those contain the full text of everything the model was
asked and answered.

### Verifying a shipped build

**Gameplay is verified on the development build; the shipped artifact is verified by starting it.**
That division is deliberate, and it is why the locked-down option set costs nothing.

`-p:Ship=true` changes exactly three things — a PE subsystem byte, two compile constants, and the
assembly name. No line of game logic differs, so the CLI suite testing narration, verbs and
outcomes against the development build is testing the same code with better tools. There is
deliberately **no flag that re-enables the development options in a shipped build**: the scripts
are built on `--seed`, `--skip-childhood`, `--observe-only`, `--location-type` and a dozen more, so
anything that made them usable would put most of the surface back and would drift every time a
script needed something new.

What only the artifact can prove — that the self-contained runtime resolves, that `assets/` and
`models/` are found from the executable's own directory, that the GGUF loads and a backend is
chosen, that a window opens with its fonts — needs **no options at all**. `publish.ps1` launches
the staged build with an empty command line, waits for a window title and for `LLM Server is ready`
on stdout, then kills it and its `llama-server` child. `ConsoleAttach` is what makes that output
readable. A pass covers every packaging failure mode there is; skip it with `-SkipSmokeTest` only
when re-pushing something already started once.

The confirmation prompt cancels rather than throwing when nothing can answer it, so an unattended
run never publishes by accident — `-Yes` is how such a run says it meant to.

