"""How often the setting rule's own words come back as narration, and whether a rewording stops it.

Replays rewrites the game recorded (logs/llm_session_*/rewrite_inputs.jsonl, or a corpus run's copy)
against the shipped model, with the setting rule (SceneSetting.Rule, closing the system prompt) and the
per-request reminder (SceneSetting.Reminder, closing the prompt) swapped for a variant's, everything
else byte for byte as the game sent it. Each record is sampled with the same seeds under every variant,
so the variants pair up.

A leak is a word of the rule or the reminder written into the answer that the neutral line did not
carry: "in this old world of iron, myth and hand labour", "without modern or real-world details",
"nothing from the real world". The words are listed in LEAK; each variant's own wording is added to it
(VARIANTS[...]['words']), so a variant cannot score well by leaking words the list does not know.

    python tools/prompts/setting_leak.py --records <rewrite_inputs.jsonl>... [--variants a b] [--n 4]
           [--kinds Observation,Outcome] [--out results/setting_leak.jsonl] [--show]

The childhood records are in data/childhood/ (childhood.cli plays more); the exploration ones in
runs/*/session/. A variant name ending "-place" also drops the place clause. NOTES.md, "The setting rule
leaking into narration", has what was measured.

Needs a llama-server on PROMPT_PORT (serve.sh).
"""
import argparse, collections, json, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P

SS = P._read("src/game/narrative/SceneSetting.cs")
RULE_NOW = P._cs_string(SS, "Rule")
REMINDER_NOW = re.search(r'const string setting = "((?:[^"\\]|\\.)*)";', SS).group(1)

# Words of every wording tried; matched only when the neutral line does not carry them.
LEAK = [r"\bold world\b", r"\bmyths?\b", r"\bhand[- ]?labou?r", r"\breal[- ]world\b", r"\bmodern\b",
        r"\bmedieval\b", r"\bnothing from\b", r"\banachronis", r"\bworld of iron\b", r"\biron and\b",
        r"\bfrom after\b", r"\bbrand names?\b"]

_REMINDER = re.compile(r" Remember: [^.]*\.")
_PLACE = re.compile(r" You are in an? [^—]+ — write only what belongs there\.")
_BANS = ("no machines, engines, guns, bullets, photographs, albums, offices, factories, hospitals, schools, "
         "plastic, rubber, electricity, chemistry, brand names, and no real country, city, people or date. ")
# The rule's first sentence, which names the world, dropped: only what may not be written is said.
RULE_BANS = ("Never write anything that could not exist before engines and gunpowder, or anything from the "
             "real world: " + _BANS + "If a thing has no name before engines, do not mention the thing at all.")

VARIANTS = {
    # As the source has it now.
    "now": dict(rule=RULE_NOW, reminder=REMINDER_NOW),
    # The wording the leak was reported under (before 2026-11), kept so it can be measured again.
    "iron": dict(rule=RULE_NOW, reminder="Remember: an old world of iron and hand labour, nothing from the real world."),
    # The reminder points back at the rule instead of restating its picture.
    "point_back": dict(rule=RULE_NOW, reminder="Remember the setting rule."),   # kept: "now"
    # The reminder as a ban, no world named.
    "ban": dict(rule=RULE_NOW, reminder="Remember: nothing from after engines and gunpowder, and no real place, "
                                        "person or date.", words=[r"\bengines?\b", r"\bgunpowder\b"]),
    # The ban, and the rule without its picture.
    "ban+rule": dict(rule=RULE_BANS, reminder="Remember: nothing from after engines and gunpowder, and no real "
                                             "place, person or date.", words=[r"\bengines?\b", r"\bgunpowder\b"]),
    # The world sentence of the reminder dropped altogether; the place clause kept.
    "no_reminder": dict(rule=RULE_NOW, reminder=""),
}
for _k in list(VARIANTS):   # every wording again, without the place clause (childhood is not on the plain)
    VARIANTS[_k + "-place"] = dict(VARIANTS[_k], place_reminder=lambda pr: _PLACE.sub("", pr))


# Layer 1's forbidden words (the first form of each N/V/P entry, a noun's plural too): an answer that
# names one has let something of the real world in. The reminder exists to prevent that, so a variant
# that stops the leak by dropping the reminder must not be paid for in these.
_FW = P._read("src/game/narrative/sanitizer/ForbiddenWords.cs")
_FORMS = set()
for _m in re.finditer(r'\b([NVP])\("([^"]+)"', _FW):
    _w = _m.group(2).lower()
    _FORMS.add(_w)
    if _m.group(1) == "N":
        _FORMS.add(_w + "s")
ANACHRONISM = re.compile(r"\b(" + "|".join(sorted(map(re.escape, _FORMS), key=len, reverse=True)) + r")\b", re.I)


