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

## Names the judge misreads: `Species.JudgeNoun`

The judge reads the options, the situation and the act as plain text, and the model brings what it
knows of every word. It knows **triceratops, stegosaurus, ankylosaurus and carnotaurus as dinosaurs
extinct for ages** (asked, it says no one could meet a living triceratops), and that sank them in every
judge at once: in play every faculty that observed an appeased triceratops — Beast Sense included —
took a tapir, its tracks, herbs or nothing. Measured with Beast Sense, a jungle and five other options:
a jaguar, boar, bear or hyena in the triceratops's place is picked 5 times in 6 at about −1; every one
of the four dinosaurs 0 times in 6 at about −12. With no other beast on the list it still took a
strangler fig, a thorn bush or a boulder over the dinosaur, and judged Husbandry *reluctant* to tame it.

`ChoiceJudge.InJudgeWords` replaces each such name, as a whole word, everywhere in the prompt, with
the species' `JudgeNoun` ("three-horned beast", "plated beast", "armoured beast", "horned hunting
beast"). The player and the narration keep the name. Read that way the beast is a beast like any other:
Beast Sense picks it 2–4 times in 6 against a jaguar, Herblore and Caution never, and the other
faculties about as often as they pick the jaguar. **What did not work**, all measured on the same case:

- **Telling it** — a system-prompt line ("a creature named is a living animal, whatever its name"),
  a kind tag on every option ("(an animal)", "(a plant)"), "(a living animal)" on the beast: no change.
  The model's knowledge of the word beats any assurance about it.
- **A glossary line** ("In this world, a triceratops is a great three-horned grazing beast"): 6 in 6 —
  and the same line glossing a *swamp* made Herblore look at the swamp 4 times in 6, from 0. A glossed
  word wins for being glossed. It is salience, not understanding; so is a description given to one
  option only.
- **The name with an apposition** ("the triceratops, a great three-horned grazing beast"): the name
  still sinks it (0–2 in 6).

**When to declare one:** only for a name the model reads as something nobody could meet — extinct,
legendary, a modern coinage. A bird it finds dull is not misread; small creatures (dove, firefly,
butterfly) score low for Beast Sense because they are not the beasts it is about. Test a name before
declaring a noun: put it in a Focus list for a mind made for it, beside a familiar creature and some
scenery, over every rotation; a misread name loses to the familiar one by ten points or more. The
noun must be plain, short and not a word of a different act (rule 3 below holds for it too).

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
| Willingness | assent if log P(eager∪willing) − log P(reluctant∪unwilling) > `AssentLogOdds` (−1.8); on assent, a second request asks `EagerQuestion` ("Is this act this faculty's own work, what it is made for?") yes or no — eager when yes beats no (`EagerYesLogOdds`, 0); on dissent, unwilling vs reluctant from the scale at `UnwillingLogOdds` (−0.6) | the likeliest letter gave 1 eager in 40; the threshold on assent was right one case more often; the eager question, below |

**The willingness degree is asked, not read off the scale's lean** — the item critic's lesson
(`CriticNode.DegreeQuestions`), applied here. `cases.py` labels the degree on 51 cases
(`WILLINGNESS_DEGREE`, and the made-for-it half of the pairs as eager), where before only assent was
labelled and the two splits were set at the bench medians. Against those labels:

| eager vs willing from | degree right (of 51) | eager | willing | reluctant | unwilling |
|---|---|---|---|---|---|
| the scale's lean at `EagerLogOdds` (−6.4), as before | 26 | 15/27 | 5/8 | 2/8 | 4/8 |
| the likeliest letter | 18 | 4/27 | 4/8 | 6/8 | 4/8 |
| **`EagerQuestion`, yes over no** | **35** | 25/27 | 4/8 | 2/8 | 4/8 |

The scale's lean does not separate the two at all (eager acts −18…+8, willing ones −12…0); the
question does (eager median about +11, willing about −3), and is right for any cut from 0 to +2, so
it is left at the even point, untuned. Assent is still the scale's and unchanged (72 of 78). The
cost is one more request, on assent only — a short yes/no prompt on the willingness slot, about
1.5 s on CPU. More assents come out eager than before (28 of 37 on the bench, against 17), since the
old split was a median by construction; an eager act's difficulty falls by one.

