"""What in a persona's wording goes with leakage: features of each field against the screening's
leak measures, per modus mentis.

    python tools/prompts/persona_features.py --samples samples/screen.jsonl --lexicon <Brysbaert.txt>

The lexicon is the Brysbaert, Warriner & Kuperman (2014) concreteness norms (40k English words rated
1 = abstract to 5 = concrete), not kept in the repository: pass its path. A "thing" word is one rated
4 or more (oil 4.9, silver 4.5, wick 4.7; delay 2.8, reluctance 2.1, memory 2.4).

Features, per field (style, reminder, persona prompt): its length in words, how many thing words it
names, their share of its content words, the longest comma- or dash-separated list of short items
("oil, stone, strap, rivet"), and for the persona prompt the quoted phrases it offers as examples
("'too dear,' 'no need,' 'mine.'") and whether it asks for a memory. Reported as Spearman correlations
with the leak measures, and as the mean leak of the personas with and without each feature.
"""
import argparse, collections, json, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
import personas, screen_metrics as SM

THING = 4.0
MEMORY = re.compile(r"\b(memor(y|ies)|remember\w*|childhood|recall\w*|once,? long ago|reminds? you of)\b", re.I)


def load_lexicon(path):
    lex = {}
    for line in open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if len(p) > 2 and p[1] == "0":
            try:
                lex[p[0].lower()] = float(p[2])
            except ValueError:
                pass
    return lex


def rating(w, lex):
    for cand in (w, w[:-1] if w.endswith("s") else None, w[:-2] if w.endswith("es") else None):
        if cand and cand in lex:
            return lex[cand]
    return None


def longest_list(text):
    """The most items in a run of short comma/dash/'and'-separated items, as in "oil, stone, strap, rivet"."""
    best = 0
    for clause in re.split(r"[.;:()]", text or ""):
        items = [i.strip() for i in re.split(r",|\s-\s|\s—\s|\band\b", clause) if i.strip()]
        run = 0
        for i in items:
            run = run + 1 if len(i.split()) <= 4 else 0
            best = max(best, run)
    return best


def field_features(text, lex, prefix):
    ws = [w for w in re.findall(r"[a-z]+", (text or "").lower()) if w not in SM.STOP and len(w) >= 3]
    rated = [r for r in (rating(w, lex) for w in ws) if r is not None]
    things = sum(r >= THING for r in rated)
    return {prefix + "_words": len(ws), prefix + "_things": things,
            prefix + "_thing_share": things / len(rated) if rated else 0.0,
            prefix + "_list": longest_list(text)}


def features(mm, lex):
    f = {}
    f.update(field_features(mm.get("style"), lex, "style"))
    f.update(field_features(mm.get("reminder2"), lex, "reminder"))
    f.update(field_features(mm.get("prompt"), lex, "prompt"))
    f["prompt_quotes"] = len(re.findall(r"['‘\"“][^'’\"”\n]{2,60}['’\"”]", mm.get("prompt") or ""))
    f["memory"] = int(bool(MEMORY.search(" ".join(x or "" for x in (mm.get("prompt"), mm.get("style"), mm.get("reminder2"))))))
    return f


def ranks(xs):
    order = sorted(range(len(xs)), key=lambda i: xs[i])
    r = [0.0] * len(xs)
    i = 0
    while i < len(order):
        j = i
        while j + 1 < len(order) and xs[order[j + 1]] == xs[order[i]]:
            j += 1
        for k in range(i, j + 1):
            r[order[k]] = (i + j) / 2
        i = j + 1
    return r


def spearman(a, b):
    ra, rb = ranks(a), ranks(b)
    n = len(a)
    ma, mb = sum(ra) / n, sum(rb) / n
    cov = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    va = sum((x - ma) ** 2 for x in ra) ** 0.5
    vb = sum((y - mb) ** 2 for y in rb) ** 0.5
    return cov / (va * vb) if va and vb else 0.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--samples", nargs="+", required=True)
    ap.add_argument("--lexicon", required=True)
    ap.add_argument("--out")
    a = ap.parse_args()
    lex = load_lexicon(a.lexicon)
    mms = personas.load_modi_mentis()
    samples = [json.loads(l) for p in a.samples for l in open(p, encoding="utf-8") if l.strip()]
    pm = SM.per_mm(SM.measure(samples, mms))
    ids = sorted(pm)
    F = {m: features(mms[m], lex) for m in ids}
    names = list(next(iter(F.values())))
    targets = ["leak", "from_style", "from_reminder2", "from_prompt", "copy", "novel"]
    print(f"{len(ids)} modi mentis\n")
    print("Spearman      " + "".join(f"{t:>15}" for t in targets))
    for n in names:
        xs = [F[m][n] for m in ids]
        print(f"{n:<16}" + "".join(f"{spearman(xs, [pm[m][t] for m in ids]):>15.2f}" for t in targets))
    if a.out:
        with open(a.out, "w", encoding="utf-8") as fo:
            fo.write("mm\t" + "\t".join(names + targets) + "\n")
            for m in ids:
                fo.write(m + "\t" + "\t".join(str(F[m][n]) for n in names) + "\t"
                         + "\t".join(f"{pm[m][t]:.3f}" for t in targets) + "\n")


if __name__ == "__main__":
    main()
