# tools/journal/

Measuring the chronicler — the background LLM session that writes the journal up
(`src/game/narrative/Chronicler.cs`, prompts in `ChroniclerPrompt.cs`) — for **speed** (it must never
slow the narration) and for **quality** (judged, not guessed). In the spirit of `tools/judge/`: the
harness holds no copy of a prompt. The game renders every prompt it would send.

| file | what |
|---|---|
| `latency.cli` | one fixed scripted run on the real model: two visits, reading pauses (`wait for`) that let the chronicler start, clicks that then preempt it |
| `latency.py` | compares the `[llm-timing]` lines of that run with and without `--no-chronicler` |
| `collect.py` | plays varied scripted runs on the real model (`--no-chronicler`) and dumps each journal (`journal dump`) |
| `bench.py` | sends every rendered prompt to a llama-server it starts, chain by chain, and records outputs and cost |
| `score.py` | automated checks, the manual scores, and the variant table |
| `journal_lib.py` | the plumbing: sampling read from `src/Config.cs`, the server, the checks |

## Speed

```bash
dotnet build Cathedral.csproj -r linux-x64
G="dotnet bin/Debug/net8.0/linux-x64/Cathedral.dll"
F=$(grep -m1 '^# FLAGS:' tools/journal/latency.cli | sed 's/^# FLAGS: *//')
eval "xvfb-run -a $G $F --hidden --silent --log-file with.txt    --cli-timeout 7200 --cli-script tools/journal/latency.cli"
eval "xvfb-run -a $G $F --hidden --silent --log-file without.txt --cli-timeout 7200 --cli-script tools/journal/latency.cli --no-chronicler"
python3 tools/journal/latency.py with.txt without.txt
```

On Windows run the same flags against `Cathedral.exe` (no xvfb), with `--llm-threads` set to the
thread count the player's Settings screen would use. Every request also lands in the crash report's
"LLM priority gate" section (`crash-report` in the CLI forces one).

## Quality

```bash
python3 tools/journal/collect.py material/                      # hours on a 4-core CPU
$G --journal-prompts material/ prompts.jsonl     # tools/journal/material/ holds the eight runs measured
python3 tools/journal/bench.py prompts.jsonl outputs.jsonl
python3 tools/journal/score.py outputs.jsonl --manual tools/journal/manual_scores.json --samples 6
```

Variants differ from `ChroniclerPrompt.Shipped` on one axis at a time: **voice** (chronicle, diary,
tinted diary, campaign log), **inputs** (milestones only; facts and milestones; narration only;
everything), **grouping** (one event, one visit, one day), **memory** (none, previous paragraph,
rolling summary), **length** (90, 160, 260 tokens).

### The rubric (manual, 1–5 each)

- **fidelity** — 5: everything in the paragraph is in the notes. 3: an embellishment that changes no
  fact (a mood, a gesture). 1: an invented person, name, place, object or event. The automated
  `invented` column flags proper names absent from the notes; read each one.
- **coverage** — 5: the events that matter are there (a wound, a lesson, a companion, a fight, who was
  met and what came of it). 1: the paragraph is about something else or about nothing.
- **readability** — 5: reads as a journal a player would want to read. 1: a list, a template, or
  broken prose.
- **voice** — 5: the voice asked for, held, and consistent with the paragraph before. 1: wrong person,
  or a different narrator from one paragraph to the next.

Keyed `"<grouping>/<variant>/<source>/<group>": [fidelity, coverage, readability, voice]` in
`manual_scores.json`.

`material/` was captured before the recorder dropped bookkeeping chips and marked lessons. It has since
been brought in line by hand — the `Modus mentis practised/learned` and `Focus lost` facts removed,
every `Learned …` milestone marked `Lesson` — so what the prompts render from it is what the game
records today.
