"""Scores the item-use critic (src/game/ItemUseCritic.cs, src/game/CriticTrees.cs) on labelled cases.

Three sets of cases:
    dev       28 cases, mostly "the obvious tool" against "absurd" (the original bench)
    held      20 held-out cases of the same kind, never iterated on
    survey    the survey grid (critic_survey.py): real verbs x real Tools and Weapons, weighted to the
              improvised middle — a blade into a wall, a staff over quicksand, a shield as a sled

Two ways of reading the critic, chosen by the variant:
    sample    the game's way before this work: the verdict is SAMPLED at temperature 0.7 under a grammar
              admitting only the ids, so one case is a distribution. Each case is asked once per seed and
              scored per sample; "flips" counts the cases whose seeds disagree on the side.
    logprob   one forward pass, the options shown as letters, the verdict read from every letter's
              log-probability (as ChoiceJudge does). Deterministic, so one seed is enough; its stability
              is the margin — the log-odds of help against refusal — and "close" counts margins under 1.

Everything is read from the C#: the system prompt, the setting line, both trees' questions and choices,
at the branch's base commit (BEFORE) for "before", and in the working tree for "current". A variant
replaces any of it.

    python tools/prompts/critic_bench.py --variant NAME [--set dev|held|survey|all] [--seeds 1,2,3,4,5]
                                         [--out file.jsonl]
"""
import argparse, json, math, os, re, sys, time
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P
import critic_survey as S

BEFORE = "baef68e"   # the commit this work started from: the critic as it shipped


def _source(rev):
    crit, trees = P._read("src/game/ItemUseCritic.cs", rev), P._read("src/game/CriticTrees.cs", rev)
    system = re.search(r'GetCriticSystemPrompt\(\) => @"((?:[^"]|"")*)"', crit).group(1).replace('""', '"')
    setting = re.search(r'"(Setting: [^"]*)"', trees).group(1)
    return system, setting, {n: _tree(trees, n) for n in ("ItemAppropriateness", "ToolSubstitution")}


def _tree(_trees, name):
    body = _trees[_trees.index(f'name: "{name}"'):]
    q = re.search(r'question: \$"((?:[^"\\]|\\.)*)"', body).group(1)
    choices = re.findall(r'new\("(\w+)",\s*"([^"]*)"(,\s*\n?\s*isFailure: true)?', body[:body.index("});")])
    return P._unescape(q), [(cid, desc, bool(fail)) for cid, desc, fail in choices]


SYSTEM, SETTING_LINE, TREES = _source(P.BASE_REV)   # the wording before the prompt overhaul (prompt_lib.BASE_REV)


# ── the prompt, mirrored from CriticContext.BuildPreamble and ItemUseCritic.BuildChoicePrompt ──────────

def preamble(T, c):
    """CriticContext.BuildPreamble as it shipped: the goal stated here and again in the question, the
    target never described."""
    goal_line = f"The character's goal is to {c['goal']}." if c["goal"] else ""
    item_line = f"The character is using: {c['item_context']}." if c.get("item_context") else ""
    lines = [T["setting_line"], f"The scene: a {c['world']}. The character is {c['node']}.", goal_line, item_line]
    return "\n".join(l for l in lines if l)


def third_person(goal):
    """The verb's first-person verbatim as the critic should read it — NeutralNarration's own swap,
    narrowed to what a goal phrase holds ("make my way", "in my hands")."""
    return re.sub(r"\bmy\b", "their", re.sub(r"\bmyself\b", "themselves", goal))


def preamble_target(T, c):
    """The preamble with the thing acted on described, and the goal said once (in the question)."""
    lines = [T["setting_line"], f"The scene: a {c['world']}. The character is {c['node']}."]
    if c.get("target"):
        lines.append(f"What they act on: {c['target'].rstrip('.')}.")
    if c.get("item_context"):
        lines.append(f"What they hold: {c['item_context']}.")
    return "\n".join(lines)


def goal_of(T, c):
    return T.get("goal", lambda g: g)(c["goal"])


def question(T, c):
    tree = "ToolSubstitution" if c.get("tool") else "ItemAppropriateness"
    q, choices = T["trees"][tree]
    text = P._fill(q.replace("{context.BuildPreamble()}", "{pre}"), pre=T["preamble"](T, c), goalText=goal_of(T, c),
                   itemName=c["item"], itemContext=c.get("item_context") or c["item"],
                   referenceToolPhrase=c.get("tool") or "")
    return tree, text, choices


def choice_prompt(question_text, choices):
    lines = [question_text, "", "Choose one of the following options. Respond with ONLY the option id, nothing else:"]
    lines += [f"- {cid}: {desc}" if desc else f"- {cid}" for cid, desc, _ in choices]
    return "\n".join(lines).rstrip()


