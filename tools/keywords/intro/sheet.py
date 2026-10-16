# Labelling sheet: per case, the reply, the pool (indexed; * = listed in the introduction) and the
# game's keyword candidates. A label line is `id | word=i[,j] word=i ...`: every candidate word that
# names a pool object (several indices when the text cannot tell which of them it means).
import json, sys
cases = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8")]
tags = {json.loads(l)["id"]: json.loads(l) for l in open(sys.argv[2], encoding="utf-8")}
prefix = sys.argv[3]
with open(sys.argv[4], "w", encoding="utf-8") as f:
    for i, c in enumerate(cases):
        cid = f"{prefix}{i}"
        f.write(f"### {cid}  [{c['key']}]\n{c['text']}\n")
        for k, p in enumerate(c["pool"]):
            f.write(f"   {k}{'*' if k in c['sights'] else ' '} {p['name']}  ({p['lemma']})\n")
        f.write("   cands: " + " ".join(s for s, _ in tags[i]["cands"]) + "\n\n")
