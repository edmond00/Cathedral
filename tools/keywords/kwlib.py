"""Shared pieces of the observation-keyword benchmark: the labelled cases, GloVe vectors with the
game's centrality correction (WordEmbedding.cs), and the grader.

cases.jsonl holds real observation rewrites from logs/ (see extract.py), each with the Catalyst tokens
and the game's exact keyword candidates (NounExtractor.ExtractKeywordCandidates, see kwtag/).
labels.txt holds, per case id, `id | good words | ok words`: good names the object (its head noun, a
synonym, a person's name, role, sex or "figure"); ok is a concrete part or feature of it. Any other
candidate counts as weird. One labeller; extend it rather than trust it blindly.
"""
import json, os, re, sys, numpy as np
sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__))
GLOVE = os.path.join(HERE, "..", "..", "models", "embeddings", "glove.6B.100d.txt")
CACHE = os.path.join(HERE, "vecs.npz")   # built on first use, not committed

def _build_cache(cases):
    """The vectors the cases need, and the centroid WordEmbedding.Centrality uses: the first 20000
    lines of the file, plain lowercase words of three letters or more."""
    need = {"body", "person", "door", "wall", "path", "track", "lane", "way"}
    for o in cases:
        for s, l, _ in o["toks"]: need.add(s.lower()); need.add((l or "").lower())
        need.update(w.lower() for w in re.findall(r"[A-Za-z]+", o["name"] + " " + o["neutral"]))
    vec = {}; cen = np.zeros(100); n = 0
    for i, line in enumerate(open(GLOVE, encoding="utf-8")):
        w, rest = line.split(" ", 1)
        if i < 20000 and len(w) >= 3 and re.fullmatch("[a-z]+", w):
            v = np.array(rest.split(), dtype=float); cen += v / np.linalg.norm(v); n += 1
            if w in need: vec[w] = v
        elif w in need: vec[w] = np.array(rest.split(), dtype=float)
    np.savez(CACHE, words=np.array(list(vec)), M=np.array(list(vec.values())), cen=cen / n)

def _raw_cases():
    return [json.loads(l) for l in open(os.path.join(HERE, "cases.jsonl"), encoding="utf-8")]
if not os.path.exists(CACHE): _build_cache(_raw_cases())
_d = np.load(CACHE); V = {w: v for w, v in zip(_d["words"], _d["M"])}; CEN = _d["cen"]

def vec(w): return V.get((w or "").lower())
def cos(a, b): return float(a @ b / np.linalg.norm(a) / np.linalg.norm(b))
def centrality(w):
    v = vec(w); return 0.0 if v is None else float(v @ CEN / np.linalg.norm(v))

# Reference lemmas of the PoI kinds (PoiKinds/AreaKinds/CityKinds and the connector kinds), used to
# approximate ObservationObject.ReferenceLemma from a neutral name.
LEMMAS = set("""anvil barrel bedroll bellows bread churn cloth coal crevice crop cross ditch fire flax flour frame grain hay hive hoop lamb lantern loom marker millstone mud mug nest net ore oven pail post rack roost rubble sack scarecrow shaving shelf stall stave stocks toll tool trough well willow wood wool workbench bell block chest firewood herb midden pallet peg grave beetroot bench boulder bush cabbage cairn candle carrot chamomile cradle deadfall driftwood flower hearth hollow ice kelp leek lichen light log mat mint mortar moss mushroom onion parsnip pea pool pot psalter radish reed rock sage stool stream stump table thyme tree turnip undergrowth washstand wheel wormwood alcove alehouse bakery basin beach bog clearing cliff coop courtyard crag crown deadwood den entrance estuary forest forge garden gorge grassland green ground grove gully hall heath hedgerow hill lane ledge market meadow memory mill orchard pen pigsty plateau ridge roof room row scree seam shaft shed shore slope square summit thicket valley vegetable walk waterside workshop yard bedroom dormitory kitchen pantry store landing loft canopy headland gallery door wall path track hedge trunk gap stack shutter""".split())
PEOPLE = re.compile(r"\b(of the|acquaintance)\b|\(a (young )?(man|woman|boy|girl)\)")
def labels():
    L = {}
    for line in open(os.path.join(HERE, "labels.txt"), encoding="utf-8"):
        if not line.strip(): continue
        i, g, o = [p.strip() for p in line.split("|")]
        L[int(i)] = (set(g.split()), set(o.split()))
    return L
def ref_lemma(name, person):
    """People are anchored on "body" (SceneViewAdapter). A connector names its obstacle or is a path."""
    if person: return "body"
    base = re.sub(r"\(.*?\)", "", name).lower()
    ws = re.findall(r"[a-z]+", base)
    if "–" in name:
        cross = [w for w in ws if w in ("boulder", "mud", "trunk", "scree", "hedge", "gap", "bramble", "nettle", "ice", "snow")]
        return cross[0] if cross else "path"
    hit = [w for w in ws if w in LEMMAS]
    return hit[-1] if hit else (ws[-1] if ws else "")
def cases():
    L = labels(); out = []
    for r in _raw_cases():
        if r["id"] not in L: continue
        c = dict(r); g, o = L[r["id"]]
        c["toks"] = [dict(s=s, l=l, p=p) for s, l, p in r["toks"]]; c["cands"] = [tuple(x) for x in r["cands"]]
        c["good"] = g; c["ok"] = o; c["person"] = bool(PEOPLE.search(r["name"]))
        c["ref"] = ref_lemma(r["name"], c["person"]); out.append(c)
    return out
def head_word(name):
    ws = re.findall(r"[A-Za-z]+", name); return ws[-1].lower() if ws else None
def grade(c, w):
    w = w.lower()
    if w in c["good"]: return 2
    if w in c["ok"]: return 1
    return 0