def letter_prompt(question_text, choices):
    """The options as letters, for a log-probability read (ChoiceJudge's form)."""
    lines = [question_text, ""]
    lines += [f"{chr(65 + i)} - {desc}" for i, (_, desc, _) in enumerate(choices)]
    lines += ["", "Answer with the letter and nothing else."]
    return "\n".join(lines)


# ── the cases ─────────────────────────────────────────────────────────────────────────────────────────

# (world, node, goal, item, item context, reference tool or None, label: "help"/"no", degree)
CASES = [
    ("windswept plain", "at the foot of a rocky slope", "climb up the rocky slope", "the rope", "Rope (a coil of hemp rope)", None, "help", None),
    ("forest", "under an old oak", "climb the old oak", "the loaf of bread", "Loaf of Bread (a round brown loaf)", None, "no", None),
    ("village", "at the smithy door", "break open the smithy door", "the hammer", "Hammer (a smith's hammer)", None, "help", "clearly_helps"),
    ("village", "at the smithy door", "break open the smithy door", "the feather", "Feather (a long grey feather)", None, "no", None),
    ("forest", "on the bank of a stream", "cross the stream", "the walking staff", "Walking Staff (a stout ash staff)", None, "help", None),
    ("forest", "on the bank of a stream", "cross the stream", "the prayer book", "Prayer Book (a small leather-bound book)", None, "no", None),
    ("village", "in a cottage", "unlock the chest", "the hairpin", "Hairpin (a bent iron hairpin)", None, "help", None),
    ("village", "in a cottage", "unlock the chest", "the apple", "Apple (a red apple)", None, "no", None),
    ("farm", "in the stable", "calm the frightened horse", "the apple", "Apple (a red apple)", None, "help", None),
    ("farm", "in the stable", "calm the frightened horse", "the hammer", "Hammer (a smith's hammer)", None, "no", None),
    ("forest", "on the bank of a wide river", "swim across the river", "the iron pot", "Iron Pot (a heavy cooking pot)", None, "no", None),
    ("village", "in the square", "gather the spilled grain", "the sack", "Sack (an empty hemp sack)", None, "help", None),
    ("village", "in the square", "sing a song for the crowd", "the shovel", "Shovel (a wooden shovel)", None, "no", None),
    ("farm", "in the barn", "listen at the barn wall", "the axe", "Axe (a woodcutter's axe)", None, "no", None),
    ("cave", "in a dark passage", "examine the cave paintings", "the lantern", "Lantern (a horn lantern)", None, "help", None),
    ("farm", "in the vegetable garden", "dig the garden bed", "the hoe", "Hoe (a long-handled hoe)", "a shovel", "help", None),
    ("farm", "in the vegetable garden", "dig the garden bed", "the spade", "Spade (an iron-shod spade)", "a shovel", "help", "is_the_tool"),
    ("farm", "in the vegetable garden", "dig the garden bed", "the feather", "Feather (a long grey feather)", "a shovel", "no", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the sledgehammer", "Sledgehammer (a heavy two-handed hammer)", "a pickaxe", "help", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the loaf of bread", "Loaf of Bread (a round brown loaf)", "a pickaxe", "no", None),
    ("forest", "beside a fallen log", "cut wood from the fallen log", "the hatchet", "Hatchet (a small hand axe)", "an axe", "help", None),
    ("forest", "beside a fallen log", "cut wood from the fallen log", "the rope", "Rope (a coil of hemp rope)", "an axe", "no", None),
    ("coast", "on the shore of a pond", "fish in the pond", "the spear", "Spear (an ash-shafted spear)", "a fishing rod or a net", "help", None),
    ("coast", "on the shore of a pond", "fish in the pond", "the lantern", "Lantern (a horn lantern)", "a fishing rod or a net", "no", None),
    ("forest", "beside a slain deer", "cut the meat from the deer", "the sword", "Sword (an arming sword)", "a knife", "help", None),
    ("forest", "beside a slain deer", "cut the meat from the deer", "the wooden spoon", "Wooden Spoon (a carved spoon)", "a knife", "no", None),
    ("forest", "beside a slain wolf", "cut the pelt from the wolf", "the sickle", "Sickle (a curved reaping blade)", "a knife", "help", None),
    ("village", "in the square", "cut wood from the stack", "the prayer book", "Prayer Book (a small leather-bound book)", "an axe", "no", None),
]


