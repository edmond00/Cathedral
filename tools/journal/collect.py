"""Play scripted runs with the real model and dump their journals — the material bench.py writes from.

    python tools/journal/collect.py <out-dir> [--only name,…] [--cycles 3] [--threads 4]

Each scenario is one launch of the game (--cli, --no-chronicler so every source survives, a fixed
--seed and --llm-seed), two visits of a few act cycles each, then `journal dump <out-dir>/<name>.json`.
The real model chooses goals and replies, so a script cannot know whether an act opens a conversation
or a fight: every step is guarded with `when <mode>`, and the run follows whatever the model did.

Then:  <game> --journal-prompts <out-dir> prompts.jsonl
"""
import argparse, os, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
COMMON = "--skip-childhood --debug --no-encounters --cpu --no-llm-probe --no-chronicler --silent --hidden --allow-reentry --llm-seed 7"

# name -> (flags, strategy, extra commands after the first cycle) — varied on purpose: people to talk
# to, a failure and a wound, a beast, a party, a quiet place, a fight on the road.
SCENARIOS = {
    "village_talk":  ("--seed 42 --location-type village --location-id 3 --period noon --npc-static", "succeed", ""),
    "coast_wound":   ("--seed 43 --location-type coast --location-id 5 --period morning", "fail-dice", "wound protagonist 1"),
    "forest_beast":  ("--seed 44 --location-type forest --location-id 2 --period dusk --spawn-beast wolf", "succeed", ""),
    "burg_party":    ("--seed 45 --location-type burg --location-id 1 --period afternoon --fill-party --npc-static", "succeed", ""),
    "plain_quiet":   ("--seed 46 --location-type plain --location-id 4 --period morning", "auto", "wound protagonist 1"),
    "road_fight":    ("--seed 47 --location-type village --location-id 6 --period noon --start-fight wolf", "succeed", ""),
    # Conversations and a tamed beast are what the model rarely chooses on its own: the test location
    # pins the phase to the person or the beast that leads there.
    "hall_dialogue": ("--seed 48 --location-type test --location-id 0 --npc-static --start-area \"Test Hall\" --period noon --observe-only brewer", "succeed", ""),
    "den_beast":     ("--seed 49 --location-type test --location-id 0 --npc-static --start-area \"Test Den\" --period noon --observe-only wolf", "succeed", ""),
}

# Leaving: the footer may first have to close a phase (END, CONTINUE) before it reads LEAVE.
LEAVE = """
when LocationInteraction click button
wait 900
advance 20 1200
when LocationInteraction click button
wait 900
advance 20 1200
when LocationInteraction click button
wait mode WorldView 900
"""

CYCLE = """
when LocationInteraction click keyword 0
when LocationInteraction choose 0
when LocationInteraction choose 0
advance 20 1200
when LocationInteraction click action 0
when LocationInteraction choose 0
advance 20 1200
when LocationInteraction click continue
advance 20 1200
when Dialogue advance 20 1200
when Dialogue click option 0
when Dialogue advance 20 1200
when Dialogue click option 0
when Dialogue advance 20 1200
when Dialogue click continue
when Dialogue advance 20 1200
when Dialogue click button
wait 900
when Fighting fight-end victory
wait 900
advance 20 1200
"""

def script(strategy, cycles, out, extra=""):
    s = ["wait mode MainMenu 1800", "click menu New", "wait mode ProtagonistCreation 120", "click continue",
         "wait 900",
         "when Fighting fight-end victory", "wait mode WorldView 900",
         f"strategy {strategy}",
         "travel here", "wait mode LocationInteraction 1200", "advance 20 1200"]
    s += [CYCLE, extra] + [CYCLE] * (cycles - 1)
    s += [LEAVE, "travel neighbour", "travel-go", "wait mode LocationInteraction 1800", "advance 20 1200"]
    s += [CYCLE] * cycles
    s += [LEAVE, f"journal dump {out}", "quit"]
    return "\n".join(s) + "\n"

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out"); ap.add_argument("--only", default="")
    ap.add_argument("--cycles", type=int, default=3); ap.add_argument("--threads", type=int, default=4)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    game = os.path.join(ROOT, "bin/Debug/net8.0/linux-x64/Cathedral.dll")
    for name, (flags, strategy, extra) in SCENARIOS.items():
        if a.only and name not in a.only.split(","): continue
        out = os.path.abspath(os.path.join(a.out, name + ".json"))
        cli = os.path.abspath(os.path.join(a.out, name + ".cli"))
        open(cli, "w").write(script(strategy, a.cycles, out, extra))
        cmd = (f"xvfb-run -a dotnet {game} {COMMON} {flags} --llm-threads {a.threads} "
               f"--log-file {os.path.join(a.out, name + '.log')} --cli-timeout 10800 --cli-script {cli}")
        print(f"== {name}", file=sys.stderr)
        with open(os.path.join(a.out, name + ".out"), "w") as o:
            subprocess.run(cmd, shell=True, cwd=ROOT, stdout=o, stderr=subprocess.STDOUT)
        print(f"   {'dumped' if os.path.exists(out) else 'NO JOURNAL'} {out}", file=sys.stderr)

if __name__ == "__main__":
    main()
