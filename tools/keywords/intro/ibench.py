"""Scores ways of linking the words of an area introduction to the objects around it — the benchmark
behind src/game/narrative/IntroductionKeywordLinker.cs (`name_match2` is its Python twin).

    python tools/keywords/intro/ibench.py            # every method on every labelled introduction
    python tools/keywords/intro/ibench.py "<method>" # ... and that method's wrong links

Data: real.jsonl (introductions played, from logs/, via real.py) and gen.jsonl (real scenes from the
game's factories, via introdump/, rewritten by the shipped model with real personas, via
personas.py + generate.py), each with the game's keyword candidates (*_tags.jsonl, tools/keywords/kwtag)
and hand labels (labels_*.txt). llm_*.jsonl caches the LLM method's answers (llm_link.py).
"""
# A link is (word, object). The renderer highlights a keyword's FIRST whole-word occurrence in the
# sentence, so a link is right only if that occurrence names the object: labels are per word, by first
# occurrence. `word=i` names object i, `word~i` is an acceptable modifier of it ("tenement" door).
import json, os, re, sys, random, collections as co
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, ".."))
from kwlib import vec, cos, centrality
sys.stdout.reconfigure(encoding="utf-8")

def load(cases_path, tags_path, labels_path, prefix):
    cases = [json.loads(l) for l in open(cases_path, encoding="utf-8")]
    tags = {json.loads(l)["id"]: json.loads(l) for l in open(tags_path, encoding="utf-8")}
    out = []
    for line in open(labels_path, encoding="utf-8"):
        if not line.strip(): continue
        cid, lab = [x.strip() for x in line.split("|", 1)]
        i = int(cid[len(prefix):]); c = dict(cases[i]); c["cid"] = cid
        c["cands"] = [tuple(x) for x in tags[i]["cands"]]; c["toks"] = tags[i]["toks"]
        gold = {}
        for tok in lab.split():
            m = re.match(r"(.+?)([=~])([\d,]+)$", tok)
            w, kind, idx = m.group(1).lower(), m.group(2), {int(x) for x in m.group(3).split(",")}
            gold[w] = (2 if kind == "=" else 1, idx)
        c["gold"] = gold; out.append(c)
    return out

STOP = set("a an the of some my and in on by at to with from".split())
CONNECTOR_HEADS = {"way", "track", "path", "lane", "passage", "crossing", "trail", "trace", "walk", "gap", "stair", "stairs", "ladder", "door", "bridge", "choke", "slide", "wallow"}
def clean_name(name):
    return re.sub(r"\(.*?\)", "", name).strip()
def name_words(o):
    """Words of an object's name, head first. A person: role (before 'of the'), their own name if an
    acquaintance, and the sex from the parenthesis; never the 'of the field' tail."""
    n = o["name"]
    if o["person"]:
        sex = re.findall(r"\((?:a )?(?:young )?(man|woman|boy|girl)\)", n)
        m = re.match(r"my acquaintance (\w+), the (.+?) of the", n)
        if m: return [m.group(1).lower()] + m.group(2).lower().split()[::-1] + sex
        role = re.split(r"\bof the\b", clean_name(n))[0]
        ws = [w for w in re.findall(r"[A-Za-z][A-Za-z'-]*", role.lower()) if w not in STOP and w != "young"]
        return ws[::-1] + sex
    ws = [w for w in re.findall(r"[A-Za-z][A-Za-z'-]*", clean_name(n).lower().replace("–", " ")) if w not in STOP]
    return ws[::-1]   # head (last word) first
def head(o):
    ws = name_words(o); return ws[0] if ws else ""
def same(a, b):
    a, b = a.lower(), b.lower()
    return a == b or a.rstrip("s") == b.rstrip("s") or a.rstrip("'s") == b
def word_is(cand, w):
    s, l = cand; return same(s, w) or same(l or "", w)
def surface_is(cand, w):
    """The word as written, allowing a plural; the tagger's lemma is not trusted ("crossing" -> cross)."""
    s = cand[0].lower(); w = w.lower()
    return s == w or s in (w + "s", w + "es") or (w.endswith("y") and s == w[:-1] + "ies")
