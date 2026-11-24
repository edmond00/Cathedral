# First person or second person — notes

Should a modus mentis narrate as the character's own "I" (what the game does) or speak to the
character as an inner voice ("Look, the well. You should listen to it")? Measured on the shipped model
(Qwen2.5-3B-Instruct Q4_K_M, llama.cpp b11515, 4 CPU cores, the game's sampling settings). Nothing in
the game was changed. The report, with every sample side by side, is `report/index.html` (also
published as an Artifact).

## Answer

**Keep the first person.** Asked for the second person, the model treats the rewrite as a grammar
exercise: it turns the pronouns and hands the line back. More facts survive, but the persona fades and
the echo of the neutral line rises. Hallucination does not improve, outcomes break more, and reasoning
reads worse. Transposing the neutral line to "you" first stops the voice slipping back to "I", but
does not bring the persona back.

## Versions (person_lib.py)

| | the request | opening forced |
|---|---|---|
| `first` | the game's wording now (`variants.final()`: working-tree templates, the neutral line as the game builds it) | "I " / "I will " |
| `second` | same neutral line; the "I" is said to be the person whose mind the faculty is part of, to be told the line as "you"; each kind's instruction is the game's with only the voice turned (literal-act, success-or-failure and do-not-soften sentences kept); `OtherPersonNote` turned too | none |
| `second_tn` | as `second`, and the neutral line itself transposed to the second person first | none |

No example openings ("Look!", "You should") were put in the request: the model copies whatever the
request names, and a tic would have been measured as the voice's.

## Data

78 recorded cases (`../cases.jsonl`), narration kinds only, stratified: 47 dev and 31 held out, 32
personas (own, swapped and loosened), with persona fields read from the current source. Two seeds per
version give 468 samples. No slot history. The server was run with `-c 8192` over its 4 slots, because
the parallel requests overflowed 2048; the sampling settings are unchanged. Latency comes from a separate
sequential pass on 12 cases (`samples/timing.jsonl`).

## Results (blind, `results.txt`; mechanical, `metrics.txt`)

Means over 156 samples a version; differences paired by case, 95% bootstrap interval, * = interval
clears 0.

| criterion | first | second | second, transposed |
|---|---|---|---|
| F no hallucination (0–2) | 1.28 | 1.25 | 1.19 |
| I info transmitted | 1.17 | **1.39*** | **1.40*** |
| K kind's task done | 1.76 | 1.76 | 1.71 |
| P persona | 1.19 | **0.95*** | **0.92*** |
| C optional context (0–1) | 0.36 | 0.40 | 0.42 |
| R no repetition / echo | 1.58 | **1.35*** | **1.38*** |
| Q writing | 1.35 | 1.34 | 1.33 |
| meaning broken (F = 0) | 19% | 24% | 22% |
| voice as asked | 99% | 71% | 94% |
| content words kept | 48% | 63% | 63% |
| 4-grams copied from the line | 10% | 17% | 18% |
| counsel / imperative | 4% | 14% | 13% |
| words | 28 | 36 | 36 |
| prompt tokens (user message) | 201 | 228 | 228 |
| wall time, sequential | 5.2 s | 6.3 s | 6.2 s |

- Each seed alone gives the same picture (persona 1.19 against 0.90–1.00; echo 1.58 against
  1.29–1.42; info kept 1.12–1.22 against 1.37–1.44). On the held-out set, persona and echo lose
  significantly and the info-kept gain does not reach significance.
- **By kind.** Observation shows the trade most clearly: info kept 1.10 → 1.50, persona −0.3.
  Reasoning loses persona, echo and writing (writing −0.35*), and in `second` it slips back to "I" in
  40% of answers. In Outcome, meaning broken rises from 4% to 23%* and persona falls most (−0.42*,
  −0.62*). Action barely changes. Emotion is the one kind where the second person does better on
  persona (+0.5), but this is not significant on 6 cases.
- **Scorer bias check.** The voice cannot be hidden from a reader. The second-person answers that
  slipped back to "I" score no better on persona (1.02 against 0.92, 45 against 111 answers), so the
  persona loss does not look like the scorers preferring "I".
- **Loosened personas** (a bare name, one vague line) are weak in every version (persona 0.32–0.50),
  and the second person does not help them.

## What a second-person game would also need

- `PersonaRewriter`'s `AddressesSomeone` guard throws out any observation that says "you".
- `DefaultForcedPrefix` forces "I " in the grammar of every narration kind.
- `NeutralNarration` writes every line in the first person (`FirstPerson`). Without transposing it
  upstream, 29% of answers fall back to "I".
- `OtherPersonNote`'s pronouns.
- Every `PersonaPrompt` calls the faculty "you" ("You are the inner voice of…; you read animals"). In
  a second-person answer, "you" is also the character. This is a likely cause of the persona loss, but
  it was not tested.

## Not tried

- First-person persona prompts ("I am the inner voice of…") beside second-person answers.
- The second person for one kind only. No kind wins clearly enough to suggest it.

## Re-running

```bash
bash ../serve.sh                       # or with -c 8192 for parallel shards
python run.py select                   # sets/ (already committed)
python run.py sample --set dev --shard 0/2 --seeds 1,2   # and 1/2; heldout the same; then cat the shards
python metrics.py --sets dev heldout --json metrics.json
python blind.py sheet --sets dev heldout --parts 4 --out sheets/p   # score per RUBRIC.md
python blind.py merge --key sheets/p.key.json --scores sheets/p*.scores.txt --out scored.jsonl
python analyze.py --scored scored.jsonl --metrics metrics.json --json results.json
python report.py
```
