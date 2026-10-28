# Prompt overhaul — notes

The overhaul of the modi mentis' global LLM instructions (see `BRIEF.md`): what was measured, what
changed and why, what was tried and lost, and what lies beyond the global instructions. Every number
is from the shipped model (Qwen2.5-3B-Instruct Q4_K_M) on 4 CPU cores, through `serve.sh`, with the
game's own sampling settings. The before/after report is `report/index.html` (also published as an
Artifact); the final numbers are in "The result" below.

## In one paragraph

The shipped model invents in about a third of its rewrites, and the global wording has less purchase on
that than the persona fields do. Adding rules made it worse every time it was tried; what helped was
**fixing what the model is handed** — a person named rather than described as a run of clauses, a lesson
said in its own sentence, two grammar faults — and **removing a clause it was copying**. Five changes
were kept, each measured on its own with the old wording scored blind in the same sheet; fifteen lost and
are listed with the reason. The item critic, which had no benchmark, got one and two fixes. The choice
judge was left alone: its errors are in persona descriptions, not in its prompts. On a stratified
final set the net fidelity is flat (0.72 = 0.72) and lower on the held-out playthroughs (0.80 → 0.70,
22 cases, within the noise but shown); requests are 18 tokens and 6% wall time cheaper.

## How it was measured

- **Corpus.** Eleven `--cli` playthroughs against the real model (`make_scripts.py`,
  `run_corpus.sh`): village, farm, forest, cave and plain; dawn to night; conversations with a
  blacksmith, a miller and a farm's people; a Speak-About hand-off to a companion. The game records
  every rewrite's inputs (`PersonaRewriter.RecordRewrite`) and every judge decision
  (`ChoiceJudge.Record`) into the session log directory — development logging only, a shipped build
  has no session directory. 143 recorded rewrites; 49 personas in play.
- **Cases** (`build_cases.py`): every recorded rewrite asked in its own persona with its real slot
  history, in two other modi mentis chosen to span the catalogue (terse and florid, bodily and
  thinking, moral and amoral: poetry, arithmetic logic, brute force, cutpurse, cruelty, courtesy,
  plain dealing, rhetoric, avarice, mercy, dread, keen nose, woodcraft, stoneface, sloth, pride,
  architecture, ruination), and on every third rewrite in a deliberately loosened persona
  (`personas.LOOSENED`: a bare name, one vague line, a name and a reminder, a vague style). 377
  cases, 61 personas. NPC lines are only asked in their own persona.
- **Held out**: three whole playthroughs (cave morning, village evening, plain afternoon with a
  companion), never looked at until the final measurement.
- **The mirror** (`prompt_lib.py`): every prompt is rebuilt from the recorded inputs, every string read
  out of the C#. All 143 recorded prompts and grammars come out byte-identical (`check_mirror`). The
  baseline is pinned to the commit the branch started from (`BASE_REV`), so a port into the C# cannot
  turn "base" into the new wording.
- **Scoring**: blind, by me, against `RUBRIC.md` — version hidden, every version's samples of a case
  shuffled together (`blind.py`), every invented or lost fact written beside its score
  (`sheets/*.scores.txt`).
- **Mechanical checks** (`auto_metrics.py`): copies out of the prompt (style, reminder, instruction,
  setting rule), lessons lost, questions flipped. Cheap enough for every sample of every version, which
  is what made 80–124-sample confirmations affordable.

### The noise, and why decisions rest on same-sheet comparisons

