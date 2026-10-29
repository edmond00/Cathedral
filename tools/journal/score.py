"""Score the chronicler's outputs and print the variant table.

    python tools/journal/score.py outputs.jsonl [--manual manual_scores.json] [--samples 6]

Automated, per output:
  invented   proper names in the paragraph found nowhere in its notes (fidelity alarm; read them)
  coverage   share of the group's milestones the paragraph mentions
  copied     share of the paragraph's word 6-grams lifted verbatim from the notes
  repeat     share of its word 4-grams already in the previous paragraph of the same chain
  voice      share of paragraphs in the expected person (first for a diary, third otherwise)
  words      length
Cost: prompt and generated tokens, and seconds of generation, per job.

Manual (the rubric in tools/journal/README.md, scored by reading): fidelity, coverage, readability and
voice, 1-5 each, keyed "<grouping>/<variant>/<source>/<group>" in the manual file. Averaged per variant
where present.
"""
import argparse, json, statistics as st
from collections import defaultdict
import journal_lib as J

ap = argparse.ArgumentParser()
ap.add_argument("outputs"); ap.add_argument("--manual", default=None)
ap.add_argument("--samples", type=int, default=0)
a = ap.parse_args()

rows = [json.loads(l) for l in open(a.outputs, encoding="utf-8")]
manual = json.load(open(a.manual)) if a.manual else {}
by = defaultdict(list)
for r in rows: by[(r["axis"], r["variant"])].append(r)

def key(r): return f"{r['grouping']}/{r['variant']}/{r['source']}/{r['group']}"

print(f"| axis | variant | n | invented/para | coverage | copied | repeat | voice | words | manual (fid/cov/read/voice) | prompt tok | gen tok | gen s/job |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|---|")
order = ["shipped", "voice", "inputs", "grouping", "memory", "length"]
for (axis, variant), rs in sorted(by.items(), key=lambda kv: (order.index(kv[0][0]) if kv[0][0] in order else 9, kv[0][1])):
    rs.sort(key=lambda r: (r["source"], r["group"]))
    inv = [len(J.invented_names(r["output"], r)) for r in rs]
    cov = [c for c in (J.milestone_coverage(r["output"], r) for r in rs) if c is not None]
    cop = [J.copied_share(r["output"], r) for r in rs]
    rep = [J.ngram_overlap(p["output"], r["output"]) for p, r in zip(rs, rs[1:]) if p["source"] == r["source"]]
    want = "first" if rs[0]["voice"].startswith("Diary") else "third"
    voice = sum(J.person(r["output"]) == want for r in rs) / len(rs)
    words = [len(r["output"].split()) for r in rs]
    man = [manual[key(r)] for r in rs if key(r) in manual]
    mtxt = "/".join(f"{st.mean(m[i] for m in man):.1f}" for i in range(4)) if man else "–"
    gen_s = [(r["cost"]["prompt_ms"] + r["cost"]["gen_ms"]) / 1000 for r in rs]
    print(f"| {axis} | {variant} | {len(rs)} | {st.mean(inv):.2f} | {st.mean(cov) if cov else 0:.2f} | {st.mean(cop):.2f} | "
          f"{st.mean(rep) if rep else 0:.2f} | {voice:.2f} | {st.mean(words):.0f} | {mtxt} | "
          f"{st.mean(r['cost']['prompt_n'] for r in rs):.0f} | {st.mean(r['cost']['gen_n'] for r in rs):.0f} | {st.mean(gen_s):.1f} |")

if a.samples:
    print("\n## Samples (shipped)\n")
    for r in [r for r in rows if r["variant"] == "shipped"][:a.samples]:
        print(f"### {r['source']} group {r['group']}\n\n**Notes sent:**\n\n```\n{r['sent_user']}\n```\n\n**Written:** {r['output']}\n")
        names = J.invented_names(r["output"], r)
        if names: print(f"*Names not in the notes:* {', '.join(names)}\n")
