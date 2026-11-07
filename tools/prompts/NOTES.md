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
**The last playthrough** (`finalplay/`, the new build against the real model, the corpus scripts
`village_noon_party` and `talk_blacksmith` at their corpus seeds): both ran to the end with no failed
expectation, and all 24 recorded prompts are byte-identical to what the harness builds from the
working tree (`check_mirror(records, T_NOW)`), so everything measured above is what the game sends.
Read end to end, the changes are visible where they apply — "It is done! I managed to strike up a
conversation with her … And I learned social interaction." came back with the lesson kept; a
reasoning about "the blacksmith of the village (a woman)" kept her a woman — and what is wrong is what
"Beyond the global instructions" describes: Childhood Memory invents a memory in every observation
("just as I did when I tried to light a fire in the rain"), Bushcraft's reminder turns "strike up a
conversation" into "strike up a fire … by the warm hearth", Pride's persona inverts a glad emotion
("heavier than before") and invents an offer turned down, a long-sentence persona lengthens a
reasoning with runes and a troubadour, and an NPC's curt "You stopped to bother me on your way" comes
back as a compliment. Requests took 12–60 s each on this machine (the first of a persona pays for its
system prompt).

## Persona fields (the second pass)

Asked afterwards: could the per-modus-mentis wording be improved too, and the rule that does it be
kept for modi mentis added later? Yes, for two of the fields — `StyleInstruction` and
`PersonaReminder2` — which are restated in every narration request. The persona prompt itself was left
as it was (see the last point of "Beyond" below).

