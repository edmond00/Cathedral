# Rubric for the persona rewrites

Every output is scored blind: the version (old or new wording) is hidden and the samples of one
input are shuffled together before scoring. The scorer sees the **input** (the neutral line, the kind,
the persona's name and reminder, the place, the addressee and whatever else the game supplied) and
the **output**, nothing else.

Criteria are listed in the brief's order of importance. Each is an integer score; a sample's total is
not a sum — fidelity gates everything else, so the report keeps the five apart.

## 1. Fidelity (0–2) — and every invented fact listed by name

| score | meaning |
|---|---|
| 2 | Nothing invented, nothing lost. Every fact of the neutral line survives: the object, the person, the action and its target, success or failure, a question as a question, the stated feeling. |
| 1 | A minor loss or blur: a detail of the description dropped, a target generalised ("the thing"), tense drift, a mild addition that is still plausibly *inside* the given scene (a sound of wind outdoors). |
| 0 | An invented object, person, event, name, outcome or setting detail that the scene does not hold; a failure told as a success or the reverse; the action or its target replaced; a statement turned into a question or the reverse; the feeling replaced with another; the persona talking to someone in a perception; the narrator taking the observed person's identity; anything modern. |

**Not a hallucination:** a figure of speech ("my heart is a stone", "like a sleeping hound"), an
interior feeling the persona adds about the given fact, a generic quality of a named thing ("the
cold iron of the anvil"). **A hallucination:** a stone, a hound or an anvil that reads as *present*
when the input did not put it there.

For each 0 or 1, the scorer writes the invented or lost items in `facts` (e.g.
`+hearth, +candle; -target "the reeve"`).

## 2. Persona (0–2)

| score | meaning |
|---|---|
| 2 | Recognisably this faculty: its preoccupations or manner shape the wording, even when the line is a bare "good day to you" or a plain outcome. |
| 1 | Some colour, but generic ("I feel a sense of…"); could be most other faculties. |
| 0 | Neutral line handed back nearly unchanged, or a voice that belongs to a different faculty. |

## 3. Speed — measured, not scored

Prompt tokens (llama-server `/tokenize` of the user message, and `usage.prompt_tokens` for the whole
request), prompt-processing ms and wall time per request. In play the persona's system prompt is
already cached, so the user message and any history are what a request pays for.

## 4. Use of optional context (0–1, or n/a)

n/a when the game supplied nothing optional. Otherwise 1 when the output uses the supplied place,
hour, the addressee's role, gender or age, or the relation to them **correctly** where it fits, 0 when
it ignores all of it or contradicts it (day words at night, "sir" to a woman). Contradicting the
context is also a fidelity 0.

## 5. Quality of writing (0–2)

| score | meaning |
|---|---|
| 2 | Pleasant, idiomatic, varied, the right length for its kind (an emotion coda short, an observation one to three sentences). |
| 1 | Readable but stiff, padded, over-long, or carrying a tic. |
| 0 | Broken grammar, a truncated sentence, run-on word salad, meta-talk ("Here is the sentence"), quotation of the instruction. |

Tics are counted across samples, not per sample: the report lists any phrase that opens or closes
more than one output in five ("I can't help but", "a testament to", "my heart").

## Judges (ChoiceJudge, ItemUseCritic)

Accuracy only: the share of labelled cases on which the game's decision rule picks the labelled
answer, per kind, before and after. For the item critic the label is a band (help: clearly /
plausibly / detoured; no help: cannot / makes no sense), and a case counts as right when the answer
falls on the labelled side of help/no help **and** — reported separately — when it is the labelled
degree.