# Held out: written once, never iterated on, scored only for the final before/after.
HELDOUT = [
    ("village", "at the well", "draw water from the well", "the bucket", "Bucket (a wooden bucket)", None, "help", None),
    ("village", "at the well", "draw water from the well", "the dagger", "Dagger (a short blade)", None, "no", None),
    ("forest", "under a beehive in a hollow oak", "take honey from the hive", "the torch", "Torch (a pitch torch)", None, "help", None),
    ("forest", "under a beehive in a hollow oak", "take honey from the hive", "the lute", "Lute (a pear-shaped lute)", None, "no", None),
    ("farm", "in the pigsty", "calm the angry boar", "the turnip", "Turnip (a fat white turnip)", None, "help", None),
    ("farm", "in the pigsty", "calm the angry boar", "the quill", "Quill (a goose quill)", None, "no", None),
    ("mountain", "at the foot of a crag", "climb the crag", "the grappling hook", "Grappling Hook (an iron hook on a line)", None, "help", None),
    ("mountain", "at the foot of a crag", "climb the crag", "the bowl", "Bowl (a clay bowl)", None, "no", None),
    ("forest", "beside a fallen birch", "cut wood from the fallen birch", "the saw", "Saw (a bow saw)", "an axe", "help", None),
    ("forest", "beside a fallen birch", "cut wood from the fallen birch", "the bread knife", "Bread Knife (a thin kitchen knife)", "an axe", "help", None),
    ("forest", "beside a fallen birch", "cut wood from the fallen birch", "the blanket", "Blanket (a wool blanket)", "an axe", "no", None),
    ("farm", "in the turnip field", "dig the turnip field", "the pitchfork", "Pitchfork (a three-tined fork)", "a shovel", "help", None),
    ("farm", "in the turnip field", "dig the turnip field", "the candle", "Candle (a tallow candle)", "a shovel", "no", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the iron bar", "Iron Bar (a heavy iron crowbar)", "a pickaxe", "help", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the rope", "Rope (a coil of hemp rope)", "a pickaxe", "no", None),
    ("coast", "at the edge of a stream", "fish in the stream", "the basket", "Basket (a wicker basket)", "a fishing rod or a net", "help", None),
    ("coast", "at the edge of a stream", "fish in the stream", "the hammer", "Hammer (a smith's hammer)", "a fishing rod or a net", "no", None),
    ("forest", "beside a slain boar", "cut the meat from the boar", "the axe", "Axe (a woodcutter's axe)", "a knife", "help", None),
    ("forest", "beside a slain boar", "cut the meat from the boar", "the cup", "Cup (a pewter cup)", "a knife", "no", None),
    ("village", "in a cottage", "unlock the door", "the key ring", "Key Ring (a ring of assorted iron keys)", None, "help", None),
]

# The band a labelled degree stands for, so the two trees are scored on one scale.
BAND = {"clearly_helps": "low", "is_the_tool": "low", "plausibly_helps": "medium", "serves_well": "medium",
        "detoured_use": "high", "serves_poorly": "high"}
BAND_ORDER = {"low": 0, "medium": 1, "high": 2}


def _old_cases(heldout):
    for i, (world, node, goal, item, ctx, tool, label, degree) in enumerate(HELDOUT if heldout else CASES):
        # NarrativeController: $"{item.WithArticle()} ({item.Description})" — "a spade (an iron-shod spade)".
        noun = item[len("the "):]
        ctx = f"{P.article(noun)} {noun} ({ctx[ctx.index('(') + 1:-1]})"
        yield dict(id=f"{'held' if heldout else 'critic'}{i:02d}", world=world, node=node, goal=goal, item=item,
                   item_context=ctx, tool=tool, label=label, degree=degree, band=BAND.get(degree), target=None,
                   family="old", group="old", kind="old")


def cases(which):
    """dev / held / survey / all, and the two halves wording was tuned and checked on:
    tune  = dev + the even-numbered survey cases — every wording below was iterated on these;
    check = held + the odd-numbered survey cases — never looked at until a wording was settled."""
    if which in ("dev", "all", "tune"):
        yield from _old_cases(False)
    if which in ("held", "all", "check"):
        yield from _old_cases(True)
    if which in ("survey", "all"):
        yield from S.survey_cases()
    if which in ("tune", "check"):
        yield from (c for i, c in enumerate(S.survey_cases()) if i % 2 == (0 if which == "tune" else 1))


# ── asking ────────────────────────────────────────────────────────────────────────────────────────────

def _post(path, body):
    return P._post(path, body)