def anachronisms(text, neutral):
    return sorted({m.group(1).lower() for m in ANACHRONISM.finditer(text)} -
                  {m.group(1).lower() for m in ANACHRONISM.finditer(neutral)})


def leak_patterns(extra=()):
    return [re.compile(p, re.I) for p in list(LEAK) + list(extra)]


def leaks(text, neutral, pats):
    return [p.pattern for p in pats if p.search(text) and not p.search(neutral)]


def place_leak(text, record):
    """Words of the reminder's place ("vast plain") written into an answer whose neutral line has none of
    them. Only meaningful where the place is not where the narration happens (the childhood
    reminescence is set on a plain it never mentions)."""
    words = [w for w in re.findall(r"[a-z]+", (record.get("place") or "").lower()) if len(w) > 3]
    stems = [w[:5] for w in words]
    neutral = record["neutralText"].lower()
    return [w for w, st in zip(words, stems) if re.search(r"\b" + st, text.lower()) and st not in neutral]


def swap(record, v):
    """The record's messages with the rule and the reminder replaced by the variant's."""
    hist = record.get("history") or []
    msgs = []
    for m in hist:
        c = m["content"]
        if m["role"] == "system":
            # The rule is the system prompt's last paragraph, whichever wording the record was made under.
            i = c.rfind("Everything you write happens in")
            assert i >= 0, "recorded system prompt lacks the rule"
            c = (c[:i] + v["rule"]).rstrip()
        msgs.append({"role": m["role"], "content": c})
    prompt = record["prompt"]
    m = _REMINDER.search(prompt)
    assert m, "recorded prompt lacks the reminder"
    prompt = (prompt[:m.start()] + (" " + v["reminder"] if v["reminder"] else "") + prompt[m.end():])
    if "place_reminder" in v:   # the place clause rewritten too
        prompt = v["place_reminder"](prompt)
    return msgs + [{"role": "user", "content": prompt}]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--records", nargs="+", required=True)
    ap.add_argument("--variants", nargs="*", default=list(VARIANTS))
    ap.add_argument("--n", type=int, default=4)
    ap.add_argument("--kinds")
    ap.add_argument("--out")
    ap.add_argument("--show", action="store_true")
    a = ap.parse_args()
    recs = []
    for p in a.records:
        for l in open(p):
            r = json.loads(l)
            if a.kinds and r["kind"] not in a.kinds.split(","):
                continue
            if r["kind"] in ("DialogueReplica", "Speaking"):
                continue          # the spoken frame is a different shape; the leak is a narration one
            recs.append(r)
    out = open(a.out, "a") if a.out else None
    tally = collections.defaultdict(collections.Counter)
    for i, r in enumerate(recs):
        for vname in a.variants:
            v = VARIANTS[vname]
            pats = leak_patterns(v.get("words", ()))
            msgs = swap(r, v)
            for s in range(a.n):
                g = P.generate(msgs, r["gbnf"], seed=1000 + s)
                hit = leaks(g["text"], r["neutralText"], pats)
                where = place_leak(g["text"], r)
                modern = anachronisms(g["text"], r["neutralText"])
                tally[vname]["modern"] += bool(modern)
                tally[vname]["n"] += 1
                tally[vname]["leak"] += bool(hit)
                tally[vname][r["kind"] + ".n"] += 1
                tally[vname][r["kind"] + ".leak"] += bool(hit)
                tally[vname]["place"] += bool(where)
                if a.show and (hit or where or modern):
                    print(f"  [{vname}] {r['kind']} {hit} {where} {modern}: {g['text'][:240]}")
                if out:
                    out.write(json.dumps(dict(i=i, variant=vname, seed=1000 + s, kind=r["kind"],
                                              neutral=r["neutralText"], text=g["text"], leak=hit, place_leak=where, modern=modern,
                                              prompt_tokens=g["prompt_tokens"])) + "\n")
                    out.flush()
        print(f"{i + 1}/{len(recs)} " + "  ".join(
            f"{k}={t['leak']}/{t['n']}" for k, t in tally.items()), flush=True)
    print()
    for k, t in tally.items():
        kinds = sorted({x[:-2] for x in t if x.endswith(".n")})
        per = ", ".join(f"{kd} {t[kd + '.leak']}/{t[kd + '.n']}" for kd in kinds)
        print(f"{k:20s} setting {t['leak']:3d}/{t['n']:3d} = {100 * t['leak'] / max(t['n'], 1):5.1f}%   "
              f"place {t['place']:3d} = {100 * t['place'] / max(t['n'], 1):5.1f}%   "
              f"modern {t['modern']:3d}   ({per})")


if __name__ == "__main__":
    main()
