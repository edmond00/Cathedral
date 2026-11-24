"""Mechanical metrics for the first/second-person experiment — every sample, every version.

    python metrics.py --sets dev heldout [--json out.json]

Per sample:

  voice_ok      first: opens on "I" and never addresses a "you" (outside a copied generic-"you"
                style); second*: addresses "you" at least as often as it says "I/me/my"
  voice_slip    second*: opens on "I", or has no "you" at all (fell back to the first person)
  recall        share of the neutral line's content words (stem of 5 letters) that survive: the
                facts transmitted, crudely
  parrot        share of the answer's word 4-grams that are the neutral line's own (pronouns folded,
                so a second-person copy of a first-person line still counts): handing the line back
  self_repeat   a content word 3-gram said twice in one answer, or one content word 4+ times
  copy_style, copy_reminder, copy_prompt, setting_word, lesson_lost
                auto_metrics.checks (instruction text lifted into the answer, lesson dropped)
  hour_clash    the neutral gives an hour and the answer names one of the other half of the day
  words         length

Across samples: the opening-three-word tics (an opening shared by more than one answer in eight).
"""
import argparse, collections, json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import person_lib as L      # noqa: E402
import auto_metrics as AM   # noqa: E402

STOP = AM.STOP | set("is am are was were be been being has have had do did will would can could should into onto "
                     "upon over under there here this that these those what which who whom how why when where "
                     "not no nor but or so yet than then very just also more most some any each every such own "
                     "same other another around about through thanks success done".split())
PRON1 = re.compile(r"\b(i|me|my|mine|myself)\b", re.I)
PRON2 = re.compile(r"\b(you|your|yours|yourself)\b", re.I)
FOLD = {"i": "P", "me": "P", "my": "Pp", "mine": "Pp", "myself": "Px", "you": "P", "your": "Pp", "yours": "Pp",
        "yourself": "Px", "am": "BE", "are": "BE", "was": "BEp", "were": "BEp"}
COUNSEL = re.compile(r"\byou (should|must|need to|ought to|had better|'d better)\b|\blet us\b|\blet's\b", re.I)
IMPER = re.compile(r"(^|[.!?;:]\s+|\s[-\u2014]\s)(look|see|listen|go|come|take|trust|mind|watch|step|keep|let|feel|hold|leave|move|stay|wait|remember|breathe|climb|ease|careful|easy|steady|now|quick|embrace|beware|note|mark|consider|heed|smell|touch|grab|strike|reach|press|turn|follow|seize)\b(?! (to|of)\b)", re.I)
DAY = re.compile(r"\b(morning|dawn|daybreak|sunrise|noon|midday|afternoon|daylight|sunlit|sunshine)\b", re.I)
NIGHT = re.compile(r"\b(night|nightfall|midnight|moon|moonlight|starlight|stars|evening|dusk|twilight|darkness falls)\b", re.I)


def words(s):
    return re.findall(r"[a-z']+", (s or "").lower())


def content(ws):
    return [w for w in ws if w not in STOP and len(w) > 2 and not PRON1.fullmatch(w) and not PRON2.fullmatch(w)]


def fold(ws):
    return [FOLD.get(w, w) for w in ws]


