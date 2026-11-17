# How a thinking modus mentis names its means

The reasoning a thinking modus mentis writes is a rewrite of `NeutralNarration.ReasoningChain`:
"I notice {target}. I want to {goal}. I will rely on {SkillMeans}." In play the last sentence came
back either pasted ("relying on long heavy bodily work") or bent into nonsense ("I shall introduce
myself with fire-making"). Its concrete words also showed up as things in the scene: in a cave,
"claw-hooked climbing over bark and stone" became "my claw-hooked climbing gear" and "bark-covered
stone".

## What was measured

All 47 recorded reasoning cases in `../cases.jsonl` that carry a goal and a means (17 situations,
own and swapped thinkers, and a loosened persona), 2 seeds, on the shipped model with the game's
sampling. Everything was exactly as the game sends it except the sentence or instruction under test
(`run_means.py`, built on `prompt_lib`).

| version | the line's end |
|---|---|
| `base` | "… I want to {goal}. I will rely on {means}." (shipped) |
| `name` | "… I will leave it to my {faculty}: {means}." |
| `name_only` | "… I want to {goal}, and I will leave the doing of it to my {faculty}." |
| `name_ref` | "… I want to {goal}, and I will leave it to my {faculty}." |
| `inme` | "… I want to {goal}, and I will leave it to the {faculty} in me." |
| `fused` | "… I want to {goal}, by way of {means}." |
| `manner` | "… I will do it with {means}." |
| `aside` | `name_ref`, plus a paragraph after the quoted sentence: "Your {faculty} is the part of you that will do it: {means}. … it describes you, not anything in the scene." |
| `none` | the means sentence dropped |
| `base+how` | `base`, plus "Say how that way of doing it suits this act, in your own words, without repeating its description." |
| `name+own` | `name`, plus "The way you will do it is yours to put in your own words: never repeat its description." |

{faculty} is the acting modus mentis's display name, lower-cased (`NeutralNarration.ModusMentisPhrase`).

### Automatic (`analyze_means.py`, 94 samples each)

| version | copies 3+ words of the means | 5+ | names the faculty | refers to the means at all | user tokens |
|---|---|---|---|---|---|
| base | 39% | 12% | 1% | 84% | 253 |
| name | 21% | 7% | 66% | 90% | 258 |
| name_only | 0% | 0% | 57% | 74% | 254 |
| name_ref | 0% | 0% | 52% | 73% | 251 |
| **inme** | **0%** | **0%** | 61% | 85% | **253** |
| fused | 39% | 12% | 2% | 80% | 252 |
| manner | 46% | 16% | 0% | 84% | 254 |
| aside | 1% | 0% | 87% | 98% | 292 |
| none | 0% | 0% | 0% | 11% | 241 |
| base+how | 21% | 10% | 0% | 67% | 274 |
| name+own | 20% | 2% | 53% | 91% | 278 |

### Blind (`scores_*.json`, keys in `blind_keys.json`)

Each output was scored blind, with versions shuffled per case. **F** is fidelity, 0–2, as in
`../RUBRIC.md`: 0 means an invented object, person or event, or the goal lost or reversed. **M** is
how the means is referred to: 2 in the voice's own words and fitting the act ("my stoneface nods in
agreement", "leaving words to the brute force in me", "I will retrace my steps"); 1 pasted, literal
or garbled ("leave the pigsty track to my stoneface", "my husbandry urges me to leave"); 0 absent.
Round A was 8 situations × 10 versions, seed 1. Round B was 15 situations × 7 versions, seed 1 or 2.
Round C was 15 swapped-thinker cases × 4 versions, seed 2.

| version | n | F | F = 0 | M = 2 | M = 1 | M = 0 |
|---|---|---|---|---|---|---|
| **inme** | 30 | **1.33** | 13% | **57%** | 30% | 13% |
| name_only | 23 | 1.35 | 22% | 43% | 26% | 30% |
| aside | 38 | 1.26 | 8% | 39% | 61% | 0% |
| name_ref | 23 | 1.00 | 39% | 30% | 39% | 30% |
| name | 23 | 1.30 | 9% | 22% | 74% | 4% |
| base | 38 | 1.08 | 21% | 5% | 84% | 11% |
| none | 38 | 1.08 | 29% | 0% | 0% | 100% |
| name+own, base+how, fused, manner | 8 each | 1.00–1.25 | 12–38% | 0–25% | 75–88% | 0–12% |

Round C alone (n = 15 each): inme F 0.93 / M2 47%, base F 0.73 / M2 7%, aside F 1.13 / M2 33%.
Swapped thinkers (dread, pride, avarice, ruination) lose the goal whatever the wording, which drags
every version down there.

## Findings

1. **The description is the problem, not the verb.** Every wording that keeps `SkillMeans` in the
   sentence (base, fused, manner, name) gets it pasted back, 12–16% near verbatim, and its concrete
   nouns (bark, hooks, guests, livestock) become things in the scene.
2. **A name plus "in me" wins.** "… and I will leave it to the {faculty} in me" copies nothing, costs
   no tokens, and gives the most natural references with fewer invented facts than the shipped line.
   "In me" matters: "my peasantry" was read as farm folk, so the snail was left "to the peasantry who
   know" and the well to "those who tend our fields". The same failure would hit "my petty thief",
   "my marksman" and "my ratcatcher". `name_only` and `name_ref`, without it, invented or reversed
   more (22% and 39% F = 0).
3. **Instructions to rephrase the means add hallucinations.** `base+how` and `name+own` did not cut
   pasting much and had the worst fidelity. Asking the model to say how the means suits the act asks
   it to invent.
4. **`aside` is the careful alternative.** It has the fewest invented facts (8%) and always refers to
   the means, but mostly literally (M = 1), and it costs +39 prompt tokens per reasoning.

## Beyond the wording

- **The means often does not fit the act.** Iron fist to cut a hide, stealth or bushcraft to
  introduce oneself, hospitality to follow a farm track, husbandry to smell hay. No wording makes
  these read well; that is the means judge (`ChoiceJudge`, `ChoiceKind.Means`), not the rewrite.
- **Some thinkers override the goal.** Dread wants to leave, pride declines and avarice hoards,
  rewriting "I want to examine the snail" into leaving or refusing it in a third of round C, under
  every version. That is a persona-fidelity issue of the reasoning instruction, not of the means.

## Porting

`ReasoningChain`, `ReasoningRetry`, `ReasoningRetrySameGoal`, `ReasoningReconsidered` and
`WillReasoningChain` take `skill.SkillMeans`. They would take
`ModusMentisPhrase(skill.DisplayName)` and end on ", and I will leave it to the {faculty} in me"
(the retry and reconsider lines, and the will's ", relying on …" clause, likewise). Only the retry
variants were not measured. `SkillMeans` stays as it is: the means judge reads it.
