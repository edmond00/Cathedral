---
name: choice-judge
description: How a modus mentis's bounded decisions are judged (ChoiceJudge) and how to write for it — the guidelines for a modus mentis's DisplayName, MenuDescription, PersonaTone and SkillMeans and for its voice (StyleInstruction, PersonaReminder2: the persona lint and probe), the guidelines for a judge's system prompt, question and options, the decision rules read from letter log-probabilities, and the tools/judge/ benchmark and audit that measure all of it. Use when adding or editing a modus mentis, when adding a new bounded decision (a new ChoiceKind or a new list a modus mentis chooses from), when rewording a judge prompt or a threshold, or when a modus mentis chooses something it plainly should not.
---

# The choice judge, and writing for it

Every bounded decision a modus mentis makes — which object to look at, whether to keep a handed
focus, the goal, the means, the willingness, the dialogue reply — is made by **`ChoiceJudge`**
(`src/game/narrative/ChoiceJudge.cs`): a neutral LLM slot per `ChoiceKind`, reading the modus
mentis's description, the situation and a lettered list, and returning the score of every letter.
`src/game/CLAUDE.md` ("How a modus mentis chooses") has the mechanism; this skill has the craft.

The judge is the small local model on CPU. It is **certain on ~90% of its answers, wrong ones
included**, and it matches *words* before it weighs *natures*. Everything below follows from those
two facts: a confident mistake cannot be thresholded away, it can only be written away.

## What the judge reads of a modus mentis

```
The faculty: SPITE - <MenuDescription> Its manner: <PersonaTone>.
```

and, when it is offered as a means, `Spite: <SkillMeans>`. Those four fields are the whole of what
the judge knows. `PersonaPrompt` and the reminders are not shown to it (they voice the rewrite).

## Writing a modus mentis for the judge

**`MenuDescription`** (player-facing too; ≤ ~60 words, continuous prose, third person, no subject):

1. **Name the acts in the plain words the options will use.** Options say "swim", "steal", "dig",
   "provoke". A description that circles its subject without naming it fails the match:
   Natation ("the held breath, the steady stroke…") never said *swim*, and the willingness judge
   called it reluctant to swim a river. "Swims, and keeps the body at ease in water…" fixed it.
2. **Say what it inclines to do, not only what it notices or feels.** The judge picks an *act*. A
   thinking modus mentis needs one sentence of inclination — "Inclines toward the safer course: the
   step back, the wide berth, the danger left alone" took Caution off examining a bear. Temerity
   described only its thrill, and had no say in how anything was done.
3. **No word that names a different act.** The judge reads literally. Spite "remembers the slight
   and *waits*… *patient*" — so it chose "sit and wait" in front of a boasting rival. Words to
   distrust in a mind that is not about them: *wait, patient, still, pause, rest, watch, dream,
   stop*. Write what the wait is *for* ("pays it back: the jibe returned…").
   **Worst of all are the judge's own answer words.** Resolve "continues after the sensible moment
   to stop… the finish reached on nothing but *refusal*" was judged *unwilling* — near certain — to
   try a stuck gate a third time: the scale's refusal is "unwilling", and the description said
   "refusal". Keep *refuse, refusal, unwilling, reluctant, eager, give up, abandon, nothing* out of a
   description unless they are what it means; "keeps going where others give up" fixed it.
4. **Never soften a violent, criminal or ugly nature.** A redemptive clause or an honourable gloss
   ("treats combat as an ancient, honorable science") makes the judge keep it away from the ugly use
   it exists for — Cruelty would not pick Pugilitas to make a prisoner talk. The willingness judge is
   told to be amoral; the description must not sneak morals back in.
5. **Nothing that belongs to another modus mentis's domain.** Butchery's means "precise
   *anatomical* cutting" made Anatomy Lore choose Butchery to treat a wounded leg. Each faculty's
   vocabulary should be its own.

