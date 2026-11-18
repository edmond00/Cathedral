"""Automatic measures for the means samples, per version:

  copy3   share of outputs that copy 3+ consecutive words of the SkillMeans description
  copy5   the same, 5+ words (a near-verbatim paste)
  named   share that names the faculty (its display name, lower-cased)
  means   share that refers to the means at all: the name, or a content word of the description
  words   mean output length in words

    python tools/prompts/means/analyze_means.py tools/prompts/means/samples.jsonl [--blind out.txt]
"""
import collections, json, os, random, re, sys
HERE = os.path.dirname(__file__)
sys.path.insert(0, HERE)
import run_means as R

STOP = set("the a an of and or to in on with for by from one one's oneself its it is that what as at "
           "their them they who which this these those be being into out over under up down".split())


def words(s):
    return re.findall(r"[a-z']+", s.lower())


def longest_run(a, b):
    best = 0
    for i in range(len(a)):
        for j in range(len(b)):
            k = 0
            while i + k < len(a) and j + k < len(b) and a[i + k] == b[j + k]:
                k += 1
            best = max(best, k)
    return best


def main():
    path = sys.argv[1]
    S = [json.loads(l) for l in open(path)]
    cases = {json.loads(l)["id"]: json.loads(l) for l in open(os.path.join(HERE, "..", "cases.jsonl"))}
    agg = collections.defaultdict(lambda: collections.Counter())
    for s in S:
        h, g, m = R.parts(cases[s["case"]]["record"]["neutralText"])
        name = R.name_of(m)
        out = words(s["text"])
        mw = words(m)
        run = longest_run(mw, out)
        content = [w for w in mw if w not in STOP and len(w) > 3]
        stems = {w[:5] for w in content} | {w[:5] for w in words(name) if len(w) > 3}
        A = agg[s["version"]]
        A["n"] += 1
        A["copy3"] += run >= 3
        A["copy5"] += run >= 5
        A["named"] += bool(re.search(r"\b" + re.escape(name) + r"\b", s["text"].lower()))
        A["means"] += any(w[:5] in stems for w in out)
        A["words"] += len(out)
        A["tok"] += s["user_tokens"]
    print(f"{'version':12} {'n':>4} {'copy3':>6} {'copy5':>6} {'named':>6} {'means':>6} {'words':>6} {'utok':>5}")
    for v, A in agg.items():
        n = A["n"]
        print(f"{v:12} {n:4} {A['copy3']/n:6.0%} {A['copy5']/n:6.0%} {A['named']/n:6.0%} {A['means']/n:6.0%} "
              f"{A['words']/n:6.1f} {A['tok']/n:5.0f}")

    if "--blind" in sys.argv:
        out = open(sys.argv[sys.argv.index("--blind") + 1], "w")
        key = {}
        rng = random.Random(7)
        by = collections.defaultdict(list)
        for s in S:
            by[s["case"]].append(s)
        for cid, ss in by.items():
            c = cases[cid]
            h, g, m = R.parts(c["record"]["neutralText"])
            out.write(f"\n=== {cid}  place={c['record'].get('place')}\nTHINKER: {c['persona']['reminder2']}\n"
                      f"SCENE: {h}\nGOAL: {g}\nMEANS: {R.name_of(m)} = {m}\n")
            rng.shuffle(ss)
            for k, s in enumerate(ss):
                tag = f"{cid}#{k}"
                key[tag] = s["version"]
                out.write(f"[{k}] {s['text']}\n")
        json.dump(key, open(sys.argv[sys.argv.index("--blind") + 1] + ".key.json", "w"))


if __name__ == "__main__":
    main()