def area_words(c):
    """Words of the 'I find myself <where>' clause: they name the area I am in, not an object."""
    first = c["neutral"].split(". ")[0]
    return {w.lower() for w in re.findall(r"[A-Za-z][A-Za-z'-]*", first)} - STOP

def positions(text):
    return [(m.start(), m.group(0).lower()) for m in re.finditer(r"[A-Za-z][A-Za-z'-]*", text)]
def first_pos(text, w):
    m = re.search(r"\b" + re.escape(w) + r"\b", text, re.I); return m.start() if m else -1

# ── methods: each returns [(word, object index)] or a distribution [(links, p)] ─────────────
def m_current(c):
    """The game today: an object matches by its reference lemma or the last word of its name (people
    by name only); one match drawn at random; none when the pool has fewer than two objects."""
    pool = c["pool"]
    if len(pool) < 2: return [([], 1.0)]
    matches = []
    for k, o in enumerate(pool):
        lemma = "" if o["person"] else o["lemma"]
        hd = (o["name"].split() or [""])[-1].strip("(),.'\"")
        for s, l in c["cands"]:
            if any(x and (s.lower() == x.lower() or (l or "").lower() == x.lower()) for x in (lemma, hd)):
                matches.append((s, k)); break
    if not matches: return [([], 1.0)]
    return [([m], 1 / len(matches)) for m in matches]

def exact_all(c, use_lemma=True, people_by_name_words=False):
    """Every object gets its first candidate equal to its lemma or the last word of its name."""
    links, used = [], set()
    for k, o in enumerate(c["pool"]):
        keys = name_words(o) if (o["person"] and people_by_name_words) else \
               [("" if o["person"] or not use_lemma else o["lemma"]), (o["name"].split() or [""])[-1].strip("(),.'\"")]
        for s, l in c["cands"]:
            if s.lower() in used: continue
            if any(x and word_is((s, l), x) for x in keys):
                links.append((s, k)); used.add(s.lower()); break
    return links
def m_exact_all(c): return exact_all(c)
def m_exact_all_people(c): return exact_all(c, people_by_name_words=True)

def context_owner(c, w, ok_objects):
    """Which object the first occurrence of w belongs to, by the words around it: each object scores
    the number of its name words within three words before and one after. None on a tie."""
    toks = positions(c["text"]); p = first_pos(c["text"], w)
    idx = next((i for i, (q, _) in enumerate(toks) if q == p), None)
    if idx is None: return None
    window = [t for _, t in toks[max(0, idx - 3): idx + 2]]
    scores = {}
    for k in ok_objects:
        nw = name_words(c["pool"][k]) + [w for w in re.findall(r"[a-z]+", paren(c["pool"][k])) if w not in STOP]
        scores[k] = sum(any(same(t, x) for t in window) for x in nw)
    best = max(scores.values()); top = [k for k, v in scores.items() if v == best]
    return top[0] if len(top) == 1 and best > 0 else None

def name_match(c, modifiers=True, disambiguate=True, lemma=True, strict=False):
    """Each candidate that is a word of some object's name (or its lemma) is claimed by that object;
    a word several objects share is given to the one whose other name words surround it. Each object
    then keeps its best word: the head of its name or its lemma before a modifier."""
    claims = co.defaultdict(list)   # word -> [(object, rank)]
    for s, l in c["cands"]:
        for k, o in enumerate(c["pool"]):
            nw = name_words(o) if modifiers else name_words(o)[:1]
            if strict:
                aw = area_words(c); nw = [x for x in nw if x not in aw]
            keys = nw + ([o["lemma"]] if lemma and not o["person"] and not (strict and o["lemma"] in area_words(c)) else [])
            match = surface_is if strict else word_is
            for r, x in enumerate(keys):
                if match((s, l), x):
                    rank = 0 if (r == 0 or x == o["lemma"]) else 1
                    claims[s].append((k, rank)); break
    per_obj = co.defaultdict(list)
    for s, cl in claims.items():
        ks = {k for k, _ in cl}
        if len(ks) > 1:
            if not disambiguate: continue
            k = context_owner(c, s, ks)
            if k is None: continue
            cl = [x for x in cl if x[0] == k]
        k, rank = cl[0]
        per_obj[k].append((rank, first_pos(c["text"], s), s))
    return [(sorted(v)[0][2], k) for k, v in per_obj.items()]
