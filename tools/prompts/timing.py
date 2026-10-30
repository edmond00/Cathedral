"""Measures the speed of two versions of the wording fairly, for the report.

    python tools/prompts/timing.py --old base --new final --ids "$(cat sets/final_dev.txt),..." --out samples/timing.jsonl

The final samples are no good for latency: one version ran after the other, so it found the
server's prompt cache warm. Here every request starts from the same state. Before each measured
request the server is asked for one token on the persona and the history alone (what the game has
cached by the time it asks for a rewrite), then the two versions are measured in turn, their order
alternating from case to case. Nothing else may run on the machine meanwhile.
"""
import argparse, json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P
import run_samples as R


def warm(msgs):
    P._post("/v1/chat/completions", dict(model="local", messages=msgs[:-1] + [dict(role="user", content=".")],
                                         max_tokens=1, stream=False, cache_prompt=True, seed=1))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cases", default=os.path.join(os.path.dirname(__file__), "cases.jsonl"))
    ap.add_argument("--old", required=True)
    ap.add_argument("--new", required=True)
    ap.add_argument("--ids", required=True)
    ap.add_argument("--seed", type=int, default=51)
    ap.add_argument("--out", required=True)
    a = ap.parse_args()
    versions = {v: R.load_variant(v) for v in (a.old, a.new)}
    ids = a.ids.split(",")
    cases = [c for c in (json.loads(l) for l in open(a.cases, encoding="utf-8") if l.strip()) if c["id"] in ids]
    out = open(a.out, "w", encoding="utf-8")
    for i, c in enumerate(cases):
        order = (a.old, a.new) if i % 2 == 0 else (a.new, a.old)
        for v in order:
            r, msgs = R.request_for(versions[v], c)
            warm(msgs)
            g = P.generate(msgs, P.grammar(r), a.seed)
            g.pop("text")
            out.write(json.dumps(dict(case=c["id"], version=v, seed=a.seed,
                                      user_tokens=P.tokens(msgs[-1]["content"]), **g)) + "\n")
            out.flush()
        print(f"{i + 1}/{len(cases)} {c['id']}", flush=True)


if __name__ == "__main__":
    main()