def sample_metrics(case, s):
    text, neutral = s["text"], s["neutral"]
    tw, nw = words(text), words(neutral)
    n1, n2 = len(PRON1.findall(text)), len(PRON2.findall(text))
    style = (s.get("_style") or "")
    style_you = len(PRON2.findall(style))
    m = {}
    if s["version"] == "first":
        m["voice_ok"] = text.startswith("I") and (n2 == 0 or (style_you and n2 <= 1))
        m["voice_slip"] = None
    else:
        m["voice_ok"] = n2 > 0 and n2 >= n1
        m["voice_slip"] = text.startswith("I ") or n2 == 0
    # recall of the neutral's content words, by 5-letter stem
    stems = {w[:5] for w in content(nw)}
    have = {w[:5] for w in content(tw)}
    m["recall"] = len(stems & have) / len(stems) if stems else 1.0
    # parrot: answer 4-grams found in the neutral (pronouns and be folded)
    a4, n4 = AM.ngrams(fold(tw), 4), AM.ngrams(fold(nw), 4)
    m["parrot"] = len(a4 & n4) / len(a4) if a4 else 0.0
    # self repetition
    cw = content(tw)
    c3 = [tuple(cw[i:i + 3]) for i in range(len(cw) - 2)]
    m["self_repeat"] = (len(c3) != len(set(c3))) or any(v >= 4 for v in collections.Counter(cw).values())
    # instruction copies, lesson loss: the corpus checks, on this version's neutral and persona
    c2 = dict(case, record=dict(case["record"], neutralText=neutral),
              persona=dict(case["persona"], style=s.get("_style"), reminder2=s.get("_reminder")))
    ch = AM.checks(c2, s)
    for k in ("copy_style", "copy_reminder", "copy_prompt", "setting_word", "lesson_lost"):
        m[k] = ch.get(k)
    # an hour named against the neutral's
    nd, nn = bool(DAY.search(neutral)), bool(NIGHT.search(neutral))
    m["hour_clash"] = (nd and not nn and bool(NIGHT.search(text))) or (nn and not nd and bool(DAY.search(text))) \
        if (nd or nn) else None
    # the inner voice counselling or urging: "you should", "you must", or a sentence opening on an
    # imperative ("Look", "Go", "Trust your…")
    m["counsel"] = bool(COUNSEL.search(text) or IMPER.search(text))
    m["words"] = len(tw)
    return m


def opening(text, n=3):
    return " ".join(words(text)[:n])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sets", nargs="+", default=["dev"])
    ap.add_argument("--json")
    a = ap.parse_args()
    cases = {c["id"]: c for c in (json.loads(l) for l in open(os.path.join(HERE, "..", "cases.jsonl")))}
    rows = []
    for st in a.sets:
        for l in open(os.path.join(HERE, "samples", f"{st}.jsonl")):
            s = json.loads(l)
            c = cases[s["case"]]
            p = L.current_persona(c)
            s["_style"], s["_reminder"] = p.get("style"), p.get("reminder2")
            s["set"], s["kind"] = st, c["record"]["kind"]
            s["m"] = sample_metrics(c, s)
            rows.append(s)
    keys = ["voice_ok", "voice_slip", "recall", "parrot", "self_repeat", "copy_style", "copy_reminder",
            "copy_prompt", "setting_word", "lesson_lost", "hour_clash", "counsel", "words"]
    summary = {}
    for group in ["all"] + list(L.KINDS):
        print(f"\n== {group}")
        print(f"{'version':10} {'n':>4} {'tok':>5} {'wall':>5} " + " ".join(f"{k[:11]:>11}" for k in keys))
        for v in L.VERSIONS:
            rs = [r for r in rows if r["version"] == v and (group == "all" or r["kind"] == group)]
            if not rs:
                continue
            cells, sm = [], {}
            for k in keys:
                vals = [r["m"][k] for r in rs if r["m"][k] is not None]
                x = sum(float(b) for b in vals) / len(vals) if vals else None
                sm[k] = x
                cells.append(f"{'-':>11}" if x is None else (f"{x:11.1f}" if k == "words" else f"{100 * x:10.0f}%"))
            sm["n"] = len(rs)
            sm["tok"] = sum(r["user_tokens"] for r in rs) / len(rs)
            sm["wall"] = sum(r["wall"] for r in rs) / len(rs)
            summary[(group, v)] = sm
            print(f"{v:10} {len(rs):4} {sm['tok']:5.0f} {sm['wall']:5.1f} " + " ".join(cells))
    print("\n== openings shared by more than 1 in 8 answers")
    for v in L.VERSIONS:
        rs = [r for r in rows if r["version"] == v]
        cnt = collections.Counter(opening(r["text"]) for r in rs)
        tics = [(o, n) for o, n in cnt.most_common(6) if n > len(rs) / 8]
        top = ", ".join(f"'{o}' {n}" for o, n in cnt.most_common(4))
        print(f"{v:10} tics: {tics or 'none'}   top: {top}")
    if a.json:
        json.dump(dict(summary={f"{g}|{v}": m for (g, v), m in summary.items()},
                       samples=[dict(case=r["case"], version=r["version"], seed=r["seed"], set=r["set"], kind=r["kind"],
                                     text=r["text"], neutral=r["neutral"], persona=r["persona"], wall=r["wall"],
                                     user_tokens=r["user_tokens"], m=r["m"]) for r in rows]),
                  open(a.json, "w"), indent=0)


if __name__ == "__main__":
    main()
