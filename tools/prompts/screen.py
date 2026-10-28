"""Screens every modus mentis's persona on a fixed set of probe lines, for the persona-field work.

    python tools/prompts/screen.py --out samples/screen.jsonl [--seed 1] [--only avarice,pride]

Each modus mentis is asked the probe lines of the kinds it actually narrates (an Observation modus
mentis observes, a Thinking one reasons, an Action one acts and hears the outcome, and so on) in the
wording the game now sends (variants.final), with its own persona prompt, reminder and style and no
prior turns. The probes are recorded lines from the dev split, two per kind and from different
places; the held-out playthroughs stay out of it. screen_metrics.py turns the samples into a leak
ranking.

Resumable: samples already in --out (same modus mentis, probe, seed) are skipped. Ordered by modus
mentis so the server's cache keeps each persona's system prompt across its probes.
"""
import argparse, json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P
import personas, run_samples as R, variants

PROBES = {
    "Observation": ["forest_evening:008:own", "talk_blacksmith:001:own"],
    "Reasoning": ["village_noon_party:005:own", "village_dawn:002:own"],
    "Action": ["farm_night:009:own", "talk_miller:003:own"],
    "Outcome": ["forest_evening:007:own", "farm_dusk:004:own"],
    "Emotion": ["farm_dusk:005:own", "talk_blacksmith:005:own"],
    "DialogueReplica": ["talk_miller:006:own", "talk_blacksmith:009:own"],
    "Speaking": ["village_noon_party:011:own"],
}

# Which narration kinds a modus mentis is given in play, by its functions (see the corpus: every
# recorded rewrite's kind against its modus mentis's functions). A will observes and thinks.
KINDS_OF = {
    "Observation": ["Observation"], "Thinking": ["Reasoning"], "Action": ["Action", "Outcome"],
    "Emotion": ["Emotion"], "Speaking": ["DialogueReplica", "Speaking"], "Will": ["Observation", "Reasoning"],
}


def kinds_for(mm):
    return [k for k in PROBES if any(k in KINDS_OF.get(f, []) for f in mm["functions"])]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--seed", type=int, default=1)
    ap.add_argument("--only")
    a = ap.parse_args()
    T = variants.final()
    cases_path = os.path.join(os.path.dirname(__file__), "cases.jsonl")
    cases = {c["id"]: c for c in map(json.loads, filter(str.strip, open(cases_path, encoding="utf-8")))}
    mms = personas.load_modi_mentis()
    if a.only:
        mms = {k: v for k, v in mms.items() if k in a.only.split(",")}
    done = set()
    if os.path.exists(a.out):
        for l in open(a.out, encoding="utf-8"):
            s = json.loads(l)
            done.add((s["mm"], s["probe"], s["seed"]))
    out = open(a.out, "a", encoding="utf-8")
    for i, (mid, mm) in enumerate(sorted(mms.items())):
        for kind in kinds_for(mm):
            for pid in PROBES[kind]:
                if (mid, pid, a.seed) in done:
                    continue
                c = dict(cases[pid], prior=[], persona=dict(id=mid, prompt=mm["prompt"], reminder2=mm["reminder2"],
                                                            style=mm["style"]))
                r, msgs = R.request_for(T, c)
                g = P.generate(msgs, P.grammar(r), a.seed)
                text = g.pop("text")
                if r["kind"] in ("DialogueReplica", "Speaking"):
                    text = P.strip_frame(text, r.get("addressee"))
                out.write(json.dumps(dict(mm=mid, probe=pid, kind=kind, seed=a.seed, text=text,
                                          prompt=msgs[-1]["content"], **g)) + "\n")
                out.flush()
        print(f"{i + 1}/{len(mms)} {mid}", flush=True)


if __name__ == "__main__":
    main()
