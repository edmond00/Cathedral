# Brief: the Bellic and Journal tabs of the management menu

For a cloud session. Two new tabs in the management menu (`src/game/management/`):

- **Bellic**: a party member's fighting profile, everything a fight uses that no other screen shows.
- **Journal**: the run's story, written up as it goes, partly by a background LLM session.

Work on a branch `feature/bellic-journal` cut from `develop`. Commit as you go, push the branch,
**never merge into `develop` or `main`, never release**. Do Bellic first and commit it complete
before starting the Journal: it is independent and gives the user something to review early.

## Before anything

1. Read `CLAUDE.md` (its six prohibitions hold for every line you write) and `src/game/CLAUDE.md`
   (the save contract, fights, the senses). Invoke the `verifying` skill before writing any `.cli`
   script, `runtime` before touching `src/LLM/`, `audits` after touching content.
2. Set up the machine:
   ```bash
   bash tools/linux/cloud-setup.sh
   bash tools/linux/fetch-assets.sh --glove --model   # the Journal work needs the real model
   dotnet build Cathedral.csproj -r linux-x64
   xvfb-run -a ./run_tests.sh audits
   ```
   See `tools/linux/README.md`. If the downloads are blocked, say so at once: the Bellic part can
   go ahead without them (`--playground`, `CLI_EXTRA_FLAGS=--no-embeddings`), the Journal part
   cannot be judged without the model.

## Part 1: Bellic

**Info only.** Nothing on this tab changes state. The criteria are that it is **pretty** and that a
player can **find a given fact quickly**.

- Add it to `ManagementTab` and `AllTabs` in `ManagementMenuRenderer.cs`, with
  `AllCharacters: true`. Its key is `B`, which no other tab uses (the type initializer checks this).
- **What goes on it, derived from the code, not from memory or `design/`**: read `src/fight/`
  (`Fighter`, `FightResolver`, `FightingSkill` and `skills/`, the medium registries for organs,
  body parts and weapons, `FightBuffEffects`, `FightStatusEffect`, `FightAI` / `AiPersonality`) and
  list every value a fight reads from a party member. Remove whatever another tab already shows
  (Anatomy, Inventory, Humors). The rest is the content: what the fight sees of this member and
  nowhere else. Write that list into your first commit message so the user can check its scope.
- **Layout**: follow the house style of the existing tabs (`HumorMenuRenderer`,
  `InventoryMenuRenderer`, the separators and headers in `ManagementMenuRenderer`): same colours,
  same spaced-letter title (`B E L L I C`), same panels. Group the values into a few named sections
  a player can scan. Give every number its source in a word ("from: left arm, sword"), and leave out
  any raw internal name a player would not understand. If something has more detail than fits,
  select-to-expand beats a crowded screen. It must work for a human, a beast companion and an
  unarmed or wounded member: check each one.
- **The grid is fixed at 100x100** (see `CLAUDE.md`). Fit the content to it, never the reverse.
- **Verify** with a `.cli` script under `cli/system/` that opens the tab for the protagonist and for
  a companion and asserts on the text. Use `snap` to take PNGs of each case, look at them yourself,
  iterate on layout until it reads well, and keep the final PNGs for the report.
- The manual describes rules, not screens: this tab needs no manual change unless you change a rule.

## Part 2: Journal

Protagonist only, as it is now. `Protagonist.JournalEntries` (`List<string>`) is saved and restored
by `PartyState` but **nothing ever writes to it**, so its type can change freely. Replace it with
structured entries (kind, day, place, acting member, source text, written text, state), following
the save contract in `src/game/CLAUDE.md` and keeping `SaveAudit` in step.

### 2a. Capture and display (no LLM)

- **Capture**: the narration the player actually saw, the dialogue lines actually spoken (not the
  options left unpicked), outcome chips as facts, and **milestones** generated from game events
  (a modus mentis learned, a wound taken or healed, a companion hired or lost, a first visit to a
  place, a fight won or fled). Find where each one is finalised and hook it there. Gate on types,
  never on strings. Narration can follow a companion: record the acting member.
- **Display**: one long scrolling list, opening at the newest entry. A header line whenever the day
  or the place changes. Mark the passages where a companion acted. The mouse wheel and Page Up/Down
  scroll. Milestones show as short distinct lines.
- **Save size**: estimate what a long run produces and cap it or move it out of the save if it is
  large. Say which, and why.