def ask(T, c, seed):
    if T.get("score") == "pair":
        # Two questions on one preamble, read off their ids' first tokens. The side is the SUM of their
        # log-odds (help when above the threshold); the id — degree or refusal — is the strict question's:
        # its answer when on that side, else its likeliest id on that side (ItemUseCritic.WeighAsync).
        s, l = ask(T["strict"], c, seed), ask(T["lenient"], c, seed)
        margin = s["margin"] + l["margin"] - T.get("threshold", 0.0)
        if s["help"] == (margin > 0):
            answer = s["answer"]
        else:
            _, _, choices = question(T["strict"], c)
            side = [i for i, ch in enumerate(choices) if ch[2] == (margin <= 0)]
            answer = choices[max(side, key=lambda i: s["lp"][i])][0]
        return dict(tree=l["tree"], answer=answer, help=margin > 0, margin=margin, strict=s["margin"],
                    lenient=l["margin"], strict_lp=s["lp"], lenient_lp=l["lp"], strict_answer=s["answer"],
                    lenient_answer=l["answer"], user_tokens=s["user_tokens"] + l["user_tokens"], calls=2,
                    generated=s.get("generated", 0) + l.get("generated", 0),
                    wall=s["wall"] + l["wall"], prompt_n=(s["prompt_n"] or 0) + (l["prompt_n"] or 0),
                    prompt_ms=(s["prompt_ms"] or 0) + (l["prompt_ms"] or 0))
    tree, q, choices = question(T, c)
    fail = {cid for cid, _, f in choices if f}
    system = T.get("system_for", lambda T, tree: T["system"])(T, tree)
    if T.get("score") == "logprob":
        msgs = [{"role": "system", "content": P.system_prompt(T.get("templates") or P.T_NOW, system)},
                {"role": "user", "content": letter_prompt(q, choices)}]
        t0 = time.time()
        d = _post("/v1/chat/completions", dict(model="local", messages=msgs, max_tokens=1, stream=False,
                                               cache_prompt=True, temperature=0, logprobs=True, top_logprobs=40))
        wall = time.time() - t0
        lp = [-30.0] * len(choices)
        for t in d["choices"][0]["logprobs"]["content"][0]["top_logprobs"]:
            tok = t["token"].strip()
            if len(tok) == 1 and 0 <= ord(tok) - 65 < len(choices):
                lp[ord(tok) - 65] = max(lp[ord(tok) - 65], t["logprob"])
        answer, margin = T["decide"](choices, lp)
        tm = d.get("timings", {})
        return dict(tree=tree, answer=answer, help=answer not in fail, margin=margin, lp=lp,
                    user_tokens=P.tokens(msgs[-1]["content"]), calls=1, wall=wall,
                    prompt_n=tm.get("prompt_n"), prompt_ms=tm.get("prompt_ms"))
    msgs = [{"role": "system", "content": P.system_prompt(T.get("templates") or P.T_NOW, system)},
            {"role": "user", "content": T["prompt"](q, choices)}]
    if T.get("score") == "idlp":
        # The ids as the model is asked to write them, read off the FIRST token's distribution: the
        # side from the help ids' mass against the refusals', the degree from the greedy continuation.
        t0 = time.time()
        d = _post("/v1/chat/completions", dict(model="local", messages=msgs, max_tokens=8, stream=False,
                                               cache_prompt=True, temperature=0, logprobs=True, top_logprobs=40))
        wall = time.time() - t0
        text = d["choices"][0]["message"]["content"].strip()
        top = [(t["token"].strip(), t["logprob"]) for t in d["choices"][0]["logprobs"]["content"][0]["top_logprobs"]]
        first = {cid: first_piece(cid) for cid, _, _ in choices}
        lp = [max([l for tok, l in top if tok and tok == first[cid]] or [-30.0]) for cid, _, _ in choices]
        answer, margin = decide_ids(choices, lp, text, T.get("threshold", 0.0))
        tm = d.get("timings", {})
        return dict(tree=tree, answer=answer, help=answer not in fail, margin=margin, lp=lp, text=text,
                    user_tokens=P.tokens(msgs[-1]["content"]), calls=1, wall=wall,
                    prompt_n=tm.get("prompt_n"), prompt_ms=tm.get("prompt_ms"), generated=tm.get("predicted_n") or 0)
    gbnf = "root ::= response\nresponse ::= " + " | ".join(f'"{cid}"' for cid, _, _ in choices)
    g = P.generate(msgs, gbnf, seed, max_tokens=20)
    answer = g["text"].strip()
    # A refusal asks again for a reason ("I think …"), a second request in the shipped critic.
    return dict(tree=tree, answer=answer, help=answer not in fail,
                user_tokens=P.tokens(msgs[-1]["content"]), calls=1 + (answer in fail and T.get("asks_reason", True)),
                **{k: g[k] for k in ("wall", "prompt_n", "prompt_ms")})


def lse(xs):
    m = max(xs)
    return m + math.log(sum(math.exp(x - m) for x in xs))


def decide_logodds(threshold=0.0):
    """Help when the help options together outweigh the refusals by `threshold` in log-odds; the degree is
    then the likeliest help option, the refusal the likeliest refusal."""
    def decide(choices, lp):
        h = [i for i, (_, _, f) in enumerate(choices) if not f]
        r = [i for i, (_, _, f) in enumerate(choices) if f]
        margin = lse([lp[i] for i in h]) - lse([lp[i] for i in r])
        pick = max(h if margin > threshold else r, key=lambda i: lp[i])
        return choices[pick][0], margin - threshold
    return decide


_PIECES = {}


def first_piece(cid):
    """The id's first token as the model's tokenizer splits it ("clear" for clearly_helps)."""
    if cid not in _PIECES:
        toks = _post("/tokenize", {"content": cid, "with_pieces": True})["tokens"]
        _PIECES[cid] = toks[0]["piece"].strip()
    return _PIECES[cid]


