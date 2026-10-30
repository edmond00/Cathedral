"""Samples the shipped model on the corpus under one version of the wording.

    python tools/prompts/run_samples.py --cases cases.jsonl --version base --out samples/base.jsonl
           [--variant variants.py:NAME] [--seeds 1,2,3] [--only dev|heldout] [--kinds Observation,...]

A case (see build_cases.py) is one recorded rewrite, possibly with its persona swapped for another
modus mentis's or for a deliberately loosened one. Every case is asked once per seed; the same seeds
for every version, so two versions' samples pair up one to one. Each sample records the output, the
prompt's token counts and the server's timings.

Resumable: samples already in --out (same case, version, seed) are skipped.
"""
import argparse, hashlib, json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P


def load_variant(spec):
    if not spec:
        return P.T0
    import variants
    return variants.VARIANTS[spec]()


def request_for(T, case):
    r = dict(case["record"], _run=case["run"])
    persona = case["persona"]
    r["personaReminder2"] = persona.get("reminder2")
    r["styleInstruction"] = persona.get("style")
    prior = case.get("prior", [])
    msgs = P.messages_for(T, r, persona=persona["prompt"], prior=prior)
    return r, msgs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cases", required=True)
    ap.add_argument("--version", required=True)
    ap.add_argument("--variant")
    ap.add_argument("--out", required=True)
    ap.add_argument("--seeds", default="1,2,3")
    ap.add_argument("--only")
    ap.add_argument("--kinds")
    ap.add_argument("--ids")
    a = ap.parse_args()
    T = load_variant(a.variant)
    seeds = [int(s) for s in a.seeds.split(",")]
    cases = [json.loads(l) for l in open(a.cases, encoding="utf-8") if l.strip()]
    if a.only:
        cases = [c for c in cases if c["split"] == a.only]
    if a.kinds:
        cases = [c for c in cases if c["record"]["kind"] in a.kinds.split(",")]
    if a.ids:
        cases = [c for c in cases if c["id"] in a.ids.split(",")]
    done = set()
    if os.path.exists(a.out):
        for l in open(a.out, encoding="utf-8"):
            s = json.loads(l)
            done.add((s["case"], s["version"], s["seed"]))
    out = open(a.out, "a", encoding="utf-8")
    for i, c in enumerate(cases):
        r, msgs = request_for(T, c)
        user = msgs[-1]["content"]
        utoks = None
        for seed in seeds:
            if (c["id"], a.version, seed) in done:
                continue
            if utoks is None:
                utoks = P.tokens(user)
            g = P.generate(msgs, P.grammar(r), seed)
            text = g.pop("text")
            if r["kind"] in ("DialogueReplica", "Speaking"):
                text = P.strip_frame(text, r.get("addressee"))
            out.write(json.dumps(dict(case=c["id"], version=a.version, seed=seed, text=text,
                                      user_tokens=utoks, prompt=user, **g)) + "\n")
            out.flush()
        print(f"{i + 1}/{len(cases)} {c['id']}", flush=True)


if __name__ == "__main__":
    main()
