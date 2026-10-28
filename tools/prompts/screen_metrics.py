"""Turns the screening samples (screen.py) into one row of leak measures per modus mentis.

    python tools/prompts/screen_metrics.py --samples samples/screen.jsonl [--out results/screen.tsv] [--show avarice]

Words are compared as crude stems (lower case, a plural or verb ending cut), stopwords and the words
every request carries left out. For each sample:

- **leak**: stems of the modus mentis's own fields (persona prompt, reminder, style) that the answer
  has and its request did not give it. This is what a persona carries into a scene: Avarice's silver,
  Bushcraft's fire. The request's footer quotes the style and reminder, so they are removed from
  "what the request gave" before comparing.
- **novel**: stems the answer has that neither the request nor the fields hold. Paraphrase lands
  here too, so it only means something against the other personas on the same probe: a word four
  personas in ten add to a probe ("testament", "shadows") is the model's, not this persona's, and is
  not counted.
- **copy**: the answer repeats five or more consecutive words of a field.
- **echo**: the share of the answer's stems that the request gave — high is a persona barely heard.

Per modus mentis: the mean of each over its samples, the share of samples with any leak, and the
leaked stems themselves.
"""
import argparse, collections, json, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
import personas

STOP = set("""a about above after again against all also am an and any are as at be because been before being
below between both but by can could did do does doing down during each even ever every few for from further had
has have having he her here hers herself him himself his how i if in into is it its itself just let like may me
more most my myself no nor not now of off on once only or other ought our ours ourselves out over own same shall
she should so some such than that the their theirs them themselves then there these they this those through to too
under until up upon very was we were what when where which while who whom why will with would yet you your yours
yourself yourselves one ones thing things way something nothing anything everything still again yet thus though
although much many must might shall here there now then""".split())
COMMON_SHARE = 0.4   # a stem this share of personas add to a probe is the model's habit, not the persona's


def stem(w):
    for suf in ("ing", "ed", "es", "s", "ly"):
        if w.endswith(suf) and len(w) - len(suf) >= 4:
            return w[: -len(suf)]
    return w


def stems(text):
    return {stem(w) for w in re.findall(r"[a-z]+", text.lower()) if len(w) >= 3 and w not in STOP}


def words(text):
    return re.findall(r"[a-z']+", text.lower())


def copied(answer, field, n=5):
    a, f = words(answer), words(field or "")
    grams = {tuple(f[i:i + n]) for i in range(len(f) - n + 1)}
    return any(tuple(a[i:i + n]) in grams for i in range(len(a) - n + 1))


def given(sample, mm):
    p = sample["prompt"]
    for f in (mm.get("style"), mm.get("reminder2")):
        if f:
            p = p.replace(f.strip(), " ")
    return stems(p)


def measure(samples, mms):
    by_probe = collections.defaultdict(list)
    rows = []
    for s in samples:
        mm = mms.get(s["mm"])
        if not mm:
            continue
        g = given(s, mm)
        fields = stems(" ".join(x for x in (mm["prompt"], mm.get("reminder2"), mm.get("style")) if x))
        out = stems(s["text"])
        r = dict(s, _given=g, _fields=fields, _out=out)
        rows.append(r)
        by_probe[s["probe"]].append(r)
    common = {}
    for pid, rs in by_probe.items():
        c = collections.Counter(w for r in rs for w in r["_out"] - r["_given"])
        common[pid] = {w for w, n in c.items() if n >= COMMON_SHARE * len(rs)}
    for r in rows:
        new = r["_out"] - r["_given"] - common[r["probe"]]
        r["leak"] = sorted(new & r["_fields"])
        r["novel"] = sorted(new - r["_fields"])
        mm = mms[r["mm"]]
        r["copy"] = any(copied(r["text"], mm.get(k)) for k in ("prompt", "reminder2", "style"))
        r["echo"] = len(r["_out"] & r["_given"]) / max(1, len(r["_out"]))
    return rows


def per_mm(rows):
    agg = collections.defaultdict(list)
    for r in rows:
        agg[r["mm"]].append(r)
    out = {}
    for mid, rs in agg.items():
        n = len(rs)
        out[mid] = dict(n=n, leak=sum(len(r["leak"]) for r in rs) / n, leak_rate=sum(bool(r["leak"]) for r in rs) / n,
                        novel=sum(len(r["novel"]) for r in rs) / n, copy=sum(r["copy"] for r in rs) / n,
                        echo=sum(r["echo"] for r in rs) / n,
                        leaked=collections.Counter(w for r in rs for w in r["leak"]).most_common(6))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--samples", nargs="+", required=True)
    ap.add_argument("--out")
    ap.add_argument("--show")
    ap.add_argument("--top", type=int, default=25)
    a = ap.parse_args()
    mms = personas.load_modi_mentis()
    samples = [json.loads(l) for p in a.samples for l in open(p, encoding="utf-8") if l.strip()]
    rows = measure(samples, mms)
    if a.show:
        for r in rows:
            if r["mm"] in a.show.split(","):
                print(f"[{r['kind']}] leak={r['leak']} novel={r['novel']} copy={r['copy']}\n  {r['text']}\n")
        return
    pm = per_mm(rows)
    head = "mm\tn\tleak\tleak_rate\tnovel\tcopy\techo\tleaked"
    lines = [head] + [f"{m}\t{v['n']}\t{v['leak']:.2f}\t{v['leak_rate']:.2f}\t{v['novel']:.2f}\t{v['copy']:.2f}\t"
                      f"{v['echo']:.2f}\t{' '.join(f'{w}:{c}' for w, c in v['leaked'])}"
                      for m, v in sorted(pm.items(), key=lambda kv: -kv[1]["leak"])]
    if a.out:
        open(a.out, "w", encoding="utf-8").write("\n".join(lines) + "\n")
    n = len(pm)
    mean = lambda k: sum(v[k] for v in pm.values()) / max(1, n)
    print(f"{n} modi mentis, {len(rows)} samples: leak {mean('leak'):.2f}  leak_rate {mean('leak_rate'):.2f}  "
          f"novel {mean('novel'):.2f}  copy {mean('copy'):.2f}  echo {mean('echo'):.2f}")
    for l in lines[: a.top + 1]:
        print(l)


if __name__ == "__main__":
    main()
