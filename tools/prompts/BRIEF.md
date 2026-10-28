# Prompt overhaul — brief for a cloud session

You are working in a Linux sandbox on a Windows game whose narration, dialogue and decisions are
written by a small local LLM (Qwen2.5-3B-Instruct, Q4_K_M, on CPU). The developer **cannot see this
session's machine**: everything you find out reaches them through commits, the notes file and the
report described at the end.

Read `CLAUDE.md` first. Its six prohibitions hold here as everywhere.

## Goal

Rewrite the **global** LLM instructions of the modi mentis (MM) — the wording every MM shares —
so that they produce better text with the shipped model, and prove it with before/after samples.

"Global" means the instructions that hold whichever MM is speaking: how a neutral sentence is
built, the wrapper that asks a persona to re-express it, the clauses appended to every request, the
dialogue and speaking frames, the judges' system prompts. The goal is general instructions that
still work **when a persona is loosely defined** — a vague or short `PersonaPrompt` must still come
out sounding like itself and still stay faithful.

### In scope

- `src/game/NeutralNarration.cs` — the neutral sentences the persona rewrites.
- `src/game/narrative/PersonaRewriter.cs` — `BuildPrompt`, `BuildSpokenPrompt`,
  `BuildDialoguePrompt`, `BuildSpeakingPrompt`, `InstructionFor`, `OtherPersonNote`, `FooterFor`.
- `src/Config.cs`, class `Narrative` (~line 1700–1850) — `OneSentenceClause`, `ShortTextClause`,
  `GroundingClause`, `DefaultStyleInstruction`, `StyleIsNotSceneClause`,
  `DialogueStyleInstruction`, `AnswerInstructionFor`, `DialogueAnswerInstructionFor`.
- `src/game/narrative/SceneSetting.cs` — the setting rule and its per-request reminder.
- `src/game/narrative/OutcomeNarrator.cs`, `ThinkingPromptConstructor.cs`, `WorldContext.cs`.
- `src/game/dialogue/runtime/` — `DialogueOptionGenerator`, `DialogueReplicaWriter`,
  `DialogueTemplate`, `DialogueTreeAdapter`.
- `src/game/ItemUseCritic.cs` — its system prompt and question.
- `src/game/narrative/ChoiceJudge.cs` — the per-`ChoiceKind` system prompts, questions, the
  willingness scale. **Invoke the `choice-judge` skill before touching it.**
- Anything else under `src/` that assembles text sent to the LLM and is not specific to one MM
  (`grep -rn "CreateInstanceAsync\|GenerateConstrained" src` finds the call sites).

### Out of scope

- **The per-MM persona text**: each MM's `PersonaPrompt`, `PersonaTone`, `PersonaReminder2`,
  `StyleInstruction`, `MenuDescription`, `SkillMeans`, and each NPC archetype's text profile. These
  are fixed inputs. You *may* write deliberately loosened variants of a few of them (one vague line,
  a bare name) as **test inputs** to measure robustness — never commit changes to them. If you
  conclude that a persona field is the real cause of a failure, write that in the notes instead.
- The model, the GBNF grammars' shape (what the parser expects back), the sampling constants in
  `Config.LLM`. If you believe one of these is the real bottleneck, say so in the notes, with
  evidence.
- The manual: no rule a player is subject to changes.

## The criteria, in order

1. **Fidelity.** To the instruction, to the facts the game supplies, and to the game context. No
   invented object, person, event, name, outcome or setting detail. A figure of speech is not a
   hallucination ("my heart is a stone") — a stone that appears in the scene is. A failed action
   stays failed, a question stays a question, the action and its target stay literal.
2. **Persona.** Each MM finds a way to express itself whatever it is asked to say: a perception, a
   plain outcome, a "good day to you". Two different MMs given the same neutral line should be
   recognisably different, and each recognisably itself.
3. **Speed.** Shorter prompts are processed faster (the game is prompt-heavy: hundreds of tokens in,
   a few dozen out, so prompt reading is most of the wait). Shorter is better as long as 1 and 2
   do not suffer. Measure prompt tokens with llama-server's `/tokenize` and wall time per request.
4. **Use of optional context.** Time of day, weather, the NPC's gender and age, the place, the
   relation to the speaker — where the game supplies them, does the text use them (correctly)?
5. **Quality of writing.** Pleasant to read, varied, idiomatic, no tics repeated across samples.