**The dissent side could not be asked.** Every wording of "is this act contrary to it?" failed:
"does it contradict / go against / betray this faculty's nature?" answered *yes* for 16–25 of the 27
acts a faculty is made for and *no* for 7–8 of the 8 contrary ones — the model reads "does this act
bear on the faculty", negation and all — and the positive wordings ("is it the opposite of everything
this faculty stands for?", "is this faculty made to avoid this kind of act?") never answered yes at
all. So unwilling vs reluctant stays on the scale's lean, and it is weak: on the 13 labelled dissents
it is right 6 times, calling 6 of 8 reluctant acts unwilling — a refusal. The lean barely separates
them (reluctant −4.2…+1.9, most within 0.6 of the line; unwilling −1.1…+3.9): moving
`UnwillingLogOdds` to 0 would make it 9 of 13, but by moving three cases sitting at −0.5…0, on 13
cases, so it is left where it is until there are more labels. The scale's blind spot is upstream as
well: three of the eight unwilling acts are judged *assent* — Stoneface "willing" to pull faces,
Obedience to defy the reeve, Hospitality "eager" to turn a starving traveller away — the act sharing a
word with the description, which is rule 1 above biting.

**Focus, Goal, Means and Reply are scored net of an onlooker.** Each option's score is
log P(letter | the faculty) − `BaselineWeight` (0.3) × log P(letter | the ONLOOKER), the same list in
the same order with `OnlookerDescription` ("an ordinary attention with no bent of its own") in the
faculty's place; the decline keeps the faculty's own score; the rules above then read the net scores.
The model has tastes of its own and they win confident answers: the means judge gave Cruelty *bribery*
to make a prisoner talk and Covetousness *busking* to earn coins, because the model prefers bribery and
busking to violence and theft. The onlooker shares those tastes, so taking part of its score off leaves
the faculty's own pull. On the bench (same server, same run) it fixed 11 decisions and broke 1: the
two Cruelty means, both Covetousness means, Bargaining's goal and Beast Sense's, two Reply cases and
three Focus cases; it broke one of Mercy's means for calming the triceratops.

Why this works where a content-free prior failed: that prior is the same list with *nothing* in it, so
it can only correct the letters' position bias, which is small. The onlooker reads the same options in
the same scene, so it measures how attractive each *option* is to the model — which is where the
confident mistakes come from. Why 0.3 and not the full contrast: at 1 the net score is the faculty's
*difference* from an ordinary mind, and a faculty whose right answer is also the obvious one (Herblore
and the herb patch) loses it — Means fell from 63 to 49 of 73, Goal from 97 to 85 of 104. The sweep:
0.1 +4, 0.2 +7, **0.3 +10**, 0.5 +9, 0.75 +7, 1.0 −19 (cases right, against none). KeepFocus and
Willingness are not read against it: neither moved, and they cost a call each.

**The cost** is a second forward pass on four kinds of six: ~150 more prompt tokens a decision (the
system prompt is cached; the user message is not, since the faculty comes first and the two prompts
part there), about +1.4 s a decision on a 4-core CPU at ~95 prompt tokens/s. `BaselineWeight = 0`
switches it off.

Measured and rejected (keep them rejected unless the model changes): averaging several option orders
(position bias is already small; it cost 4× and lost accuracy), dividing out a per-letter or
content-free prior (worse), sampling (it only adds noise to a near-certain answer). Also, from the
triceratops investigation:

- **The faculty after the options**, so the faculty and onlooker prompts would share everything up to
  the faculty and the second call would cost ~70 tokens instead of ~150: 39 cases fewer at weight 0
  (346 → 307 of 386), and the onlooker could not win them back. The faculty comes first.
- **The full onlooker contrast** (weight 1): see above.
- **A baseline with no faculty at all** — the same prompt with the faculty line removed, in place of
  the onlooker. Measured in the same run as the onlooker (plain nouns on, 354 of 386 with no
  baseline): best at weight 0.2 with 358, against the onlooker's 362 at 0.3; 357 at 0.3, 354 at 0.5,
  299 at 1. It never fixed the two Reply cases the onlooker fixes, and it falls apart faster as the
  weight rises (Means 32 of 73 at weight 1, the onlooker 49): with no faculty, the question "which
  would *it* attend to?" has nobody to be about, and the model's answer to that is a poorer picture of
  its own tastes than an ordinary mind is. Same cost as the onlooker, so there is no reason to prefer it.
