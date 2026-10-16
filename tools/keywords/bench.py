"""Scores keyword-picking methods against the labelled real observations (kwlib). Each method returns
the draw it would make, as (word, probability) pairs, so a random pool is scored by its exact expectation.
"current" is KeywordExtractor as shipped. Seconds, no model needed beyond the GloVe file.

    python tools/keywords/bench.py          # every method
    python tools/keywords/bench_sweep.py    # pool threshold sweep, people vs things, weird words drawn
    python tools/keywords/bench_hybrids.py  # per-kind hybrids and the second keyword of long texts
"""
import sys, re, numpy as np
from kwlib import *
CS = cases()
THR = 0.25

def own_word(c, words):
    for w in words:
        if not w: continue
        for s, l in c["cands"]:
            if l.lower() == w or s.lower() == w: return s
    return None
def scored(c, anchor, cands=None):
    out = []
    for s, l in (cands or c["cands"]):
        v = vec(l)
        if v is None: continue
        out.append((s, cos(anchor, v) - centrality(l)))
    return sorted(out, key=lambda x: -x[1])
def pool_pick(c, sc, own):
    pool = [s for s, x in sc if x >= THR]
    if own and own not in pool: pool.append(own)
    if pool: return [(w, 1 / len(pool)) for w in pool]
    if sc: return [(sc[0][0], 1)]
    return [(max((s for s, _ in c["cands"]), key=len), 1)] if c["cands"] else []
def current(c):
    """KeywordExtractor today: candidates scored cos(ref)-centrality, pool >= 0.25 plus the own word
    (head word of the neutral name, else the reference lemma), drawn uniformly."""
    rv = vec(c["ref"]); own = own_word(c, [head_word(c["name"]), c["ref"]])
    if rv is None: return [(own, 1)] if own else pool_pick(c, [], None)
    return pool_pick(c, scored(c, rv), own)

# --- signals ---------------------------------------------------------------
def name_words(c):
    """The words of the neutral name that name the object: an acquaintance's name and role, a
    person's role and sex, never the 'of the field' tail."""
    n = c["name"]
    m = re.match(r"my acquaintance (\w+), the (.+?) of the", n)
    if m: return [m.group(1).lower()] + m.group(2).lower().split()
    n = re.sub(r"\bof the [^(]*", "", n).replace("young ", "")
    return [w.lower() for w in re.findall(r"[A-Za-z]+", n) if w.lower() not in ("and", "s", "a")]
DESC_STOP = set("this is a an the of with and in on to at by from its it his her their there here my i am as for into out up".split())
def desc_words(c):
    """Words of the neutral description, the sentence(s) after the attention line."""
    d = c["neutral"].split(". ", 1)[1] if ". " in c["neutral"] else ""
    return {w.lower() for w in re.findall(r"[A-Za-z][A-Za-z-]+", d)} - DESC_STOP
def lem_in(l, s, S):
    l = l.lower(); s = s.lower()
    return l in S or s in S or s.rstrip("s") in S or (s + "s") in S
def hits(c, S): return [s for s, l in c["cands"] if lem_in(l, s, S)]
def uniform(ws): return [(w, 1 / len(ws)) for w in ws]

def m_name(c):
    h = hits(c, set(name_words(c))); return uniform(h) if h else current(c)
def m_name_desc(c, fallback=current):
    h = hits(c, set(name_words(c)))
    if h: return uniform(h)
    h = hits(c, desc_words(c))
    return uniform(h) if h else fallback(c)
def m_name_desc_pool(c):
    h = hits(c, set(name_words(c)) | desc_words(c)); return uniform(h) if h else current(c)
def anchor_mean(words):
    vs = [vec(w) for w in words if vec(w) is not None]
    return np.mean([v / np.linalg.norm(v) for v in vs], axis=0) if vs else None
def m_anchor_name(c):
    a = anchor_mean(name_words(c) + (["body"] if c["person"] else []))
    return current(c) if a is None else pool_pick(c, scored(c, a), own_word(c, name_words(c)))
def m_anchor_desc(c):
    a = anchor_mean(name_words(c) + sorted(desc_words(c)))
    return current(c) if a is None else pool_pick(c, scored(c, a), own_word(c, name_words(c)))
def m_name_desc_anchor(c): return m_name_desc(c, fallback=m_anchor_desc)
def m_nouns(c):
    nouns = {t["s"].lower() for t in c["toks"] if t["p"] in ("NOUN", "PROPN")}
    cc = [x for x in c["cands"] if x[0] in nouns] or c["cands"]
    rv = vec(c["ref"])
    return current(c) if rv is None else pool_pick(c, scored(c, rv, cc), own_word(c, [head_word(c["name"]), c["ref"]]))
def m_top1(c):
    rv = vec(c["ref"]); sc = scored(c, rv) if rv is not None else []
    return [(sc[0][0], 1)] if sc else current(c)
def m_rawcos(c):
    rv = vec(c["ref"])
    if rv is None: return current(c)
    sc = sorted(((s, cos(rv, vec(l))) for s, l in c["cands"] if vec(l) is not None), key=lambda x: -x[1])
    return [(sc[0][0], 1)] if sc else current(c)

M = {"current": current, "top1, no draw": m_top1, "raw cosine top1": m_rawcos, "nouns only": m_nouns,
     "name words": m_name, "name, then description": m_name_desc, "name+description pool": m_name_desc_pool,
     "anchor = name words": m_anchor_name, "anchor = name+description": m_anchor_desc,
     "name, desc, then anchor": m_name_desc_anchor}
def evaluate(f, cs=None):
    cs = cs or CS; g2 = g1 = 0; per = []
    for c in cs:
        dist = f(c); e2 = sum(w for k, w in dist if grade(c, k) == 2); e1 = sum(w for k, w in dist if grade(c, k) >= 1)
        g2 += e2; g1 += e1; per.append((c, dist, e1))
    return g2 / len(cs), g1 / len(cs), per
if __name__ == "__main__":
    n = len(CS); print(f"{n} cases ({sum(c['person'] for c in CS)} people)\n")
    print(f"{'method':28} {'names it':>9} {'acceptable':>11} {'weird':>7}")
    for k, f in M.items():
        a, b, _ = evaluate(f); print(f"{k:28} {a:9.1%} {b:11.1%} {1 - b:7.1%}")
    print(f"{'(ceiling)':28} {sum(any(grade(c, s) == 2 for s, _ in c['cands']) for c in CS) / n:9.1%} "
          f"{sum(any(grade(c, s) >= 1 for s, _ in c['cands']) for c in CS) / n:11.1%}")