### 2b. The chronicler: a background LLM session

A separate LLM session (`LlamaServerManager.CreateInstanceAsync`) with its own system prompt, which
works through a queue of event groups and turns each group into a written journal paragraph. Until a
group is written, the journal shows its raw text and milestones, so the journal is never empty.

**Requirement 1, speed: it must never slow the gameplay narration down.** This outranks quality.

- Build a priority gate **inside `LlamaServerManager`**, not a list of game states that would go
  stale: requests are foreground or background, a foreground request cancels any running background
  one (`CurrentRequestCancellation` already exists), and background work starts only after
  foreground has been quiet for a delay. A cancelled job goes back on the queue whole; never keep a
  half-written paragraph. The queue is saved, so a quit loses nothing.
- **The known cost**: the server runs with one context (`-c 4096`, no `--parallel`, see
  `BuildServerArguments`), so a chronicler request evicts the narrator's cached prompt, and the next
  narration request reprocesses its whole system prompt. Measure this, then decide. **You may change
  the llama-server settings** (e.g. `--parallel 2` with a larger `-c` so the chronicler gets its own
  slot, `--slot-save-path` to save and restore the narrator's KV cache, `--cache-reuse`, thread
  counts, `n_predict` caps), within the `runtime` skill's rules: still no `-ngl` unless a player set
  one, everything through `BuildServerArguments`, invariant formatting, llama.cpp version unchanged.
  Report the memory cost of whatever you choose.
- **Measure on CPU, with the thread count of a player's machine, not the cloud's.** Limit llama to
  4 to 8 threads (`-t`) rather than every core the cloud machine has. The user's own machine has 20
  logical cores and runs on CPU because its GPU is unstable. Record for a fixed scripted run (fixed
  `--seed`, the same commands) the time to first token and the total time of every foreground
  request, **with and without the chronicler**, plus a counter of foreground requests a background
  job delayed and by how many ms. The pass bar: no foreground request measurably slower. Put the
  counter in the crash report too (`CrashReport.AddProvider`).
- `--playground` answers LLM calls without making them, so the queue logic and the saved state can
  be tested by script, but preemption and timing need real runs. Say which is which.

**Requirement 2, quality: judge it, don't guess it.**

Build a small offline harness under `tools/journal/`, in the spirit of `tools/judge/` (read the
`choice-judge` skill for how this project writes and measures prompts):

1. **Collect real material**: play a few scripted runs with the real model and dump the event groups
   the chronicler would have received (narration, chips, milestones, dialogue), at least ~30 groups
   from varied situations: a quiet visit, a dialogue, a fight, a wound, a recruitment.
2. **Vary one axis at a time** and generate journal text for every group under each variant:
   - **instructions**: a neutral chronicle; the protagonist's first-person diary; that diary tinted by
     the dominant humor or emotion; a terse campaign-log style;
   - **inputs**: milestones only; chips and milestones; raw narration only; everything;
   - **grouping**: one event; one visit; one day;
   - **short memory**: none; the previous paragraph; a rolling one-line summary of the run so far;
   - **length** caps and sampling settings.
3. **Judge every output** against a written rubric, scored by you reading them, and add automated
   checks where possible:
   - **fidelity**: no invented person, place or event. Check names automatically against the input;
   - **coverage**: the events that matter are there;
   - **readability**, **consistency of voice** across consecutive paragraphs, **no repetition**
     between paragraphs;
   - **cost**: tokens in and out, CPU seconds per job.
4. **Report a table** (variant against score and cost), pick a configuration, and give its outputs
   side by side with the raw input for five or six groups.

Keep the prompt text in the game's own conventions (wherever the other session prompts live), not
in the harness.

## Testing and finishing

- Run `./run_tests.sh audits` plus the `cli/` folders you touched and your new scripts. **Not the
  full suite**: that is the user's release gate. Say exactly which you ran.
- End git commits with the attribution line this environment gives you.
- **Final report** (put it in the session and in `design/BELLIC_JOURNAL_REPORT.md`): the Bellic
  contents list and PNGs; the Journal design and save-size estimate; the latency numbers with and
  without the chronicler, the thread count and core count they were measured on, and any
  llama-server change with its memory cost; the quality table, the chosen configuration and its
  samples; what still needs checking by hand on the user's Windows machine, because the timing will
  differ there.