**`PersonaTone`** (read as "Its manner: …"): **lead with the manner** — how it goes about things.
An origin anecdote may follow but must never *be* the manner: "a dreamer who once dreamt a golden
arch" reads as *dreamer*. Several tones echo a childhood reminiscence (`ReminescenceCatalog`); keep
the echo as a trailing clause ("…ever since a dream of a golden arch"), and check the catalogue when
editing one. No modern or out-of-world words (the setting rule applies to the judge too).

**`SkillMeans`** (the means option, and "with [SkillMeans]" in action lists): **the act itself, in
the verb a goal would use, without echoing a goal's wording by accident.** "the crossing of water on
foot" beats "swimming" for *every* thinking faculty asked to "cross the river" — the means judge
matches words before temperaments. Prefer what distinguishes it from its neighbours ("swimming
across deep water and staying afloat").

## Writing the voice: `StyleInstruction` and `PersonaReminder2`

The judge never reads these two; the rewrite reads them **in every narration request**, and the
shipped model treats whatever they name as present in the scene. Measured over the whole catalogue
(`tools/prompts`, NOTES.md, "Persona fields"): the more concrete things a style named, the more of
them reached scenes that held none, and the craft personas replaced the act itself — a greeting
became "I will dip the wick in the tallow… twenty times", breathing in a haystack became oiling a
blade. Rewritten to the two rules below, the worst of them went from fidelity 0.25 to 1.41 of 2.

1. **A style is a manner, then a lens on whatever is in the scene.** "Slow, patient and exact -
   judge things by the patience in them, well made or rank." Not "Use images of rendering fat, the
   dip and the cooling coat, wicks, wax and the steady flame." No "images of…", "narrate the…",
   "describe the…" — they open a list of props. The manner (two or three attitude words) is what a
   one-line action can carry without inventing an act; a lens alone left actions bland.
2. **A reminder is a disposition, not a picture.** "someone who listens before looking and is rarely
   surprised", not "someone who finds the hidden nest by listening" (a nest in nine answers of
   sixteen).
3. **Never ask for a memory** ("let each smell pull a memory with it"): the model invents one every
   time.

Both are checked twice:

- **`--mm-audit`** (`PersonaLint`, free, part of `./run_tests.sh audits`) fails a style or reminder
  naming two or more concrete nouns, or a style opening a props list. Concreteness and noun-ness
  come from the GloVe vectors the game already loads. `python tools/prompts/persona_lint.py --text
  "…"` checks a wording before it is written in.
- **`tools/prompts/persona_probe.py <id>`** (the shipped model, ~40 s a modus mentis) asks the
  persona the screening's probe lines and prints every answer with the words it carried in, and a
  verdict against the rewritten catalogue. Run it on a new modus mentis, and read the answers — the
  lint cannot see a persona prompt whose own examples are copied, or a memory invented.

## Writing a judge (a new ChoiceKind, or rewording one)

- **One slot, one question, one system prompt per kind.** The prompt is cached; keep it fixed and
  put everything variable in the user message.
- **Say "neutral … no morals of your own".** Asked as itself, the model brings its own caution: a
  cutpurse came out reluctant to pick a pocket. The willingness prompt states outright that many
  faculties exist for criminal or violent work (20% → 92% right on crimes).
- **Ask a balanced question.** Asked "keep your focus, or turn elsewhere?" the keep judge turned away
  72% of the time; asked whether the thing "holds anything for this faculty — yes / no", 51%, all
  labelled cases right. Both sides of a yes/no must be stated as plainly as each other.
- **Ask what the faculty *is*, not what it would *want*.** "How far does this act belong to this
  faculty?" beat "how willingly would it do it?".
- **An ordinal scale stays in order and is defined.** Each degree gets its definition
  (`StanceLabel`); shuffled, the same judge fell from 93% to 83%. Reversed, it reads the scale badly
  (95% → 80%), so do not average orders of a scale either.
