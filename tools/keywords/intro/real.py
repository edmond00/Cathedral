# The area introductions actually played, from logs/: neutral text, the listed sights, and the reply.
# The pool is only what was listed (the rest of the area's objects are not in the log); lemmas are
# approximated from the names, as in tools/keywords.
import glob, json, os, re, sys
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(HERE, ".."))
from kwlib import ref_lemma, PEOPLE
seen = set(); out = []
for um in sorted(glob.glob(os.path.join(ROOT, "logs", "llm_session_*", "slot_*", "request_*", "user_message.txt"))):
    u = open(um, encoding="utf-8").read()
    m = re.search(r'own voice: "(I find myself (.+?)"\s*\n)', u, re.S)
    if not m: continue
    neutral = m.group(1).strip().rstrip('"').strip()
    a = re.search(r"Around me (?:are|is) (.+)\.$", neutral)
    if not a: continue
    r = open(os.path.join(os.path.dirname(um), "llm_response.txt"), encoding="utf-8").read().strip()
    if r in seen: continue
    seen.add(r)
    body = a.group(1)
    # split "x, y and z" without breaking "my acquaintance Kevin, the plowman of the field"
    body = re.sub(r"(acquaintance \w+), the", r"\1 _COMMA_ the", body)
    parts = body.split(", ")
    parts = parts[:-1] + parts[-1].rsplit(" and ", 1)
    sights = [p.replace("_COMMA_", ",").strip() for p in parts if p.strip()]
    pool = []
    for s in sights:
        name = re.sub(r"^(a|an|the|some)\s+", "", s)
        person = bool(PEOPLE.search(s)) or name in ("stranger",)
        pool.append(dict(phrase=s, name=name, lemma=ref_lemma(name, person), person=person, type="?"))
    out.append(dict(key=re.search(r"You are in an? ([^—]+) —", u).group(1).strip() if "You are in" in u else "?",
                    src=os.path.relpath(os.path.dirname(um), os.path.join(ROOT, "logs")), neutral=neutral, text=r,
                    pool=pool, sights=list(range(len(pool))), real=True))
with open(sys.argv[1], "w", encoding="utf-8") as f:
    for o in out: f.write(json.dumps(o, ensure_ascii=False) + "\n")
print(len(out))
