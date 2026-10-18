"""Hybrids by kind of object (people vs things), and the second keyword of a long observation. See bench.py."""
import random
from bench import *

def ranked(c, anchor_words=None):
    rv = vec(c["ref"]); return [s for s, _ in scored(c, rv)] if rv is not None else [s for s, _ in c["cands"]]
def head_hit(c):
    h = head_word(re.sub(r"\(.*?\)", "", c["name"]))
    return [s for s, l in c["cands"] if lem_in(l, s, {h, c["ref"]})]
def h1(c):
    if c["person"]:
        h = hits(c, set(name_words(c))); return uniform(h) if h else m_top1(c)
    return m_top1(c)
def h2(c):
    if c["person"]:
        h = hits(c, set(name_words(c))); return uniform(h) if h else m_top1(c)
    h = head_hit(c); return uniform(h[:1]) if h else m_top1(c)

def order(c, f):
    """A full ranking for a method: its draw first (sampled), then the reference ranking."""
    dist = f(c); first = [k for k, _ in dist]
    random.Random(c["id"]).shuffle(first)
    rest = [s for s in ranked(c) if s not in first]
    return first + rest
def second_eval(f):
    long_ = [c for c in CS if len(c["text"]) > 400]
    both = sum(all(grade(c, w) >= 1 for w in order(c, f)[:2]) for c in long_)
    sec = sum(grade(c, order(c, f)[1]) >= 1 for c in long_ if len(order(c, f)) > 1)
    return len(long_), both / len(long_), sec / len(long_)

def h3(c, t=0.5):
    """People: name words. Things: every candidate scoring >= t, plus the head word, drawn uniformly."""
    if c["person"]:
        h = hits(c, set(name_words(c))); return uniform(h) if h else m_top1(c)
    rv = vec(c["ref"]); sc = scored(c, rv) if rv is not None else []
    pool = [s for s, x in sc if x >= t]
    for s in head_hit(c)[:1]:
        if s not in pool: pool.append(s)
    return uniform(pool) if pool else m_top1(c)
def things_pool(f):
    T = [c for c in CS if not c["person"]]; return sum(len(f(c)) for c in T) / len(T)

if __name__ == "__main__":
    for k, f in {"H3 things pool >= .5": h3, "H3 things pool >= .4": lambda c: h3(c, .4),
                 "H3 things pool >= .6": lambda c: h3(c, .6)}.items():
        a, b, _ = evaluate(f); print(f"{k:34} names {a:6.1%}  acceptable {b:6.1%}  things pool {things_pool(f):.2f}")
    for k, f in {"current": current, "top1": m_top1, "name, else top1": lambda c: (uniform(hits(c, set(name_words(c)))) if hits(c, set(name_words(c))) else m_top1(c)),
                 "H1 people:name / things:top1": h1, "H2 people:name / things:head word": h2}.items():
        a, b, _ = evaluate(f)
        n, both, sec = second_eval(f)
        print(f"{k:34} names {a:6.1%}  acceptable {b:6.1%}  | {n} long: 2nd ok {sec:6.1%}, both ok {both:6.1%}")
    print("\nH1 misses:")
    _, _, per = evaluate(h1)
    for c, dist, e1 in per:
        if e1 < 1:
            print(f"  #{c['id']} [{c['name']}] ref={c['ref']} -> {', '.join(k for k, _ in dist)}   good={' '.join(sorted(c['good']))}")