**For the bounded-decision judges (`ChoiceJudge`, `ItemUseCritic`) the only criterion is the
accuracy of the decision.** `tools/judge/` already has a benchmark (`judge_bench.py`, cases in
`cases.py`) and an audit (`mm_audit.py`) that read the prompts straight out of the C# source; use
them, extend the cases if a decision kind is under-covered, and report accuracy before and after.

## Read before changing anything

The doc comments on the clauses above are a record of failures already fought against this exact
model — the hearth that appeared in a temple, the literal word `(thought)`, the persona that talked
itself out of its own shame, the line handed back unchanged, the two wordings of one rule read as
two rules. **Read them all before the first edit, and do not reintroduce a failure they describe.**
When you change a clause, rewrite its comment the same way: what failed, what was measured, why the
new wording.

## Method

1. **Environment.** `bash tools/linux/cloud-setup.sh`, then
   `bash tools/linux/fetch-assets.sh --model --glove`. The game opens a window, so runs go through
   `xvfb-run -a -s "-screen 0 1280x1024x24"` with `ALSOFT_DRIVERS=null`. Everything is CPU.
   Invoke the `runtime` skill for the llama-server contracts and the `logs/` tree, and the
   `verifying` skill before writing or running any `.cli` script. Copy known-good `.cli` scripts
   from `cli/` rather than composing sequences from the docs — preview boxes stall a hand-made
   sequence.

2. **A corpus of real requests.** Play the game through `--cli` with the real model and a fixed
   `--seed` and collect what it actually sends from `logs/` (`LLMLogger` writes every prompt and
   response). Cover every request kind: observation, reasoning, action, outcome, emotion, speaking,
   dialogue replica, dialogue options, thinking, item-use critic, every `ChoiceKind`. Cover
   **at least 15 MMs**, chosen to span the catalogue (terse and florid, thinking and bodily, moral
   and amoral, well and loosely written personas), plus a few loosened persona variants of your
   own. Vary the context: day and night, male and female and old and young NPCs, indoors and
   outdoors, success and failure.

3. **An offline harness** under `tools/prompts/`, in the manner of `tools/judge/judge_lib.py`:
   it rebuilds each request from the corpus's inputs using the prompt templates **read from the C#
   source** (so a measurement is a measurement of what the game sends and cannot drift from it),
   sends it to a llama-server on the shipped model, and stores the outputs. Several samples per
   request — sampling is not deterministic in effect, and one sample proves nothing. Keep a
   **held-out** part of the corpus you never iterate on, and report on it at the end.

4. **Judge yourself, blind.** You are the judge of the text. Score each output against criteria
   1–5 with a written rubric (commit it), listing every invented fact by name for criterion 1.
   Score with the version hidden and the order shuffled, so you cannot favour your own wording.

5. **Iterate.** Change one thing at a time, re-run, keep what wins, record what lost and why in the
   notes. Prefer removing words to adding them; a new clause must earn its tokens.

6. **Port to the game.** Move the winning wording into the C# with comments in the codebase's
   style. `dotnet build`, `./run_tests.sh audits`, and the `cli/` scripts that reach the code you
   touched (grep `cli/` for the feature). Then a last `--cli` playthrough with the real model, read
   end to end, to catch what the harness cannot see (the interplay of successive requests in one
   slot, the sanitizer, name faking). Do not run the full suite.

## Ground rules

- Work on the branch `prompt-overhaul`. Commit in small single-purpose commits and push after each,
  so progress survives the session. Never push to `main` or `develop`, never run the `release`
  skill, never touch itch.io.
- Never `new Random()` — `GameRng`, and `--seed` on every run.
- If a change helps one request kind and hurts another, the shared clause splits; do not trade one
  kind's quality for another's silently.

## Deliverables

1. **A before/after report for the developer to judge by eye.** Publish it as an Artifact if the
   tool is available; otherwise commit a self-contained `tools/prompts/report/index.html` and push.
   It shows, per request kind and per MM, the same input with the old and the new output side by
   side (several samples each, held-out set clearly marked), the context the game supplied, the
   scores, prompt token counts and latency, and a summary table per criterion. Include the
   loosened-persona cases, and the cases where the new wording is *worse*. The judges get an
   accuracy table per `ChoiceKind`, before and after.
2. **`tools/prompts/NOTES.md`**: what changed and why, what was tried and lost, numbers, what you
   believe is beyond the global instructions (persona fields, grammar, sampling), and what the
   developer has to check by hand on Windows.
3. The harness, corpus and rubric under `tools/prompts/`, so the measurement can be re-run.