def decide_ids(choices, lp, text, threshold=0.0):
    """Side: the first-token mass of the help ids against the refusals' (ids sharing a first token are
    counted once). Degree: the greedy text when it is an id on the chosen side, else that side's
    likeliest first token."""
    seen, h, r = set(), [], []
    for i, (cid, _, f) in enumerate(choices):
        key = (first_piece(cid), f)
        if key in seen:
            continue
        seen.add(key)
        (r if f else h).append(lp[i])
    margin = lse(h) - lse(r) - threshold
    side = [i for i, c in enumerate(choices) if c[2] == (margin <= 0)]
    if text in [choices[i][0] for i in side]:
        return text, margin
    return choices[max(side, key=lambda i: lp[i])][0], margin


# ── variants ──────────────────────────────────────────────────────────────────────────────────────────

_TEMPLATES = {}


def _templates(rev):
    """The setting rule every system prompt is closed by (SceneSetting.Rule), as of `rev`. It was read
    from prompt_lib.T0 — the rule BEFORE the prompt overhaul — until the live check of 2026-10-31
    caught the game sending the newer one; every critic number before that date carries the old rule."""
    if rev not in _TEMPLATES:
        _TEMPLATES[rev] = P.load_templates(rev)
    return _TEMPLATES[rev]


def _from(rev, **kw):
    system, setting, trees = _source(rev)
    T = dict(system=system, setting_line=setting, trees=trees, preamble=preamble, prompt=choice_prompt,
             templates=_templates(rev))
    T.update(kw)
    return T


def base():
    """The wording before the 2026 prompt overhaul (prompt_lib.BASE_REV), setting rule included."""
    return dict(system=SYSTEM, setting_line=SETTING_LINE, trees=TREES, preamble=preamble, prompt=choice_prompt,
                templates=P.T0)


def before():
    """The critic as this work found it: commit BEFORE, sampled."""
    return _from(BEFORE)


def _node(trees, name):
    """One named CriticNode of the working-tree C#: the question after "{head}\\n\\n", and its choices."""
    body = trees[trees.index(f'name: "{name}"'):]
    q = P._unescape(re.search(r'question: \$"((?:[^"\\]|\\.)*)"', body).group(1))
    block = body[body.index("new List<CriticChoice>"):]
    block = block[:block.index("})")]
    choices = re.findall(r'new\("(\w+)",\s*"([^"]*)"(,\s*\n?\s*isFailure: true)?', block)
    return q.split("{head}\n\n", 1)[1], [(cid, desc, bool(fail)) for cid, desc, fail in choices]


def current():
    """Whatever the working tree's C# says now: once the trees carry a strict counterweight, the pair
    as CriticTrees and ItemUseCritic.WeighAsync build and read it; before that, the single question."""
    crit = P._read("src/game/CriticTrees.cs")
    if "ItemAppropriatenessLenient" in crit:
        system = re.search(r'GetCriticSystemPrompt\(\) => @"((?:[^"]|"")*)"',
                           P._read("src/game/ItemUseCritic.cs")).group(1).replace('""', '"')
        sa, so = _node(crit, "ItemAppropriateness")
        la, lo = _node(crit, "ItemAppropriatenessLenient")
        ssa, sso = _node(crit, "ToolSubstitution")
        lsa, lso = _node(crit, "ToolSubstitutionLenient")
        lenient, strict = _physical(la, lsa, lo, lso), _physical(sa, ssa, so, sso)
        setting = re.search(r'"(Setting: [^"]*)"', crit).group(1)
        for T in (lenient, strict):
            T["system"], T["setting_line"], T["templates"] = system, setting, _templates(None)
        return dict(score="pair", strict=strict, lenient=lenient, system=system, threshold=_cs_threshold())
    shows_target = "What they act on" in crit
    T = _from(None, preamble=preamble_target if shows_target else preamble)
    if shows_target:
        T["goal"] = third_person
    if "GetFirstTokenLogProbsAsync" in P._read("src/game/ItemUseCritic.cs"):
        T.update(score="logprob", decide=decide_logodds(_cs_threshold()))
    return T


def _cs_threshold():
    m = re.search(r'HelpLogOdds\s*=\s*([-+0-9.]+)', P._read("src/game/ItemUseCritic.cs"))
    return float(m.group(1)) if m else 0.0


def target():
    """H3: the thing acted on described (its factory description), the goal said once and in the third
    person. Trees unchanged."""
    return _from(BEFORE, preamble=preamble_target, goal=third_person)


