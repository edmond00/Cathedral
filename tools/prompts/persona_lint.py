"""The persona lint: which modi mentis's style or reminder is written as a list of things.

    python tools/prompts/persona_lint.py [--list] [--mm avarice] [--text "a style to check"]

The rules it checks were measured (NOTES.md, "Persona fields"): a style or reminder that names the
persona's own objects and acts puts them into scenes that hold none of them, and the craft personas
replace the act outright ("I will wipe the blade, oil it…" for a greeting). The two rules:

1. **A style is a manner, then a lens on whatever is in the scene** — "Slow, patient and exact - judge
   things by the patience in them, well made or rank" — never a list of the persona's own things
   ("Use images of rendering fat, the dip and the cooling coat, wicks, wax and the steady flame").
2. **A reminder is a disposition, not a picture** — "someone who listens before looking", not
   "someone who finds the hidden nest by listening".

A field is flagged when it names THINGS_MAX or more concrete things, or (a style) opens a list of
props with "images of", "narrate the…". A word's concreteness is predicted from its GloVe vector by
data/concreteness_glove100.json (a linear fit to human ratings, held-out r = 0.79), and whether it
is a noun by a second fit in the same file (accuracy 0.83), so the game can run the same check with
the vectors it already loads; the C# twin is PersonaLint.cs. A distinct noun rated 3.8 or more counts
as a thing (oil 4.9, wick 4.7, silver 4.5; delay 2.8, patience 2.1).
"""
import argparse, json, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
import personas

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
THING = 3.8
NOUN = 0.5
STYLE_THINGS_MAX = 2
REMINDER_THINGS_MAX = 2
# Words a style is written with rather than about.
BOILER = set("""colour color line lines image images imagery picture voice tone words word sentence sentences
thing things way back dry clipped reach let keep make give take put feel feeling
front around whatever whoever judge see read look meet treat notice small next old single open""".split())
STOP = set("""a about above after again against all also am an and any are as at be because been before being below
between both but by can could did do does doing down during each even ever every few for from further had has have
having he her here hers him his how i if in into is it its just me more most my no nor not now of off on once only or
other our out over own same she should so some such than that the their them then there these they this those through
to too under until up upon very was we were what when where which while who whom why will with would yet you your one
ones something nothing anything everything still though much many must might""".split())
PROPS = re.compile(r"\b(images? of|imagery of|narrate (the|by|through)|describe (the|smoke|water)|use (the )?images)\b", re.I)


class Lint:
    def __init__(self, words=None):
        m = json.load(open(os.path.join(HERE, "data", "concreteness_glove100.json")))
        self.w, self.b = m["weights"], m["bias"]
        self.nw, self.nb = m["noun"]["weights"], m["noun"]["bias"]
        self.vec = {}
        path = os.path.join(ROOT, "models", "embeddings", "glove.6B.100d.txt")
        for line in open(path, encoding="utf-8"):
            word, rest = line.split(" ", 1)
            if words is None or word in words:
                self.vec[word] = [float(x) for x in rest.split()]

    def concreteness(self, word):
        v = self.vec.get(word)
        return None if v is None else self.b + sum(a * b for a, b in zip(self.w, v))

    def noun(self, word):
        v = self.vec.get(word)
        return v is not None and self.nb + sum(a * b for a, b in zip(self.nw, v)) >= NOUN

    def things(self, text):
        """The distinct concrete nouns a field names."""
        ws = dict.fromkeys(w for w in re.findall(r"[a-z]+", (text or "").lower())
                           if len(w) >= 3 and w not in STOP and w not in BOILER)
        return [w for w in ws if (self.concreteness(w) or 0) >= THING and self.noun(w)]

    def check(self, mm):
        out = []
        st = self.things(mm.get("style"))
        if len(st) >= STYLE_THINGS_MAX or PROPS.search(mm.get("style") or ""):
            out.append(("style", st, bool(PROPS.search(mm.get("style") or ""))))
        rt = self.things(mm.get("reminder2"))
        if len(rt) >= REMINDER_THINGS_MAX:
            out.append(("reminder", rt, False))
        return out


def words_of(mms):
    return {w for m in mms.values() for k in ("style", "reminder2") for w in re.findall(r"[a-z]+", (m.get(k) or "").lower())}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true", help="every flagged modus mentis, one line each")
    ap.add_argument("--mm")
    ap.add_argument("--text")
    ap.add_argument("--json", help="write {mm: [fields]} of the flagged ones")
    a = ap.parse_args()
    mms = personas.load_modi_mentis()
    words = words_of(mms) | set(re.findall(r"[a-z]+", (a.text or "").lower()))
    L = Lint(words)
    if a.text:
        print(L.things(a.text), bool(PROPS.search(a.text)))
        return
    flagged = {m: L.check(mm) for m, mm in sorted(mms.items()) if (not a.mm or m == a.mm)}
    flagged = {m: f for m, f in flagged.items() if f}
    if a.list or a.mm:
        for m, f in flagged.items():
            print(m, "; ".join(f"{k}: {', '.join(t)}{' +props phrase' if p else ''}" for k, t, p in f))
    n_style = sum(any(k == "style" for k, _, _ in f) for f in flagged.values())
    n_rem = sum(any(k == "reminder" for k, _, _ in f) for f in flagged.values())
    print(f"{len(mms)} modi mentis: {n_style} styles and {n_rem} reminders written as things")
    if a.json:
        json.dump({m: [k for k, _, _ in f] for m, f in flagged.items()}, open(a.json, "w"), indent=0)


if __name__ == "__main__":
    main()
