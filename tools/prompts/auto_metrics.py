"""Mechanical checks over samples, cheap enough to run on every sample of every version.

They measure the failures that are text, not judgement — the ones the blind scoring kept finding:

  copy_style      a run of 3+ words lifted from the persona's style, not in the neutral line
  copy_reminder   the same, from the persona's reminder
  copy_prompt     a run of 4+ words lifted from the rest of the instruction (clauses, setting rule)
  setting_word    "medieval", "iron and myth", "real world", "nothing modern" — words of the setting
                  rule written into the scene
  lesson_lost     an outcome that says "learned X" whose X is not in the answer
  question_lost   a spoken line or an address whose neutral asks a question and whose answer does not
  statement_asked a spoken line whose neutral asks nothing and whose answer is a question

None of these is the fidelity score; each is a floor under it. A sample that trips one is almost
always a fidelity failure, and one that trips none may still invent a hearth.

    python tools/prompts/auto_metrics.py --samples samples/iter.jsonl [--versions a b] [--kinds ...]
"""
import argparse, collections, json, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P

SETTING = re.compile(r"\bmedieval\b|\biron,? (and )?myth\b|\breal world\b|\bnothing modern\b|\bhand labou?r\b", re.I)
STOP = set("a an the of and to in on at by with for from as is are was were be it its this that i my me you your".split())


def words(s):
    return re.findall(r"[a-z']+", (s or "").lower())


def ngrams(ws, n):
    return {tuple(ws[i:i + n]) for i in range(len(ws) - n + 1)}


def lifted(text, source, neutral, n):
    """Content-bearing n-grams of `source` that appear in `text` and not in `neutral`."""
    src = ngrams(words(source), n) - ngrams(words(neutral), n)
    hits = [g for g in ngrams(words(text), n) & src if sum(w not in STOP for w in g) >= 2]
    return hits


def checks(case, sample, T=P.T0):
    r = case["record"]
    neutral = r["neutralText"]
    text = sample["text"]
    style = case["persona"].get("style") or ""
    rem = case["persona"].get("reminder2") or ""
    prompt = sample.get("prompt", "")
    rest = prompt.replace(neutral, " ").replace(style, " ").replace(rem, " ")
    out = dict(copy_style=bool(style and lifted(text, style, neutral, 3)),
               copy_reminder=bool(rem and lifted(text, rem, neutral, 3)),
               copy_prompt=bool(lifted(text, rest, neutral, 4)),
               setting_word=bool(SETTING.search(text)) and not SETTING.search(neutral))
    m = re.search(r"learned ([a-z' -]+?)[.,]", neutral)
    if r["kind"] in ("Outcome", "Emotion") and m:
        key = [w for w in words(m.group(1)) if w not in STOP]
        out["lesson_lost"] = not all(re.search(r"\b" + re.escape(w[:5]), text.lower()) for w in key)
    if r["kind"] in ("DialogueReplica", "Speaking"):
        asks = "?" in neutral
        out["question_lost"] = asks and "?" not in text
        out["statement_asked"] = (not asks) and "?" in text
    out["any"] = any(v for k, v in out.items())
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--samples", nargs="+", required=True)
    ap.add_argument("--versions", nargs="*")
    ap.add_argument("--kinds")
    ap.add_argument("--ids")
    a = ap.parse_args()
    cases = {c["id"]: c for c in (json.loads(l) for l in open(os.path.join(os.path.dirname(__file__), "cases.jsonl")))}
    ids = set(open(a.ids).read().split(",")) if a.ids else None
    agg = collections.defaultdict(lambda: collections.Counter())
    for p in a.samples:
        for l in open(p):
            s = json.loads(l)
            if a.versions and s["version"] not in a.versions:
                continue
            c = cases.get(s["case"])
            if not c or (ids and s["case"] not in ids):
                continue
            if a.kinds and c["record"]["kind"] not in a.kinds.split(","):
                continue
            ch = checks(c, s)
            agg[s["version"]]["n"] += 1
            agg[s["version"]]["tokens"] += s["user_tokens"]
            for k, v in ch.items():
                agg[s["version"]][k] += int(bool(v))
                agg[s["version"]][k + "_n"] += int(k in ch)
    keys = ["copy_style", "copy_reminder", "copy_prompt", "setting_word", "lesson_lost", "question_lost",
            "statement_asked", "any"]
    print(f"{'version':30} {'n':>4} {'tok':>5} " + " ".join(f"{k[:13]:>13}" for k in keys))
    for v, c in sorted(agg.items()):
        cells = []
        for k in keys:
            d = c[k + "_n"]
            cells.append(f"{c[k]:>4}/{d:<4} {100 * c[k] / d:3.0f}%" if d else f"{'-':>13}")
        print(f"{v:30} {c['n']:4} {c['tokens'] / c['n']:5.0f} " + " ".join(cells))


if __name__ == "__main__":
    main()
