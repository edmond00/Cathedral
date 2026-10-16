"""Scores the choice judge against the labelled cases in cases.py, with the game's own prompts and
decision rules (see judge_lib). Run it before and after changing a judge prompt, a threshold, or a
modus mentis description that a case uses.

    python tools/judge/judge_bench.py [--kinds Goal,Means] [--out results.jsonl]

Every multi-choice case is asked in every rotation of its real options (the decline stays last, as
the game lists it), since the game shuffles them and a case that is right in one order only is not
right. KeepFocus and Willingness are asked once, in their fixed order. About 300 forward passes;
roughly ten minutes on CPU.
"""
import argparse, collections as co, json, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import judge_lib as J
import cases as C

ap = argparse.ArgumentParser()
ap.add_argument("--kinds", default=",".join(J.KINDS))
ap.add_argument("--out")
args = ap.parse_args()
kinds = args.kinds.split(",")

MM = J.load_modi_mentis()
todo = [c for c in C.all_cases(MM) if c["kind"] in kinds]
out = open(args.out, "w", encoding="utf-8") if args.out else None

def rotations(c):
    n = len(c["options"]); d = c.get("decline_index", -1)
    real = [i for i in range(n) if i != d]
    if c.get("fixed"): return [list(range(n))]
    return [[real[(k + r) % len(real)] for k in range(len(real))] + ([d] if d >= 0 else []) for r in range(len(real))]

score = co.defaultdict(lambda: [0, 0]); stances = co.Counter(); errors = []
for c in todo:
    mm = MM[c["mm"]]
    if c["kind"] == "Willingness":
        lp = J.letter_logprobs("Willingness", J.build_prompt("Willingness", mm, c["situation"], J.SCALE), 4)
        w = J.willingness(lp); stances[w] += 1
        ok = (w in ("eager", "willing")) == (c["sign"] > 0)
        score["Willingness"][0] += ok; score["Willingness"][1] += 1
        if c["crim"]: score["Willingness (crimes)"][0] += ok; score["Willingness (crimes)"][1] += 1
        if not ok: errors.append(f"Willingness  {c['id']:55} -> {w}")
        if out: out.write(json.dumps(dict(id=c["id"], kind=c["kind"], lp=lp, pick=w, ok=ok)) + "\n")
        continue
    for order in rotations(c):
        shown = [c["options"][i] for i in order]
        d = order.index(c["decline_index"]) if c.get("decline_index", -1) >= 0 else -1
        lp = J.letter_logprobs(c["kind"], J.build_prompt(c["kind"], mm, c["situation"], shown, c.get("question")), len(shown))
        pick = shown[J.decide(c["kind"], lp, d)]
        ok = pick in c["ok"]
        score[c["kind"]][0] += ok; score[c["kind"]][1] += 1
        if not ok: errors.append(f"{c['kind']:12} {c['id']:55} -> {pick[:50]}")
        if out: out.write(json.dumps(dict(id=c["id"], kind=c["kind"], order=order, lp=lp, pick=pick, ok=ok)) + "\n")
    print(".", end="", flush=True)

print("\n\naccuracy (game decision rules)")
T = N = 0
for k, (ok, n) in score.items():
    print(f"  {k:22} {ok:3}/{n:<3} {100 * ok / n:5.1f}%")
    if "(" not in k: T += ok; N += n
print(f"  {'all':22} {T:3}/{N:<3} {100 * T / N:5.1f}%")
if stances: print("\nwillingness stances:", dict(stances))
print("\nerrors:" if errors else "\nno errors")
for e in errors: print("  " + e)