REFRAMED_APPROPRIATENESS = (
    "{context.BuildPreamble()}\n\nThe character wants to {goalText}.\nThey are holding: {itemName}.\n\n"
    "Could {itemName} be used to help with this, even clumsily or in a way it was not made for?",
    [("clearly_helps", "yes: it is the kind of thing this is done with, or near enough", False),
     ("plausibly_helps", "yes: it is a different thing, but it would help properly", False),
     ("detoured_use", "yes, clumsily: it could be made to help, with difficulty or by an unusual use", False),
     ("cannot_help", "no: it cannot help with this, however it is handled", True),
     ("makes_no_sense", "no: using it for this makes no sense at all", True)])

REFRAMED_SUBSTITUTION = (
    "{context.BuildPreamble()}\n\nThe character wants to {goalText}.\nThis work is normally done with {referenceToolPhrase}.\n"
    "They are holding: {itemName}.\n\nCould {itemName} be used in place of {referenceToolPhrase} for this, even clumsily?",
    [("is_the_tool", "yes: it is that tool, or near enough to be the same thing", False),
     ("serves_well", "yes: it is a different thing, but it would do the work properly", False),
     ("serves_poorly", "yes, clumsily: it could be made to do the work, with difficulty or by an unusual use", False),
     ("cannot_serve", "no: it cannot do this work, however it is handled", True),
     ("makes_no_sense", "no: using it for this work makes no sense at all", True)])


def reframe():
    """H1 + H3 + H6: the appropriateness question asked the way substitution is ("could X be used for
    this, even clumsily"), no bare-hands yardstick, both trees on one five-step scale whose options say
    yes or no first; plus the target described."""
    T = target()
    T["trees"] = dict(ItemAppropriateness=REFRAMED_APPROPRIATENESS, ToolSubstitution=REFRAMED_SUBSTITUTION)
    return T


def one_refusal():
    """reframe with a single refusal: cannot_help/cannot_serve and makes_no_sense merged (H6)."""
    T = reframe()
    def merged(tree, refusal):
        q, ch = T["trees"][tree]
        ch = [c for c in ch if not c[2]] + [(refusal, "no: it cannot do this, however it is handled", True)]
        return q, ch
    T["trees"] = dict(ItemAppropriateness=merged("ItemAppropriateness", "cannot_help"),
                      ToolSubstitution=merged("ToolSubstitution", "cannot_serve"))
    return T


def preamble_lean(T, c):
    """Setting, scene, and the thing acted on; the item is described once, in the question."""
    lines = [T["setting_line"], f"The scene: a {c['world']}. The character is {c['node']}."]
    if c.get("target"):
        lines.append(f"What they act on: {c['target'].rstrip('.')}.")
    return "\n".join(lines)


def _physical(ask_app, ask_sub, opts_app, opts_sub, lean=True):
    """A reframed pair of trees whose degrees name a physical means. With `lean`, the item's description
    sits in the "They are holding" line and the preamble does not repeat it."""
    hold = "{itemContext}" if lean else "{itemName}"
    app = ("{context.BuildPreamble()}\n\nThe character wants to {goalText}.\nThey are holding: " + hold + ".\n\n" + ask_app,
           opts_app)
    sub = ("{context.BuildPreamble()}\n\nThe character wants to {goalText}.\nThis work is normally done with "
           "{referenceToolPhrase}.\nThey are holding: " + hold + ".\n\n" + ask_sub, opts_sub)
    T = _idlp(target())
    T["trees"] = dict(ItemAppropriateness=app, ToolSubstitution=sub)
    if lean:
        T["preamble"] = preamble_lean
    return T


PHYS_APP = [
    ("clearly_helps", "yes: it is the kind of thing this is done with, or near enough", False),
    ("plausibly_helps", "yes: a different thing, but its shape and strength would do the work properly", False),
    ("detoured_use", "yes, clumsily: its edge, point, weight, length or grip would do part of the work, badly", False),
    ("cannot_help", "no: nothing about its shape, edge, weight or length would do any of this work", True),
    ("makes_no_sense", "no: it has nothing to do with this work", True)]
PHYS_SUB = [
    ("is_the_tool", "yes: it is that tool, or near enough to be the same thing", False),
    ("serves_well", "yes: a different thing, but its shape and strength would do the work properly", False),
    ("serves_poorly", "yes, clumsily: its edge, point, weight, length or grip would do part of the work, badly", False),
    ("cannot_serve", "no: nothing about its shape, edge, weight or length would do any of this work", True),
    ("makes_no_sense", "no: it has nothing to do with this work", True)]


def physical():
    """reframe, with every yes tied to a physical means and the item described once."""
    return _physical("Could {itemName} do any real part of this, even clumsily?",
                     "Could {itemName} do any real part of this work in place of {referenceToolPhrase}, even clumsily?",
                     PHYS_APP, PHYS_SUB)


def physical_sys():
    """physical, and the system prompt told that an imagined trick is not a use."""
    T = physical()
    T["system"] = T["system"].replace(
        "- Judge only the specific item question asked",
        "- An item counts only for what its shape, edge, weight, length or grip can really do here; a trick you have to imagine does not count.\n"
        "- Judge only the specific item question asked")
    return T


