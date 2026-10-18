"""Pulls observation rewrites out of logs/ to grow the keyword dataset: every request whose neutral
sentence opens "My attention is drawn to / shifts to / My memory drifts to X.", with the model's reply.

    python tools/keywords/extract.py new_raw.jsonl
    dotnet run --project tools/keywords/kwtag -- bin/Debug/net8.0-windows/catalyst-models new_raw.jsonl new_tags.jsonl

then label the new ids in labels.txt and fold raw + tags into cases.jsonl (toks as [surface, lemma,
POS], cands as the tagger wrote them). Old replies wrapped in JSON must be unwrapped first.
"""
import os, re, json, glob, sys
HERE = os.path.dirname(os.path.abspath(__file__))
LOGS = os.path.join(HERE, "..", "..", "logs")
out = []; seen = set()
pat = re.compile(r'own voice: "(My (?:attention is drawn to|attention shifts to|memory drifts to) (.+?)\. (.*?))"\s*\r?\n', re.S)
for um in glob.glob(os.path.join(LOGS, "llm_session_*", "slot_*", "request_*", "user_message.txt")):
    d = os.path.dirname(um)
    try:
        u = open(um, encoding="utf-8").read()
        m = pat.search(u)
        if not m: continue
        r = open(os.path.join(d, "llm_response.txt"), encoding="utf-8").read().strip()
    except Exception: continue
    if not r or r in seen: continue
    seen.add(r)
    np_ = re.sub(r"^(a|an|the|some)\s+", "", m.group(2).strip(), flags=re.I)
    out.append(dict(src=os.path.relpath(d, LOGS), name=np_, neutral=m.group(1), text=r))
out.sort(key=lambda x: x["src"])
with open(sys.argv[1], "w", encoding="utf-8") as f:
    for i, o in enumerate(out): o["id"] = i; f.write(json.dumps(o) + "\n")
print(len(out), "cases;", len({o['name'] for o in out}), "distinct objects")