- **Options are short, parallel and second person** ("focus on a herb patch"); the judge turns them
  to the third. The decline option goes last and is phrased as a real option ("do something else
  entirely"), never as "none of the above".
- **Warn against the word-match where it bites** — see the means prompt ("never because a
  description shares words with the goal").
- **Read scores, not a sampled letter.** `PickAsync` takes the likeliest letter unless the kind has a
  threshold. A new kind starts on the likeliest letter; add a threshold only where the bench shows
  the model *hedges* on that kind (it never helps a confident error).

## The decision rules

| kind | rule | why |
|---|---|---|
| Focus, KeepFocus, Means, Reply | likeliest letter | thresholds measured no better |
| Goal | decline only if its log-odds over all real goals > `GoalDeclineLogOdds` (10) | right declines scored +11…+22, wrong ones ≤ +4.4: 89% → 92% |
| Willingness | assent if log P(eager∪willing) − log P(reluctant∪unwilling) > `AssentLogOdds` (−1.8); then eager vs willing at `EagerLogOdds` (−6.4), unwilling vs reluctant at `UnwillingLogOdds` (−0.6) | the likeliest letter gave 1 eager in 40; this gives ~10 each and one more right |

Measured and rejected (keep them rejected unless the model changes): averaging several option orders
(position bias is already small; it cost 4× and lost accuracy), dividing out a per-letter or
content-free prior (worse), sampling (it only adds noise to a near-certain answer).

The eager/unwilling splits are **bench medians**; the game's real mix of acts differs (the means is
usually chosen for fit), so the shares in play will differ too. Every decision logs its log-odds
(`ChoiceJudge:` lines in the log), which is the data to re-tune them from.

## Measuring — `tools/judge/`

All three read the prompts, the scale, the thresholds and every modus mentis straight from the C#
source, so they measure what the game sends. They need the shipped model on a llama-server:

```bash
models/llama/llama-server.exe -m models/model.gguf -c 8192 -ngl 0 --port 8091 &   # CPU
python tools/judge/judge_bench.py                 # labelled cases, ~10 min; --kinds Goal,Means
python tools/judge/mm_audit.py --ids spite,fording # label-free probes; the full set takes > 1 h
```

- **`cases.py`** — the labelled cases. When you add a modus mentis or a judge, **add cases it must
  get right**: one where the right answer is the decline, one where an obvious wrong answer is on the
  list, and for willingness a pair (a mind made for the act, one foreign to it), crimes included.
  A case must have one clearly right answer; "wading crosses a river too" makes a bad case.
- **`judge_bench.py`** — scores them with the game's rules, every rotation of the options. Run it
  before and after any prompt, threshold or description change and compare; a change that wins one
  case and loses another elsewhere is not a fix. Baseline on this branch: 94.5% overall.
- **`mm_audit.py`** — three probes that need no labels: SELF (does the judge recognise its own
  means from its description), OWN (does an action modus mentis assent to its own work), GOAL (a
  fixed scene, picks listed for reading). A pass is necessary, not sufficient — Spite passed all
  three while choosing "sit and wait" in a labelled case. An OWN flag must be **confirmed with a
  concrete act** before anything is edited: the probe's "an act that calls for the working of a
  ship" is awkward enough that Seamanship and Self-Command dissented from it and assented to
  "haul in the mainsail" and "keep a straight face while the lord insults you". Resolve dissented
  from both, and that one was real.

**A known limit of the goal judge:** in a scene that offers a mind nothing, it seldom declines — the
decline's log-odds were negative for most of the 455 in the audit's market-square scene — and the
pick among irrelevant goals then follows the option order (Apothecary's Nose "attacked" the stranger
in one order and "examined" him in another). A description cannot fix that; the goal prompt, or how
the goal list is built, would have to.

Procedure for a new modus mentis: write the four fields by the rules above → add cases → run
`mm_audit.py --ids <id>` and `judge_bench.py --kinds <the kinds it takes part in>` → read every error
before touching anything, since some are bad labels → fix the description (not the prompt) unless
every modus mentis fails the same way, which is the sign of a prompt problem (the river case).