def physical_full():
    """physical, with the item's description also kept in the preamble (not lean)."""
    return _physical("Could {itemName} do any real part of this, even clumsily?",
                     "Could {itemName} do any real part of this work in place of {referenceToolPhrase}, even clumsily?",
                     PHYS_APP, PHYS_SUB, lean=False)


STRICT_APP_ASK = "Compared to attempting this with bare hands, does {itemName} provide a meaningful advantage for this action?"
STRICT_SUB_ASK = "Could {itemName} be used in place of {referenceToolPhrase} for this, even if less well?"


def strict_lean():
    """The shipped questions and options, unchanged, on the shared lean preamble."""
    _, _, shipped = _source(BEFORE)
    return _physical(STRICT_APP_ASK, STRICT_SUB_ASK, shipped["ItemAppropriateness"][1], shipped["ToolSubstitution"][1])


def pair():
    """The shipped (strict) question and the physical (lenient) one on one preamble, log-odds summed."""
    return dict(score="pair", strict=strict_lean(), lenient=physical(), system=SYSTEM)


def _lp(T, threshold=0.0):
    T.update(score="logprob", decide=decide_logodds(threshold))
    return T


def before_lp():   return _lp(before())          # H4 alone: the shipped prompt, read by log-probability
def target_lp():   return _lp(target())
def reframe_lp():  return _lp(reframe())
def one_refusal_lp(): return _lp(one_refusal())


def _idlp(T, threshold=0.0):
    T.update(score="idlp", threshold=threshold)
    return T


def before_idlp():      return _idlp(before())    # H4 alone, read off the ids rather than letters
def target_idlp():      return _idlp(target())
def reframe_idlp():     return _idlp(reframe())
def one_refusal_idlp(): return _idlp(one_refusal())


def no_strict():
    """(historical) The system prompt without "Be strict but fair; value plausibility over generosity"."""
    T = base()
    T["system"] = T["system"].replace("- Be strict but fair; value plausibility over generosity.\n", "")
    return T


def in_its_place():
    """(historical) The substitution asked as standing in for the tool, "even if less well"."""
    T = no_strict()
    q, ch = T["trees"]["ToolSubstitution"]
    T["trees"] = dict(T["trees"], ToolSubstitution=(q.replace(
        "Can {itemName} do the work of {referenceToolPhrase} here?",
        "Could {itemName} be used in place of {referenceToolPhrase} for this, even if less well?"), ch))
    return T


VARIANTS = {f.__name__: f for f in (current, before, base, no_strict, in_its_place, target, reframe, one_refusal,
                                     before_lp, target_lp, reframe_lp, one_refusal_lp,
                                     before_idlp, target_idlp, reframe_idlp, one_refusal_idlp,
                                     physical, physical_sys, physical_full, strict_lean, pair)}


# ── the report ────────────────────────────────────────────────────────────────────────────────────────

def report(rows, name):
    """rows: one per (case, seed), with the case's labels merged in."""
    def pct(o, n): return f"{o}/{n} = {100 * o / n:.1f}%" if n else "-"
    by_case = {}
    for r in rows:
        by_case.setdefault(r["case"], []).append(r)
    ok = sum(r["ok"] for r in rows)
    print(f"\n{name}: right side {pct(ok, len(rows))}")
    banded = [r for r in rows if r["band"] and r["label"] == "help"]
    exact = sum(r["got_band"] == r["band"] for r in banded)
    near = sum(r["got_band"] is not None and abs(BAND_ORDER[r["got_band"]] - BAND_ORDER[r["band"]]) <= 1 for r in banded)
    print(f"  band, of labelled help cases: exact {pct(exact, len(banded))}   within one {pct(near, len(banded))}")
    fr = [r for r in rows if r["label"] == "help"]
    fp = [r for r in rows if r["label"] == "no"]
    print(f"  false refusals {pct(sum(not r['help'] for r in fr), len(fr))}   false passes {pct(sum(r['help'] for r in fp), len(fp))}")
    for key in ("tree", "kind", "family", "group"):
        groups = {}
        for r in rows:
            groups.setdefault(r[key], []).append(r)
        if len(groups) < 2:
            continue
        print(f"  by {key}:")
        for k, rs in sorted(groups.items()):
            h = [r for r in rs if r["label"] == "help"]
            n = [r for r in rs if r["label"] == "no"]
            print(f"    {k:22} {pct(sum(r['ok'] for r in rs), len(rs)):>16}   false refusal "
                  f"{sum(not r['help'] for r in h)}/{len(h)}   false pass {sum(r['help'] for r in n)}/{len(n)}")
    if "margin" in rows[0]:
        close = sum(abs(r["margin"]) < 1 for r in rows)
        print(f"  stability: log-odds margin under 1 on {close}/{len(rows)} cases; median |margin| "
              f"{sorted(abs(r['margin']) for r in rows)[len(rows) // 2]:.2f}")
    else:
        flips = sum(len({r["help"] for r in rs}) > 1 for rs in by_case.values())
        print(f"  stability: {flips}/{len(by_case)} cases change side between seeds")
    calls = sum(r["calls"] for r in rows) / len(rows)
    toks = sum(r["user_tokens"] for r in rows) / len(rows)
    wall = sum(r["wall"] for r in rows) / len(rows)
    print(f"  cost: {calls:.2f} model calls a tool use, {toks:.0f} user-message tokens, {wall:.2f} s a verdict request (warm cache)")


