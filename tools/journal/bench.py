"""Generate journal paragraphs for every rendered prompt, and record their cost.

    python tools/journal/bench.py prompts.jsonl outputs.jsonl [--threads 4] [--only shipped,voice]

prompts.jsonl comes from the game:  Cathedral --journal-prompts <journals-dir> prompts.jsonl
Starts its own llama-server (one slot, CPU, the game's sampling and penalties) on JOURNAL_PORT.

Rows of one (source, grouping, variant) are a chain, written in order: the memory variants fill
{MEMORY} with the previous paragraph, or with a rolling one-line summary kept by a second request
(whose cost is added to the job's).
"""
import argparse, json, sys, time
from collections import defaultdict
import journal_lib as J

ap = argparse.ArgumentParser()
ap.add_argument("prompts"); ap.add_argument("outputs")
ap.add_argument("--threads", type=int, default=4)
ap.add_argument("--only", default="")
a = ap.parse_args()

rows = [json.loads(l) for l in open(a.prompts, encoding="utf-8")]
if a.only:
    keep = set(a.only.split(","))
    rows = [r for r in rows if r["axis"] in keep]
chains = defaultdict(list)
for r in rows: chains[(r["source"], r["grouping"], r["variant"])].append(r)

server = J.start_server(threads=a.threads)
out = open(a.outputs, "w", encoding="utf-8")
try:
    for key, chain in chains.items():
        chain.sort(key=lambda r: r["group"])
        memory = None
        for r in chain:
            user = r["user"]
            if "{MEMORY}" in user:
                user = user.replace("{MEMORY}", memory) if memory else \
                    "\n".join(l for l in user.split("\n") if "{MEMORY}" not in l)
            text, tm, wall = J.chat(r["system"], user, r["max_tokens"])
            text = J.clean(text)
            cost = dict(prompt_n=tm.get("prompt_n", 0), gen_n=tm.get("predicted_n", 0),
                        prompt_ms=tm.get("prompt_ms", 0), gen_ms=tm.get("predicted_ms", 0), wall_s=wall)
            if r["memory"] == "PreviousParagraph":
                memory = text
            elif r["memory"] == "RollingSummary":
                s_user = f"Summary so far: {memory or '(nothing yet)'}\nNewest entry: {text}\n\nWrite the new summary."
                s, stm, swall = J.chat(r["summary_system"], s_user, 80)
                memory = J.clean(s)
                for k_out, k_in in (("prompt_n", "prompt_n"), ("gen_n", "predicted_n"), ("prompt_ms", "prompt_ms"), ("gen_ms", "predicted_ms")):
                    cost[k_out] += stm.get(k_in, 0)
                cost["wall_s"] += swall
            rec = dict(r, sent_user=user, output=text, cost=cost)
            out.write(json.dumps(rec) + "\n"); out.flush()
            print(f"{key} group {r['group']}: {cost['prompt_n']}+{cost['gen_n']} tok {cost['wall_s']:.1f}s", file=sys.stderr)
finally:
    out.close()
    server.kill()