- **The onlooker on KeepFocus and Willingness as well**: no case changed at 0.3 or 0.5, for a call
  each.

**On a bigger model the onlooker stops paying** (Qwen2.5-7B-Instruct Q4_K_M, the same server build,
the same 386 decisions):

| | all | old cases | new cases |
|---|---|---|---|
| develop's prompts | 362 | 317 | 45 |
| plain nouns only | 361 | 317 | 44 |
| plain nouns + onlooker 0.3 | 358 | 313 | 45 |

The sweep on the 7B: 361, 361, 360, 358, 359, 358, 327 at weights 0 to 1. The 7B no longer makes the
errors the onlooker corrects (it gives Cruelty the fist unaided), and the onlooker costs it declines.
The onlooker itself declines near-certainly (log P ≈ 0), so netting the decline changes nothing,
while every real option, which the onlooker scores −6 to −17, is lifted by 2 to 5: the decline's
log-odds fall under `GoalDeclineLogOdds`. And that threshold is the 3B's: all eight of the 7B's Goal
errors on develop are missed declines. Re-tuned for the 7B (best near 2, against 10), Goal reads 99 of
104 without the onlooker and 98 with it — so even with thresholds that fit, the onlooker buys the 7B
nothing. The plain nouns are neutral there: the 7B already reads a triceratops as a beast, and
"three-horned beast" does it no harm. **`BaselineWeight` is a property of the model, like the
thresholds**: re-measure both on any swap (the `models` skill says so), from a `judge_bench.py --out`
run, which records the faculty's and the onlooker's scores so every weight and threshold can be swept
offline.
- **Telling the judge what a misread name is**, a glossary line, kind tags, appositions — see "Names
  the judge misreads".
- **A description on every Focus option** ("focus on hanging lianas: lianas dropping out of the dark
  above, thick as cables…", the game's own description text): measured on ten Focus cases, 29 → 37 of
  40 on top of the plain nouns, mostly the new cases (the temple stair 0 → 4, herbs 2 → 4) — but a
  decline case lost one (the cellar, 3 → 2): described options make the bare "let your focus go"
  thin beside them. Not shipped: ~14 more tokens an option (~+110 on a typical list, in every Focus
  call), measured on a subset with hand-written descriptions, and the game's descriptions do not carry
  an NPC's state (an appeased triceratops's hint still "lowers its three horns toward you"). Worth
  measuring on the whole Focus set if a state-aware description is ever written.

The unwilling/reluctant split is a **bench median**; the game's real mix of acts differs (the means
is usually chosen for fit), so the shares in play will differ too. Every decision logs its log-odds
and, on assent, the eager question's answer (`ChoiceJudge:` lines in the log), which is the data to
re-tune them from.

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
  case and loses another elsewhere is not a fix. **The model's server matters**: the numbers below
  are from llama.cpp built from the source vendored in llama-cpp-python 0.3.36 (not the b11515 the game
  ships) on a 4-core Linux CPU, where develop's prompts scored 95.9% on the 338 decisions of the old
  cases (published: 94.5% on b11515). The server is not perfectly deterministic: the same prompts
  scored 346 and 348 of 386 on two runs. Compare runs from one server only.
  On that server, with the great-reptile cases added (48 decisions):

  | kind | develop's prompts | plain nouns + onlooker |
  |---|---|---|
  | Focus | 60/79 | 69/79 |
  | KeepFocus | 28/28 | 28/28 |
  | Goal | 97/104 | 99/104 |
  | Means | 63/73 | 65/73 |
  | Willingness | 52/54 | 53/54 |
  | Reply | 48/48 | 48/48 |
  | **all** | **348/386 (90.2%)** | **362/386 (93.8%)** |
  | the old cases only | 324/338 (95.9%) | 329/338 (97.3%) |
  | the new cases only | 24/48 | 33/48 |

  The plain nouns alone gave 354 (the old cases are untouched by them, by construction). What the
  new cases still miss: Beast Sense prefers a strangler fig to "the three-horned beast" (3 of 5),
  Architecture lets its focus go in a ruined temple whatever the beast (a description fixed that, see
  above), and Mercy's means for calming the beast is Brawling — a weak label, Husbandry is "livestock".
- **`mm_audit.py`** — reads the raw faculty scores, not net of the onlooker (it probes descriptions,
  and the onlooker is the same for all). Three probes that need no labels: SELF (does the judge recognise its own
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