**Screening the catalogue** (`screen.py`, `screen_metrics.py`). Every one of the 460 personas was asked
the probe lines of the kinds it narrates in play (two recorded lines a kind, dev split; 2,362 answers,
one seed), and each answer measured mechanically: the words of the persona's own fields that reach an
answer whose request did not give them (*leak* — words specific to that persona only, a word four
personas in ten add to a probe being the model's habit), verbatim copies, and echo of the neutral line.
The worst were the crafts: Chandlery, Arms Care, Anointing, Bell Ringing replaced the act itself
("I will breathe in the smell of the hay stack as I wipe the blade, oil it, look along the edge…").

**What predicts it** (`persona_features.py`, Spearman over 460): the number of concrete things a style
names (0.44 with style leak; Brysbaert concreteness norms), the concreteness of the persona prompt
(0.38), of the reminder (0.23). Quoted examples in the prompt, and memory requests, did not show on
this measure (a memory invented is not a field word leaked).

**The rules, each blind-tested** (old and new on one sheet, the field being compared hidden):

| round | change | personas | fidelity | answers breaking meaning | persona |
|---|---|---|---|---|---|
| 11 | style as a manner ("patient and exact"), no things | 20 worst, dev | 0.25 → 0.88 | 33 → 15 of 40 | 1.85 → 1.07 |
| 12 | style as a lens ("judge whatever is in front of you by…") vs manner | same | 0.88 → 1.18 | 15 → 9 | 1.07 → 1.25 |
| 13 | lens, held out (12 worst of L–Z, never looked at) | held out | 0.08 → 1.21 | 22 → 6 of 24 | 2.00 → 1.25 |
| 13 | reminder as a disposition, on top of the lens | 16, dev | 1.06 → 1.50 | 7 → 2 of 32 | 1.31 → 0.91 |
| 14 | a manner joined to the lens ("Slow, patient and exact - judge things by…") | same 16 | 1.50 → 1.41 | 2 → 2 | 0.94 → **1.16** |
| 15 | the whole rewrite, 30 personas drawn at random from the 313 | random | 0.63 → **1.30** | 15 → 7 of 30 | 1.47 → 0.93 |

Persona falls with every round, and that is the trade to know about: much of the old persona *was* the
invention (Arms Care oiling a blade in a greeting scores high on persona and zero on fidelity). The
loss concentrates in Action lines, which are one sentence of intent; round 14's manner words were
added to give those something to carry, and won back a third of it (Action persona 0.54 → 0.85).
Fidelity ranks first, so the rewrite was applied; the user chose to iterate once more before applying
it, and round 14 is that iteration.

**The rewrite** (`rules/catalogue_*.json`, 313 modi mentis; ported by `port_personas.py`): every style
the lint flagged was rewritten as manner + lens, every reminder as a disposition, keeping each persona's
attitude. Re-screened on the same probes: leaked field words 2.20 → 0.70 an answer, answers with any
leak 72% → 42%, verbatim copies 13% → 4%; 246 personas better, 41 worse — mostly by manner words now
appearing (the colour asked for), a few by props still in their persona prompt (Abstinence's plate,
Relic Lore's relic).

**Keeping it** — two checks, the first free:

- `PersonaLint` in `--mm-audit` (✗ fails `./run_tests.sh audits`): a style or reminder naming two or
  more distinct concrete nouns, or a style opening a props list ("images of", "narrate the"). Concrete
  and noun are predicted from the GloVe vectors the game already loads, by two ridge fits to the
  Brysbaert norms (`data/concreteness_glove100.json`: concreteness r = 0.79 held out, noun accuracy
  0.83). Over the old catalogue its count ranks personas by style leak at 0.46; the rewritten catalogue
  passes with none flagged. `persona_lint.py` is the Python twin; both flag the same personas.
- `persona_probe.py <id>`: the persona asked the probe lines on the shipped model, every answer printed
  with the words it carried in, and LEAKS above 1.75 leaked words an answer (the rewritten catalogue's
  90th percentile). For a new modus mentis, and for reading what the lint cannot see.

The rules are written where a writer meets them: the doc comments of the two fields in
`ModusMentis.cs`, and the `choice-judge` skill ("Writing the voice"), which is the skill invoked when a
modus mentis is added. The ChoiceJudge does not read these fields, so its benchmark is unaffected.

**Found on the way**: three classes (Brine Sense, Herblore, Iron Stomach) already carried a comment
recording this lesson by hand ("Not 'weed, wet rope, a running tide': listed, a tide turned up in the
desert"). The harness's field reader did not skip such comments and asked them with no style; fixed.

## Beyond the global instructions

- **Persona fields cause most of what is left** (written before the second pass above, which addressed
  the styles and reminders; the persona prompts are still as described). Styles that name props or acts are copied or acted out
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
  same. *(Since done — read off the ids, not letters; see "The item critic and improvised tools".)*

## The item critic and improvised tools

A later pass, on one complaint from a playtest (2026-11-01): a character with Medium tool proficiency
tried to scale the grove house wall with an iron dagger, then a short sword, and was refused both —
`cannot_help`, which no band clears — with the reason "I think the dagger's blunt nature prevents
effective climbing". The design says an improvised tool is allowed to a practised hand and punished by
the dice; in practice the critic refused it before any hand was consulted. Same model, same server
(`serve.sh`, llama.cpp b11515 CPU build, `models/model.gguf` checked against its SHA-256), 4 cores.

### The survey

`critic_survey.py`: 119 cases, every one a real pairing — the 21 verbs that reach the critic (13
Optional, 8 Required) against the 75 Tools and 23 Weapons, the only items `GetCombinableItems` offers
(containers, food and the rest never reach it; a pot as a scoop or a loaf at a seam are unreachable in
play). Item names and descriptions are copied from the item classes, goals are the verbs' own
verbatims, targets carry their factory's description. Each case is labelled help / no help and, where
clear, the band its verdict should demand. 65 are **improvised** (should help, not the obvious tool: a
blade into a wall, a staff over quicksand, a tower shield as a sled on a snow bridge, a torch at a bees'
nest, a razor at a purse string, a pitchfork to spear fish), 17 **obvious**, 34 **refuse** (a real tool
or weapon that cannot do it: bone dice up a wall, a candle across thin ice, a warhammer at a lock) and 3
**anchors** (the brief's loaf, feather and lute, unreachable, reported apart). The playtest prompt is
case `survey000`, and the bench rebuilds it byte-for-byte from the C#.

The shipped critic on it (5 samples a case, as the game samples; final measurement, see below):

| | |
|---|---|
| improvised uses passed | **57.8%** — the failure, exactly where the brief put it |
| obvious tools passed / refusals refused | 95.3% / 91.2% |
| the band asked for, exactly / within one | 15.7% / 46.3% |
| cases whose side changes between samples | 43 of 167 |
| dagger and short sword on the wall | `cannot_help` in 5 of 5, both |

**A harness fault, found late and fixed.** `critic_bench.py` closed every system prompt with the setting
rule of `prompt_lib.T0` — the wording from *before* the earlier prompt overhaul — while the game sends
the newer rule. The live check below caught it (the game's logged system prompt differed). Every critic
number in this file before this section, and the exploration in "What was tried" below, carries the
old rule; the before/after tables are both measured under the rule the game sends. The old rule made
the shipped critic look stricter than it is (35.9% false refusals, 1.7% false passes under it, against
30.5% and 5.0% under the real one); it did not change which wording won.

It is not that weapons are read as weapons (hypothesis 2): weapons and tools were refused at the same
rate, 42% and 40% of their sensible uses. It is not the sampling (hypothesis 4): read
deterministically the same question is no better (35.7% false refusals against 34.6% sampled, tune
half) — sampling costs stability, not accuracy. Describing the target (hypothesis 3) does not help
that question either: told the wall is coursed stone with gaps for fingers, it refused *more* (44.6%).
**The question is the cause** (hypothesis 1): asked whether X beats bare hands, the model answers for
the obvious tool and nothing else. The substitution tree has the same flaw in its degrees: a spade to
dig ("normally done with a shovel") came back `serves_poorly` five times out of five.

### What was tried

The survey was split in two (`--set tune` = the old dev set + even-numbered survey cases, `--set check`
= the held-out set + odd-numbered ones); every wording below was iterated on `tune` only, and under the
old setting rule (above).

| variant | result | |
|---|---|---|
| any question read as **letters** (A–E), as ChoiceJudge does | 61–68% right | the model answers "D" in 112–126 of 167 cases, a hammer to break a door and a knife at a sleeper among them; the ids carry the meaning, the letters do not |
| the shipped question read off the **ids' first tokens** | tune: 35.7% false refusals, 0 false passes | deterministic, same accuracy as sampled |
| the same, target described | tune: 44.6% false refusals | stricter, not better |
| **"could X be used to help, even clumsily or in a way it was not made for?"** (all sets) | 7.5% false refusals, **36.7% false passes** | the improvisations pass; so do bone dice up a wall and a tally stick through brambles (log-odds up to +15). "Unusual use" lets anything through |
| each yes tied to a physical means ("its edge, point, weight, length or grip would do part of the work, badly") | tune: 16.1% / 21.9% | better, still a fifth of the wrong items |
| the same, with "a trick you have to imagine does not count" in the system prompt | tune: 14.3% / 34.4% | worse: a small model reads a system-prompt rule as a lean, as "be strict" was read |
| a threshold on any single question's log-odds | — | the right and wrong items overlap; a cut that keeps every wrong item out refuses 46% of the right ones |
| **the two questions together, log-odds summed** | tune: 21.4% / 0 of 32; check: 17.6% / 1 of 28 | kept |
| the same, degree from the lenient question | Low band 0%, Medium 3% usable | the lenient question puts nearly all its mass on the clumsy degree, the hammer at a door included |

The two questions err on different cases. The strict one ("compared to bare hands…") almost never
passes a wrong item and refuses improvisations; the lenient one ("could X do any real part of this,
even clumsily?") passes improvisations and imagines uses for wrong items. Summing their log-odds —
an item passes when the lenient question is surer of it than the strict one is against it — keeps
what each is right about. The threshold is 0 and was not fitted; fitting a weight and a threshold on
`tune` bought one case and was not kept. The degree is the strict question's: the lenient one cannot
give it.

### The change

- **Two questions per tree on one preamble** (`CriticTrees`, `CriticNode.Counterweight`): the shipped
  question word for word gives the verdict's id; the lenient one adds its weight to the side.
- **Read, not sampled** (`ItemUseCritic.WeighAsync`): one request per question at temperature 0, no
  grammar, the first token's 40 likeliest log-probabilities. Side = the log-odds of the help ids' first
  tokens against the refusals', summed over the two questions; id = the strict question's greedy reply
  when on that side, else its likeliest id there. `serves_well` and `serves_poorly` share a first token
  ("s"), which is why the greedy reply is read too.
- **The preamble describes the target** (`CriticTarget.Describe`: the factory's description; an item
  lying in the scene with what holds it, "a speckled green pear …, from a pear tree …"; never a
  person), states the goal and the item once each, and puts the goal in the third person ("make their
  way across", not "make my way"; "catch the frog in their hands").
- **No "why" request.** A refusal's reason is a fixed first-person sentence per refusal id.
- `Threshold`, the gates before the critic, the bands and the cost of a refusal are unchanged.

### Before and after

All 167 cases, both under the setting rule the game sends (`results/survey_critic_before.*`, sampled 5
times a case; `results/survey_critic_current.*`, read once — the read is deterministic):

| | before | after |
|---|---|---|
| right side | 78.7% | **86.2%** |
| old bench, dev / held out (28 / 20 cases) | 94.3% / 86.0% | **100% / 90.0%** |
| false refusals (107 help cases) | 30.5% | **18.7%** |
| false passes (60 no-help cases) | 5.0% (15 of 300 samples) | 5.0% (3 of 60) |
| improvised / obvious / refuse | 57.8% / 95.3% / 91.2% | **72.3% / 100% / 91.2%** |
| appropriateness / substitution tree | 80.7% / 76.5% | **91.9% / 80.2%** |
| stability | 43 of 167 cases change side between samples | deterministic; 6 of 167 within 1 log-odds of the line |

What a player meets is not the side but whether their band clears the verdict
(`critic_bands.py`): a sensible item labelled for band B or below, passed at a degree B clears.

| usable at | before | after | + degree questions (below) |
|---|---|---|---|
| Low | 37.3% | 40.9% | **68.2%** |
| Medium | 39.7% | 46.7% | **55.0%** |
| High | 69.5% | 81.3% | **81.3%** |
| a wrong item, at Low / Medium / High | 1.7% / 1.7% / 5.0% | 0% / 0% / 5.0% | **0% / 0% / 5.0%** |

The two halves agree — `tune`: Low 43 → 50%, Medium 37 → 43%, High 70 → 82%, wrong at High 5% → 3%;
`check`: Low 30 → 30%, Medium 43 → 50%, High 69 → 80%, wrong at High 5% → 7% (28 cases) — so the
wording was not fitted to the cases it was tuned on. The three wrong items that pass do so at the
clumsy degree only, for a High hand: a whetstone up the toe-hold trail (strict −1.7, lenient +10.1), a
tally stick through brambles (−1.9, +2.3) and a heavy crossbow through lianas (−3.9, +5.9). No
threshold was fitted to remove them.

**The live check.** The game itself, on the real model (`--goal-only scale_up` — which now narrows the
real goal judge as well as the playground's draw — `--grant-item iron_dagger_weapon`, the test yard's
coursed-stone wall, a Rooftops will as the thinker): `ItemAppropriateness -> clearly_helps (strict
+6.2, lenient +10.9)`, cleared at High. Its two logged requests are byte-identical to what the bench
builds for the same inputs, system prompt included, and the bench reads the same verdict (strict +5.9,
lenient +9.7). The margins differ by up to ~1.2 between the game's server and the bench's; the cause
was not pinned down (the launch arguments are the same but for `--verbose` and `-t`), and it matters only
for the few cases near the line.

**Cost** (`critic_bench.py --timing survey`, `results/survey_critic_timing.txt`): one tool use per
survey case, the two versions alternating which goes first, the old one including its "why" request
after a refusal, 4 CPU cores, warm server.

| per tool use | before | after |
|---|---|---|
| mean wall time | 3.92 s | 4.64 s (+0.7 s, +18%) |
| median / worst | 3.02 s / 7.03 s | 4.58 s / 5.82 s |
| requests | 1.46 | 2.00 |
| tokens generated | 13.6 | 9.0 |
| user-message tokens | 218 | 445 over two requests, of which the second re-reads only its last lines from the prompt cache |

With the substitution tree's degree questions (second pass, below; `results/survey_critic_timing2.txt`,
both versions again in one run): before 3.52 s mean (2.72 median, 5.92 worst), now 4.29 s (4.19, 5.69),
2.26 requests a tool use. The gap is +0.77 s against +0.72 s without them: they are asked only of a
substitute that passed, and each re-reads one line.

**It is not cheaper.** Dropping the reason saves ~60 generated tokens after a refusal; the second
question costs a prompt read every time, and prompt reading dominates on CPU. A pass costs more than it
did, a refusal less, and the worst case is shorter. The single-question alternatives cost one read
(~2.6 s) but break the false-pass constraint.

### The substitution tree's degree (second pass)

The pass above left the required verbs' degrees almost all `serves_poorly` (23 of 36 passes): on the
81 required-verb cases (`--set required`) a Low hand could use **none** of the 13 stand-ins labelled
for it and a Medium hand **1** of 27. Two causes, found by recording each whole id's log-probability
(`critic_bench.id_logprob`, one greedy reply's per-token lists — `serves_well` and `serves_poorly`
part at "_w" against "_p"):

1. **A rule of my own.** A pass the strict question refused and the counterweight carried took the
   strict question's likeliest help id by its *first token* — a tie between `serves_well` and
   `serves_poorly` ("s"), broken toward the better: a chisel and a hammer at an ore seam, a cleaver at
   a log, a watering pot as a shovel all came out Medium. Such a pass is an improvisation and now takes
   the clumsy degree outright (4 over-grants → 0).
2. **The question.** Where the strict question accepts, it is near-certain of `serves_poorly` for
   nearly everything — a dagger offered as a knife, a war axe as an axe, fish-hooks to fish (−0.0
   each). It judges what the item *is* ("near enough to be the same thing"), not how it works.
   Rewording the options to grade by performance — "about as well as the usual tool" / "properly, if
   not quite as well" / "only clumsily, with real difficulty", with the question kept (`sub_w1`) or
   asked as "how well…" (`sub_w2`) — moved Low 0 → 0–1 of 13 and Medium 1 → 4–5 of 27: the menu is
   the wrong instrument.

**Asked as yes/no instead** (`CriticNode.DegreeQuestions`, `critic_degree_yesno.py` for the prototype),
on the same head, of a pass the strict question accepted: "Is X a kind of Y, or near enough the same
tool?" — yes → `is_the_tool`; else "Would X do this work properly, the way Y does, if a little less
well?" — yes → `serves_well`; else `serves_poorly`. Both read as the log-odds of yes against no at 0,
nothing fitted. On the required verbs: Low **0 → 6 of 13**, Medium **1 → 9 of 27**, exact degree 4 → 10
of 26, pass/refuse unchanged, no wrong item usable below High; both halves improve (Low 0 → 4 and
0 → 2, Medium 0 → 5 and 1 → 4). The two items it grants a better degree than labelled are a war pick
and a baker's peel given Low at an ore seam and a clay bank — and the peel's own description calls it
"a long flat wooden shovel", so the label is the stricter of the two.

Not kept: two rewordings of the second question ("a proper tool for this work, even though it is not
Y?", "could a worker do this job properly with X instead of Y?") did no better on `tune` — the model
says no to a machete for an axe, a cudgel or a chisel at a strongbox whatever the wording.

A mistake caught on the way: the prototype read yes/no from a dict keyed by the stripped token, so a
rare spelling (" Yes") overwrote a likely one ("Yes") and the dagger read −4 where the model says +8.
The bench and the C# sum every spelling.

**Not checked in the live game.** Two real-model runs aimed at `mine` with a mattock both reached
the critic by another road (each will took its other want: gathering the ore, examining the stone).
The substitution prompts are built by the same `CriticTrees.Head` the live check verified byte for
byte, and the bench reads their last lines from the C#; the yes/no request itself has not been seen
in a game log.

### What is left

- **The substitution tree's middle degree.** "Would X do this work properly" still says no to most
  proper stand-ins, so Medium hands get about half of what they should (55%).
- **The appropriateness tree errs the other way**: generous degrees (the dagger on the wall is
  `clearly_helps`, usable by a Low hand; the survey labels it High). Over-granting a degree is not a
  false pass, but it is the next thing to look at there.
- **Still refused at every band** (18 of 65 improvisations): every small spike at a lock — lancet, awl,
  hoof pick, marlinspike, fish-hook (both questions sure of it, −14 to −1) — a flint to skin, a
  pitchfork or a basket to fish, a pruning hook or a rake for a pear, a razor at a purse, a shield to
  crush a scorpion or scoop clay. These are where the model's picture of the object is thinnest.
- **Hypothesis 6** (one refusal option instead of two) was only tried under the letter read, where
  everything was swamped by the D bias; it is untested on ids.
- `critic_bench.py --variant current` reads the C# and runs this pair; `--set tune|check|survey`, and
  `--timing` for the cost.

## The setting rule leaking into narration

Reported in play: the closing reminder came back as scenery, most of all in the childhood
reminescence ("pulling me toward sleep in this old world of iron and myth, where hands did the work").
`setting_leak.py` replays recorded rewrites with the rule (system prompt) and the reminder (last
sentence of the request) swapped for a variant's, the same seeds under every variant, and counts the
rule's words written into an answer whose neutral line lacks them; it also counts the place's words
(where the place is not where the narration happens) and Layer 1 forbidden words, so a variant cannot
win by letting the real world in.

Childhood: 15 rewrites recorded from three new runs (seeds 11, 23, 37, the first REMEMBER of each),
four samples each. Exploration: 40 non-spoken rewrites from `runs/`, two samples each.

In `setting_leak.py` the rows are `iron`, `iron-place`, `point_back`, `no_reminder`, `ban`, `ban+rule`, and
the same with `-place`; the kept one is now `now`. In `results/setting_leak_*.jsonl`, written before the
change, the variant called `now` is `iron`.

| variant | childhood setting | childhood place | exploration setting | forbidden words (ch. / ex.) |
|---|---|---|---|---|
| shipped: "Remember: an old world of iron and hand labour, nothing from the real world." | **16/60** | **20/60** | 2/80 | 3 / 5 |
| shipped, no place clause | 7/60 | 1/60 | | 6 / |
| "Remember the setting rule." | 0/60 | 39/60 | 1/80 | 3 / 4 |
| no world sentence at all | 0/60 | 17/60 | 1/80 | 1 / 4 |
| a ban: "Remember: nothing from after engines and gunpowder, and no real place, person or date." | 3/60 | 16/60 | 0/80 | 1 / 7 |
| the ban, and the rule's first sentence ("an old world of iron, myth…") replaced by the ban | 0/60 | 16/60 | 1/80 | 2 / 8 |
| the ban, no place clause | 2/60 | 1/60 | | 2 / |
| the ban and the rule, no place clause | 1/60 | 0/60 | | 3 / |
| **"Remember the setting rule.", no place clause** (kept) | **0/60** | **0/60** | | 3 / |
| no world sentence, no place clause | 0/60 | 0/60 | | 2 / |

- **The reminder is what leaks, not the rule.** Whatever picture is in the last sentence is narrated;
  a ban leaks its own words ("before engines and gunpowder"). Pointing back at the rule carries no
  picture. The rule itself still leaks now and then (one "old world of iron and myth" in 80 exploration
  answers under the pointer).
- **Childhood was set on a plain it never narrates.** It runs on a `PlainBiomeContext`, so the
  reminder said "You are in a vast plain — write only what belongs there", and the memories dissolved
  "into the vastness of the plain". With the picture gone the place is the only noun left last, and the
  leak doubled — so the two changes go together: `NarrativeController` clears the place for the
  reminescence. The get-up phase really is on the plain and keeps it.
- The pointer was kept over dropping the sentence: equal on every count here, and it keeps a reminder
  at the end of the request, which the doc comment on `SceneSetting.Reminder` says is why it exists.
  Its protection against anachronism is not measured by this — the forbidden-word counts are lore
  words ("earth", "year") at the same rate under every variant.
- Also seen: on a slow CPU the choice judge can finish initialising after the childhood reminescence
  has begun, and the first observation throws `ChoiceJudge is not ready` (the scripts here hold 150 s on
  the main menu, `wait for 150`).

## State hallucinations: the open door and the found needle

Reported in play: a **locked door narrated as open**, and a lockpicking refusal narrated as having
**found the needle** it was refused for. Both are one failure — the rewrite states a world state the
neutral line only names as wanted, needed or seemed — so `hallucination_probe.py` measures it wherever
the game hands the model such a line: the missing-tool refusal of all seven tool-gated verbs, the
door's observation and the reasoning about it, its failure, the witness refusal and the unwilling
refusal, and the failures of the other verbatims that bundle a second result after "and" (open and
step through, tame and keep, follow and see, hide and watch).

Prompts are built by `prompt_lib` with the **live** wording (`T0` is frozen at the overhaul's base and
differs in the rule and the reminder) in the personas likely to act on each line, the game's sampling,
seeds 1000+. Every neutral line was checked against a `--playground` run, where the rewriter returns
it verbatim: the door refusal in the test yard read exactly *"I wanted to unlock and open the test door,
but I cannot do that with my bare hands. I would need to find a hairpin or a bone needle for that, and
take it in hand to do this work."* The probe's regex is a recall screen only; **every count below is
from reading the samples**.

### Baseline (402 samples)

| line | what goes wrong | count |
|---|---|---|
| missing-tool refusal, 7 verbs | the tool found, taken up or used: "Found a bone needle, took it to pry open the door", "Found one, took it, and laboured", "I found a sturdy blade to begin this work" | **8/144** (+ ~12 that echo "take it in hand" as a plan, 3 invented searches) |
| locked door, observation + reasoning | the locked state not said at all | **25/42** |
| the same, "pale where an older lock was prised away" | read as no lock: "where the lock was removed", "forced", "pulled free", "hinges creaking in the breeze" | 3/3 keywise, 2/3 poetry |
| "I don't want to unlock and open the door." | the door already open: "I close the door quickly, unnoticed, and lock it tight", "draw back the latch, let the door close" | **4/36** |
| witness refusal | the unlock narrated anyway ("the lock's mechanism yielding to a gentle touch"), or slipping through | **4/36** |
| door failure, and the failures of the four compound verbatims | the result claimed | **0/144** |

The last row is the finding the rest turns on: **a failure says "Alas, I failed to …", and that one
word is never overridden**. A refusal never says the act did not happen, and the missing-tool refusal
went further and handed the model two acts to narrate, "find" and "take it in hand".

### Variants, same seeds

| variant | change | result |
|---|---|---|
| `tool_v1` | "…and I have no {tool} with me. Without one I left it undone." | tool found **0/96**; door always shut; but the game's sentence is **false** when the tool is carried and not combined (`RequiredToolRule` reads only the combination) |
| `tool_v2` | "it takes {tool}, and I carry none. So I did not {act}." | brute force does the act another way: "So I crushed the lock with my bare hands until it crumbled", 2/4 |
| **`tool_v3`** (kept) | "…but my bare hands could not do it: it takes {tool}, and my hands held none for it. So I left it undone." | tool found **0/96**, brute force leaves the door 4/4, true whether or not the tool is carried. Tool named 77% (base 84%) |
| `unlock_only` | `UnlockDoorVerb.Verbatim` "unlock the door", without "and open" | unwilling "close the door" 0/24, but "the lock is unobstructed", "hinges creaking" remain; tool refusals unchanged. A minor lever — not kept |
| `combo` | verbatim "pick the lock of", `tool_v2`, "failed …; it did not happen", "is locked" | **brute force smashes through 4/4** (a method named invites another method); unwilling opens the door 3/24. Lost |
| **`refusal_undone`** (witness kept) | `ActionImpossible` + " So I leave it undone." | witness **0/36** (base 4/36) |
| `refusal_undone`, `refusal_left` (unwilling) | `ActionRefusal` + "so I leave it undone" / "so I leave things as they are" | unwilling unchanged (~5/36), and brute force smashes the door down 4/6 under the second. Lost |
| `hint_is` / `hint_sentence` / `name_locked` | "is locked" / the state in its own sentence / "drawn to a locked door" | lock kept 14/24 / 9/24 / **4/24** against 11/24. No wording carries it |

### Ported

- `NeutralNarration.ToolRequired` ← `tool_v3`; `ActionImpossible` ends "So I leave it undone."
  `cli/system/tool_required_refusal.cli` asserts the new sentence and the absence of "would need to find".
- `BuildingDescriptions.DoorDetail`: the prised-away lock replaced by a lock-plate streaking rust.
- `DoorPointOfInterest.DescribeFrom`: a locked door seen from its back reads "the door can be opened
  from this side" — it said "seems locked" while `open_door` offered the way out through it.

### Splitting a line into two requests

Asked after the above: does the dropped fact survive when it is the whole of a request of its own?
`hallucination_probe.py split` rewrites two neutral parts in sequence on one slot, the second carrying
the first exchange in its history as a `keepHistory` slot does; `--form one` sends the same parts as
one request, the control for the wording (`results/halluc_split_{two,one}.jsonl`, 6 seeds a persona).

| line | parts | split | one request |
|---|---|---|---|
| door observation (keywise, poetry) | "…This is {desc}." / "The door is locked." | lock kept **39/48**; 1 contradiction ("leaving the door unlocked for easy access") | lock kept 21/48 |
| unwilling refusal (6 action personas) | "I don't want to unlock and open the door." / "The door stays shut and locked, and I leave it so." | "I close the door and lock it", "I lock it tight": **12/36** | 10/36 |
| missing tool (6 action personas) | "…my bare hands could not do it." / "It takes {tool}, and my hands held none for it. So I left it undone." | the tool **used or sought**: "I tried with a hairpin", "I searched for…", "I found nothing but a bone needle": **24/36** | 0/36 |

**Splitting works where the second part is a description, and fails where it is the end of an act.**
A lone "The door is locked." is a perception and stays one. A lone "It takes a hairpin, and my hands
held none" has lost the act it belonged to, and the model supplies one: trying with the hairpin. A
state put in an act's mouth ("the door stays shut and locked, and I leave it so") is read as the act
of locking it, split or not. So the missing-tool refusal stays one request (`ToolRequired` as
shipped), the unwilling refusal is still open, and the door's lock is best said by a short second
rewrite after the observation — at the price of one more request per locked door observed.

### A judge to catch it

Asked next: could a judge slot, neutral and specialised, catch these after the rewrite?
`tools/judge/halluc_judge.py` puts each rewrite with its neutral line to the shipped model, read as
`ChoiceJudge` reads a decision (first-token letter log-probabilities, one forward pass, ~1.0–1.4 s and
~200 prompt tokens on 4 CPU cores). Cases: `judge_halluc_cases.jsonl`, 229 rewrites labelled by hand
before any judge saw them, 50 positive — a stratified draw (half from a broad keyword screen, half at
random) plus the positives found earlier, with the near misses kept as hard negatives ("an older lock
was forcibly removed" restates its line; "I close the door on my thoughts" is an idiom).

| phrasing | AUC | at even odds: precision / recall / false alarms | best F1 |
|---|---|---|---|
| generic ("…happened, was done, found or used, or is in a state the original does not say?") | 0.65 | 0.50 / 0.24 / 7% | 0.42 |
| checklist (the act done; a tool held, found or tried; a thing opened, closed, locked, broken) | **0.72** | 0.69 / 0.18 / 2% | 0.48 |
| consistent ("could both be true at once?") | 0.56 | answers one letter whatever the case | 0.39 |

(Both letter orders averaged; one order alone moved the generic judge's recall from 56% to 4%.)

**It does not work.** The best phrasing catches the blatant (a door smashed, 5 of 7) and almost nothing
else: a door closed or locked by the character 0 of 17, a tool found, taken or tried 3 of 20, the lock
gone 1 of 5. At the rate these occur in play (~5% of the affected rewrites), its even-odds setting
would catch about one in five and be wrong about two flags in three; lowered to catch more, it flags a
third of faithful rewrites. The model that writes the hallucination does not see it when asked.

What the cases do show is that the forms are few and fixed — "found one", "took it", "tried with a",
"I close the door", "smashed through" — and the game knows which tool is missing and that the door is
locked. A pattern check keyed to those facts, on refusals only, found 47 of 50 with 7 false alarms
here; it was written from these cases, so that number is fitted, not measured, and it would need a
fresh set to mean anything. Falling back to the neutral line on a hit would be its whole cost.

### Not solved by wording, and where it would be

- **The locked state of a door is dropped by 6 rewrites in 10**, whatever the wording, and the
  observation is the only place the player learns it short of trying the door. The fix is outside the
  prose: append the state to the rewritten observation when the rewrite lacks it (the game knows it,
  as it knows the "you" the observation guard catches), or mark it in the UI.
- **The unwilling refusal is a persona inversion**: lockpicking "closes the door", brute force
  "smashes it down". It is one clause long and the persona swamps it. A guard on the rewrite (a door
  verb's refusal mentioning close/open/smash) or keeping a refusal's neutral text unvoiced are the
  options; neither was tried.
- `DoorPointOfInterest` calls an unlocked, **closed** door "the door seems open". Not measured;
  `BuildingAudit` asserts the phrase.
- Side faults seen and not counted: failures moved in time ("until noon" read "before noon"), roles
  reversed ("I faltered in his grasp" taming), the act replaced ("I labored in the fields" stalking),
  and stealth's floorboards in half its rewrites.

## To check by hand on Windows

1. Play an outcome that teaches something new and read the narration: the lesson now comes in its own
   sentence of the neutral line ("And I learned …") — check the persona's rewrite still names it.
2. Think about a person after observing them: the reasoning should name them as the observation did
   ("the blacksmith of the village", or their name once introduced) and keep their gender.
3. Combine a substitute tool with a tool-gated verb (a sword to `cut` a carcass, a hatchet to
   `cut_wood`): it should now pass where it was always refused, while a feather or a loaf still fails.
4. A stealthy means: the action button reads "discreetly".
5. Nothing visual changed; no rule a player is subject to changed, so the manual is untouched.
6. *(The item critic pass.)* Take up "scale" on a house wall with a short sword: it should now be
   accepted at High (the clumsy degree), and refused at Low/Medium with the "beyond an unpractised
   hand" sentence rather than "wrong tool". The dagger is accepted at every band (`clearly_helps`, in
   the survey and in the live check) — more generous than the survey's label, which asks for High. A
   refusal now ends on a fixed line ("I think nothing about it could do any of this work") instead of a
   generated reason. The critic's log line reads `ItemUseCritic: ItemAppropriateness -> … (strict …,
   lenient …, sum …)`; each tool use is two requests in `logs/llm_session_*/slot_0/`.