def plural_in(w, S): return w in S or w + "s" in S or w + "es" in S or w.rstrip("s") in S
def paren(o):
    m = re.search(r"\(([^)]*)\)", o["name"]); return m.group(1).lower() if m else ""
def guarded(c, w):
    """The first occurrence of w sits inside a place phrase some object carries in its parenthesis
    ("a wooden chest by the door"): it names where that object is, not a thing of its own."""
    p = first_pos(c["text"], w); t = c["text"].lower()
    for o in c["pool"]:
        ph = paren(o)
        if ph and " " in ph and not o["person"]:
            for m in re.finditer(re.escape(ph), t):
                if m.start() <= p < m.end(): return True
    return False
def name_match2(c, modifiers=True, connector_heads=True, paren_guard=True, lemma=True):
    """name_match, strict, plus: a connector (a name joining two places with a dash) answers only to
    its own kind of way (track, path, crossing...), never to the places it joins; a word inside an
    object's place phrase ("by the door") is not taken; a name word that names the area I am in,
    singular or plural, is dropped."""
    aw = area_words(c)
    claims = co.defaultdict(list)
    for s, l in c["cands"]:
        if paren_guard and guarded(c, s): continue
        for k, o in enumerate(c["pool"]):
            nw = name_words(o)
            if connector_heads and "–" in o["name"]: nw = nw[:1]
            if not modifiers: nw = nw[:1]
            nw = [x for x in nw if not plural_in(x, aw)]
            keys = nw + ([o["lemma"]] if lemma and not o["person"] and not plural_in(o["lemma"], aw) else [])
            for r, x in enumerate(keys):
                if surface_is((s, l), x):
                    claims[s].append((k, 0 if (r == 0 or x == o["lemma"]) else 1)); break
    per_obj = co.defaultdict(list)
    for s, cl in claims.items():
        ks = {k for k, _ in cl}
        if len(ks) > 1:
            k = context_owner(c, s, ks)
            if k is None: continue
            cl = [x for x in cl if x[0] == k]
        k, rank = cl[0]
        per_obj[k].append((rank, first_pos(c["text"], s), s))
    return [(sorted(v)[0][2], k) for k, v in per_obj.items()]
def m_name(c): return name_match(c)
def m_strict(c): return name_match(c, strict=True)
def m_strict_heads(c): return name_match(c, strict=True, modifiers=False)
def m_name_heads(c): return name_match(c, modifiers=False)
def m_name_nodisamb(c): return name_match(c, disambiguate=False)

def embed(c, thr=0.45, exclude=()):
    """Greedy assignment by cos(word, object's lemma or head) - centrality, best pairs first."""
    pairs = []
    for s, l in c["cands"]:
        v = vec(l) if vec(l) is not None else vec(s)
        if v is None: continue
        for k, o in enumerate(c["pool"]):
            if k in exclude: continue
            anchors = [a for a in ([o["lemma"]] if not o["person"] else []) + name_words(o)[:1] if vec(a) is not None]
            if not anchors: continue
            sc = max(cos(vec(a), v) for a in anchors) - centrality(l or s)
            if sc >= thr: pairs.append((sc, s, k))
    links, used_w, used_k = [], set(), set(exclude)
    for sc, s, k in sorted(pairs, reverse=True):
        if s.lower() in used_w or k in used_k: continue
        links.append((s, k)); used_w.add(s.lower()); used_k.add(k)
    return links
def m_name_then_embed(c, thr=0.45, strict=False):
    a = name_match(c, strict=strict); taken = {k for _, k in a}; words = {w.lower() for w, _ in a}
    b = [(w, k) for w, k in embed(c, thr, exclude=taken) if w.lower() not in words]
    return a + b

def score(c, links):
    good = ok = wrong = 0; got = set()
    for w, k in links:
        g = c["gold"].get(w.lower())
        if g and k in g[1]:
            if g[0] == 2: good += 1
            else: ok += 1
            got.add(k)
        else: wrong += 1
    mentioned = {k for lvl, ks in c["gold"].values() if lvl == 2 for k in ks} | {k for lvl, ks in c["gold"].values() for k in ks}
    return good, ok, wrong, len(got & mentioned), len(mentioned)

