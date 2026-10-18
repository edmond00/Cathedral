"""Threshold sweep of the current pool, name-word combinations, people vs things, and the weird words
the current method draws most. See bench.py."""
import collections as co
from bench import *

def pool_size(f):
    return sum(len(f(c)) for c in CS) / len(CS)
def sweep():
    global THR
    print(f"{'threshold':>9} {'names it':>9} {'acceptable':>11} {'pool':>6}")
    for t in (0.25, 0.3, 0.35, 0.4, 0.45, 0.5, 0.6, 0.7, 9):
        THR = t
        import bench; bench.THR = t
        a, b, _ = evaluate(current); print(f"{t:9} {a:9.1%} {b:11.1%} {pool_size(current):6.2f}")
    bench.THR = 0.25

def m_name_top1(c):
    h = hits(c, set(name_words(c)))
    return uniform(h) if h else m_top1(c)
def m_name_top1_desc(c):
    h = hits(c, set(name_words(c)))
    if h: return uniform(h)
    h = hits(c, desc_words(c))
    return uniform(h) if h else m_top1(c)
def m_name_plus_strong(c, t=0.5):
    """Name words, plus any candidate scoring >= t against the reference lemma: some variety."""
    h = hits(c, set(name_words(c)))
    rv = vec(c["ref"]); sc = scored(c, rv) if rv is not None else []
    pool = h + [s for s, x in sc if x >= t and s not in h]
    if pool: return uniform(pool)
    return [(sc[0][0], 1)] if sc else current(c)

if __name__ == "__main__":
    sweep(); print()
    for k, f in {"name, else top1": m_name_top1, "name, desc, else top1": m_name_top1_desc,
                 "name + strong(.5) pool": m_name_plus_strong,
                 "name + strong(.6) pool": lambda c: m_name_plus_strong(c, .6),
                 "name + strong(.4) pool": lambda c: m_name_plus_strong(c, .4)}.items():
        a, b, _ = evaluate(f); print(f"{k:28} {a:9.1%} {b:11.1%} {1 - b:7.1%}  pool {pool_size(f):.2f}")
    print("\npeople only / things only:")
    P = [c for c in CS if c["person"]]; T = [c for c in CS if not c["person"]]
    for k, f in {"current": current, "top1": m_top1, "name words": m_name, "name, else top1": m_name_top1}.items():
        a1, b1, _ = evaluate(f, P); a2, b2, _ = evaluate(f, T)
        print(f"{k:20} people {b1:6.1%} acceptable   things {b2:6.1%} acceptable")
    _, _, per = evaluate(current)
    bad = co.Counter()
    for c, dist, e1 in per:
        for k, w in dist:
            if grade(c, k) == 0: bad[k.lower()] += w
    print("\ncurrent: the weird words it draws most (expected count):")
    print(", ".join(f"{k} {v:.1f}" for k, v in bad.most_common(40)))