One sample proves nothing and fifty prove little. Two samples of one prompt at one seed differ
(llama-server's cache state moves the numerics); the same 8 dialogue cases with *identical prompts*
scored 1.38 and 0.88 in two rounds. My scoring also grew stricter as I learned the failure patterns:
base re-sampled in round 3b scored 0.74 against 0.9 in round 1 on the same cases. So from round 3b on,
**every candidate was scored in one sheet with fresh base samples**, and kept only on that comparison or
on mechanical counts over 60–124 samples a side. The cross-round tables below are for reading, not for
comparing rounds with each other.

## What the shipped model does wrong (baseline)

About a third of baseline samples break the meaning (fidelity 0). By cause, most to least common:

1. **It copies the request into the answer.** The persona's style ("strings in hand, in the two
   seconds it takes" — Cutpurse following a track on a farm at night), the reminder ("a world where
   gentle songs soothe a beating heart"), the instruction clauses themselves ("it only colours how the
   given facts are told", word for word, from Pride and Dread), the setting rule ("this medieval
   village where iron and myth held sway"). Whatever is in the request is material.
2. **It acts the persona out.** Woodcraft examining a snail "set my plane to the grain"; Brute Force
   asked to examine the snail "will crush the snail beneath a boulder" (8 of 8); Courtesy greets a
   farmhouse; Pride "turned down the offer" in an emotion about learning to make acquaintances.
3. **Invented memories.** Childhood Memory and Keen Nose, whose personas ask for one, invent a memory
   every time ("the year the barn roof caved in", "my preceptor's lessons", "the storm I sought shelter
   from").
4. **Lost consequences.** 45% of outcomes and emotions drop or replace the lesson ("learned water
   voice" → "learned earth's song").
5. **Spoken lines change meaning**: a statement asked back as a question ("My name is Dennis" →
   "Dennis, dost thou hear?"), a greeting made a farewell, the addressee given a trade ("fellow
   ironworker"); loosened personas drift into fake-archaic gibberish ("Dennis jest me name").
6. **A person's traits become the narrator's**, in reasoning — see "Kept", 3.

## Kept

| | change | where | evidence (same sheet, blind unless said) |
|---|---|---|---|
| 1 | Item critic: substitution asked as "could X be used in place of Y for this, even if less well?", and the system prompt's "Be strict but fair; value plausibility over generosity" removed | `CriticTrees.cs`, `ItemUseCritic.cs` | 28 labelled cases × 5 samples: 83.6% → 90.7% right side of help/no help; substitution tree 69% → 83%; nothing that should be refused started passing |
| 2 | "discreetly" for "discretely"; "It is done! I managed to …" for "I succeeded to …" | `NeutralNarration`, `ThinkingExecutor` | grammar; "managed" measured alone over 62 samples a side moved nothing else |
| 3 | A person or a beast in a reasoning is named by the observation's label ("the blacksmith of the village (a woman)") instead of their appearance clause | `ThinkingExecutor` | every recorded reasoning about a person, 52 a side: fidelity 0.60 → 0.92, meaning broken 26 → 15, gender wrong 5 → 0, persona 1.19 → 1.50, −49 tokens |
| 4 | `StyleIsNotSceneClause` removed | `Config.Narrative` | 40 styled cases, 80 a side mechanically: style copies 11% → 9%, setting words 6% → 1%; blind 0.53 → 0.60 (40 a side) and 0.74 = 0.74 (38 a side); −25 tokens a request; the clause itself was being copied word for word |
| 5 | A lesson learned said in a sentence of its own, after the other consequences ("… made my way to the coop. And I learned trailcraft."); lessons told apart by type (`Outcome.IsLesson`) | `NeutralNarration`, `Outcome`, `OutcomeNarrator`, `ActionExecutionController`, `EmotionResolver` | 31 lesson cases, 124 a side: lesson lost 45% → 34%; blind 0.64 → 0.73 (22 a side) |

## Tried and lost

| change | result | why it lost |
|---|---|---|
| The reminder said as a voice ("Speak as X — that is who is speaking, not something in the scene") | 0.98 vs 1.02 | no gain; the reminder kept leaking |
| Reminder and style together, one not-in-the-scene clause after both | 1.04 vs 1.02, persona 0.86 vs 1.04 | persona lost |
| No reminder at all | fidelity 0.88, persona 0.90 | both lower |
| The sentence moved to the end of the prompt (facts last) | 0.92 | reasoning and emotion worse |
| A concrete closed-world clause ("add no object, person, place, sound, smell, weather, memory or event of your own") | 0.94, persona 0.78 | the list read as a prompt; persona fell |
| Both of the above | 0.70 | the worst version measured |
| The hour in the per-request reminder ("You are in a village, at dusk") | 0.72 | no gain; more invented scenery, not less |
| The per-request setting sentence shortened (no "medieval world") | 0.82 | no gain (mechanically, setting words were already rare: 3–6%) |
| A minimal prompt (half the tokens: 121 vs 234) | 0.47 vs 0.74 | fidelity and writing clearly lower — the instructions are not all dead weight |
| A compact spoken prompt (the line once, at the end; −67 tokens) | 0.70 vs 0.91 (46 a side) | the line stated twice is load-bearing, as `BuildSpokenPrompt`'s comment says; persona rose (1.15 vs 0.85) but fidelity comes first |
| No style in the request (the persona prompt alone carries it) | fidelity 0.89 vs 0.74, persona 0.50 vs 1.03 | a trade of criterion 2 for criterion 1, which the brief rules out |
| The lesson named as a craft ("learned the craft of water voice") | lesson lost 40% | no better than its own sentence, and clumsy |
| `StyleIsNotSceneClause` kept for Reasoning alone (`final_reasoning_clause`), after Reasoning fell without it in round 6 (0.92 → 0.75) and in the final measurement (0.88 → 0.71) | 0.70 vs 0.67, F = 0 24 = 24, persona 1.62 vs 1.72 (30 fresh dev cases, 60 a side, round 10) | the two falls were noise: on fresh cases the clause buys nothing, costs 26 tokens and some persona; its own words were still copied ("it colors how I perceive") |
| Critic: "Judge as a practised worker would…" in place of "be strict" | 85.0% | no better than removing the line |
| Critic: "How well could X do the work of Y here?" | 88.6% | beaten by "in place of … even if less well" |

## The judges

**ChoiceJudge** (`tools/judge/judge_bench.py`, 319 decisions): **95.9%**, unchanged — no judge prompt
was edited. Its 13 errors are the ones the `choice-judge` skill already documents (Cruelty's means at
the gate, Temerity taking Fording to cross a river, Gluttony and the bellows, Piety's reply) and are
description problems in persona fields, which are out of scope. Nothing tried on the narration side
bears on it.

**Item-use critic** (`critic_bench.py`, new): it had no benchmark. 28 labelled cases for iteration and
20 held out, each asked 5 times at the game's sampling (the critic samples a grammar-constrained id, so
one case is a distribution). Final numbers below.

## The result

**The final measurement shows no net fidelity gain.** The kept changes each won where they were aimed;
on a stratified set of cases they are a small part of what the model is asked, and the noise is larger
than they are.

The set (`sets/final_dev.txt`, `sets/final_heldout.txt`): 47 dev cases and 22 held-out cases, every
kind, own, swapped and loosened personas. Each case was asked at seeds 51 and 52 under the old wording
(`BASE_REV`) and the new one, 138 samples a side, all scored blind on one sheet (`sheets/final.*`).

| | old | new |
|---|---|---|
| fidelity, all | 0.72 (57 at 0) | 0.72 (60 at 0) |
| fidelity, dev | 0.69 | 0.72 |
| fidelity, **held out** | **0.80** (15 at 0) | **0.70** (20 at 0) |
| persona | 1.13 | 1.14 |
| writing | 0.99 | 0.91 |
| user-message tokens | 239 | 221 |
| copies a style, mechanically | 7% | 9% |
| loses a lesson, mechanically (32 a side) | 50% | 50% |

By kind, fidelity rose for Observation (0.62 → 0.75), Action (0.75 → 0.83) and dialogue lines (0.79 →
0.88), and fell for Outcome (0.58 → 0.46), Reasoning (0.88 → 0.71) and Emotion (0.58 → 0.50). With 12
to 24 samples a kind and the same prompt scoring 0 to 2 across seeds, none of these is outside the
noise. Reasoning was checked anyway, since it had also fallen in round 6 when the style clause went:
the clause put back for Reasoning alone, on 30 fresh dev cases, scored 0.70 against 0.67 with as many
zeros (round 10, in the lost table), so the fall is not the clause. The cases whose prompt the new wording actually changes (15 of 69: a person in a reasoning, a
lesson, "managed", "discreetly") moved by −0.07 on average; the 54 whose only difference is the
missing style clause moved by +0.01. The held-out fall is reported as measured: it is 22 cases, its
cases are mostly unchanged prompts, and the targeted measurements above (52, 124 and 80 samples a side)
are the better evidence for each change. It is still a fall, and the honest reading is "no worse
within what can be measured, cheaper, and right on the specific faults it set out to fix".

**Speed** (`timing.py`, `samples/timing.jsonl`): the same 69 cases, one seed, the server's cache warmed
on the persona and the history before every request (what the game has cached when it asks), the two
versions alternating in order, nothing else running. User message 239 → 221 tokens, tokens read 244 →
226, prompt reading 4.79 s → 4.47 s, tokens written 35.1 → 33.4, wall time 9.18 s → 8.62 s a request
(−6%). The samples of the final measurement are not used for speed: the new version ran after the
old one and found the cache warm, which made it look 15% faster than it is.

**Item critic**, final run (5 samples a case): dev 83.6% → 92.1% (substitution 69% → 85%), **held out
78.0% → 85.0%** (substitution 69% → 80%, appropriateness 89% → 91%).

**Choice judge**: 95.9%, unchanged; no judge prompt was edited.

**Checks**: `dotnet build`; `./run_tests.sh audits` (12 passed); the cli scripts that reach the
changed text (`reconsider`, `will_step_retry`, `preview_regression`, the `propose_to_buy`,
`pickpocket`, `slay`, `listen`, `beg_for_coin`, `examine`, `gather` folders, `dialogue_success`,
`emotion` success and dialogue, `speaking_puts_conversation_first`, `confronted_witness`,
`thinking_iterates`, `thinking_doubt`, `will_equal_paths`) all passed. The full suite was not run.
PLAYTHROUGH

## Beyond the global instructions

- **Persona fields cause most of what is left.** Styles that name props or acts are copied or acted out
  however the wrapper frames them: Cutpurse ("the press of bodies, the found strings, the two seconds"),
  Physiognomy ("hands, teeth, colour, the set of the shoulders" — it catalogues bodies in an empty lane),
  Woodcraft ("grain, joint and shaving" — it carves the snail), Pride ("the offer declined, the tone
  noted"), Stoneface ("masks, stone"), Dread ("the cold along the arms … the eyes going to the exit" —
  it leaves rooms it is not in). Brute Force's whole persona turns any act into crushing. Personas that
  ask for a memory (Childhood Memory, Keen Nose: "let each smell pull a memory with it") invent one
  every time. Reminders written as a picture leak the picture (Lullaby's "a sung breath calms a beating
  heart", Stealth's "floorboards betray a step", Bushcraft's "kindling, weather and shelter"). The
  same rule the codebase already states for styles — a way of seeing, never a list of props — would
  help most if extended to reminders and to the persona prompts' own examples. Arithmetic Logic's
  persona prompt is copied ("the column is right", "one short"), as is Plain Dealing's ("there is a
  crack in it, there — look, that is the price").
- **NPC names in the neutral lines**: path and link names built from area names ("a chicken
  coop–storage shed track", "a mud wallow (storage shed–rabbit warren)") are read as places: the model
  writes "a storage shed where rabbits burrow". These come from scene content (`OutdoorLayout`), not
  from the global wording.
- **Two neutral-line faults left as they were**, both rare: an area description written as a clause is
  wrapped as a noun phrase ("This place is the cave mouth opens into…", `AreaIntroduction`); and a group
  of ways becomes "a ways lead on from here" (`NounPhrase`'s plural test reads "lead"). Fixing either
  needs a way to tell a clause from a noun phrase that the descriptions do not carry.
- **The model.** Fidelity at 3B is the ceiling. The same prompt at the same seed scored from 0 to 2;
  most of the remaining failures are confident invention that no clause measured here moved. A larger
  model, or a sanitizer pass that checks named nouns against the neutral line, would do more than
  wording.
- **Sampling.** Out of scope, but worth knowing: the critic samples its verdict at temperature 0.7.
  The choice judge reads the scores of every letter instead and is far steadier; the critic could do the
  same.

## To check by hand on Windows

1. Play an outcome that teaches something new and read the narration: the lesson now comes in its own
   sentence of the neutral line ("And I learned …") — check the persona's rewrite still names it.
2. Think about a person after observing them: the reasoning should name them as the observation did
   ("the blacksmith of the village", or their name once introduced) and keep their gender.
3. Combine a substitute tool with a tool-gated verb (a sword to `cut` a carcass, a hatchet to
   `cut_wood`): it should now pass where it was always refused, while a feather or a loaf still fails.
4. A stealthy means: the action button reads "discreetly".
5. Nothing visual changed; no rule a player is subject to changed, so the manual is untouched.
