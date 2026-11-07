"""Can a judge slot catch a state hallucination in a persona rewrite?

A rewrite that states as fact what its neutral line does not — the act done, the missing tool found or
used, a locked door open or closed or smashed (see tools/prompts/NOTES "State hallucinations") — is
put, with its neutral line, to a neutral judge on the shipped model, read the way ChoiceJudge reads a
decision: one forward pass, the first token's log-probabilities over the offered letters, no grammar.

Cases: tools/prompts/judge_halluc_cases.jsonl, rewrites from hallucination_probe.py labelled by hand
(label 1 = a state hallucination by the rule in NOTES; imagery, intentions, dropped facts and invented
scenery are 0).

    python tools/judge/halluc_judge.py --variants generic,checklist,consistent [--flip] --out results.jsonl
    python tools/judge/halluc_judge.py --score results.jsonl

Needs tools/prompts/serve.sh's llama-server (PROMPT_PORT, default 8092).
"""
import argparse, json, math, os, time, urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PORT = int(os.environ.get("PROMPT_PORT", "8092"))
CASES = os.path.join(ROOT, "tools/prompts/judge_halluc_cases.jsonl")

SYSTEM = ("You check a rewritten sentence against the original it was rewritten from. The rewrite may "
          "change the words, the style and the imagery freely; what it may not do is state as fact "
          "something the original does not say happened. Answer with the letter of the right option only.")

# Each variant: the question, the two options (faithful first), and which letter is the hallucination.
VARIANTS = {
    "generic": (
        "Does the rewrite say that something happened, was done, was found or used, or is in a state, "
        "that the original does not say?",
        ["No: it only says what the original says, in other words.",
         "Yes: it states as fact an event or a state the original does not."]),
    "checklist": (
        "Read the rewrite for three things the original must agree with. Does the rewrite say, as "
        "something that really happened, that (1) the act was carried out or its result reached, or "
        "(2) a tool or object was held, found, taken or tried, or (3) a thing was opened, unlocked, "
        "closed, locked, broken or changed, when the original does not say so? Wishes, plans, "
        "imagery and feelings do not count.",
        ["No: none of the three is stated beyond the original.",
         "Yes: at least one of the three is stated as fact beyond the original."]),
    "consistent": (
        "Could everything the rewrite states as fact be true at the same time as the original?",
        ["Yes: the rewrite is consistent with the original.",
         "No: the rewrite states something as fact that the original contradicts or does not allow."]),
}
LETTERS = "AB"


def prompt_for(variant, case, flip):
    q, opts = VARIANTS[variant]
    if flip:
        opts = opts[::-1]
    lines = "\n".join(f"{LETTERS[i]}) {o}" for i, o in enumerate(opts))
    return (f"Original: \"{case['neutral']}\"\n\nRewrite: \"{case['output']}\"\n\n{q}\n\n{lines}\n\n"
            f"Answer with A or B.")


def letter_logprobs(prompt):
    body = dict(messages=[dict(role="system", content=SYSTEM), dict(role="user", content=prompt)],
                max_tokens=1, temperature=0, logprobs=True, top_logprobs=40, cache_prompt=True)
    req = urllib.request.Request(f"http://127.0.0.1:{PORT}/v1/chat/completions", json.dumps(body).encode(),
                                 {"Content-Type": "application/json"})
    t0 = time.time()
    with urllib.request.urlopen(req, timeout=600) as r:
        d = json.load(r)
    lp = {L: -40.0 for L in LETTERS}
    for t in d["choices"][0]["logprobs"]["content"][0]["top_logprobs"]:
        tok = t["token"].strip()
        if tok in lp:
            lp[tok] = max(lp[tok], t["logprob"])
    return lp, time.time() - t0, d.get("usage", {}).get("prompt_tokens")


def run(args):
    cases = [json.loads(l) for l in open(CASES)]
    done = set()
    if os.path.exists(args.out):
        for l in open(args.out):
            d = json.loads(l); done.add((d["variant"], d["flip"], d["i"]))
    with open(args.out, "a") as fo:
        for v in args.variants.split(","):
            for flip in ([False, True] if args.flip else [False]):
                for i, c in enumerate(cases):
                    if (v, flip, i) in done:
                        continue
                    lp, wall, ptok = letter_logprobs(prompt_for(v, c, flip))
                    halluc_letter = "A" if flip else "B"
                    other = "B" if flip else "A"
                    score = lp[halluc_letter] - lp[other]      # log-odds that it is a hallucination
                    fo.write(json.dumps(dict(variant=v, flip=flip, i=i, label=c["label"], score=score,
                                             wall=wall, prompt_tokens=ptok)) + "\n")
                    fo.flush()
                    print(f"{v:10} flip={int(flip)} {i:3} label={c['label']} score={score:+6.2f} {wall:4.1f}s", flush=True)


def auc(pos, neg):
    wins = sum((p > n) + 0.5 * (p == n) for p in pos for n in neg)
    return wins / (len(pos) * len(neg))


def score(args):
    rows = [json.loads(l) for p in args.paths for l in open(p)]
    groups = {}
    for r in rows:
        groups.setdefault((r["variant"], r["flip"]), []).append(r)
    # Both orders averaged, the way judge_bench reads rotations, when both are present.
    for v in {r["variant"] for r in rows}:
        a = {r["i"]: r for r in groups.get((v, False), [])}
        b = {r["i"]: r for r in groups.get((v, True), [])}
        if a and b:
            groups[(v, "mean")] = [dict(a[i], score=(a[i]["score"] + b[i]["score"]) / 2, flip="mean")
                                   for i in a if i in b]
    print(f"{'variant':11} {'order':5} {'n':>4} {'AUC':>5}  {'@0: prec':>8} {'recall':>6} {'FPR':>5}   best-F1 (thr: prec/rec/FPR)   ms")
    for (v, f), g in sorted(groups.items(), key=lambda x: (x[0][0], str(x[0][1]))):
        pos = [r["score"] for r in g if r["label"]]; neg = [r["score"] for r in g if not r["label"]]
        def at(t):
            tp = sum(s > t for s in pos); fp = sum(s > t for s in neg)
            prec = tp / (tp + fp) if tp + fp else float("nan")
            return prec, tp / len(pos), fp / len(neg)
        best = max(((2 * p * r / (p + r) if p + r and p == p else 0), t) for t in sorted(set(pos + neg))
                   for p, r, _ in [at(t - 1e-9)])
        p0, r0, f0 = at(0.0)
        bp, br, bf = at(best[1] - 1e-9)
        ms = 1000 * sum(r["wall"] for r in g) / len(g) if "wall" in g[0] else float("nan")
        print(f"{v:11} {str(f):5} {len(g):4} {auc(pos, neg):5.2f}  {p0:8.2f} {r0:6.2f} {f0:5.2f}   "
              f"{best[0]:.2f} ({best[1]:+.2f}: {bp:.2f}/{br:.2f}/{bf:.2f})   {ms:5.0f}")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--variants", default="generic,checklist,consistent")
    ap.add_argument("--flip", action="store_true")
    ap.add_argument("--out")
    ap.add_argument("--score", nargs="*", dest="paths")
    a = ap.parse_args()
    if a.paths:
        score(a)
    else:
        run(a)