def evaluate(f, cases):
    T = co.Counter()
    for c in cases:
        r = f(c)
        dist = r if (r and isinstance(r[0], tuple) and isinstance(r[0][0], list)) else [(r, 1.0)]
        for links, p in dist:
            g, o, w, hit, men = score(c, links)
            T["good"] += p * g; T["ok"] += p * o; T["wrong"] += p * w; T["hit"] += p * hit
            T["links"] += p * len(links)
        T["mentioned"] += score(c, [])[4]; T["n"] += 1
    return T
def report(name, T):
    n = T["n"]; L = T["links"] or 1
    print(f"{name:32} links/intro {T['links']/n:4.2f}  right {100*(T['good']+T['ok'])/L:5.1f}%  "
          f"(names {100*T['good']/L:5.1f}%)  wrong/intro {T['wrong']/n:4.2f}  objects reached {100*T['hit']/max(T['mentioned'],1):5.1f}%")

LLM = {}
for f in ("llm_real.jsonl", "llm_gen.jsonl"):
    try:
        for l in open(os.path.join(HERE, f), encoding="utf-8"): o = json.loads(l); LLM[o["cid"]] = {int(k): v for k, v in o["links"].items()}
    except FileNotFoundError: pass
def m_llm(c):
    """The shipped model asked which word names each object (one extra request); a word claimed
    twice keeps only its first claim."""
    seen, out = set(), []
    for k, w in sorted(LLM.get(c["cid"], {}).items()):
        if w.lower() in seen: continue
        seen.add(w.lower()); out.append((w, k))
    return out
def m_llm_and_rules(c):
    a = set(m_llm(c)); return [x for x in name_match2(c) if x in a]
def m_rules_then_llm(c):
    a = name_match2(c); ks = {k for _, k in a}; ws = {w.lower() for w, _ in a}
    return a + [(w, k) for w, k in m_llm(c) if k not in ks and w.lower() not in ws]
METHODS = {
    "current (one, random)": m_current,
    "exact lemma/last word, all": m_exact_all,
    "  + people by name words": m_exact_all_people,
    "name words, heads only": m_name_heads,
    "name words, no context": m_name_nodisamb,
    "name words + context": m_name,
    "STRICT name words + context": m_strict,
    "STRICT heads only": m_strict_heads,
    "STRICT, then embed .55": lambda c: m_name_then_embed(c, .55, True),
    "V2 (connector heads, place guard)": name_match2,
    "V2 heads only": lambda c: name_match2(c, modifiers=False),
    "V2 no lemma": lambda c: name_match2(c, lemma=False),
    "V2 no lemma, heads only": lambda c: name_match2(c, modifiers=False, lemma=False),
    "LLM (extra request)": m_llm,
    "LLM, agreed by V2": m_llm_and_rules,
    "V2, then LLM for the rest": m_rules_then_llm,
    "embedding .35": lambda c: embed(c, .35),
    "embedding .45": lambda c: embed(c, .45),
    "embedding .55": lambda c: embed(c, .55),
    "name+context, then embed .45": m_name_then_embed,
    "name+context, then embed .55": lambda c: m_name_then_embed(c, .55),
}
if __name__ == "__main__":
    H = lambda f: os.path.join(HERE, f)
    sets = [load(H("real.jsonl"), H("real_tags.jsonl"), H("labels_real.txt"), "r"),
            load(H("gen.jsonl"), H("gen_tags.jsonl"), H("labels_gen.txt"), "g")]
    allc = [c for s in sets for c in s]
    print(f"{len(allc)} introductions, {sum(score(c, [])[4] for c in allc)} objects mentioned\n")
    for k, f in METHODS.items(): report(k, evaluate(f, allc))
    if len(sys.argv) > 1:
        f = METHODS[sys.argv[1]]
        for c in allc:
            links = f(c)
            bad = [(w, k) for w, k in links if not (c["gold"].get(w.lower()) and k in c["gold"][w.lower()][1])]
            if bad: print(c["cid"], [(w, c["pool"][k]["name"]) for w, k in bad])
