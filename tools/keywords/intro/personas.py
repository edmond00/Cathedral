# Collects real rewrite requests (system prompt + user message) from October logs, one per persona, as
# templates: the quoted neutral sentence and the place name are what a generated intro substitutes.
import glob, json, os, re, sys
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
out = {}
for fc in sorted(glob.glob(os.path.join(ROOT, "logs", "llm_session_2026-10-*", "slot_*", "request_*", "full_context.json"))):
    try: msgs = json.load(open(fc, encoding="utf-8"))
    except Exception: continue
    if len(msgs) != 2 or msgs[0]["role"] != "system": continue
    u = msgs[1]["content"]
    m = re.match(r'Re-express the following sentence in your own voice: "((?:My attention is drawn to|I find myself)[^"]*)"', u)
    if not m: continue
    if not re.search(r"You are in an? [^—]+ — write only what belongs there\.", u): continue
    persona = msgs[0]["content"].split(",")[0][:80]
    g = os.path.join(os.path.dirname(fc), "gbnf_constraints.txt")
    out[persona] = dict(system=msgs[0]["content"], user=u.replace(m.group(1), "<<NEUTRAL>>"),
                        grammar=open(g, encoding="utf-8").read() if os.path.exists(g) else None)
json.dump(list(out.values()), open(sys.argv[1], "w", encoding="utf-8"), indent=1)
print(len(out), "personas")
