"""The model-side check for a new or edited modus mentis: does its persona carry its own things into
scenes that hold none of them?

    bash tools/prompts/serve.sh &                       # the shipped model, on port 8092
    python tools/prompts/persona_probe.py avarice,cruelty
    python tools/prompts/persona_probe.py chandlery --override my_rewrite.json   # try a wording first

Each modus mentis is asked the screening's probe lines for the kinds it narrates (screen.py), twice
(seeds 1 and 2), in the wording the game sends; its answers are measured as the screening measured the
whole catalogue (screen_metrics.py): how many words of its own fields reach a scene that did not give
them (leak), how many answers carry any (rate), whether a field is copied five words running (copy).
It prints each answer, so the leaks can be read, and a verdict against the catalogue as rewritten:
LEAKS when the mean leak is above LEAK_MAX, the 90th percentile of the rewritten catalogue. The
static half of the check is persona_lint.py (and --mm-audit), which is free and should pass first.

About 40 seconds a modus mentis on four CPU cores. Samples go to samples/probe.jsonl.
"""
import argparse, json, os, subprocess, sys
sys.path.insert(0, os.path.dirname(__file__))
import personas, persona_lint as PL, screen_metrics as SM

HERE = os.path.dirname(os.path.abspath(__file__))
LEAK_MAX = None   # set from the rewritten catalogue's screening; see NOTES.md, "Persona fields"
REFERENCE = [os.path.join(HERE, "samples", "screen.jsonl"), os.path.join(HERE, "samples", "screen_catalogue.jsonl")]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("ids", help="modus mentis ids, comma-separated")
    ap.add_argument("--override", help="JSON {id: {style, reminder2}} to try before writing it into the class")
    ap.add_argument("--out", default=os.path.join(HERE, "samples", "probe.jsonl"))
    a = ap.parse_args()
    ids = a.ids.split(",")
    version = "probe" if not a.override else "probe_override"
    if os.path.exists(a.out):     # a probe is of the wording as it is now, never of an earlier run
        keep = [l for l in open(a.out, encoding="utf-8") if json.loads(l)["mm"] not in ids]
        open(a.out, "w", encoding="utf-8").writelines(keep)
    for seed in (1, 2):
        cmd = [sys.executable, os.path.join(HERE, "screen.py"), "--out", a.out, "--seed", str(seed),
               "--only", a.ids, "--version", version]
        if a.override:
            cmd += ["--override", a.override]
        subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)

    mms = personas.load_modi_mentis()
    over = {version: json.load(open(a.override, encoding="utf-8"))} if a.override else {}
    if a.override:
        over["catalogue"] = over[version]
    ref = [json.loads(l) for p in REFERENCE if os.path.exists(p) for l in open(p, encoding="utf-8") if l.strip()]
    if os.path.exists(os.path.join(HERE, "rules", "catalogue.json")):
        over.setdefault("catalogue", json.load(open(os.path.join(HERE, "rules", "catalogue.json"), encoding="utf-8")))
    probe = [json.loads(l) for l in open(a.out, encoding="utf-8") if json.loads(l)["mm"] in ids]
    rows = [r for r in SM.measure(ref + probe, mms, over) if r.get("version") == version]
    lint = PL.Lint(PL.words_of({m: dict(mms[m], **over.get(version, {}).get(m, {})) for m in ids}))
    for mid in ids:
        rs = [r for r in rows if r["mm"] == mid]
        mm = dict(mms[mid], **over.get(version, {}).get(mid, {}))
        print(f"\n═══ {mid}\n  style:    {mm.get('style')}\n  reminder: {mm.get('reminder2')}")
        for r in rs:
            lk = ", ".join(w for f in ("style", "reminder2", "prompt") for w in r["leak_from"][f])
            print(f"  [{r['kind']}] {r['text']}" + (f"\n      leaked: {lk}" if lk else "") + ("  (copies a field)" if r["copy"] else ""))
        n = len(rs) or 1
        leak = sum(len(r["leak"]) for r in rs) / n
        verdict = "" if LEAK_MAX is None else ("  LEAKS" if leak > LEAK_MAX else "  ok")
        faults = lint.check(mm)
        print(f"  leak {leak:.2f} a answer (catalogue limit {LEAK_MAX}), {sum(bool(r['leak']) for r in rs)}/{n} answers leak, "
              f"{sum(r['copy'] for r in rs)} copy a field{verdict}")
        print("  lint: " + ("; ".join(f"{k} names {', '.join(t)}" for k, t, _ in faults) if faults else "passes"))


if __name__ == "__main__":
    main()