WHY = 'In one short sentence (around 12 words), explain why you think that way. Begin with "I think".'
WHY_GBNF = 'root ::= "I think " [^\\n]+'


def timed_before(c, seed):
    """One tool use as the shipped critic made it: the verdict sampled under its grammar, then, after a
    refusal, the "why" request on the same slot history (ItemUseCritic.GetFailureReasonAsync, removed)."""
    T = before()
    tree, q, choices = question(T, c)
    msgs = [{"role": "system", "content": P.system_prompt(T.get("templates") or P.T_NOW, T["system"])},
            {"role": "user", "content": T["prompt"](q, choices)}]
    gbnf = "root ::= response\nresponse ::= " + " | ".join(f'"{cid}"' for cid, _, _ in choices)
    t0 = time.time()
    g = P.generate(msgs, gbnf, seed, max_tokens=20)
    calls, gen = 1, g["predicted_n"] or 0
    if g["text"].strip() in {cid for cid, _, f in choices if f}:
        msgs += [{"role": "assistant", "content": g["text"]}, {"role": "user", "content": WHY}]
        w = P.generate(msgs, WHY_GBNF, seed, max_tokens=60)
        calls, gen = 2, gen + (w["predicted_n"] or 0)
    return dict(wall=time.time() - t0, calls=calls, generated=gen)


def timing(which, limit=None):
    """Wall time of one tool use, before and now, each case once, the two alternating which goes first
    so neither always finds the other's prompt in the cache."""
    now = current()
    rows = []
    for i, c in enumerate(cases(which)):
        if limit and i >= limit:
            break
        order = [("before", lambda: timed_before(c, 1)), ("now", lambda: ask(now, c, 1))]
        for name, f in (order if i % 2 == 0 else order[::-1]):
            t0 = time.time()
            r = f()
            rows.append(dict(version=name, wall=time.time() - t0, calls=r["calls"], generated=r.get("generated", 0)))
    for name in ("before", "now"):
        rs = [r for r in rows if r["version"] == name]
        walls = sorted(r["wall"] for r in rs)
        print(f"{name:7} {len(rs)} tool uses: mean {sum(walls) / len(rs):.2f} s, median {walls[len(rs) // 2]:.2f} s, "
              f"worst {walls[-1]:.2f} s, {sum(r['calls'] for r in rs) / len(rs):.2f} requests, "
              f"{sum(r['generated'] for r in rs) / len(rs):.1f} tokens generated")


def main():
    if "--timing" in sys.argv:
        i = sys.argv.index("--timing")
        timing(sys.argv[i + 1] if len(sys.argv) > i + 1 else "survey")
        return
    ap = argparse.ArgumentParser()
    ap.add_argument("--variant", default="current")
    ap.add_argument("--set", default="dev", choices=["dev", "held", "survey", "all", "tune", "check"])
    ap.add_argument("--heldout", action="store_true", help="same as --set held")
    ap.add_argument("--seeds", default=None, help="default 1..5 when sampled, 1 when read by log-probability")
    ap.add_argument("--out")
    ap.add_argument("--quiet", action="store_true")
    a = ap.parse_args()
    T = VARIANTS[a.variant]()
    which = "held" if a.heldout else a.set
    seeds = [int(s) for s in (a.seeds or ("1" if T.get("score") in ("logprob", "idlp", "pair") else "1,2,3,4,5")).split(",")]
    out = open(a.out, "a") if a.out else None
    rows = []
    for c in cases(which):
        for s in seeds:
            r = ask(T, c, s)
            got_band = BAND.get(r["answer"])
            row = dict(variant=a.variant, case=c["id"], seed=s, ok=r["help"] == (c["label"] == "help"),
                       got_band=got_band, **{k: c[k] for k in ("label", "band", "family", "group", "kind", "goal", "item")}, **r)
            rows.append(row)
            if out:
                out.write(json.dumps(row) + "\n")
        if not a.quiet:
            got = [x["answer"] for x in rows if x["case"] == c["id"]]
            print(c["id"], c["goal"], "/", c["item"], c["label"], c.get("band") or "", "->", " ".join(got), flush=True)
    report(rows, a.variant)


if __name__ == "__main__":
    main()
